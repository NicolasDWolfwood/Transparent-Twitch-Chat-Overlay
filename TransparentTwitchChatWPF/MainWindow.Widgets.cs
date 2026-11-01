using System.Windows;

namespace TransparentTwitchChatWPF;

public partial class MainWindow
{
    internal string GetWidgetTrackingId(string url)
    {
        var ids = App.Settings.GeneralSettings.WidgetIds ??= new();
        if (ids.TryGetValue(url, out var existing)) return existing;
        string id = Utils.Hasher.Create64BitHash(url);
        // The original URL may have been edited while retaining this identity.
        if (ids.ContainsValue(id)) id = Guid.NewGuid().ToString("N");
        ids[url] = id;
        return id;
    }

    public event Action WidgetsChanged;
    public void NotifyWidgetsChanged() => WidgetsChanged?.Invoke();
    public IReadOnlyList<CustomWindow> GetManagedWidgets() =>
        windows.OfType<CustomWindow>().Where(w => !w.IsClosed).ToArray();

    public void SetWidgetHidden(CustomWindow widget, bool hidden)
    {
        if (widget.IsClosed) return;
        widget.IsHidden = hidden;
        // Exit global hide without clearing other individually hidden widgets.
        if (!hidden) App.Settings.GeneralSettings.OverlaysHidden = false;
        widget.Persist();
        App.Settings.Persist();
        ApplyOverlayVisibility();
        NotifyWidgetsChanged();
    }

    public void BringWidgetIntoView(CustomWindow widget)
    {
        if (widget.IsClosed) return;
        SetWidgetHidden(widget, false);
        widget.WindowState = WindowState.Normal;
        var area = SystemParameters.WorkArea;
        widget.Width = Math.Max(widget.MinWidth, Math.Min(widget.Width, area.Width - 32));
        widget.Height = Math.Max(widget.MinHeight, Math.Min(widget.Height, area.Height - 32));
        widget.Left = area.Left + (area.Width - widget.Width) / 2;
        widget.Top = area.Top + (area.Height - widget.Height) / 2;
        widget.drawBorders();
        widget.Persist();
        NotifyWidgetsChanged();
    }

    public void ValidateWidgetUrl(CustomWindow widget, string url)
    {
        if (widget.IsClosed) throw new InvalidOperationException("This widget has been closed. Select another widget.");
        url = url?.Trim() ?? "";
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            !(uri.Scheme is "http" or "https" or "file" || url == "about:blank"))
            throw new ArgumentException("Enter a complete http://, https:// or file:// URL.");
        if (widget.Url != url && App.Settings.GeneralSettings.CustomWindows.Contains(url))
            throw new ArgumentException("Another widget already uses that URL. Its settings have not been changed.");
    }

    public async Task EditManagedWidgetAsync(CustomWindow widget, string url, string name, string css, bool interaction)
    {
        ValidateWidgetUrl(widget, url);
        url = url.Trim();
        name = name?.Trim() ?? "";
        var settings = App.Settings.GeneralSettings;
        string oldUrl = widget.Url;

        settings.WidgetIds ??= new();
        settings.WidgetIds.Remove(oldUrl);
        settings.WidgetIds[url] = widget.HashCode;
        int index = settings.CustomWindows.IndexOf(oldUrl);
        if (index >= 0) settings.CustomWindows[index] = url;
        else settings.CustomWindows.Add(url);
        var update = widget.ApplyManagedEditAsync(url, name, css ?? "", interaction);
        App.Settings.Persist();
        await update;
        NotifyWidgetsChanged();
    }
}
