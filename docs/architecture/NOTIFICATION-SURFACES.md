# Notification Surfaces

`NotificationSurfaceDefinition` selects Notification Center, Top Action Bar, Bottom Action Bar, Banner, Alert Card, Toast, or Blocking Notice and independently controls density and visible elements. Display modes are IconOnly, Compact, Standard, and Detailed.

All surfaces reference one resolved `NotificationInstance`; they never own separate lifecycle, dismissal, progress, permission, or company state. Severity is semantic and never implies a surface: Critical remains non-blocking unless `BlockingNotice` is explicitly requested.

The Avalonia `NotificationHost` renders Center, Toast, Banner, Alert Card, and Blocking Notice with semantic design tokens, visible severity text, keyboard buttons, accessible names, localized copy, textual progress, and theme resources. `NotificationActionBarAdapter` contributes actions to the existing `DynamicActionBarHost` for top and bottom placement.

Notification Center orders by priority, recency, then stable instance ID and groups Needs Attention separately from Recent / Information. Search, pagination, analytics, and persistent history are deliberately absent.
