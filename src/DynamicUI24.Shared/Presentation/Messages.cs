namespace DynamicUI24.Shared.Presentation;

public enum MessageKind
{
    Information,
    Warning,
    Error,
    Confirmation,
}

public sealed record MessageRequest(
    MessageKind Kind,
    string Title,
    string Message,
    string? DiagnosticCode = null);

public enum MessageResult
{
    Acknowledged,
    Confirmed,
    Cancelled,
}

public interface IMessageService
{
    Task<MessageResult> ShowAsync(MessageRequest request, CancellationToken cancellationToken = default);
}

public interface IApplicationExitService
{
    void RequestExit();
}
