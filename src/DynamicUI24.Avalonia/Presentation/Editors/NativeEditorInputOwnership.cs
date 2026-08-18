using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Rendering;
using Avalonia.VisualTree;
using System.Runtime.CompilerServices;

namespace DynamicUI24.Avalonia.Presentation.Editors;

/// <summary>Shared native-input boundary. It contains no language or composition engine.</summary>
public static class NativeEditorInputOwnership
{
    private static readonly ConditionalWeakTable<TextBox, LifecycleState> States = new();

    public static void Enable(TextBox textBox)
    {
        ArgumentNullException.ThrowIfNull(textBox);
        var state = States.GetOrCreateValue(textBox);
        if (state.IsRegistered) return;
        state.IsRegistered = true;
        InputMethod.SetIsInputMethodEnabled(textBox, true);
        textBox.AttachedToVisualTree += (_, args) => Attach(textBox, state, args.Root);
        textBox.DetachedFromVisualTree += (_, _) => Detach(state);
        textBox.GotFocus += (_, _) => Activate(textBox, state, "focus", requestClient: true);
        textBox.LostFocus += (_, _) => state.HasFocus = false;
        textBox.TextInputMethodClientRequested += (_, args) =>
        {
            state.ClientRequestCount++;
            state.ClientAvailable = args.Client is not null;
        };
        if (textBox.IsAttachedToVisualTree()) Attach(textBox, state, TopLevel.GetTopLevel(textBox));
    }

    public static bool IsLifecycleReady(TextBox textBox) =>
        textBox is not null && States.TryGetValue(textBox, out var state) && state.IsReady;

    public static NativeEditorInputSnapshot Snapshot(TextBox textBox)
    {
        ArgumentNullException.ThrowIfNull(textBox);
        return States.TryGetValue(textBox, out var state) ? state.Snapshot(textBox) : default;
    }

    /// <summary>Runs after a workspace becomes active and its retained control tree has been assigned to the host.</summary>
    public static void WorkspaceActivated(TextBox textBox)
    {
        ArgumentNullException.ThrowIfNull(textBox);
        var state = States.GetOrCreateValue(textBox);
        state.WorkspaceActivationCount++;
        Activate(textBox, state, "workspace", requestClient: textBox.IsKeyboardFocusWithin);
    }

    public static bool Owns(InputElement? eventSource) => eventSource is TextBox ||
        eventSource is Visual visual && visual.FindAncestorOfType<TextBox>() is not null;

    private static void Attach(TextBox textBox, LifecycleState state, IRenderRoot? root)
    {
        state.AttachCount++;
        state.AttachmentRootAvailable = root is not null;
        state.VisualRootIdentity = root is null ? 0 : RuntimeHelpers.GetHashCode(root);
        var topLevel = root as TopLevel ?? TopLevel.GetTopLevel(textBox);
        BindTopLevel(textBox, state, topLevel);
        Activate(textBox, state, "attach", requestClient: false, topLevel);
    }

    private static void Detach(LifecycleState state)
    {
        if (state.TopLevel is WindowBase window)
        {
            window.Activated -= state.WindowActivatedHandler;
            window.Deactivated -= state.WindowDeactivatedHandler;
        }
        state.TopLevel = null;
        state.IsReady = false;
        state.HasFocus = false;
        state.WindowActive = false;
    }

    private static void BindTopLevel(TextBox textBox, LifecycleState state, TopLevel? topLevel)
    {
        if (ReferenceEquals(state.TopLevel, topLevel)) return;
        if (state.TopLevel is WindowBase previous)
        {
            previous.Activated -= state.WindowActivatedHandler;
            previous.Deactivated -= state.WindowDeactivatedHandler;
        }
        state.TopLevel = topLevel;
        if (topLevel is not WindowBase window) return;
        state.WindowActive = window.IsActive;
        state.WindowActivatedHandler = (_, _) =>
        {
            state.WindowActive = true;
            state.WindowActivationCount++;
            Activate(textBox, state, "window-activated", requestClient: textBox.IsKeyboardFocusWithin);
        };
        state.WindowDeactivatedHandler = (_, _) => state.WindowActive = false;
        window.Activated += state.WindowActivatedHandler;
        window.Deactivated += state.WindowDeactivatedHandler;
    }

    private static void Activate(TextBox textBox, LifecycleState state, string source, bool requestClient,
        TopLevel? knownTopLevel = null)
    {
        var topLevel = knownTopLevel ?? state.TopLevel ?? TopLevel.GetTopLevel(textBox);
        if (topLevel is null) return;
        BindTopLevel(textBox, state, topLevel);
        InputMethod.SetIsInputMethodEnabled(textBox, true);
        state.IsReady = true;
        state.HasFocus = textBox.IsKeyboardFocusWithin;
        state.LastActivationSource = source;
        state.NativeActivationCallbackCount++;
        if (requestClient)
        {
            state.ClientRequeryCount++;
            textBox.RaiseEvent(new RoutedEventArgs(InputMethod.TextInputMethodClientRequeryRequestedEvent));
        }
    }

    private sealed class LifecycleState
    {
        public bool IsRegistered { get; set; }
        public bool IsReady { get; set; }
        public bool AttachmentRootAvailable { get; set; }
        public bool WindowActive { get; set; }
        public bool HasFocus { get; set; }
        public bool ClientAvailable { get; set; }
        public int AttachCount { get; set; }
        public int WorkspaceActivationCount { get; set; }
        public int ClientRequestCount { get; set; }
        public int ClientRequeryCount { get; set; }
        public int WindowActivationCount { get; set; }
        public int NativeActivationCallbackCount { get; set; }
        public int VisualRootIdentity { get; set; }
        public string? LastActivationSource { get; set; }
        public TopLevel? TopLevel { get; set; }
        public EventHandler? WindowActivatedHandler { get; set; }
        public EventHandler? WindowDeactivatedHandler { get; set; }

        public NativeEditorInputSnapshot Snapshot(TextBox textBox) => new(IsRegistered, IsReady,
            AttachmentRootAvailable, TopLevel is not null, WindowActive, HasFocus,
            InputMethod.GetIsInputMethodEnabled(textBox), ClientRequestCount, ClientAvailable,
            AttachCount, WorkspaceActivationCount, LastActivationSource, RuntimeHelpers.GetHashCode(textBox),
            VisualRootIdentity, TopLevel is null ? 0 : RuntimeHelpers.GetHashCode(TopLevel),
            WindowActivationCount, NativeActivationCallbackCount, ClientRequeryCount);
    }
}

public readonly record struct NativeEditorInputSnapshot(bool IsRegistered, bool IsReady,
    bool AttachmentRootAvailable, bool TopLevelAvailable, bool WindowActive, bool HasFocus,
    bool InputMethodEnabled, int ClientRequestCount, bool ClientAvailable, int AttachCount,
    int WorkspaceActivationCount, string? LastActivationSource, int TextBoxIdentity,
    int VisualRootIdentity, int TopLevelIdentity, int WindowActivationCount,
    int NativeActivationCallbackCount, int ClientRequeryCount);
