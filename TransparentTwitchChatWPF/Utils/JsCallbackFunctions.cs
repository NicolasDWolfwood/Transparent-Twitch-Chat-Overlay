using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TransparentTwitchChatWPF.Utils;

[ClassInterface(ClassInterfaceType.AutoDual)]
[ComVisible(true)]
public class JsCallbackFunctions : IDisposable
{
    private readonly AudioPlaybackService _player = new();
    private string _lastReportedError;
    public string MediaFile { get; set; }
    public void OnAudioDeviceChanged() => _player.Dispose();
    public void playSound()
    {
        var settings = App.Settings.GeneralSettings;
        bool played = _player.TryPlay(MediaFile, settings.OutputVolume, settings.DeviceID, settings.DeviceName);
        if (!played && _player.LastError != _lastReportedError)
            Growl.Warning(_player.LastError);
        _lastReportedError = played ? null : _player.LastError;
    }
    public void Dispose() => _player.Dispose();
    public void logMessage(string msg) => Debug.WriteLine($"[JS] {msg}");
    public void showMessageBox(string msg) => Growl.Info(msg);
}
