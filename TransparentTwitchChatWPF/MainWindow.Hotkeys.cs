using NHotkey;
using NHotkey.Wpf;
using Microsoft.Extensions.Logging;
using System.Windows.Input;
using TransparentTwitchChatWPF.Utils;

namespace TransparentTwitchChatWPF;

public partial class MainWindow
{
    private void RegisterShortcut(string name, Hotkey shortcut, EventHandler<HotkeyEventArgs> handler)
    {
        if (shortcut == null || shortcut.Key == Key.None) return;
        try { HotkeyManager.Current.AddOrReplace(name, shortcut.Key, shortcut.Modifiers, handler); }
        catch (Exception ex)
        {
            HotkeyRegistration.Errors.Add($"{name}: {shortcut} could not be registered; another app may use it.");
            _logger.LogWarning(ex, "Could not register shortcut {ShortcutName}", name);
        }
    }
    private void ToggleOverlayVisibility(object sender, HotkeyEventArgs e) { SetOverlaysHidden(!App.Settings.GeneralSettings.OverlaysHidden); e.Handled = true; }
    private void ShowOverlays(object sender, HotkeyEventArgs e) { SetOverlaysHidden(false); e.Handled = true; }
    private void HideOverlays(object sender, HotkeyEventArgs e) { SetOverlaysHidden(true); e.Handled = true; }
}
