Fixes the duplicate **Chat connected** labels shown in earlier reliability previews.
NativeChat now owns the single visible connection indicator inside the captured
chat page. Connection health checks and automatic reconnect behavior remain active.

Also includes the Settings-opening fix from v1.1.9-preview.2 and all ten stream
reliability improvements. Existing settings remain compatible.

Validated locally with **79 Windows integration checks and 18 JavaScript tests**.
The connection-status checks exercise the actual packaged JavaScript in a real
WebView, verify exactly one page badge, verify no duplicate native badge, and
check repeated reconnects. Full Settings open/navigation/reopen/Save checks pass.

Download the Windows x64 installer or fully extract the portable ZIP. SHA-256
checksums are included. For OBS/OVR Toolkit, turn off Always on top under Settings
> General and keep the captured window unminimized. This remains a preview for
actual VR capture and physical headset-reconnection testing.

Independent fork by NicolasDWolfwood; original application by
[Baffler](https://github.com/baffler/Transparent-Twitch-Chat-Overlay).
