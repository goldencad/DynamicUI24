# Grid clipboard architecture

```text
DataEntryGridRuntime -> IGridClipboardService <- AvaloniaGridClipboardService
        |                       |
 ClipboardMatrix          platform clipboard
        |
 validation -> GridEditTransaction -> optional batch provider
```

Core contains no platform API or key-modifier logic. It reads/writes text only through `IGridClipboardService`; `AvaloniaGridClipboardService` resolves the current top-level clipboard. Keyboard mapping translates Ctrl/Cmd shortcuts in the presentation layer.

Copy projects only selected visible cells. Paste parsing is rectangular and intentionally not a CSV or file-import engine. Structured `GridPasteResult` provides applied/rejected counts, validation errors, warnings, atomic/partial flags, confirmation need, and one diagnostic code for Notification/Action Bar integration.

Safety thresholds prevent symbolic select-all and oversized clipboard operations from synchronously allocating unbounded strings. The architecture does not own OS permission prompts, file formats, formula payloads, or business validation.

Focused architecture guard: `DataEntryGridArchitectureTests`.
