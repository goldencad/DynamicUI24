using System.Collections.Immutable;

namespace DynamicUI24.Core.Authoring;

public sealed class UiDefinitionDraft
{
    private readonly List<UiElementDefinition> elements;
    private readonly Stack<UiDraftChange> undo = new();
    private readonly Stack<UiDraftChange> redo = new();

    public UiDefinitionDraft(UiDefinition published)
    {
        ArgumentNullException.ThrowIfNull(published);
        Code = published.Code; BasedOnVersion = published.Version; SchemaVersion = published.SchemaVersion;
        elements = [.. published.Elements];
    }

    public UiDefinitionCode Code { get; }
    public UiDefinitionVersion BasedOnVersion { get; }
    public int SchemaVersion { get; }
    public bool IsDirty => undo.Count > 0;
    public ImmutableArray<UiElementDefinition> Elements => [.. elements];
    public bool CanUndo => undo.Count > 0;
    public bool CanRedo => redo.Count > 0;

    public void Upsert(UiElementDefinition element)
    {
        ArgumentNullException.ThrowIfNull(element);
        var index = elements.FindIndex(x => x.Code == element.Code);
        var before = index < 0 ? null : elements[index];
        Apply(new(element.Code, before, element), record: true);
    }

    public bool Remove(UiElementCode code)
    {
        var current = elements.FirstOrDefault(x => x.Code == code);
        if (current is null) return false;
        Apply(new(code, current, null), record: true); return true;
    }

    public bool Undo() { if (!undo.TryPop(out var change)) return false; Apply(change.Reverse(), false); redo.Push(change); return true; }
    public bool Redo() { if (!redo.TryPop(out var change)) return false; Apply(change, false); undo.Push(change); return true; }

    public UiDefinition CreatePublished(UiDefinitionVersion version, DateTimeOffset publishedAt, string safeSummary) =>
        new(Code, version, SchemaVersion, publishedAt, elements, safeSummary);

    private void Apply(UiDraftChange change, bool record)
    {
        var index = elements.FindIndex(x => x.Code == change.Code);
        if (index >= 0) elements.RemoveAt(index);
        if (change.After is not null) elements.Insert(index < 0 ? elements.Count : index, change.After);
        if (record) { undo.Push(change); redo.Clear(); }
    }

    private sealed record UiDraftChange(UiElementCode Code, UiElementDefinition? Before, UiElementDefinition? After)
    { public UiDraftChange Reverse() => new(Code, After, Before); }
}

public sealed class UiAuthoringRuntimeState
{
    public UiAuthoringRuntimeState(UiDefinitionDraft draft) => Draft = draft ?? throw new ArgumentNullException(nameof(draft));
    public UiDefinitionDraft Draft { get; }
    public UiElementCode? SelectedElement { get; set; }
    public string SearchText { get; set; } = string.Empty;
    public bool IsPreview { get; private set; }
    public long PreviewGeneration { get; private set; }
    public void BeginPreview() { IsPreview = true; PreviewGeneration = checked(PreviewGeneration + 1); }
    public void EndPreview() => IsPreview = false;
}
