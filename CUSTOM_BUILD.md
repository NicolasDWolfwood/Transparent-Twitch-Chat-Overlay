# Stream reliability preview

Version 1.1.9-preview.1 implements the ten stream reliability improvements on top
of this fork's v1.1.8. The original application is by Baffler; this is an independent fork.

## OBS and OVR Toolkit

Open Settings > General, turn off **Always on top**, and Save. The default remains
on for compatibility. Keep the captured window unminimized. Use the tray menu to
show/hide all overlays or restore the main chat. **Widgets only** hides the main
chat while keeping widgets alive. Ctrl+Alt+F10 toggles all overlays by default;
show and hide can also have their own shortcuts.

Window titles remain stable across focus and border changes. Borderless widgets
remain on the taskbar unless Hide Taskbar Icon is enabled. Existing OBS/OVR window
selections may need selecting once after upgrading to these new titles.

NativeChat now shows connection state and automatically reconnects after failed
heartbeats, network loss, or Twitch reconnect requests. Optional metadata has a
10-second startup watchdog. This does not recover messages missed while offline.
Browser initialization and renderer failures retry up to three times; a Retry
button remains available afterward. Settings stay accessible after a browser failure.

Audio devices are resolved for each playback, falling back to the Windows default
when a saved headset is unavailable. Missing files and unavailable outputs report
recoverable errors. Reconnect a headset and play another sound to retry.

## Performance controls

Settings > General offers reduced motion and NativeChat display intervals of
100/200/500/1000 ms; the default stays 200 ms. A widget's context menu offers
reduced motion and scripted animation limits of Unlimited/15/30/60 FPS. These
limits control requestAnimationFrame callbacks, not video, animated GIFs, CSS
animation frame rates, or OVR Toolkit's capture rate. Reduced motion disables CSS
animations/transitions; it can be reversed. Independent network timers continue.
Sites that deliberately stop work while unfocused may need site-specific changes.

## Build and verify

Use .NET SDK 8 and Node.js 24/npm on Windows:

```powershell
powershell -NoProfile -File .\publish.ps1 -Version 1.1.9-preview.1
```

The script runs Node tests, builds NativeChat, publishes a self-contained Windows
x64 app, runs real WPF/WebView2 checks, and packages installer/portable/update
assets and SHA-256 checksums in artifacts/packages-1.1.9-preview.1. The portable
ZIP must be fully extracted. Its profile stays with the portable installation;
installer packages use the separate TransparentTwitchChatAlwaysOnTop app ID.
Automatic update checks default to off and point at this fork.

After one full build, rerun Windows checks with verification/run.ps1 or JavaScript
checks with npm test --prefix NativeChatClient. Logs and the General settings
preview are under artifacts. See IMPLEMENTATION_PLAN.md for coverage and limits.
Pushing a v* tag builds and publishes through GitHub Actions; preview tags are
marked prerelease and do not replace the latest stable release.

## Acceptance

Desktop checks cover native window flags, persisted settings/hotkeys, actual
renderer crashes, disposal, audio fallback, hidden browser timers, and two-widget
animation measurements. Builds retain upstream nullable/unused-member warnings
and a NativeChat asset-size warning. Headset hot-plug, actual OBS/OVR Toolkit
capture, and a full streaming session require checking with your VR setup.
