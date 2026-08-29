using SharpHook.Data;

namespace SidebarDiagnostics.App.Services.Shortcuts;

public sealed record ShortcutGesture(KeyCode Key, bool Ctrl, bool Alt, bool Shift, bool Meta)
{
    public static bool TryParse(string? value, out ShortcutGesture? gesture, out string? error)
    {
        gesture = null;
        error = null;
        if (string.IsNullOrWhiteSpace(value)) return true;

        var parts = value.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var ctrl = parts.Any(IsCtrl);
        var alt = parts.Any(part => part.Equals("Alt", StringComparison.OrdinalIgnoreCase));
        var shift = parts.Any(part => part.Equals("Shift", StringComparison.OrdinalIgnoreCase));
        var meta = parts.Any(part => part.Equals("Meta", StringComparison.OrdinalIgnoreCase) || part.Equals("Win", StringComparison.OrdinalIgnoreCase) || part.Equals("Cmd", StringComparison.OrdinalIgnoreCase));
        var keys = parts.Where(part => !IsModifier(part)).ToArray();
        if (keys.Length != 1 || !Enum.TryParse<KeyCode>($"Vc{keys.SingleOrDefault()}", true, out var key) || key == KeyCode.VcUndefined)
        {
            error = $"Invalid shortcut '{value}'. Use modifiers plus one letter, number, or function key.";
            return false;
        }
        if (!ctrl && !alt && !shift && !meta)
        {
            error = $"Shortcut '{value}' must include a modifier.";
            return false;
        }
        gesture = new(key, ctrl, alt, shift, meta);
        return true;
    }

    public bool Matches(KeyboardEventData data, EventMask mask) => data.KeyCode == Key
        && mask.HasCtrl() == Ctrl
        && mask.HasAlt() == Alt
        && mask.HasShift() == Shift
        && mask.HasMeta() == Meta;

    private static bool IsCtrl(string value) => value.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) || value.Equals("Control", StringComparison.OrdinalIgnoreCase);
    private static bool IsModifier(string value) => IsCtrl(value) || value.Equals("Alt", StringComparison.OrdinalIgnoreCase) || value.Equals("Shift", StringComparison.OrdinalIgnoreCase) || value.Equals("Meta", StringComparison.OrdinalIgnoreCase) || value.Equals("Win", StringComparison.OrdinalIgnoreCase) || value.Equals("Cmd", StringComparison.OrdinalIgnoreCase);
}
