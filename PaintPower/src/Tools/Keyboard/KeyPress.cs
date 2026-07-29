using System.Collections.Generic;
using System.Linq;
using Avalonia.Input;

namespace PaintPower.Tools.Keyboard;

public class KeyPress
{
    private readonly KeyEventArgs? e;

    // Tracks which keys are currently held down
    private static readonly Dictionary<Key, bool> PressedKeys = new();

    // Maps string names ("ctrl", "s", "shiftleft") to Avalonia Keys
    private static readonly Dictionary<string, Key> Keys = new();

    private static readonly string[] TwoSidedKeys = { "alt", "shift", "ctrl" };

    public static void init()
    {
        // Letters
        for (char c = 'a'; c <= 'z'; c++)
            Keys[c.ToString()] = Key.A + (c - 'a');

        // Numbers
        for (int i = 0; i <= 9; i++)
            Keys[i.ToString()] = Key.D0 + i;

        // Utility keys
        Keys["space"] = Key.Space;
        Keys["enter"] = Key.Enter;
        Keys["escape"] = Key.Escape;
        Keys["tab"] = Key.Tab;

        // Left/right modifier keys
        Keys["shiftleft"] = Key.LeftShift;
        Keys["shiftright"] = Key.RightShift;
        Keys["ctrlleft"] = Key.LeftCtrl;
        Keys["ctrlright"] = Key.RightCtrl;
        Keys["altleft"] = Key.LeftAlt;
        Keys["altright"] = Key.RightAlt;

        // Function keys
        for (int i = 1; i <= 24; i++)
            Keys[$"fn{i}"] = Key.F1 + (i - 1);

        // Fn arrow keys
        Keys["fnup"] = Key.FnUpArrow;
        Keys["fndown"] = Key.FnDownArrow;
        Keys["fnleft"] = Key.FnLeftArrow;
        Keys["fnright"] = Key.FnRightArrow;

        // Arrow keys
        Keys["up"] = Key.Up;
        Keys["down"] = Key.Down;
        Keys["left"] = Key.Left;
        Keys["right"] = Key.Right;

        // Special keys
        Keys["apps"] = Key.Apps;
        Keys["abntc1"] = Key.AbntC1;
        Keys["abntc2"] = Key.AbntC2;
        Keys["add"] = Key.Add;
    }

    public KeyPress(KeyEventArgs? e)
    {
        this.e = e;
    }

    // Called from MainWindow.OnKeyDown
    public static void RegisterKeyDown(Key key)
    {
        PressedKeys[key] = true;
    }

    // Called from MainWindow.OnKeyUp
    public static void RegisterKeyUp(Key key)
    {
        PressedKeys[key] = false;
    }

    private bool HasLeftRight(string key) =>
        TwoSidedKeys.Contains(key);

    public bool IsPressed(string key)
    {
        key = key.ToLower();

        if (!Keys.TryGetValue(key, out var mappedKey))
            return false;

        return PressedKeys.TryGetValue(mappedKey, out bool down) && down;
    }

    public bool IsAnyPressed(string key)
    {
        key = key.ToLower();

        return HasLeftRight(key)
            ? IsPressed($"{key}left") || IsPressed($"{key}right")
            : IsPressed(key);
    }

    public bool IsComboPressed(params string[] keys)
    {
        if (keys.Length == 0) return true;

        foreach (var key in keys)
            if (!IsAnyPressed(key))
                return false;

        return true;
    }

    public bool IsCtrlComboPressed(string key) =>
        IsComboPressed("ctrl", key);
}
