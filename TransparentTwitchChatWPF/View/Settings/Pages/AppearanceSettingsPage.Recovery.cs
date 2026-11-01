using System.Windows;
using TransparentTwitchChatWPF.Utils;

namespace TransparentTwitchChatWPF.View.Settings;

public partial class AppearanceSettingsPage
{
    private readonly CancellationTokenSource _lifetime = new();
    private Task _browserTask = Task.CompletedTask;
    private bool _closing;

    public Task RetryBrowserAsync()
    {
        if (_closing || !_browserTask.IsCompleted) return _browserTask;
        _browserTask = BrowserRecovery.RunAsync(SetupWebViewAsync, CleanupBrowser, message =>
        {
            if (_closing) return;
            RecoveryText.Text = message ?? "";
            RecoveryPanel.Visibility = message == null ? Visibility.Collapsed : Visibility.Visible;
        }, _lifetime.Token);
        return _browserTask;
    }
    private async void RetryBrowser_Click(object sender, RoutedEventArgs e) => await RetryBrowserAsync();
    private void CleanupBrowser()
    {
        var previous = webView;
        webView = null;
        if (previous == null) return;
        try
        {
            previous.CoreWebView2InitializationCompleted -= webView_CoreWebView2InitializationCompleted;
            previous.NavigationCompleted -= webView_NavigationCompleted;
            previous.WebMessageReceived -= webView_WebMessageReceived;
            if (previous.CoreWebView2 != null)
            {
                previous.CoreWebView2.NewWindowRequested -= webView_NewWindowRequested;
                previous.CoreWebView2.NavigationStarting -= webView_NavigationStarting;
                previous.CoreWebView2.ProcessFailed -= webView_CoreWebView2ProcessFailed;
            }
        }
        catch (Exception ex) when (ex is ObjectDisposedException || ex is InvalidOperationException || ex is System.Runtime.InteropServices.COMException) { }
        mainGrid.Children.Remove(previous);
        try { previous.Dispose(); } catch (Exception ex) when (ex is ObjectDisposedException || ex is InvalidOperationException || ex is System.Runtime.InteropServices.COMException) { }
    }
    public void DisposeBrowser()
    {
        _closing = true;
        _lifetime.Cancel();
        CleanupBrowser();
    }
}
