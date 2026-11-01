using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using TransparentTwitchChatWPF.Utils;

namespace TransparentTwitchChatWPF;

public partial class MainWindow
{
    private readonly CancellationTokenSource _lifetime = new();
    private Task _browserRecoveryTask = Task.CompletedTask;
    private bool _isClosing;
    private bool _widgetsRestored;
    public bool IsClosing => _isClosing;
    public string NativeChatConnectionState { get; private set; } = string.Empty;
    public const string CaptureTitle = "Twitch Chat Overlay — Main";

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
        hasWebView2Runtime = webView?.CoreWebView2 != null;
        if (message != null)
        {
            var handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            if (handle != IntPtr.Zero) WindowHelper.SetWindowInteractable(handle);
        }
    }

    private void CleanupBrowser()
    {
        hasWebView2Runtime = false;
        jsCallbackFunctions?.Dispose();
        jsCallbackFunctions = null;
        var previous = webView;
        webView = null;
        if (previous == null) return;
        try
        {
            previous.NavigationCompleted -= webView_NavigationCompleted;
            previous.WebMessageReceived -= webView_WebMessageReceived;
            previous.CoreWebView2InitializationCompleted -= webView_CoreWebView2InitializationCompleted;
            if (previous.CoreWebView2 != null) previous.CoreWebView2.ProcessFailed -= webView_CoreWebView2ProcessFailed;
        }
        catch (Exception ex) when (ex is ObjectDisposedException || ex is InvalidOperationException || ex is System.Runtime.InteropServices.COMException) { }
        mainWindowGrid.Children.Remove(previous);
        try { previous.Dispose(); } catch (Exception ex) when (ex is ObjectDisposedException || ex is InvalidOperationException || ex is System.Runtime.InteropServices.COMException) { }
    }

    private async void RetryBrowser_Click(object sender, RoutedEventArgs e) => await RetryBrowserAsync();

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_isClosing)
        {
            _isClosing = true;
            App.IsShuttingDown = true;
            _lifetime.Cancel();
            _timerCheckForegroundFocus?.Stop();
            _timerCheckWebView2Install?.Stop();
            _twitchService.ChannelPointsRewardRedeemed -= OnChannelPointsRewardRedeemed;
        }
        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        CleanupBrowser();
        foreach (var widget in windows.OfType<CustomWindow>().ToArray()) widget.Close();
        base.OnClosed(e);
    }

    public void UnregisterWidget(CustomWindow widget) => windows.Remove(widget);

    public void ApplyOverlayVisibility()
    {
        if (_isClosing) return;
        var settings = App.Settings.GeneralSettings;
        if (settings.OverlaysHidden || settings.WidgetsOnly) Hide();
        else if (!IsVisible) { ShowActivated = false; Show(); }
        foreach (var widget in windows.OfType<CustomWindow>().ToArray())
            widget.ApplyGroupVisibility(settings.OverlaysHidden);
    }

    public void SetOverlaysHidden(bool hidden)
    {
        App.Settings.GeneralSettings.OverlaysHidden = hidden;
        if (!hidden)
            foreach (var widget in windows.OfType<CustomWindow>().ToArray()) widget.IsHidden = false;
        ApplyOverlayVisibility();
        foreach (var widget in windows.OfType<CustomWindow>().ToArray()) widget.Persist();
        App.Settings.Persist();
        NotifyWidgetsChanged();
    }

    private void ShowOverlays_Click(object sender, RoutedEventArgs e) => SetOverlaysHidden(false);
    private void HideOverlays_Click(object sender, RoutedEventArgs e) => SetOverlaysHidden(true);
    private void ShowMainChat_Click(object sender, RoutedEventArgs e)
    {
        App.Settings.GeneralSettings.WidgetsOnly = false;
        SetOverlaysHidden(false);
    }

    private void RestoreDisplayState()
    {
        if (App.Settings.GeneralSettings.AutoHideBorders || App.Settings.GeneralSettings.MainBordersHidden) hideBorders();
        else drawBorders();
    }

    private void ShowOperationStatus(string message)
    {
        if (_isClosing) return;
        OperationStatusText.Text = message ?? "";
        OperationStatusPanel.Visibility = string.IsNullOrEmpty(message) ? Visibility.Collapsed : Visibility.Visible;
    }
}
