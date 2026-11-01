using System.Windows.Input;

namespace TransparentTwitchChatWPF.Utils;

public static class HotkeyRegistration
{
    public static List<string> Errors { get; } = new();
    public static List<string> FindDuplicates(IEnumerable<(string Name, Hotkey Value)> shortcuts)
    {
        var assigned = new Dictionary<(Key, ModifierKeys), string>();
        var errors = new List<string>();
        foreach (var shortcut in shortcuts)
        {
            if (shortcut.Value == null || shortcut.Value.Key == Key.None) continue;
            var key = (shortcut.Value.Key, shortcut.Value.Modifiers);
            if (assigned.TryGetValue(key, out var previous)) errors.Add($"{shortcut.Name} and {previous} use the same shortcut ({shortcut.Value}).");
            else assigned[key] = shortcut.Name;
        }
        return errors;
    }
}
