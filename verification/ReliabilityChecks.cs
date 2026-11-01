using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using TransparentTwitchChatWPF;
using TransparentTwitchChatWPF.Utils;
using TransparentTwitchChatWPF.View.Settings;

internal static partial class Program
{
    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr handle, int id, uint modifiers, uint key);
    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr handle, int id);
    private static T Await<T>(Task<T> task) { PumpUntil(() => task.IsCompleted); return task.GetAwaiter().GetResult(); }
    private static void Await(Task task) { PumpUntil(() => task.IsCompleted); task.GetAwaiter().GetResult(); }
    private static void PumpFor(int milliseconds)
    {
        var end = DateTime.UtcNow.AddMilliseconds(milliseconds);
        PumpUntil(() => DateTime.UtcNow >= end);
    }
    private static string Eval(WebView2 view, string script) => Await(view.ExecuteScriptAsync(script));
    private static int Number(WebView2 view, string script) => JsonSerializer.Deserialize<int>(Eval(view, script));

    private static void CheckReliability(MainWindow main, CustomWindow widget, GeneralSettingsPage page)
    {
        widget.WindowState = WindowState.Normal;
        string captureTitle = widget.Title;
        widget.hideBorders(); widget.SetInteractable(true); widget.SetInteractable(false); widget.drawBorders();
        Check(widget.Title == captureTitle && main.Title == MainWindow.CaptureTitle, "capture titles survive interaction and border changes");
        Check(widget.ShowInTaskbar, "widgets remain discoverable when borders are hidden");

        var settings = App.Settings.GeneralSettings;
        settings.AutoHideBorders = true;
        widget.RestoreDisplayState(); Invoke(main, "RestoreDisplayState");
        Check(widget.BorderThickness.Left == 0 && main.BorderThickness.Left == 0, "startup auto-hide is applied after browser initialization");
        settings.AutoHideBorders = false;
        widget.hideBorders();
        widget.Persist();
        var restored = new CustomWindow(main, widget.Url, "ignored", "", true) { ShowActivated = false, Opacity = 0, Left = -10000 };
        restored.Show();
        Await(GetField<Task>(restored, "_browserRecoveryTask"));
        GetField<System.Collections.IList>(main, "windows").Add(restored);
        Check(restored.BordersHidden == true && restored.BorderThickness.Left == 0, "saved widget border state is restored on a new window");
        Check(restored.Title == captureTitle, "widget capture identity survives recreation");
        restored.IsHidden = true; restored.AnimationFps = 30; restored.ReduceMotion = true; restored.Persist();
        var savedWidget = new CustomWindow(main, widget.Url, "ignored", "", true);
        Check(savedWidget.IsHidden && savedWidget.AnimationFps == 30 && savedWidget.ReduceMotion,
            "widget visibility and performance choices survive recreation");
        App.IsShuttingDown = true; savedWidget.Close(); App.IsShuttingDown = false;
        restored.IsHidden = false; restored.ReduceMotion = false; restored.AnimationFps = 0;

        settings.WidgetsOnly = true; main.ApplyOverlayVisibility();
        Check(!main.IsVisible && widget.IsVisible && restored.IsVisible, "widgets-only mode hides the main chat without closing widgets");
        main.SetOverlaysHidden(true);
        Check(!main.IsVisible && !widget.IsVisible && !restored.IsVisible, "hide-all hides every overlay without destroying browsers");
        main.SetOverlaysHidden(false);
        Check(!main.IsVisible && widget.IsVisible, "show-all respects widgets-only mode");
        settings.WidgetsOnly = false; main.ApplyOverlayVisibility();
        Check(main.IsVisible, "main chat can be restored from widgets-only mode");

        var duplicate = new Hotkey(Key.F8, ModifierKeys.None);
        Check(HotkeyRegistration.FindDuplicates(new[] { ("show", duplicate), ("hide", duplicate) }).Count == 1,
            "duplicate shortcut assignments are rejected");
        Check(HotkeyRegistration.FindDuplicates(new[] { ("show", (Hotkey)null), ("hide", duplicate) }).Count == 0,
            "unset shortcuts and modifier-free shortcuts remain valid");
        settings.ShowOverlaysHotkey = new Hotkey(Key.F11, ModifierKeys.None);
        settings.HideOverlaysHotkey = null;
        App.Settings.Persist();
        var disk = new AppSettings();
        disk.Tracker.Configure(disk).Properties<AppSettings>(s => new { s.GeneralSettings, s.jChatSettings }).Id(s => s.GetType().Name, null, false);
        disk.Tracker.Track(disk);
        Check(disk.GeneralSettings.ShowOverlaysHotkey.Key == Key.F11 && disk.GeneralSettings.ShowOverlaysHotkey.Modifiers == ModifierKeys.None,
            "modifier-free hotkey survives a disk reload");
        Check(disk.GeneralSettings.HideOverlaysHotkey == null, "cleared hotkey survives a disk reload");
        Invoke(main, "SetupOrReplaceHotkeys");
        if (RegisterHotKey(IntPtr.Zero, 19119, 7, 0x7B))
        {
            try
            {
                Invoke(main, "RegisterShortcut", "Verification conflict", new Hotkey(Key.F12, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift),
                    new EventHandler<NHotkey.HotkeyEventArgs>((_, _) => { }));
                Check(HotkeyRegistration.Errors.Any(error => error.StartsWith("Verification conflict")), "OS hotkey conflicts produce actionable feedback instead of being swallowed");
            }
            finally { UnregisterHotKey(IntPtr.Zero, 19119); }
        }

        var environmentField = typeof(WebView2EnvironmentManager).GetField("_environmentTask", BindingFlags.NonPublic | BindingFlags.Static);
        var existingEnvironment = environmentField.GetValue(null);
        var failedEnvironment = Task.FromException<CoreWebView2Environment>(new InvalidOperationException("test initialization failure"));
        environmentField.SetValue(null, failedEnvironment);
        try
        {
            var freshEnvironment = WebView2EnvironmentManager.GetEnvironmentAsync();
            Check(!ReferenceEquals(failedEnvironment, freshEnvironment), "failed shared browser environment is not cached forever");
            Await(freshEnvironment);
        }
        finally { environmentField.SetValue(null, existingEnvironment); }

        int attempts = 0, cleanups = 0;
        bool recovered = Await(BrowserRecovery.RunAsync(() => ++attempts < 2 ? Task.FromException(new InvalidOperationException("test")) : Task.CompletedTask,
            () => cleanups++, _ => { }, CancellationToken.None));
        Check(recovered && attempts == 2 && cleanups >= 2, "browser initialization retries a transient failure");
        attempts = 0;
        bool failed = Await(BrowserRecovery.RunAsync(() => { attempts++; return Task.FromException(new InvalidOperationException("test")); },
            () => { }, _ => { }, CancellationToken.None, attempts: 2));
        Check(!failed && attempts == 2, "browser retry count is bounded");
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        Check(!Await(BrowserRecovery.RunAsync(() => throw new Exception("must not execute"), () => { }, _ => { }, cancellation.Token)),
            "shutdown cancellation prevents browser resurrection");

        int widgetCount = GetField<System.Collections.IList>(main, "windows").Count;
        var retryOne = main.RetryBrowserAsync(); var retryTwo = main.RetryBrowserAsync();
        Check(ReferenceEquals(retryOne, retryTwo), "overlapping main browser recovery requests share one operation");
        Await(retryOne);
        Check(GetField<System.Collections.IList>(main, "windows").Count == widgetCount, "main browser recovery does not duplicate widgets");
        Await(widget.RetryBrowserAsync());
        Check(widget.Title == captureTitle && widget.IsVisible, "widget browser recovery preserves its identity and window");

        var oldMainBrowser = GetField<WebView2>(main, "webView");
        _ = oldMainBrowser.CoreWebView2.CallDevToolsProtocolMethodAsync("Page.crash", "{}")
            .ContinueWith(task => { _ = task.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
        PumpUntil(() => GetField<WebView2>(main, "webView") is { CoreWebView2: not null } current &&
            !ReferenceEquals(current, oldMainBrowser) && GetField<Task>(main, "_browserRecoveryTask").IsCompleted);
        Check(GetField<System.Collections.IList>(main, "windows").Count == widgetCount, "real renderer crash recovers main chat without duplicate widgets");
        var oldWidgetBrowser = GetField<WebView2>(widget, "webView");
        _ = oldWidgetBrowser.CoreWebView2.CallDevToolsProtocolMethodAsync("Page.crash", "{}")
            .ContinueWith(task => { _ = task.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
        PumpUntil(() => GetField<WebView2>(widget, "webView") is { CoreWebView2: not null } current &&
            !ReferenceEquals(current, oldWidgetBrowser) && GetField<Task>(widget, "_browserRecoveryTask").IsCompleted);
        Check(widget.IsVisible && widget.Title == captureTitle, "real widget renderer crash recovers without closing the window");

        string healthPage = Path.Combine(AppContext.BaseDirectory, "browser", "overlays", "native-chat", "verification-status.html");
        File.WriteAllText(healthPage, "<html><body>Local health test<script>setTimeout(()=>chrome.webview.postMessage({type:'NativeChatConnection',state:'reconnecting'}),100)</script></body></html>");
        settings.ChatType = (int)ChatTypes.NativeChat;
        var healthView = GetField<WebView2>(main, "webView");
        healthView.CoreWebView2.Navigate("https://nativechat.overlay/verification-status.html");
        PumpUntil(() => ((System.Windows.Controls.TextBlock)main.FindName("ConnectionStatusText")).Text.Contains("retrying"));
        Check(((FrameworkElement)main.FindName("ConnectionStatusPanel")).Visibility == Visibility.Visible, "native connection loss produces a visible overlay status");
        Eval(healthView, "chrome.webview.postMessage({type:'NativeChatConnection',state:'connected'})");
        PumpUntil(() => ((System.Windows.Controls.TextBlock)main.FindName("ConnectionStatusText")).Text == "Chat connected");
        Check(main.Title == MainWindow.CaptureTitle, "connection status changes preserve capture identity");
        settings.ChatType = (int)ChatTypes.CustomURL;
        Invoke(main, "SetupChatProvider");

        var appearance = new AppearanceSettingsPage();
        var appearanceHost = new Window { Content = appearance, ShowActivated = false, ShowInTaskbar = false, Opacity = 0, Left = -10000 };
        appearanceHost.Show();
        Await(appearance.RetryBrowserAsync());
        Check(GetField<WebView2>(appearance, "webView")?.CoreWebView2 != null, "appearance browser initializes through the recovery path");
        appearance.DisposeBrowser(); appearanceHost.Close();
        Check(GetField<WebView2>(appearance, "webView") == null, "closing settings disposes its appearance browser");

        using (var player = new AudioPlaybackService())
        {
            Check(!player.TryPlay(Path.Combine(AppContext.BaseDirectory, "missing-audio.wav"), 0, int.MaxValue, "removed headset"),
                "invalid audio cannot crash the app");
            string silence = Path.Combine(AppContext.BaseDirectory, "verification-silence.wav");
            using (var writer = new BinaryWriter(File.Create(silence)))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(1636);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16); writer.Write((short)1); writer.Write((short)1);
                writer.Write(8000); writer.Write(16000); writer.Write((short)2); writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(1600); writer.Write(new byte[1600]);
            }
            bool available = player.TryPlay(silence, 0, int.MaxValue, "removed headset");
            Console.WriteLine($"AUDIO: fallback device {(available ? "played silent test" : "unavailable; handled safely")}");
            Check(available || !string.IsNullOrEmpty(player.LastError), "missing headset falls back or reports a recoverable audio error");
            player.Dispose();
            bool retried = player.TryPlay(silence, 0, -1, "Default");
            Check(retried || !string.IsNullOrEmpty(player.LastError), "audio can be retried after disposal/device failure");
        }

        var firstView = GetField<WebView2>(widget, "webView");
        var secondView = GetField<WebView2>(restored, "webView");
        widget.AnimationFps = restored.AnimationFps = 0;
        Await(widget.ApplyPerformanceSettingsAsync()); Await(restored.ApplyPerformanceSettingsAsync());
        const string benchmark = "window.benchFrames=0;window.benchTicks=0;window.benchRunning=true;setInterval(()=>window.benchTicks++,50);function benchFrame(){if(!window.benchRunning)return;window.benchFrames++;document.body.textContent='frame '+window.benchFrames;requestAnimationFrame(benchFrame)}requestAnimationFrame(benchFrame);";
        Eval(firstView, benchmark); Eval(secondView, benchmark); PumpFor(1200);
        int baselineA = Number(firstView, "benchFrames"), baselineB = Number(secondView, "benchFrames");
        int timerBaseline = Number(firstView, "benchTicks");
        widget.AnimationFps = restored.AnimationFps = 15;
        Await(widget.ApplyPerformanceSettingsAsync()); Await(restored.ApplyPerformanceSettingsAsync());
        Eval(firstView, "benchFrames=0;benchTicks=0"); Eval(secondView, "benchFrames=0;benchTicks=0"); PumpFor(1200);
        int limitedA = Number(firstView, "benchFrames"), limitedB = Number(secondView, "benchFrames"), timerLimited = Number(firstView, "benchTicks");
        Console.WriteLine($"PERFORMANCE (two widgets, 1.2s): unlimited={baselineA}/{baselineB}, 15fps={limitedA}/{limitedB}; timers={timerBaseline}/{timerLimited}");
        Check(baselineA > 20 && baselineB > 20 && limitedA > 0 && limitedB > 0 && limitedA < baselineA * .7 && limitedB < baselineB * .7,
            "per-widget animation caps reduce measured render callbacks in two live WebViews");
        Check(timerLimited >= Math.Max(8, timerBaseline / 2), "animation caps do not throttle independent chat/network timers");
        widget.ReduceMotion = true; Await(widget.ApplyPerformanceSettingsAsync());
        Check(JsonSerializer.Deserialize<string>(Eval(firstView, "document.getElementById('ttco-reduced-motion').textContent")).Contains("animation:none"),
            "reduced motion applies to an already-running widget");
        int beforeHidden = Number(firstView, "benchTicks"); widget.Hide(); PumpFor(600);
        Check(Number(firstView, "benchTicks") > beforeHidden + 3, "background browser timers continue while the overlay is hidden");
        widget.ApplyGroupVisibility(false);
        Eval(firstView, "benchRunning=false"); Eval(secondView, "benchRunning=false");

        // Closing an already disposed browser must also be safe and unregister its window.
        App.IsShuttingDown = true;
        secondView.Dispose(); restored.Close();
        App.IsShuttingDown = false;
        Check(!GetField<System.Collections.IList>(main, "windows").Contains(restored), "closed widgets leave the live collection");
        settings.AlwaysOnTop = true; Invoke(main, "CheckForegroundWindow");
        settings.AlwaysOnTop = false; main.ApplyAlwaysOnTopSetting();
        Check(true, "group commands tolerate a previously closed widget");
        SaveSettingsPreview(page);
    }

    private static void SaveSettingsPreview(GeneralSettingsPage page)
    {
        page.SetupValues();
        page.Measure(new Size(560, 720)); page.Arrange(new Rect(0, 0, 560, 720)); page.UpdateLayout();
        var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(560, 720, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
        bitmap.Render(page);
        var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
        encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
        string destination = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "settings-preview.png"));
        using var output = File.Create(destination); encoder.Save(output);
    }
}
