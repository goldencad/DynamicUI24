using System.Collections.Immutable;
using System.Diagnostics;
using DynamicUI24.Core.DataEntry;

namespace DynamicUI24.Core.ImportExport;

public sealed class ImportSession
{
    private readonly object sync = new();
    public ImportSession(ImportDefinition definition, ImportExportOperationContext context)
    { Definition = definition ?? throw new ArgumentNullException(nameof(definition)); Context = context ?? throw new ArgumentNullException(nameof(context)); }
    public Guid SessionId { get; } = Guid.NewGuid();
    public ImportDefinition Definition { get; }
    public ImportExportOperationContext Context { get; }
    public ImportSessionState State { get; private set; } = ImportSessionState.SelectSource;
    public string? DiagnosticCode { get; private set; }
    public bool IsTerminal => State is ImportSessionState.Completed or ImportSessionState.Failed or ImportSessionState.Cancelled or ImportSessionState.Invalidated;
    public bool IsCurrent(ImportExportOperationContext context) => context.GridContext.Company.CompanyId == Context.GridContext.Company.CompanyId &&
        string.Equals(context.GridContext.WorkspaceId, Context.GridContext.WorkspaceId, StringComparison.Ordinal) && context.Generation == Context.Generation;
    public bool MoveTo(ImportSessionState next)
    {
        lock (sync)
        {
            if (IsTerminal || !Allowed(State, next)) return false;
            State = next; return true;
        }
    }
    public void Cancel() { lock (sync) { if (!IsTerminal) State = ImportSessionState.Cancelled; } }
    public void Fail(string code) { lock (sync) { if (!IsTerminal) { State = ImportSessionState.Failed; DiagnosticCode = code; } } }
    public void Invalidate(string code = "IMPORT_CONTEXT_CHANGED") { lock (sync) { if (!IsTerminal) { State = ImportSessionState.Invalidated; DiagnosticCode = code; } } }
    private static bool Allowed(ImportSessionState from, ImportSessionState to) => (from, to) switch
    {
        (ImportSessionState.SelectSource, ImportSessionState.Inspect) => true,
        (ImportSessionState.Inspect, ImportSessionState.Map) => true,
        (ImportSessionState.Map, ImportSessionState.Preview) => true,
        (ImportSessionState.Preview, ImportSessionState.Validate) => true,
        (ImportSessionState.Validate, ImportSessionState.Ready) => true,
        (ImportSessionState.Ready, ImportSessionState.Committing) => true,
        (ImportSessionState.Committing, ImportSessionState.Completed) => true,
        (_, ImportSessionState.Failed or ImportSessionState.Cancelled or ImportSessionState.Invalidated) => true,
        _ => false,
    };
}

public sealed class ImportEngine
{
    private readonly ImportExportRegistry registry;
    private readonly ImportMappingEngine mapping;
    private readonly ImportSafetyLimits limits;
    public ImportEngine(ImportExportRegistry registry, ImportSafetyLimits? limits = null)
    { this.registry = registry ?? throw new ArgumentNullException(nameof(registry)); mapping = new(registry); this.limits = limits ?? new(); }

    public async Task<ImportSourceSchema> InspectAsync(Stream source, ImportDefinition definition, string sourceName = "stream",
        CancellationToken cancellationToken = default)
    {
        if (!registry.TryGetParser(definition.ParserCode, out var parser)) throw new InvalidOperationException("IMPORT_PARSER_UNKNOWN");
        return await parser.InspectAsync(source, new(definition, limits, sourceName), cancellationToken).ConfigureAwait(false);
    }

    public async Task<ImportPreviewResult> PreviewAsync(Stream source, ImportDefinition definition, ResolvedGridDefinition grid,
        IProgress<ImportExportProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        if (!registry.TryGetParser(definition.ParserCode, out var parser))
            return FailurePreview("IMPORT_PARSER_UNKNOWN", "The selected import parser is not registered.", definition.MaxPreviewRows);
        var rows = ImmutableArray.CreateBuilder<ImportCandidateRecord>();
        var details = ImmutableArray.CreateBuilder<ImportDiagnostic>();
        long examined = 0, valid = 0, warnings = 0, invalid = 0, totalDiagnostics = 0;
        try
        {
            await foreach (var record in parser.ParseAsync(source, new(definition, limits), cancellationToken).ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (++examined > limits.MaximumRecords) { Add(ImportDiagnostic.Error("IMPORT_RECORD_LIMIT", "The configured record safety limit was exceeded.")); break; }
                var candidate = await mapping.MapAsync(record, definition, grid, cancellationToken: cancellationToken).ConfigureAwait(false);
                if (candidate.IsValid)
                {
                    valid++;
                    if (candidate.Diagnostics.Any(x => x.Severity == ImportDiagnosticSeverity.Warning)) warnings++;
                }
                else invalid++;
                foreach (var diagnostic in candidate.Diagnostics) Add(diagnostic);
                if (rows.Count < definition.MaxPreviewRows) rows.Add(candidate);
                progress?.Report(new("Previewing", examined));
                if (rows.Count >= definition.MaxPreviewRows) break;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            Add(ImportDiagnostic.Error("IMPORT_PARSE_FAILED", "The source could not be parsed safely.", exception: exception));
        }
        return new(rows.ToImmutable(), details.ToImmutable(), examined, valid, warnings, invalid,
            examined >= definition.MaxPreviewRows, definition.MaxPreviewRows, totalDiagnostics);

        void Add(ImportDiagnostic value) { totalDiagnostics++; if (details.Count < limits.MaximumDetailedDiagnostics) details.Add(value); }
    }

    public async Task<ImportCommitSummary> CommitAsync(Stream source, ImportDefinition definition, ResolvedGridDefinition grid,
        IGridBatchRowImportProvider provider, ImportExportOperationContext context,
        Func<bool>? contextIsCurrent = null, IProgress<ImportExportProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(provider);
        if (!registry.TryGetParser(definition.ParserCode, out var parser)) return ImportCommitSummary.Failed("IMPORT_PARSER_UNKNOWN");
        var watch = Stopwatch.StartNew();
        var batch = ImmutableArray.CreateBuilder<ImportBatchRow>(limits.BatchSize);
        var details = ImmutableArray.CreateBuilder<ImportDiagnostic>();
        long total = 0, imported = 0, invalid = 0, skipped = 0, warnings = 0, diagnosticCount = 0;

        if (definition.CommitMode == ImportCommitMode.Atomic)
        {
            if (!source.CanSeek) return ImportCommitSummary.Failed("IMPORT_ATOMIC_REQUIRES_REPLAYABLE_STREAM");
            var initialPosition = source.Position; long validationTotal = 0, validationInvalid = 0;
            try
            {
                await foreach (var record in parser.ParseAsync(source, new(definition, limits), cancellationToken).ConfigureAwait(false))
                {
                    if (++validationTotal > limits.MaximumRecords) return ImportCommitSummary.Failed("IMPORT_RECORD_LIMIT");
                    var candidate = await mapping.MapAsync(record, definition, grid, cancellationToken: cancellationToken).ConfigureAwait(false);
                    if (!candidate.IsValid)
                    {
                        validationInvalid++;
                        foreach (var diagnostic in candidate.Diagnostics) Add(diagnostic);
                    }
                    progress?.Report(new("Validating", validationTotal));
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            { return new(validationTotal, 0, 0, validationInvalid, 0, details.ToImmutable(), diagnosticCount, watch.Elapsed, [], "IMPORT_CANCELLED"); }
            catch (Exception exception)
            { Add(ImportDiagnostic.Error("IMPORT_PARSE_FAILED", "The source could not be parsed safely.", exception: exception)); return new(validationTotal, 0, 0, validationInvalid, 0, details.ToImmutable(), diagnosticCount, watch.Elapsed, [], "IMPORT_ATOMIC_VALIDATION_FAILED"); }
            if (validationInvalid > 0) return new(validationTotal, 0, 0, validationInvalid, 0, details.ToImmutable(), diagnosticCount, watch.Elapsed, [], "IMPORT_ATOMIC_VALIDATION_FAILED");
            source.Position = initialPosition;
        }

        async Task<bool> FlushAsync()
        {
            if (batch.Count == 0) return true;
            if (contextIsCurrent?.Invoke() == false) { Add(ImportDiagnostic.Error("IMPORT_STALE_CONTEXT", "Company or workspace context changed.")); return false; }
            var request = new ImportBatchRequest(Guid.NewGuid(), batch.ToImmutable(), definition.CommitMode,
                definition.ImportCode, context.GridContext.Company.CompanyId, context.GridContext.WorkspaceId);
            batch.Clear();
            try
            {
                var result = await provider.ImportRowsAsync(context.GridContext, request, cancellationToken).ConfigureAwait(false);
                imported += result.Rows.Count(x => x.IsSuccess); skipped += result.Rows.Count(x => !x.IsSuccess);
                foreach (var failed in result.Rows.Where(x => !x.IsSuccess))
                    Add(ImportDiagnostic.Error(failed.DiagnosticCode ?? "IMPORT_PROVIDER_ROW_REJECTED", "The provider rejected an import row."));
                progress?.Report(new("Committing", imported + skipped, total));
                return result.DiagnosticCode is null;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception exception) { Add(ImportDiagnostic.Error("IMPORT_PROVIDER_FAILED", "The provider failed the import batch.", exception: exception)); return false; }
        }

        try
        {
            await foreach (var record in parser.ParseAsync(source, new(definition, limits), cancellationToken).ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested(); total++;
                if (total > limits.MaximumRecords) { Add(ImportDiagnostic.Error("IMPORT_RECORD_LIMIT", "The configured record safety limit was exceeded.")); break; }
                var candidate = await mapping.MapAsync(record, definition, grid, cancellationToken: cancellationToken).ConfigureAwait(false);
                foreach (var diagnostic in candidate.Diagnostics) Add(diagnostic);
                if (!candidate.IsValid)
                {
                    invalid++;
                    continue;
                }
                if (candidate.Diagnostics.Any(x => x.Severity == ImportDiagnosticSeverity.Warning)) warnings++;
                batch.Add(new(candidate.Values, definition.MatchKeyVariableCodes, definition.MutationMode));
                if (batch.Count >= limits.BatchSize && !await FlushAsync().ConfigureAwait(false)) break;
            }
            await FlushAsync().ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        { return new(total, imported, skipped, invalid, warnings, details.ToImmutable(), diagnosticCount, watch.Elapsed, [], "IMPORT_CANCELLED"); }
        return new(total, imported, skipped, invalid, warnings, details.ToImmutable(), diagnosticCount, watch.Elapsed, [],
            details.Any(x => x.Severity == ImportDiagnosticSeverity.Error) && imported == 0 ? "IMPORT_FAILED" : null);
        void Add(ImportDiagnostic value) { diagnosticCount++; if (details.Count < limits.MaximumDetailedDiagnostics) details.Add(value); }
    }

    private static ImportPreviewResult FailurePreview(string code, string message, int max) =>
        new([], [ImportDiagnostic.Error(code, message)], 0, 0, 0, 0, false, max, 1);
}

public sealed record ImportCommitSummary(long TotalSourceRecords, long ImportedRecords, long SkippedRecords,
    long InvalidRecords, long WarningRecords, ImmutableArray<ImportDiagnostic> Diagnostics, long TotalDiagnosticCount,
    TimeSpan Elapsed, ImmutableArray<RowKey> ResultingRowKeys, string? DiagnosticCode = null)
{
    public bool IsSuccess => DiagnosticCode is null;
    public static ImportCommitSummary Failed(string code) => new(0, 0, 0, 0, 0, [], 1, TimeSpan.Zero, [], code);
}

public sealed class ExportEngine
{
    private readonly ImportExportRegistry registry;
    public ExportEngine(ImportExportRegistry registry) => this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
    public async Task<ExportWriteResult> ExportAsync(Stream destination, ExportDefinition definition,
        ResolvedGridDefinition grid, IGridExportProvider provider, ImportExportOperationContext context,
        IEnumerable<RowKey>? selectedRows = null, int batchSize = 1_000,
        IProgress<ImportExportProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        if (!registry.TryGetWriter(definition.WriterCode, out var writer))
            return new(0, [ImportDiagnostic.Error("EXPORT_WRITER_UNKNOWN", "The selected export writer is not registered.")]);
        var visible = grid.Columns.Where(x => x.IsVisible).Select(x => x.Definition.VariableCode).ToHashSet();
        var fields = definition.Fields.Where(x => x.Include && visible.Contains(x.SourceVariableCode) &&
            ImportExportAuthorization.IsAllowed(x.PermissionCode, x.CapabilityCode, context.Authorization)).ToImmutableArray();
        if (!ImportExportAuthorization.IsAllowed(definition.PermissionCode, definition.CapabilityCode, context.Authorization))
            return new(0, [ImportDiagnostic.Error("EXPORT_NOT_AUTHORIZED", "Export is unavailable in the current context.")]);
        var request = new ExportProviderRequest(context.GridContext, definition.Scope, fields.Select(x => x.SourceVariableCode).ToImmutableArray(),
            (selectedRows ?? []).ToImmutableArray(), definition.Sort, definition.Filter, batchSize);
        var total = await provider.GetExportCountAsync(request, cancellationToken).ConfigureAwait(false);
        async IAsyncEnumerable<ExportRecord> Records([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken token = default)
        {
            long index = 0;
            await foreach (var row in provider.ExportRowsAsync(request, token).ConfigureAwait(false))
            {
                var values = fields.ToImmutableDictionary(x => x.SourceVariableCode, x => row.Values.GetValueOrDefault(x.SourceVariableCode));
                yield return new(++index, values); progress?.Report(new("Exporting", index, total));
            }
        }
        try { return await writer.WriteAsync(destination, new(definition, fields), Records(cancellationToken), progress, cancellationToken).ConfigureAwait(false); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception) { return new(0, [ImportDiagnostic.Error("EXPORT_WRITE_FAILED", "The destination could not be written safely.", exception: exception)]); }
    }
}
