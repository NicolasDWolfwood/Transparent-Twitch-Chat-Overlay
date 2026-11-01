using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Web.WebView2.Wpf;

namespace TransparentTwitchChatWPF.Utils;

public static class BrowserPerformance
{
    private sealed class Registration { public string ScriptId; }
    private static readonly ConditionalWeakTable<WebView2, Registration> Registrations = new();
    private static readonly string Script = LoadScript();
    private static string LoadScript()
    {
        using var stream = typeof(BrowserPerformance).Assembly.GetManifestResourceStream("TransparentTwitchChatWPF.browser-performance.js");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public static async Task ApplyAsync(WebView2 view, bool reducedMotion, int maximumFps, int updateInterval)
    {
        var core = view.CoreWebView2;
        if (core == null) return;
        string config = JsonSerializer.Serialize(new
        {
            reducedMotion,
            maximumFps = new[] { 0, 15, 30, 60 }.Contains(maximumFps) ? maximumFps : 0,
            updateIntervalMs = Math.Clamp(updateInterval, 100, 1000)
        });
        string script = Script + $"\nwindow.ttcoPerformance.configure({config});";
        var registration = Registrations.GetOrCreateValue(view);
        if (registration.ScriptId != null) core.RemoveScriptToExecuteOnDocumentCreated(registration.ScriptId);
        registration.ScriptId = await core.AddScriptToExecuteOnDocumentCreatedAsync(script);
        await core.ExecuteScriptAsync(script);
    }
}
