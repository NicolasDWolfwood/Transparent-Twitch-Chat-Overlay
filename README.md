# Transparent Twitch Chat Overlay
[![GitHub](https://img.shields.io/github/license/baffler/Transparent-Twitch-Chat-Overlay)](https://raw.githubusercontent.com/baffler/Transparent-Twitch-Chat-Overlay/master/LICENSE)
[![GitHub release (with filter)](https://img.shields.io/github/v/release/NicolasDWolfwood/Transparent-Twitch-Chat-Overlay)](https://github.com/NicolasDWolfwood/Transparent-Twitch-Chat-Overlay/releases)

.NET application for Windows that will display Twitch chat on top of a windowed or borderless windowed game. Typically would be used by single monitor streamers who need to see chat while playing.

This is the **NicolasDWolfwood fork** of the [original application by Baffler](https://github.com/baffler/Transparent-Twitch-Chat-Overlay).

### Optional always on top (v1.1.8)

Open **Settings > General**, uncheck **Always on top**, and click **Save** to let OBS cover chat while OVR Toolkit captures it. The choice applies to chat and widget windows and survives restarts. Keep chat open and unminimized for capture. OVR Toolkit's own window focusing is independent of this checkbox.

Always on top initially remains enabled for compatibility. Update checks default to off; when enabled, they check this fork. See [release notes](RELEASE_NOTES.md) and [build instructions](CUSTOM_BUILD.md).

**Be sure to check out the wiki** [at this link](https://github.com/baffler/Transparent-Twitch-Chat-Overlay/wiki). There's explanations on there on how to add different types of chat, Twitch and Youtube chat integrated together, show subs/followers/redemptions etc, how to show viewer count, or any widget you might need.

* For this fork's issues or feature requests: https://github.com/NicolasDWolfwood/Transparent-Twitch-Chat-Overlay/issues
* Original project: https://github.com/baffler/Transparent-Twitch-Chat-Overlay

![Screenshot](https://i.imgur.com/keN4f9F.jpg)

## Installation and Usage
* Download this fork's latest release from https://github.com/NicolasDWolfwood/Transparent-Twitch-Chat-Overlay/releases/latest
* You can download the Installer now to make it easier to install the program and it will automatically install the required dependencies ([WebView2 runtimes](https://go.microsoft.com/fwlink/p/?LinkId=2124703)).
* "Windows protected your PC" message may appear which is the Windows Defender Smart Screen. You'll need to click "More info" and then "Run anyway". You'll only have to do this the first time you launch it.
* After launching the program for the first time, you'll need to set your twitch channel in the settings. You can click the button at the top-left to open settings, right-click the black border at top, or right-click the icon in the taskbar
* You can move the application by clicking and dragging the thin black border at top, and you can resize it by clicking and dragging the grip at the bottom-right
* To hide the borders, just click the [o] button in the top-right, or right-click the icon in your taskbar

![Screenshot](https://i.imgur.com/B3SDcrk.png)

## Adding widgets (subs/follows/viewer count/etc...)
Some commonly requested features can usually be added with widgets. Please check out the wiki for an explanation on how to do this: https://github.com/baffler/Transparent-Twitch-Chat-Overlay/wiki

## Star History

<a href="https://star-history.com/#baffler/Transparent-Twitch-Chat-Overlay&Date">
 <picture>
   <source media="(prefers-color-scheme: dark)" srcset="https://api.star-history.com/svg?repos=baffler/Transparent-Twitch-Chat-Overlay&type=Date&theme=dark" />
   <source media="(prefers-color-scheme: light)" srcset="https://api.star-history.com/svg?repos=baffler/Transparent-Twitch-Chat-Overlay&type=Date" />
   <img alt="Star History Chart" src="https://api.star-history.com/svg?repos=baffler/Transparent-Twitch-Chat-Overlay&type=Date" />
 </picture>
</a>
