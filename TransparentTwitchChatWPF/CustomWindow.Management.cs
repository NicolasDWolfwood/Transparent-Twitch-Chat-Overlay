namespace TransparentTwitchChatWPF;

public partial class CustomWindow
{
    public async Task ApplyManagedEditAsync(string url, string name, string css, bool interaction)
    {
        Url = url;
        DisplayName = string.IsNullOrEmpty(name) ? HashCode : name;
        customCSS = css;
        AllowInteraction = interaction;
        menuItemCheckboxAllowInteraction.IsChecked = interaction;
        UpdateTitle();
        Persist();
        await RefreshState();
        // A fresh document removes old injected CSS, including for CSS-only edits.
        if (!_isClosing && webView?.CoreWebView2 != null) NavigateToUrl(Url);
    }

    public void DeleteFromManager()
    {
        if (_isClosing) return;
        _mainWindow.RemoveCustomWindow(Url);
        _isClosing = true; // The manager owns confirmation, rather than the overlay.
        Close();
    }
}
