# Search Palette Design

The palette is a compact upper-workspace overlay. Wide Top Shell shows `Search…` plus the platform shortcut; narrow layout retains an accessible icon trigger. The input receives focus on open. Results show a semantic icon, title, safe subtitle, and concise group label.

Use semantic surface, border, text, muted-text, accent, selection, and focus tokens. Never hard-code application colors inside results. Respect the existing UI/font scale. Required keyboard flow is Cmd/Ctrl+K, Up/Down, Enter, Escape, and normal Tab traversal.

The input, icon-only trigger, result list, selected result, and disabled state require accessible names/semantics. Loading, partial-provider failure, empty, and unavailable messages stay short and localized in vi-VN/en-US. Do not toast on each provider failure.
