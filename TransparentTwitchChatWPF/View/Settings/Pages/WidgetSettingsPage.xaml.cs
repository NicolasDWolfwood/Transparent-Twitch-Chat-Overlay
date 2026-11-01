using System.Windows;
using System.Windows.Controls;

namespace TransparentTwitchChatWPF.View.Settings;

public partial class WidgetSettingsPage : UserControl
{
    public event Action WidgetCreationRequested;
    private MainWindow _main;
    private bool _busy;
    private sealed record WidgetRow(CustomWindow Window, string Name, string Source, string Status);
    private CustomWindow SelectedWidget => (WidgetList.SelectedItem as WidgetRow)?.Window;

    public WidgetSettingsPage()
    {
        InitializeComponent();
        Loaded += (_, _) => { Subscribe(); RefreshWidgets(); };
        Unloaded += (_, _) => { if (_main != null) _main.WidgetsChanged -= RefreshWidgets; };
    }

    public void Configure(MainWindow main)
    {
        if (_main != null) _main.WidgetsChanged -= RefreshWidgets;
        _main = main;
        if (IsLoaded) Subscribe();
        RefreshWidgets();
    }

    private void Subscribe()
    {
        if (_main == null) return;
        _main.WidgetsChanged -= RefreshWidgets;
        _main.WidgetsChanged += RefreshWidgets;
    }

    public void RefreshWidgets()
    {
        var selected = SelectedWidget;
        var widgets = _main?.GetManagedWidgets() ?? Array.Empty<CustomWindow>();
        var rows = widgets.Select(w => new WidgetRow(w, w.DisplayName, DescribeSource(w.Url),
            w.IsHidden || App.Settings.GeneralSettings.OverlaysHidden ? "Hidden" : "Shown")).ToArray();
        WidgetList.ItemsSource = rows;
        WidgetList.SelectedItem = rows.FirstOrDefault(r => r.Window == selected) ?? rows.FirstOrDefault();
        WidgetCountText.Text = $"Saved widgets ({rows.Length})";
        EmptyText.Visibility = rows.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        UpdateButtons();
    }

    // Provider URLs can contain private tokens; show only the hostname in the list.
    private static string DescribeSource(string url) => Uri.TryCreate(url, UriKind.Absolute, out var uri)
        ? uri.IsFile ? "Local file" : string.IsNullOrEmpty(uri.Host) ? uri.Scheme : uri.Host
        : "Custom source";

    private void UpdateButtons()
    {
        var widget = SelectedWidget;
        bool enabled = !_busy && widget != null && !widget.IsClosed;
        VisibilityButton.IsEnabled = EditButton.IsEnabled = ReloadButton.IsEnabled = LocateButton.IsEnabled = DeleteButton.IsEnabled = enabled;
        VisibilityButton.Content = widget != null && !widget.IsHidden && !App.Settings.GeneralSettings.OverlaysHidden ? "Hide" : "Show";
        NewWidgetButton.IsEnabled = !_busy;
    }

    private async Task RunActionAsync(Func<Task> action, string success)
    {
        if (_busy) return;
        _busy = true;
        WidgetStatusText.Text = "";
        UpdateButtons();
        try { await action(); WidgetStatusText.Text = success; }
        catch (Exception ex)
        {
            WidgetStatusText.Text = ex is ArgumentException or InvalidOperationException
                ? ex.Message : "Could not update the widget. Check the settings folder and try again.";
        }
        finally { _busy = false; RefreshWidgets(); }
    }

    private void NewWidgetButton_Click(object sender, RoutedEventArgs e)
    {
        WidgetCreationRequested?.Invoke();
        RefreshWidgets();
    }

    private void WidgetList_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateButtons();

    private async void VisibilityButton_Click(object sender, RoutedEventArgs e)
    {
        var widget = SelectedWidget;
        if (widget == null) return;
        bool hide = !widget.IsHidden && !App.Settings.GeneralSettings.OverlaysHidden;
        await RunActionAsync(() => { _main.SetWidgetHidden(widget, hide); return Task.CompletedTask; }, hide ? "Widget hidden." : "Widget shown.");
    }

    private async void EditButton_Click(object sender, RoutedEventArgs e)
    {
        var widget = SelectedWidget;
        if (widget == null || _busy) return;
        var dialog = new Input_Custom(widget.Url, widget.DisplayName, widget.customCSS, widget.AllowInteraction)
        { Owner = Window.GetWindow(this), Title = "Edit Widget" };
        dialog.ValidateInput = url =>
        {
            try { _main.ValidateWidgetUrl(widget, url); return null; }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException) { return ex.Message; }
        };
        if (dialog.ShowDialog() != true) return;
        await RunActionAsync(() => _main.EditManagedWidgetAsync(widget, dialog.Url, dialog.DisplayName, dialog.CustomCSS, dialog.AllowInteraction), "Widget updated.");
    }

    private async void ReloadButton_Click(object sender, RoutedEventArgs e)
    {
        var widget = SelectedWidget;
        if (widget != null) await RunActionAsync(widget.RetryBrowserAsync, "Reload requested. Check the widget for browser errors.");
    }

    private async void LocateButton_Click(object sender, RoutedEventArgs e)
    {
        var widget = SelectedWidget;
        if (widget != null) await RunActionAsync(() => { _main.BringWidgetIntoView(widget); return Task.CompletedTask; }, "Widget moved to the primary monitor. Close Settings to interact with it.");
    }

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        var widget = SelectedWidget;
        if (widget == null || _busy) return;
        if (MessageBox.Show(Window.GetWindow(this), "Remove the selected widget from your saved widgets?", "Delete Widget",
            MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        await RunActionAsync(() => { widget.DeleteFromManager(); return Task.CompletedTask; }, "Widget removed.");
    }
}
