namespace DynamicUI24.Shared.Presentation;

public enum PresentationStateKind
{
    Empty,
    Loading,
    Ready,
    Error,
    ReadOnly,
    PermissionDenied,
    Unavailable,
}

public sealed record ErrorPresentation(
    string FriendlyMessage,
    string DiagnosticCode,
    string? Details = null,
    bool CanRetry = false)
{
    public bool HasDetails => !string.IsNullOrWhiteSpace(Details);
}

/// <summary>
/// Explicit presentation state. Unavailable is a state, never a stand-in for a value such as zero.
/// </summary>
public sealed record PresentationState(
    PresentationStateKind Kind,
    LocalizationKey MessageKey,
    ErrorPresentation? Error = null)
{
    public static PresentationState Ready { get; } = new(PresentationStateKind.Ready, new("State.Ready"));

    public static PresentationState For(PresentationStateKind kind, ErrorPresentation? error = null) =>
        new(kind, new LocalizationKey($"State.{kind}"), error);
}
