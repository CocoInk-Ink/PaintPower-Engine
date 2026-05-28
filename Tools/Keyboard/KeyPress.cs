using System.Collections.Generic;
using System.Linq;
using Avalonia.Input;

namespace PaintPower.Tools.Keyboard;

public class KeyPress
{
    private readonly KeyEventArgs? e;

    // Initialize the dictionary so it is never null
    private static readonly Dictionary<string, Key> Keys = new();

    // For keys like, alt. There is a left alt and a right alt keys. Excluding arrow keys.
    private static string[] twosidedkeys = { "alt", "shift", "ctrl" };

    public static void init()
    {

        // Letters
        Keys["a"] = Key.A;
        Keys["b"] = Key.B;
        Keys["c"] = Key.C;
        Keys["d"] = Key.D;
        Keys["e"] = Key.E;
        Keys["f"] = Key.F;
        Keys["g"] = Key.G;
        Keys["h"] = Key.H;
        Keys["i"] = Key.I;
        Keys["j"] = Key.J;
        Keys["k"] = Key.K;
        Keys["l"] = Key.L;
        Keys["m"] = Key.M;
        Keys["n"] = Key.N;
        Keys["o"] = Key.O;
        Keys["p"] = Key.P;
        Keys["q"] = Key.Q;
        Keys["r"] = Key.R;
        Keys["s"] = Key.S;
        Keys["t"] = Key.T;
        Keys["u"] = Key.U;
        Keys["v"] = Key.V;
        Keys["w"] = Key.W;
        Keys["x"] = Key.X;
        Keys["y"] = Key.Y;
        Keys["z"] = Key.Z;

        // Numbers
        Keys["0"] = Key.D0;
        Keys["1"] = Key.D1;
        Keys["2"] = Key.D2;
        Keys["3"] = Key.D3;
        Keys["4"] = Key.D4;
        Keys["5"] = Key.D5;
        Keys["6"] = Key.D6;
        Keys["7"] = Key.D7;
        Keys["8"] = Key.D8;
        Keys["9"] = Key.D9;

        // Util keys
        Keys["space"] = Key.Space;
        Keys["enter"] = Key.Enter;
        Keys["escape"] = Key.Escape;
        Keys["tab"] = Key.Tab;

        // Double keys (left and right) (left)
        Keys["shiftleft"] = Key.LeftShift;
        Keys["ctrlleft"] = Key.LeftCtrl;
        Keys["altleft"] = Key.LeftAlt;

        // Double keys (left and right) (right)
        Keys["shiftright"] = Key.RightShift;
        Keys["ctrlright"] = Key.RightCtrl;
        Keys["altright"] = Key.RightAlt;

        // Function keys
        Keys["fn1"] = Key.F1;
        Keys["fn2"] = Key.F2;
        Keys["fn3"] = Key.F3;
        Keys["fn4"] = Key.F4;
        Keys["fn5"] = Key.F5;
        Keys["fn6"] = Key.F6;
        Keys["fn7"] = Key.F7;
        Keys["fn8"] = Key.F8;
        Keys["fn4"] = Key.F4;
        Keys["fn9"] = Key.F9;
        Keys["fn10"] = Key.F10;
        Keys["fn11"] = Key.F11;
        Keys["fn12"] = Key.F12;
        Keys["fn13"] = Key.F13;
        Keys["fn14"] = Key.F14;
        Keys["fn15"] = Key.F15;
        Keys["fn16"] = Key.F16;
        Keys["fn17"] = Key.F17;
        Keys["fn18"] = Key.F18;
        Keys["fn19"] = Key.F19;
        Keys["fn20"] = Key.F20;
        Keys["fn21"] = Key.F21;
        Keys["fn22"] = Key.F22;
        Keys["fn23"] = Key.F23;
        Keys["fn24"] = Key.F24;

        // Function arrows
        Keys["fnleft"] = Key.FnUpArrow;
        Keys["fndown"] = Key.FnDownArrow;
        Keys["fnleft"] = Key.FnLeftArrow;
        Keys["fnright"] = Key.FnRightArrow;

        // Arrows
        Keys["up"] = Key.Up;
        Keys["down"] = Key.Down;
        Keys["left"] = Key.Left;
        Keys["right"] = Key.Right;

        // Special keys
        Keys["apps"] = Key.Apps;
        Keys["bbntc1"] = Key.AbntC1;
        Keys["abntc2"] = Key.AbntC2;
        Keys["add"] = Key.Add;

    }

    public KeyPress(KeyEventArgs? e)
    {
        this.e = e;
    }

    // Does this string have a key on the left and right?
    public bool hasLeftRight(string key)
    {
        return twosidedkeys.Contains(key);
    }

    // Is this key pressed?
    public bool isPressed(string key)
    {
        key = key.ToLower();

        return Keys.TryGetValue(key, out var mappedKey) && mappedKey == e?.Key;
    }

    // For keys that have both left and right, is at least one of them pressed?
    public bool isAnyPressed(string key)
    {
        key = key.ToLower();

        return hasLeftRight(key) ? isPressed($"{key}left") || isPressed($"{key}right") : isPressed(key);
    }

    public bool isComboPressed(params string[] keys)
    {
        if (keys.Length == 0) return true;
        else
        {
            // Is a key missing from the combo
            bool isKeyMissing = false;

            for (int i = 0; i < keys.Length; i++)
            {
                if (!isAnyPressed(keys[i])) isKeyMissing = true;
            }

            return !isKeyMissing;
        }
    }

    public bool isTwoKeyComboPressed(string key1, string key2)
    {
        return isComboPressed(key1, key2);
    }

    public bool isCtrlComboPressed(string key)
    {
        return isComboPressed("ctrl", key);
    }
}
