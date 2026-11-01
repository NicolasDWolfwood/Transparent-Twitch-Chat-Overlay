using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using Jot;
using Jot.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Web.WebView2.Wpf;
using TransparentTwitchChatWPF;
using TransparentTwitchChatWPF.Helpers;
using TransparentTwitchChatWPF.Twitch;
using TransparentTwitchChatWPF.Utils;
using TransparentTwitchChatWPF.View.Settings;
using TwitchLib.EventSub.Websockets.Extensions;
using Application = System.Windows.Application;

internal static partial class Program
{
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int index);
    private static int checks;
    private static readonly BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    [STAThread]
    private static int Main()
    {
        Velopack.VelopackApp.Build().Run();
        var app = new Application();
        app.Resources.MergedDictionaries.Add(new ModernWpf.ThemeResources());
        app.Resources.MergedDictionaries.Add(new ModernWpf.Controls.XamlControlsResources());
        app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        app.Dispatcher.BeginInvoke(new Action(() => app.Shutdown(RunChecks(app))));
        return app.Run();
    }

    private static int RunChecks(Application app)
    {
        try
        {
            Check(AppInfo.IsPortable, "test process uses portable storage");
            App.Settings.Init();
            App.Settings.GeneralSettings = new GeneralSettings
            {
                AlwaysOnTop = false, CheckForUpdates = false,
                ChatType = (int)ChatTypes.CustomURL, CustomURL = "about:blank"
            };
            App.Settings.Persist();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddTwitchLibEventSubWebsockets();
            services.AddSingleton<TwitchService>();
            services.AddSingleton<WebViewConfigurator>();
            services.AddSingleton<NativeChatFileManager>();
            services.AddSingleton<MainWindow>();
            services.AddSingleton<ITwitchAuthService, TwitchAuthService>();
            services.AddTransient<ConnectionSettingsPage>();
            services.AddTransient<ChatSettingsPage>();
            services.AddTransient<AppearanceSettingsPage>();
            services.AddTransient<GeneralSettingsPage>();
            services.AddTransient<WidgetSettingsPage>();
            services.AddTransient<AboutSettingsPage>();
            services.AddTransient<SettingsWindow>();
            using var provider = services.BuildServiceProvider();
            var main = provider.GetRequiredService<MainWindow>();
            main.ShowActivated = false;
            main.ShowInTaskbar = false;
            main.Opacity = 0;
            main.Show();
            main.Left = -10000;
            PumpUntil(() => GetField<WebView2>(main, "webView")?.CoreWebView2 != null);
            PumpUntil(() => GetField<object>(main, "jsCallbackFunctions") != null);
            PumpUntil(() => GetField<Task>(main, "_browserRecoveryTask").IsCompleted);

            var widget = new CustomWindow(main, "about:blank", "Verification widget", "", true);
            widget.ShowActivated = false;
            widget.ShowInTaskbar = false;
            widget.Opacity = 0;
            widget.Show();
            widget.Left = -10000;
            PumpUntil(() => GetField<WebView2>(widget, "webView")?.CoreWebView2 != null);
            PumpUntil(() => GetField<Task>(widget, "_browserRecoveryTask").IsCompleted);
            GetField<System.Collections.IList>(main, "windows").Add(widget);

            CheckTopmost(main, false, "main starts with saved off setting");
            CheckTopmost(widget, false, "new widget starts with saved off setting");
            Check(!GetField<DispatcherTimer>(main, "_timerCheckForegroundFocus").IsEnabled,
                "startup bring-to-top timer stays stopped when disabled");

            var page = new GeneralSettingsPage();
            page.SetupValues();
            var checkbox = (CheckBox)page.FindName("cbAlwaysOnTop");
            Check(checkbox != null && checkbox.IsChecked == false, "checkbox loads saved off setting");
            checkbox.IsChecked = true;
            Check(!App.Settings.GeneralSettings.AlwaysOnTop, "checkbox edit does not apply before Save");
            page.SaveValues();
            main.ApplyAlwaysOnTopSetting();
            CheckTopmost(main, true, "saving on updates main native window");
            CheckTopmost(widget, true, "saving on updates existing widget native window");
            Invoke(main, "StartCheckForegroundWindowTimer");
            Check(GetField<DispatcherTimer>(main, "_timerCheckForegroundFocus").IsEnabled,
                "bring-to-top timer runs when enabled");

            checkbox.IsChecked = false;
            page.SaveValues();
            main.ApplyAlwaysOnTopSetting();
            CheckTopmost(main, false, "saving off clears main native topmost flag");
            CheckTopmost(widget, false, "saving off clears existing widget native topmost flag");
            Check(!GetField<DispatcherTimer>(main, "_timerCheckForegroundFocus").IsEnabled,
                "saving off stops an already-running timer");

            Invoke(main, "Window_Deactivated", main, EventArgs.Empty);
            CheckTopmost(main, false, "losing foreground does not restore topmost");
            Invoke(main, "Window_PreviewLostKeyboardFocus", main, (KeyboardFocusChangedEventArgs)null);
            CheckTopmost(main, false, "losing keyboard focus does not restore topmost");
            main.hideBorders();
            CheckTopmost(main, false, "hiding borders keeps topmost off");
            main.drawBorders();
            CheckTopmost(main, false, "showing borders keeps topmost off");
            main.SetInteractable(false);
            main.SetInteractable(true);
            CheckTopmost(main, false, "toggling click-through keeps topmost off");
            Invoke(main, "CheckForegroundWindow");
            Invoke(main, "StartCheckForegroundWindowTimer");
            CheckTopmost(main, false, "bring-to-top command respects off setting");
            CheckTopmost(widget, false, "foreground check leaves widgets behind other apps");
            Check(!GetField<DispatcherTimer>(main, "_timerCheckForegroundFocus").IsEnabled,
                "bring-to-top command cannot restart timer while disabled");

            widget.WindowState = WindowState.Minimized;
            widget.SetTopMost(false);
            Check(widget.WindowState == WindowState.Minimized, "turning topmost off does not restore minimized widgets");
            main.SetTopMost(false);
            CheckTopmost(main, false, "SetTopMost(false) honors its argument");

            App.Settings.Persist();
            var restored = new AppSettings();
            restored.Tracker.Configure(restored)
                .Properties<AppSettings>(s => new { s.GeneralSettings, s.jChatSettings })
                .Id(s => s.GetType().Name, null, false);
            restored.Tracker.Track(restored);
            Check(!restored.GeneralSettings.AlwaysOnTop, "off setting survives a disk reload");
            Check(new GeneralSettings().AlwaysOnTop, "older settings without the property retain original default");

            CheckReliability(main, widget, page);
            CheckSettingsDialog(provider);
            CheckWidgetManager(main, provider);

            App.IsShuttingDown = true;
            widget.Close();
            main.Close();
            Console.WriteLine($"PASS: {checks} checks");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        checks++;
        Console.WriteLine("PASS: " + message);
    }

    private static void CheckTopmost(Window window, bool expected, string message)
    {
        bool native = (GetWindowLongPtr(new WindowInteropHelper(window).Handle, -20).ToInt64() & 8) != 0;
        Check(window.Topmost == expected && native == expected, message);
    }

    private static T GetField<T>(object obj, string name) =>
        (T)obj.GetType().GetField(name, PrivateInstance).GetValue(obj);

    private static void Invoke(object obj, string name, params object[] args) =>
        obj.GetType().GetMethod(name, PrivateInstance).Invoke(obj, args);

    private static void PumpUntil(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(25);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline) throw new TimeoutException("WebView2 did not initialize");
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
                new Action(() => frame.Continue = false));
            Dispatcher.PushFrame(frame);
            Thread.Sleep(20);
        }
    }
}
