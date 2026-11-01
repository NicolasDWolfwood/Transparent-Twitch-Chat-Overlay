# Stream reliability implementation

Baseline: v1.1.8 (`57fced5`). Branch: `improvements/stream-reliability`.
Delivery: Windows x64 v1.1.9-preview.2, for OBS/OVR Toolkit acceptance.

All ten implementation items are complete:

| # | Improvement | Implemented behavior and evidence |
|---|---|---|
| 1 | NativeChat connection health | Visible in-page connection badge, JOIN acknowledgement, heartbeat checks, sleep/network recovery, capped 30-second retry delays, stale-socket guards. Optional metadata fails open after 10 seconds; no duplicate connection on late completion. Deterministic WebSocket/timer tests. |
| 2 | Background updates | Disable Chromium background timer, occlusion and renderer throttling. Real hidden WebView timers keep advancing. This cannot override websites that explicitly pause themselves on focus/visibility events. |
| 3 | Capture discovery | Stable main title and per-widget display name plus persisted URL hash. Borders/focus changes preserve identities; widgets remain on taskbar unless the user disables it. Capture selections may need updating once. |
| 4 | Audio resilience | One shared playback implementation for chat/previews; resolve device name on each play, fall back to default, recreate playback resources, handle missing files/outputs, safely enumerate devices. Real zero-volume WAV fallback and retry checks. |
| 5 | Browser recovery | Shared bounded retry helper, cancellable serialized initialization, failed environment recreation, in-window Retry controls. Real main/widget renderer crashes recover without replacing the window or duplicating widgets; settings browser disposal is checked. |
| 6 | Widget lifetime | Remove closed widgets from live collection, snapshot group iterations, detach/dispose browsers and audio, cancel retries and stop timers on shutdown. Removal confirmation remains only for deleting widget settings; hiding preserves them. |
| 7 | Startup state | Track widget borders/visibility/performance; restore auto-hide after browser initialization. Main border choice is remembered. Recreated-widget and live layout checks. |
| 8 | Hotkeys | Explicit JSON constructors preserve cleared/unmodified shortcuts. Duplicate validation before save and visible registration errors for system conflicts. Real RegisterHotKey conflict and settings disk reload checks. |
| 9 | Visibility | Tray show/hide/main-chat commands, widgets-only mode, distinct show/hide shortcuts and default Ctrl+Alt+F10 toggle. Hide keeps browsers alive; Show respects widgets-only and restores hidden widgets. |
| 10 | Performance | NativeChat configurable 100/200/500/1000 ms display updates; reversible reduced motion; per-widget scripted animation caps Unlimited/15/30/60. Two real WebViews benchmarked; independent timers continue. |

## Local verification

- 76 WPF/WebView2/native-window checks passed, including all 24 v1.1.8 checks.
- 18 deterministic Node checks cover heartbeat/reconnect/startup, status badge,
  fair animation delivery, cancellation and reversible motion settings.
- Two-widget measurement, requested 1.2-second intervals: unlimited 226/226
  callbacks, limited 19/20; independent timer counts 27/30. Counts include host
  script round-trip overhead; this is an animation callback measurement, not a
  CPU/GPU utilization or stream FPS claim.
- `publish.ps1` runs both suites, rebuilds the native assets, publishes and packages
  Windows x64 installer/portable/update assets, and writes SHA-256 checksums.
- Logs: `artifacts/verification-results.txt`,
  `artifacts/native-reliability-tests.txt`; settings image:
  `artifacts/settings-preview.png`. Local output is intentionally not committed.
- Upstream nullable/unused-member and NativeChat asset-size warnings remain.
  GitHub Actions has not been claimed as passed; these results are local Windows tests.

## VR acceptance

Actual OBS/OVR Toolkit capture, headset hot-plug and a full live streaming session
require the user's headset setup. Test Always on top off while OBS covers chat,
then interact in OVR Toolkit; verify capture still advances while the desktop
window stays behind OBS. Check two widgets, show/hide, audio after reconnecting
the headset, and network interruption recovery. Keep captured windows unminimized.
Some capture methods cannot capture hidden windows; use widgets-only only when
the main chat is not a capture target. Offline chat history is not replayed.
Animation caps do not limit video, GIFs or OVR capture FPS. v1.1.8 remains stable
while this preview is evaluated.

## Settings regression correction

The complete Settings dialog failed to construct because its About page passed
a string from x:Static into a Uri-valued NavigateUri property. WPF does not apply
the literal-string URI converter to the markup-extension result. A typed
AppInfo.RepositoryUri property fixes this. The full dialog was not covered by the
original 61 checks; preview.2 adds real modal open, page navigation, close/reopen,
hyperlink type validation and Save checks. The expanded suite passes 76 checks.
