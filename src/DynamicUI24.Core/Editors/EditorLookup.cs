using System.Collections.Immutable;
using DynamicUI24.Core.Companies;

namespace DynamicUI24.Core.Editors;

public sealed record EditorLookupRequest(EditorCode EditorCode, EditorSemanticId ConsumerSemanticId,
    string SearchText, IReadOnlyDictionary<string, string> Filters, int Offset, string? ContinuationToken,
    int WindowSize, CompanyId? CompanyId, string? ContextRevision, long Generation,
    CancellationToken CancellationToken = default)
{
    public const int MaximumWindowSize = 200;
    public int BoundedWindowSize => Math.Clamp(WindowSize, 1, MaximumWindowSize);
}

public sealed record EditorLookupOption(string SemanticOptionId, string SafeDisplayText,
    string? SafeSecondaryText = null, bool IsEnabled = true)
{
    public override string ToString() => SafeDisplayText;
}
public enum EditorLookupStatus { Ready, Empty, NoMatch, Error, Unauthorized }
public sealed record EditorLookupResult(ImmutableArray<EditorLookupOption> Items, string? ContinuationToken,
    long? LogicalCount, long Generation, CompanyId? CompanyId, string? ContextRevision,
    EditorLookupStatus Status = EditorLookupStatus.Ready, string? DiagnosticCode = null);

public interface IEditorLookupProvider
{
    string ProviderCode { get; }
    ValueTask<EditorLookupResult> QueryAsync(EditorLookupRequest request);
}

public enum EditorLookupRuntimeStatus { Idle, Loading, Ready, Empty, NoMatch, Error, Unauthorized, StaleIgnored }

/// <summary>Bounded, generation/context-safe lookup coordinator. Cancellation is only an optimization.</summary>
public sealed class EditorLookupCoordinator
{
    private long generation;
    private CompanyId? companyId;
    private string? contextRevision;
    public EditorLookupRuntimeStatus Status { get; private set; }
    public ImmutableArray<EditorLookupOption> Items { get; private set; } = [];
    public string? ContinuationToken { get; private set; }
    public long CurrentGeneration => generation;

    public void SetContext(CompanyId? company, string? revision)
    { companyId = company; contextRevision = revision; generation++; Items = []; ContinuationToken = null; Status = EditorLookupRuntimeStatus.Idle; }

    public async ValueTask<bool> QueryAsync(IEditorLookupProvider provider, EditorCode editorCode,
        EditorSemanticId target, string searchText, int windowSize = 50, int offset = 0,
        IReadOnlyDictionary<string, string>? filters = null, CancellationToken cancellationToken = default)
    {
        var requestGeneration = ++generation;
        var requestCompany = companyId;
        var requestRevision = contextRevision;
        Status = EditorLookupRuntimeStatus.Loading;
        try
        {
            var request = new EditorLookupRequest(editorCode, target, searchText ?? string.Empty, filters ??
                new Dictionary<string, string>(), offset, offset == 0 ? null : ContinuationToken,
                Math.Clamp(windowSize, 1, EditorLookupRequest.MaximumWindowSize), requestCompany,
                requestRevision, requestGeneration, cancellationToken);
            var result = await provider.QueryAsync(request);
            if (requestGeneration != generation || result.Generation != requestGeneration ||
                result.CompanyId != companyId || result.ContextRevision != contextRevision)
            { Status = EditorLookupRuntimeStatus.StaleIgnored; return false; }
            Items = result.Items.Take(EditorLookupRequest.MaximumWindowSize).ToImmutableArray();
            ContinuationToken = result.ContinuationToken;
            Status = result.Status switch
            {
                EditorLookupStatus.Ready => Items.Length == 0 ? EditorLookupRuntimeStatus.Empty : EditorLookupRuntimeStatus.Ready,
                EditorLookupStatus.Empty => EditorLookupRuntimeStatus.Empty,
                EditorLookupStatus.NoMatch => EditorLookupRuntimeStatus.NoMatch,
                EditorLookupStatus.Unauthorized => EditorLookupRuntimeStatus.Unauthorized,
                _ => EditorLookupRuntimeStatus.Error,
            };
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { if (requestGeneration == generation) Status = EditorLookupRuntimeStatus.Error; return false; }
    }
}

public sealed record EditorLookupSelection(string SemanticOptionId, string SafeDisplayText);

/// <summary>Separates a highlighted lookup row from the authoritative semantic selection.</summary>
public sealed class EditorLookupSelectionState
{
    private ImmutableArray<EditorLookupOption> items = [];
    public int ActiveIndex { get; private set; } = -1;
    public EditorLookupSelection? Selected { get; private set; }
    public IReadOnlyList<EditorLookupOption> Items => items;

    public void SetItems(IEnumerable<EditorLookupOption> values)
    {
        items = (values ?? []).Take(EditorLookupRequest.MaximumWindowSize).ToImmutableArray();
        ActiveIndex = items.Length == 0 ? -1 : Math.Clamp(ActiveIndex, 0, items.Length - 1);
    }

    public bool SetActive(EditorLookupOption? option)
    {
        var index = option is null ? -1 : items.IndexOf(option);
        if (index < 0) return false;
        ActiveIndex = index; return true;
    }

    public bool MoveActive(int delta)
    {
        if (items.Length == 0) return false;
        ActiveIndex = Math.Clamp((ActiveIndex < 0 ? 0 : ActiveIndex) + delta, 0, items.Length - 1);
        return true;
    }

    public EditorLookupSelection? CommitActive()
    {
        if (ActiveIndex < 0 || ActiveIndex >= items.Length) return null;
        var option = items[ActiveIndex];
        return Selected = new(option.SemanticOptionId, option.SafeDisplayText);
    }

    public bool RestoreSemanticSelection(string semanticOptionId)
    {
        var index = -1;
        for (var i = 0; i < items.Length; i++)
            if (items[i].SemanticOptionId.Equals(semanticOptionId, StringComparison.Ordinal)) { index = i; break; }
        if (index < 0) return false;
        ActiveIndex = index; CommitActive(); return true;
    }
}

/// <summary>Adoption seam; consumers retain filter/query semantics.</summary>
public sealed record FilterEditorTarget(EditorSemanticId SemanticId, EditorValueType ValueType,
    EditorKind? ExplicitKind = null);
/// <summary>Adoption seam for report parameters without introducing a report runtime.</summary>
public sealed record ReportParameterEditorTarget(EditorSemanticId SemanticId, EditorCode EditorCode,
    EditorValueType ValueType);
/// <summary>Adoption seam for setup, dialogs and future metadata forms.</summary>
public sealed record FormFieldEditorTarget(EditorSemanticId SemanticId, EditorCode EditorCode,
    EditorValueType ValueType);
