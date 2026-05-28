// Easy static version

using Avalonia.Input;

namespace PaintPower.Tools.Keyboard;

public static class SKeyPress
{
    public static bool strict(KeyEventArgs? e, string key)
    {
        return new KeyPress(e).isPressed(key);
    }

    public static bool any(KeyEventArgs? e, string key)
    {
        return new KeyPress(e).isAnyPressed(key);
    }

    public static bool combo(KeyEventArgs? e, params string[] keys)
    {
        return new KeyPress(e).isComboPressed(keys);
    }

    public static bool ctrlcombo(KeyEventArgs? e, params string[] keys)
    {
        return combo(e, "ctrl", "");
    }
}