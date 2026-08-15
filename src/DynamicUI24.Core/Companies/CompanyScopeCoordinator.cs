using DynamicUI24.Core.Authorization;

namespace DynamicUI24.Core.Companies;

public enum CompanyScopeLoadStatus
{
    Loading,
    Ready,
    Error,
    Unavailable,
}

public sealed record CompanyScopeSnapshot(
    long Version,
    CompanyDescriptor Company,
    CompanyScopeLoadStatus Status,
    CompanyProfileResult? ProfileResult = null,
    EffectiveAuthorizationContext? AuthorizationContext = null);

/// <summary>
/// Coordinates company selection with profile and authorization refresh. Only the latest request may publish.
/// </summary>
public sealed class CompanyScopeCoordinator : IDisposable
{
    private readonly ICompanyContextProvider companyContext;
    private readonly ICompanyProfileProvider profileProvider;
    private readonly IAuthorizationPresentationProvider authorizationProvider;
    private readonly UserContext userContext;
    private readonly object sync = new();
    private CancellationTokenSource? refreshCancellation;
    private long version;
    private bool disposed;

    public CompanyScopeCoordinator(
        ICompanyContextProvider companyContext,
        ICompanyProfileProvider profileProvider,
        IAuthorizationPresentationProvider authorizationProvider,
        UserContext userContext)
    {
        this.companyContext = companyContext ?? throw new ArgumentNullException(nameof(companyContext));
        this.profileProvider = profileProvider ?? throw new ArgumentNullException(nameof(profileProvider));
        this.authorizationProvider = authorizationProvider ?? throw new ArgumentNullException(nameof(authorizationProvider));
        this.userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        Snapshot = new(0, companyContext.CurrentCompany, CompanyScopeLoadStatus.Loading);
    }

    public CompanyScopeSnapshot Snapshot { get; private set; }
    public event EventHandler<CompanyScopeSnapshot>? SnapshotChanged;

    public Task InitializeAsync(CancellationToken cancellationToken = default) =>
        RefreshLatestAsync(companyContext.CurrentCompany, cancellationToken);

    public async Task<CompanySwitchResult> SwitchCompanyAsync(
        CompanyId companyId,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        var result = await companyContext.SwitchCompanyAsync(companyId, cancellationToken).ConfigureAwait(false);
        if (result.IsSuccess)
        {
            await RefreshLatestAsync(result.CurrentCompany, cancellationToken).ConfigureAwait(false);
        }

        return result;
    }

    public Task RefreshAsync(CancellationToken cancellationToken = default) =>
        RefreshLatestAsync(companyContext.CurrentCompany, cancellationToken);

    private async Task RefreshLatestAsync(CompanyDescriptor company, CancellationToken externalCancellation)
    {
        ThrowIfDisposed();
        CancellationTokenSource requestCancellation;
        long requestVersion;
        CompanyScopeSnapshot loadingSnapshot;
        lock (sync)
        {
            refreshCancellation?.Cancel();
            refreshCancellation?.Dispose();
            refreshCancellation = CancellationTokenSource.CreateLinkedTokenSource(externalCancellation);
            requestCancellation = refreshCancellation;
            requestVersion = ++version;
            loadingSnapshot = new(requestVersion, company, CompanyScopeLoadStatus.Loading);
            Snapshot = loadingSnapshot;
        }
        SnapshotChanged?.Invoke(this, loadingSnapshot);

        try
        {
            var profileTask = profileProvider.GetProfileAsync(company.CompanyId, requestCancellation.Token);
            var authorizationTask = authorizationProvider.RefreshAsync(
                userContext, company.CompanyId, requestCancellation.Token);
            await Task.WhenAll(profileTask, authorizationTask).ConfigureAwait(false);

            var profile = await profileTask.ConfigureAwait(false);
            var authorization = await authorizationTask.ConfigureAwait(false);
            var status = authorization.Status == EffectiveAuthorizationStatus.Unavailable
                ? CompanyScopeLoadStatus.Unavailable
                : authorization.Status == EffectiveAuthorizationStatus.Error || profile.Status == CompanyProfileStatus.Error
                    ? CompanyScopeLoadStatus.Error
                    : CompanyScopeLoadStatus.Ready;
            TryPublish(requestVersion, company, status, profile, authorization);
        }
        catch (OperationCanceledException) when (requestCancellation.IsCancellationRequested)
        {
            // A newer request or caller cancellation owns the resulting state.
        }
        catch
        {
            TryPublish(requestVersion, company, CompanyScopeLoadStatus.Error, null, null);
        }
    }

    private void TryPublish(
        long requestVersion,
        CompanyDescriptor company,
        CompanyScopeLoadStatus status,
        CompanyProfileResult? profile,
        EffectiveAuthorizationContext? authorization)
    {
        CompanyScopeSnapshot? published = null;
        lock (sync)
        {
            if (requestVersion != version || companyContext.CurrentCompany.CompanyId != company.CompanyId)
            {
                return;
            }

            published = new(requestVersion, company, status, profile, authorization);
            Snapshot = published;
        }
        SnapshotChanged?.Invoke(this, published);
    }

    public void Dispose()
    {
        lock (sync)
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            refreshCancellation?.Cancel();
            refreshCancellation?.Dispose();
            refreshCancellation = null;
        }
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(disposed, this);
}
