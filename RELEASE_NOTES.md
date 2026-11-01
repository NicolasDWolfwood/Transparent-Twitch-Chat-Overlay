Adds an **Always on top** checkbox under **Settings > General**, so chat can stay behind OBS while OVR Toolkit captures it in VR.

- Saving applies the setting immediately to the main chat window and existing widgets, and remembers it across launches.
- Turning it off prevents focus changes, border changes, click-through controls, and the Bring to Top timer from making chat topmost again.
- Newly opened widgets follow the same setting.
- Portable WebView2 data stays with the portable settings folder.
- Update checks and release links now follow this NicolasDWolfwood fork. Automatic checks default to off.

**For OBS and OVR Toolkit:** open Settings > General, uncheck Always on top, and click Save. The initial setting stays on for compatibility. Keep chat open and unminimized. OVR Toolkit can independently bring windows forward when you interact with them; its Disable alt tabbing option can reduce that behavior.

Windows x64 downloads include an installer, a portable ZIP, and SHA-256 checksums. Extract the entire portable ZIP before running its launcher.

Validation: 24 WPF/native-window and settings-persistence checks passed locally. Headset capture still needs testing in an OVR Toolkit session.

This is an independent fork release. The original application is by [Baffler](https://github.com/baffler/Transparent-Twitch-Chat-Overlay). It includes the current source baseline, which contains all commits from upstream v1.1.7 and 18 subsequent upstream commits.
