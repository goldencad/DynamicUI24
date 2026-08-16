# Capture Protection

## CAPTURE BOUNDARY

`ICaptureProtectionService` is platform-neutral and exposes explicit `SUPPORTED`, `PARTIAL`, `UNSUPPORTED`, or `UNKNOWN` capability for `WINDOW`, `REGION`, or `CONTENT_SURFACE` scope. Request results state whether protection is actually active; a call that merely does not throw is not success.

Core contains no Windows, macOS, Linux, compositor, or Avalonia API. Platform adapters belong outside Core. P1 ships a safe unsupported adapter and a testable contract; it does not use undocumented macOS APIs or chase X11/Wayland/vendor-specific behavior.

`CAPTURE_PROTECT` always has a safe `MASK`, `PARTIAL_MASK`, or `HIDE` fallback. Partial, unsupported, unknown, malformed fallback, and adapter failure use the safe fallback. Manual Privacy On remains the dependable cross-platform control. Auto uses application/default policy only when reliable platform signals are absent and never fabricates screen-share or remote-session detection.
