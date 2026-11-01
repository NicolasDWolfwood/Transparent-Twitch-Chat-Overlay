Adds a central widget manager under **Settings > Widgets**.

- List existing widgets, including hidden ones.
- Show/hide, edit name/URL/custom CSS/interaction, reload, or delete with confirmation.
- Bring hidden, minimized or off-screen widgets onto the primary monitor.
- Keep the same window and saved placement/appearance when editing URLs.
- Hide private overlay paths and tokens from list labels.

Actions apply immediately, including when closing Settings with Cancel. Hidden
widgets keep running and may play audio. **Bring into view** moves a widget onto
your primary monitor: use it off stream if that monitor is captured.

Includes the single connection-indicator fix, the Settings-opening fix and all
ten stream reliability improvements. Existing settings remain compatible.

Validated locally with **101 Windows integration checks and 18 JavaScript tests**. Coverage includes
real WPF/WebView2 windows, full Settings open/reopen/Save,
widget Edit/Cancel, visibility, renderer reload, URL edits and disk persistence,
duplicate URL rejection, off-screen recovery, capture handle preservation,
private URL labels, and the existing chat reliability checks.

Download the Windows x64 installer or fully extract the portable ZIP. SHA-256
checksums are included. For OBS/OVR Toolkit, turn off Always on top under Settings
> General and keep the captured window unminimized. This remains a preview for
actual VR capture and physical headset-reconnection testing.

Independent fork by NicolasDWolfwood; original application by
[Baffler](https://github.com/baffler/Transparent-Twitch-Chat-Overlay).
