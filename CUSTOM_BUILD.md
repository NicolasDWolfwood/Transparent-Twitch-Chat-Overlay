# Always on top option

This fork adds Settings > General > Always on top. Saving applies the
setting to chat and widget windows immediately and remembers it across launches.
When off, losing focus, changing borders, toggling interaction, and the Bring to
Top command cannot re-enable the topmost window flag. Temporary settings dialogs
retain their original behavior.

The default remains on for compatibility with existing settings. Turn it off
and click Save for use with OBS and OVR Toolkit. Automatic update checks default
to off; when enabled, updates come from the NicolasDWolfwood fork. Portable
browser data follows the portable settings directory. Installer packages use
the separate TransparentTwitchChatAlwaysOnTop application ID.

## Build

Use .NET SDK 8 and Node.js/npm on Windows:

```powershell
powershell -NoProfile -File .\publish.ps1 -Version 1.1.8
```

## Verify

The publish script builds NativeChat, publishes a self-contained Windows x64
app, runs the window checks, restores the pinned Velopack tool, and produces an
installer, portable ZIP, update package, and SHA-256 checksums under
`artifacts/packages-1.1.8`. Pushing a `v*` tag runs the same process in GitHub
Actions and publishes the release. Update RELEASE_NOTES.md for each release.

After building NativeChat and the app once, the checks can also be run with
`powershell -NoProfile -File verification\run.ps1`.
The WPF integration runner uses real chat/widget classes, native Windows window
flags, an isolated portable settings folder, and blank browser pages. It checks
the checkbox's save behavior, live changes to existing widgets, focus handlers,
border/interaction controls, timer behavior, minimized widgets, and a disk reload.

Builds have upstream nullable/unused-member warnings and NativeChat asset-size
warnings. Headset capture and OVR Toolkit's own window-focusing behavior still
need a check in the user's VR session.
