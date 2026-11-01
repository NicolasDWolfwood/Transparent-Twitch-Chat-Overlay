using System.ComponentModel;
using System.Windows;
using TransparentTwitchChatWPF.Utils;

namespace TransparentTwitchChatWPF;

public partial class CustomWindow
{
    private readonly CancellationTokenSource _lifetime = new();
    private Task _browserRecoveryTask = Task.CompletedTask;
    private bool _isClosing;
    public bool IsClosed => _isClosing;
    public bool? BordersHidden { get; set; }
    public bool IsHidden { get; set; }
    public bool ReduceMotion { get; set; }
    public int AnimationFps { get; set; }

    public Task RetryBrowserAsync()
    {
        if (_isClosing || !_browserRecoveryTask.IsCompleted) return _browserRecoveryTask;
        _browserRecoveryTask = BrowserRecovery.RunAsync(async () =>
        {
            await SetupWebViewAsync();
            _lifetime.Token.ThrowIfCancellationRequested();
        }, CleanupBrowser, ReportBrowserStatus, _lifetime.Token);
        return _browserRecoveryTask;
    }

    private void ReportBrowserStatus(string message)
    {
        if (_isClosing) return;
        if (message == null) PlaceholderOverlay.Visibility = Visibility.Collapsed;
        BrowserErrorText.Text = message ?? "";
        BrowserErrorPanel.Visibility = message == null ? Visibility.Collapsed : Visibility.Visible;
        if (message != null)
        {
            var handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            if (handle != IntPtr.Zero) WindowHelper.SetWindowInteractable(handle);
        }
    }

    private void CleanupBrowser()
    {
        var previous = webView;
        webView = null;
        if (previous == null) return;
        try
        {
            previous.NavigationCompleted -= webView_NavigationCompleted;
            if (previous.CoreWebView2 != null) previous.CoreWebView2.ProcessFailed -= webView_CoreWebView2ProcessFailed;
        }
        catch (Exception ex) when (ex is ObjectDisposedException || ex is InvalidOperationException || ex is System.Runtime.InteropServices.COMException) { }
        mainWindowGrid.Children.Remove(previous);
        try { previous.Dispose(); } catch (Exception ex) when (ex is ObjectDisposedException || ex is InvalidOperationException || ex is System.Runtime.InteropServices.COMException) { }
    }

    private async void RetryBrowser_Click(object sender, RoutedEventArgs e) => await RetryBrowserAsync();

    protected override void OnClosed(EventArgs e)
    {
        _isClosing = true;
        _lifetime.Cancel();
        CleanupBrowser();
        _mainWindow.UnregisterWidget(this);
        _mainWindow.NotifyWidgetsChanged();
        base.OnClosed(e);
    }

    public void ApplyGroupVisibility(bool groupHidden)
    {
        if (_isClosing) return;
        if (groupHidden || IsHidden) Hide();
        else if (!IsVisible) { ShowActivated = false; Show(); }
    }

    private void HideWindow_Click(object sender, RoutedEventArgs e)
    {
        IsHidden = true;
        Persist();
        ApplyGroupVisibility(false);
        _mainWindow.NotifyWidgetsChanged();
    }

    public void RestoreDisplayState()
    {
        SetDisplayMode(App.Settings.GeneralSettings.AutoHideBorders || BordersHidden == true
            ? WindowDisplayMode.Overlay : WindowDisplayMode.Setup);
    }

    public async Task ApplyPerformanceSettingsAsync()
    {
        try
        {
            if (!_isClosing && webView?.CoreWebView2 != null)
                await BrowserPerformance.ApplyAsync(webView, ReduceMotion || App.Settings.GeneralSettings.ReduceMotion,
                    AnimationFps, App.Settings.GeneralSettings.ChatUpdateIntervalMs);
        }
        catch (Exception ex) when (ex is ObjectDisposedException || ex is InvalidOperationException || ex is System.Runtime.InteropServices.COMException)
        {
            if (!_isClosing) System.Diagnostics.Debug.WriteLine("Performance settings: " + ex.GetType().Name);
        }
    }

    private void UpdatePerformanceMenu()
    {
        menuAnimationRate.Header = AnimationFps == 0 ? "Animation frame limit — Unlimited" : $"Animation frame limit — {AnimationFps} FPS";
        foreach (var item in menuAnimationRate.Items.OfType<System.Windows.Controls.MenuItem>())
            item.IsChecked = item.Tag?.ToString() == AnimationFps.ToString();
    }

    private async void ReduceMotion_Click(object sender, RoutedEventArgs e)
    {
        ReduceMotion = menuReduceMotion.IsChecked;
        Persist();
        await ApplyPerformanceSettingsAsync();
    }

    private async void AnimationFps_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.MenuItem item && int.TryParse(item.Tag?.ToString(), out int fps))
        {
            AnimationFps = fps;
            UpdatePerformanceMenu();
            Persist();
            await ApplyPerformanceSettingsAsync();
        }
    }
}
