// Easy static version

using Avalonia.Input;

namespace PaintPower.Tools.Keyboard;

public static class SKeyPress
{
    public static bool strict(KeyEventArgs? e, string key)
    {
        return new KeyPress(e).IsPressed(key);
    }

    public static bool any(KeyEventArgs? e, string key)
    {
        return new KeyPress(e).IsAnyPressed(key);
    }

    public static bool combo(KeyEventArgs? e, params string[] keys)
    {
        return new KeyPress(e).IsComboPressed(keys);
    }

    public static bool ctrlcombo(KeyEventArgs? e, params string[] keys)
    {
        // Prepend "ctrl" to the keys
        var newKeys = new string[keys.Length + 1];
        newKeys[0] = "ctrl";

        for (int i = 0; i < keys.Length; i++)
            newKeys[i + 1] = keys[i];

        return combo(e, newKeys);
    }
}
