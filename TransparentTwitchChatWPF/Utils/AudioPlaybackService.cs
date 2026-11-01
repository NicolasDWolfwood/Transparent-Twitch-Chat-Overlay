using NAudio.Wave;

namespace TransparentTwitchChatWPF.Utils;

/// <summary>Resolve devices on every play, including after a VR headset reconnects.</summary>
public sealed class AudioPlaybackService : IDisposable
{
    private readonly object _gate = new();
    private WaveOutEvent _output;
    private AudioFileReader _reader;
    public string LastError { get; private set; }

    public bool TryPlay(string file, float volume, int deviceId, string deviceName)
    {
        lock (_gate)
        {
            DisposeCurrent();
            LastError = null;
            if (string.IsNullOrWhiteSpace(file) || file.Equals("none", StringComparison.OrdinalIgnoreCase)) return true;
            if (!System.IO.File.Exists(file)) { LastError = "The selected sound file is unavailable."; return false; }
            try
            {
                if (WaveOut.DeviceCount == 0) { LastError = "No audio output device is available."; return false; }
                int selected = ResolveDevice(deviceId, deviceName);
                try { Start(file, volume, selected); }
                catch when (selected != -1) { DisposeCurrent(); Start(file, volume, -1); }
                return true;
            }
            catch (Exception ex)
            {
                DisposeCurrent();
                LastError = "Sound could not play. Check your output device and try again.";
                System.Diagnostics.Debug.WriteLine($"Audio playback: {ex.GetType().Name}");
                return false;
            }
        }
    }

    private static int ResolveDevice(int deviceId, string deviceName)
    {
        if (deviceId < 0) return -1;
        for (int index = 0; index < WaveOut.DeviceCount; index++)
        {
            string name = WaveOut.GetCapabilities(index).ProductName;
            if (!string.IsNullOrEmpty(deviceName) && deviceName.StartsWith(name, StringComparison.Ordinal)) return index;
        }
        return -1;
    }

    private void Start(string file, float volume, int device)
    {
        _reader = new AudioFileReader(file) { Volume = Math.Clamp(volume, 0, 1) };
        _output = new WaveOutEvent { DeviceNumber = device };
        _output.Init(_reader);
        // Keep handles until the next play/disposal; no callback races with headset removal.
        _output.Play();
    }

    private void DisposeCurrent()
    {
        try { _output?.Stop(); } catch { }
        try { _output?.Dispose(); } catch { }
        _output = null;
        try { _reader?.Dispose(); } catch { }
        _reader = null;
    }

    public void Dispose() { lock (_gate) DisposeCurrent(); }
}
