using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DynamicUI24.Shared.Presentation;

/// <summary>Generic, mutable shell state kept independent from any consumer business model.</summary>
public sealed class ShellPresentation : INotifyPropertyChanged
{
    private ThemeMode theme;
    private string cultureName;
    private string? currentWorkspaceId;
    private string? currentWorkspaceTitle;
    private PresentationState state = PresentationState.Ready;
    private string? statusMessage;

    public ShellPresentation(ApplicationBrand brand, ThemeMode theme = ThemeMode.System, string cultureName = "vi-VN")
    {
        Brand = brand ?? throw new ArgumentNullException(nameof(brand));
        this.theme = theme;
        this.cultureName = cultureName;
    }

    public ApplicationBrand Brand { get; }
    public ThemeMode Theme { get => theme; set => Set(ref theme, value); }
    public string CultureName { get => cultureName; set => Set(ref cultureName, value); }
    public string? CurrentWorkspaceId { get => currentWorkspaceId; set => Set(ref currentWorkspaceId, value); }
    public string? CurrentWorkspaceTitle { get => currentWorkspaceTitle; set => Set(ref currentWorkspaceTitle, value); }
    public PresentationState State { get => state; set => Set(ref state, value); }
    public string? StatusMessage { get => statusMessage; set => Set(ref statusMessage, value); }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
