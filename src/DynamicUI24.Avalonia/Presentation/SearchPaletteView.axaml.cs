using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using DynamicUI24.Core.Privacy;
using DynamicUI24.Core.Search;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Avalonia.Presentation;

public sealed partial class SearchPaletteView : UserControl
{
    private readonly SearchCoordinator coordinator;
    private readonly SearchResultPresenter presenter;
    private readonly ILocalizationService localization;
    private readonly IIconRegistry icons;
    private readonly Func<SearchQuery> contextFactory;
    private readonly Func<SearchResult, CancellationToken, Task<SearchActivationResult>> activate;
    private readonly IQuickAccessStore? quickAccess;

    public SearchPaletteView() : this(new SearchCoordinator([]),
        new(new PrivacyPolicyResolver(), new SensitiveValuePresenter()), new DictionaryLocalizationService(),
        new SemanticIconRegistry(), () => new("", SearchScope.GlobalSearch),
        (_, _) => Task.FromResult(SearchActivationResult.Unavailable("SEARCH_NOT_CONFIGURED")), null) { }

    public SearchPaletteView(SearchCoordinator coordinator, SearchResultPresenter presenter,
        ILocalizationService localization, IIconRegistry icons, Func<SearchQuery> contextFactory,
        Func<SearchResult, CancellationToken, Task<SearchActivationResult>> activate, IQuickAccessStore? quickAccess = null)
    {
        this.coordinator = coordinator; this.presenter = presenter; this.localization = localization;
        this.icons = icons; this.contextFactory = contextFactory; this.activate = activate;
        this.quickAccess = quickAccess;
        InitializeComponent();
        AvaloniaTypography.ApplyUiFont(this);
        localization.CultureChanged += (_, _) => RefreshLabels();
        KeyDown += PaletteKeyDown;
        RefreshLabels();
    }

    public event EventHandler? CloseRequested;
    public IReadOnlyList<SearchResultView> CurrentResults =>
        (Results.ItemsSource as IEnumerable<SearchResultView> ?? []).ToArray();
    public bool IsInputFocused => QueryBox.IsFocused;
    public void Open()
    {
        QueryBox.Text = string.Empty;
        QueryBox.Focus();
        _ = RefreshAsync();
    }
    public async Task SetQueryAsync(string query)
    {
        QueryBox.Text = query;
        await RefreshAsync();
    }
    public async Task<SearchActivationResult> ActivateAsync(string resultId)
    {
        var item = CurrentResults.FirstOrDefault(x => x.Result.ResultId.Equals(resultId, StringComparison.OrdinalIgnoreCase));
        return item is null ? SearchActivationResult.Unavailable("SEARCH_RESULT_UNKNOWN")
            : await activate(item.Result, CancellationToken.None);
    }

    private async void QueryChanged(object? sender, TextChangedEventArgs e) => await RefreshAsync();
    public async Task RefreshAsync()
    {
        StatusText.Text = Localized("Search.Loading", "Searching…");
        var query = contextFactory() with { QueryText = QueryBox.Text ?? string.Empty,
            Culture = localization.CurrentCulture };
        var response = await coordinator.SearchAsync(query);
        if (response.IsStale) return;
        var views = response.Results.Select(result => presenter.Present(result, query.PrivacyContext))
            .Where(x => !x.IsHidden).Select(x => new SearchResultView(x.Result, x.Title, x.Subtitle,
                Localized($"Search.Group.{x.Result.ResultKind}", x.Result.ResultKind.ToString()),
                Geometry.Parse(icons.Resolve(x.Result.IconKey ?? StandardIconKeys.Search).SvgPathData), x.IsActionable)).ToArray();
        Results.ItemsSource = views;
        Results.SelectedIndex = views.Length == 0 ? -1 : 0;
        StatusText.Text = views.Length == 0 ? Localized("Search.Empty", "No results. Try another query.")
            : response.FailedProviderCodes.Length == 0 ? string.Empty : Localized("Search.Partial", "Some sources are unavailable.");
    }

    private void RefreshLabels()
    {
        QueryBox.Watermark = Localized("Search.Placeholder", "Search…");
        AutomationProperties.SetName(QueryBox, Localized("Search.AccessibleName", "Search commands and destinations"));
    }
    private string Localized(string key, string fallback)
    { var value = localization.Get(new(key)); return value == key ? fallback : value; }
    private void PaletteKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) { CloseRequested?.Invoke(this, EventArgs.Empty); e.Handled = true; return; }
        if (e.Key == Key.Down) { Results.SelectedIndex = Math.Min(Results.ItemCount - 1, Results.SelectedIndex + 1); e.Handled = true; }
        else if (e.Key == Key.Up) { Results.SelectedIndex = Math.Max(0, Results.SelectedIndex - 1); e.Handled = true; }
        else if (e.Key == Key.Enter) { _ = ActivateSelectedAsync(); e.Handled = true; }
    }
    private async Task ActivateSelectedAsync()
    {
        if (Results.SelectedItem is not SearchResultView { IsActionable: true } selected) return;
        var result = await activate(selected.Result, CancellationToken.None);
        if (result.Status == SearchActivationStatus.Success) CloseRequested?.Invoke(this, EventArgs.Empty);
        else StatusText.Text = Localized("Search.Unavailable", "This result is unavailable.");
    }
    private void SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (Results.SelectedItem is not SearchResultView selected)
        { FavoriteButton.IsVisible = PinButton.IsVisible = false; return; }
        AutomationProperties.SetName(Results, selected.Title);
        if (quickAccess is null) return;
        var id = selected.Result.SemanticIdentity;
        var favorite = quickAccess.Favorites.Any(x => x.EntryId.Equals(id, StringComparison.OrdinalIgnoreCase));
        var pinned = quickAccess.Pinned.Any(x => x.EntryId.Equals(id, StringComparison.OrdinalIgnoreCase));
        FavoriteButton.IsVisible = selected.Result.CanFavorite || favorite;
        PinButton.IsVisible = selected.Result.CanPin || pinned;
        FavoriteButton.Content = favorite ? Localized("Search.RemoveFavorite", "Remove Favorite") : Localized("Search.AddFavorite", "Add to Favorites");
        PinButton.Content = pinned ? Localized("Search.Unpin", "Unpin") : Localized("Search.Pin", "Pin");
    }
    private async void FavoriteClicked(object? sender, RoutedEventArgs e)
    {
        if (quickAccess is null || Results.SelectedItem is not SearchResultView selected) return;
        var entry = Entry(selected.Result);
        if (!quickAccess.RemoveFavorite(entry.EntryId)) quickAccess.AddFavorite(entry);
        await RefreshAsync();
    }
    private async void PinClicked(object? sender, RoutedEventArgs e)
    {
        if (quickAccess is null || Results.SelectedItem is not SearchResultView selected) return;
        var entry = Entry(selected.Result);
        if (!quickAccess.Unpin(entry.EntryId)) quickAccess.Pin(entry);
        await RefreshAsync();
    }
    private static QuickAccessEntry Entry(SearchResult result) => new(result.SemanticIdentity, result.ResultKind,
        result.WorkspaceId ?? result.NavigationTarget ?? result.RegisteredCommandCode ?? result.ResultId,
        result.ProviderCode, result.CompanyScope, result.CompanyId, result.WorkspaceId);
    private async void ResultDoubleTapped(object? sender, RoutedEventArgs e) => await ActivateSelectedAsync();
}

public sealed record SearchResultView(SearchResult Result, string Title, string Subtitle, string Group,
    Geometry IconPath, bool IsActionable);
