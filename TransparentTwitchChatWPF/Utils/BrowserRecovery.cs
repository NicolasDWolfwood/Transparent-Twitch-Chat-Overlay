using System.IO;

namespace TransparentTwitchChatWPF.Utils;

public static class BrowserRecovery
{
    public static async Task<bool> RunAsync(Func<Task> initialize, Action cleanup,
        Action<string> report, CancellationToken cancellationToken, int attempts = 3)
    {
        for (int attempt = 1; attempt <= attempts; attempt++)
        {
            if (cancellationToken.IsCancellationRequested) return false;
            try
            {
                cleanup();
                await initialize();
                cancellationToken.ThrowIfCancellationRequested();
                report(null);
                return true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                cleanup();
                return false;
            }
            catch (Exception ex)
            {
                cleanup();
                // Details go to local diagnostics, never include a widget URL/token in the overlay.
                System.Diagnostics.Debug.WriteLine($"Browser attempt {attempt}: {ex.GetType().Name}: {ex.Message}");
                report(attempt == attempts
                    ? "Chat could not load. Check the WebView2 runtime, then select Retry."
                    : $"Restoring chat ({attempt}/{attempts})…");
                if (attempt < attempts)
                {
                    try { await Task.Delay(TimeSpan.FromSeconds(attempt), cancellationToken); }
                    catch (OperationCanceledException) { return false; }
                }
            }
        }
        return false;
    }
}
