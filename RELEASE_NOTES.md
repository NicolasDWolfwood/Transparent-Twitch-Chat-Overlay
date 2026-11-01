Fixes Settings failing to open in previous fork builds. The About page now supplies
a typed URI for its repository hyperlink, preventing a XAML construction error
that blocked the entire Settings dialog.

Validated locally with **76 Windows integration checks and 18 JavaScript tests**.
New regression coverage opens the complete modal Settings dialog, navigates its
pages, closes and reopens it, checks the About link, and saves successfully.

This preview replaces v1.1.9-preview.1. Fully extract the portable ZIP or use the
Windows installer. Existing settings remain compatible.

---

Included stream reliability improvements:

Ten stream reliability improvements on top of v1.1.8:

1. NativeChat connection status, heartbeat checks, bounded reconnect delays and a metadata startup watchdog.
2. Disable Chromium background/occlusion throttling; verify hidden browser timers continue.
3. Stable main/widget titles and borderless taskbar discovery for capture tools.
4. Fresh audio device selection on each play, default-device fallback, and recoverable sound errors.
5. Serialized, cancellable WebView2 initialization and renderer recovery with manual Retry controls.
6. Dispose closed browsers, stop timers during shutdown, and remove closed widgets from group operations.
7. Restore widget borders/visibility and apply Auto-Hide Borders after browser setup.
8. Preserve hotkeys, reject duplicate assignments, and show operating-system registration conflicts.
9. Real show/hide commands, separate hotkeys, a default Ctrl+Alt+F10 toggle, and widgets-only mode.
10. Reversible reduced motion, per-widget scripted animation limits, and NativeChat display update intervals.

**For OBS/OVR Toolkit:** turn off Always on top in Settings > General and Save.
Keep the captured window unminimized. Select the new stable window title once in
capture tools if necessary. Restore hidden windows through the tray menu.

This is a **preview** for VR acceptance. Desktop integration and deterministic
JavaScript checks pass locally, including real renderer crashes and measured
two-widget animation behavior. Headset capture and physical headset hot-plug
remain to be tested in a real session. No messages missed offline are replayed.
Scripted animation caps do not cap video, GIFs, or the capture application's FPS.

Download the Windows x64 installer or fully extract the portable ZIP. SHA-256
checksums are provided. v1.1.8 remains the latest stable release.

Independent fork by NicolasDWolfwood; original application by
[Baffler](https://github.com/baffler/Transparent-Twitch-Chat-Overlay).
