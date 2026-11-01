using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Web.WebView2.Wpf;
using TransparentTwitchChatWPF;
using TransparentTwitchChatWPF.View.Settings;

internal static partial class Program
{
    private static void CheckWidgetManager(MainWindow main, IServiceProvider provider)
    {
        string folder = Path.Combine(AppContext.BaseDirectory, "widget-manager-fixtures", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string a = Path.Combine(folder, "first.html"), b = Path.Combine(folder, "edited.html");
        File.WriteAllText(a, "<html><head><style>body{color:rgb(20,20,20)}</style></head><body>First widget</body></html>");
        File.WriteAllText(b, "<html><head><style>body{color:rgb(20,20,20)}</style></head><body>Edited widget</body></html>");
        string firstUrl = new Uri(a).AbsoluteUri, editedUrl = new Uri(b).AbsoluteUri;
        string secondFile = Path.Combine(folder, "viewer.html");
        File.WriteAllText(secondFile, "<html><body>Viewer count</body></html>");
        string secondUrl = new Uri(secondFile).AbsoluteUri;
        var first = new CustomWindow(main, firstUrl, "Alerts", "body{color:rgb(1,2,3)}", true)
            { ShowActivated = false, Opacity = 0, Left = -10000, Width = 430, Height = 320 };
        var second = new CustomWindow(main, secondUrl, "Viewer count", "", false)
            { ShowActivated = false, Opacity = 0, Left = -10000 };
        var windows = GetField<System.Collections.IList>(main, "windows");
        windows.Add(first); windows.Add(second);
        App.Settings.GeneralSettings.CustomWindows.Add(firstUrl);
        App.Settings.GeneralSettings.CustomWindows.Add(secondUrl);
        first.Show(); second.Show();
        Await(GetField<Task>(first, "_browserRecoveryTask"));
        Await(GetField<Task>(second, "_browserRecoveryTask"));
        App.Settings.GeneralSettings.OverlaysHidden = false;
        first.IsHidden = false; second.IsHidden = false;
        main.ApplyOverlayVisibility();

        var dialog = provider.GetRequiredService<SettingsWindow>();
        dialog.ConfigureWidgetManager(main);
        dialog.ShowActivated = false; dialog.Opacity = 0; dialog.Left = -10000;
        dialog.WindowStartupLocation = WindowStartupLocation.Manual;
        dialog.Show();
        ((ListView)dialog.FindName("lvSettings")).SelectedIndex = 4;
        var page = (WidgetSettingsPage)((ContentControl)dialog.FindName("SettingsContentControl")).Content;
        dialog.UpdateLayout(); PumpFor(50);
        var list = (ListBox)page.FindName("WidgetList");
        CustomWindow RowWindow(object row) => (CustomWindow)row.GetType().GetProperty("Window").GetValue(row);
        void Select(CustomWindow w) => list.SelectedItem = list.Items.Cast<object>().First(r => RowWindow(r) == w);
        string Caption(string button) => ((Button)page.FindName(button)).Content.ToString();
        void Click(string button) => ((Button)page.FindName(button)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Select(first);
        Check(list.Items.Count == main.GetManagedWidgets().Count && Caption("VisibilityButton") == "Hide", "manager lists existing widgets with their visibility");
        Click("VisibilityButton");
        Check(first.IsHidden && !first.IsVisible && second.IsVisible && Caption("VisibilityButton") == "Show", "manager hides only the selected widget and preserves selection");
        Select(second); Click("VisibilityButton"); Click("VisibilityButton");
        Check(first.IsHidden && !first.IsVisible && second.IsVisible, "showing one widget preserves other individual hidden states");

        Select(first);
        int editorPass = 0;
        Exception editorFailure = null;
        EventManager.RegisterClassHandler(typeof(Input_Custom), FrameworkElement.LoadedEvent, new RoutedEventHandler((sender, _) =>
        {
            if (editorPass >= 2) return;
            var editor = (Input_Custom)sender;
            editor.Opacity = 0;
            editorPass++;
            int currentPass = editorPass;
            editor.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, new Action(() =>
            {
                try
                {
                    Check(editor.Owner == dialog && editor.Url == firstUrl && editor.CustomCSS == "body{color:rgb(1,2,3)}" && editor.AllowInteraction,
                        "Edit opens an owned dialog prefilled with the selected widget's URL, CSS and interaction setting");
                    ((TextBox)editor.FindName("txtName")).Text = currentPass == 1 ? "Discard this change" : "Renamed in manager";
                    if (currentPass == 1) editor.Close();
                    else Invoke(editor, "btnDialogOk_Click", editor.FindName("btnDialogOk"), new RoutedEventArgs());
                }
                catch (Exception ex) { editorFailure = ex; editor.Close(); }
            }));
        }));
        Click("EditButton");
        if (editorFailure != null) throw editorFailure;
        Check(first.DisplayName == "Alerts", "canceling Edit leaves the existing widget unchanged");
        Click("EditButton");
        if (editorFailure != null) throw editorFailure;
        PumpUntil(() => ((Button)page.FindName("EditButton")).IsEnabled);
        Check(first.DisplayName == "Renamed in manager" && first.IsHidden, "saving through the Edit button updates the live widget without showing it");

        string id = first.HashCode;
        var handle = new System.Windows.Interop.WindowInteropHelper(first).Handle;
        first.ZoomLevel = 1.4; first.OpacityLevel = 70; first.AnimationFps = 30; first.Persist();
        Await(main.EditManagedWidgetAsync(first, editedUrl, "VR alerts", "body{color:rgb(4,5,6)}", false));
        var browser = GetField<WebView2>(first, "webView");
        PumpUntil(() => Eval(browser, "document.body?.textContent") == "\"Edited widget\"" && Eval(browser, "getComputedStyle(document.body).color") == "\"rgb(4, 5, 6)\"");
        Check(first.HashCode == id && new System.Windows.Interop.WindowInteropHelper(first).Handle == handle &&
              first.Width == 430 && first.ZoomLevel == 1.4 && first.OpacityLevel == 70 && first.IsHidden,
              "URL edits preserve capture handle, size, zoom, opacity and hidden state");
        Check(App.Settings.GeneralSettings.CustomWindows.Contains(editedUrl) && !App.Settings.GeneralSettings.CustomWindows.Contains(firstUrl),
            "URL edits replace the saved widget entry rather than duplicating it");
        Await(main.EditManagedWidgetAsync(first, editedUrl, "VR alerts", "", true));
        PumpUntil(() => Eval(browser, "getComputedStyle(document.body).color") == "\"rgb(20, 20, 20)\"");
        Check(first.customCSS == "" && first.AllowInteraction, "removing custom CSS clears its old effect on the live page");
        bool rejected = false;
        try { Await(main.EditManagedWidgetAsync(first, secondUrl, "wrong", "", false)); }
        catch (ArgumentException) { rejected = true; }
        Check(rejected && first.Url == editedUrl && first.DisplayName == "VR alerts", "duplicate URL edits are rejected without changing the widget");
        rejected = false;
        try { Await(main.EditManagedWidgetAsync(first, "javascript:alert(1)", "wrong", "", false)); }
        catch (ArgumentException) { rejected = true; }
        Check(rejected && first.Url == editedUrl, "unsupported URL edits are rejected before changing saved state");

        var disk = new AppSettings();
        disk.Tracker.Configure(disk).Properties<AppSettings>(s => new { s.GeneralSettings, s.jChatSettings }).Id(s => s.GetType().Name, null, false);
        disk.Tracker.Track(disk);
        Check(disk.GeneralSettings.WidgetIds[editedUrl] == id && disk.GeneralSettings.CustomWindows.Contains(editedUrl),
            "edited widget identity and URL survive a disk reload");
        var restored = new CustomWindow(main, editedUrl, "ignored", "ignored", false) { Opacity = 0 };
        Check(restored.HashCode == id && restored.DisplayName == "VR alerts" && restored.Width == 430 && restored.IsHidden && restored.AnimationFps == 30,
            "reopening an edited widget restores its original placement, name and performance settings");
        App.IsShuttingDown = true; restored.Close(); App.IsShuttingDown = false;
        var reused = new CustomWindow(main, firstUrl, "New widget at old URL", "", false) { Opacity = 0 };
        Check(reused.HashCode != id && reused.DisplayName == "New widget at old URL", "reusing an edited widget's old URL does not steal its settings");
        App.IsShuttingDown = true; reused.Close(); App.IsShuttingDown = false;

        Select(first); Click("ReloadButton");
        PumpUntil(() => ((Button)page.FindName("ReloadButton")).IsEnabled);
        Check(first.IsHidden && !first.IsVisible && first.HashCode == id, "manager reload preserves visibility and identity");
        first.Left = -10000; first.Top = -10000; first.WindowState = WindowState.Minimized;
        main.SetOverlaysHidden(true);
        Select(first); Click("LocateButton");
        var area = SystemParameters.WorkArea;
        Check(first.IsVisible && !first.IsHidden && first.WindowState == WindowState.Normal &&
              first.Left >= area.Left && first.Top >= area.Top && first.Left + first.Width <= area.Right && first.Top + first.Height <= area.Bottom && first.BorderThickness.Left > 0,
              "Bring into view recovers an off-screen, minimized, globally hidden widget with controls");
        CheckTopmost(first, false, "manager recovery respects Always on top being disabled");
        var describe = typeof(WidgetSettingsPage).GetMethod("DescribeSource", BindingFlags.Static | BindingFlags.NonPublic);
        Check((string)describe.Invoke(null, new object[] { "https://overlay.example/private-token?secret=hidden" }) == "overlay.example",
            "widget list source labels do not disclose private overlay paths or tokens");

        // Render the actual settings page for visual QA without capturing the user's desktop.
        Select(first); dialog.UpdateLayout();
        var host = (ContentControl)dialog.FindName("SettingsContentControl");
        var size = new Size(host.ActualWidth, host.ActualHeight);
        host.Content = null;
        var preview = new Border { Child = page, Background = new SolidColorBrush(Color.FromRgb(34, 34, 34)) };
        preview.Measure(size); preview.Arrange(new Rect(size)); preview.UpdateLayout();
        var image = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96, 96, PixelFormats.Pbgra32);
        image.Render(preview);
        var png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(image));
        using (var stream = File.Create(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../widget-manager-preview.png")))) png.Save(stream);
        dialog.Close(); // Closing Settings must not undo immediate widget changes.
        App.Settings.RevertChanges();
        Check(App.Settings.GeneralSettings.CustomWindows.Contains(editedUrl) && !first.IsHidden, "closing Settings with Cancel retains immediate widget actions");
        int before = main.GetManagedWidgets().Count;
        second.DeleteFromManager();
        Check(second.IsClosed && !main.GetManagedWidgets().Contains(second) && main.GetManagedWidgets().Count == before - 1 &&
              !App.Settings.GeneralSettings.CustomWindows.Contains(secondUrl), "manager deletion closes and unregisters only the selected saved widget");
        var empty = new WidgetSettingsPage(); empty.RefreshWidgets();
        Check(((TextBlock)empty.FindName("EmptyText")).Visibility == Visibility.Visible && !((Button)empty.FindName("EditButton")).IsEnabled,
            "empty widget manager presents a clear state and disables unavailable actions");
        first.DeleteFromManager();
    }
}
