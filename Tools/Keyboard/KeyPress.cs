using System.Collections.Generic;
using Avalonia.Input;

namespace PaintPower.Tools.Keyboard;

public class KeyPress
{
    private readonly KeyEventArgs? e;

    // Initialize the dictionary so it is never null
    private static readonly Dictionary<string, Key> Keys = new();

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

        // Special keys
        Keys["space"] = Key.Space;
        Keys["enter"] = Key.Enter;
        Keys["escape"] = Key.Escape;
        Keys["tab"] = Key.Tab;
        Keys["shift"] = Key.LeftShift;
        Keys["ctrl"] = Key.LeftCtrl;
        Keys["alt"] = Key.LeftAlt;

        // Arrows
        Keys["up"] = Key.Up;
        Keys["down"] = Key.Down;
        Keys["left"] = Key.Left;
        Keys["right"] = Key.Right;
    }

    public KeyPress(KeyEventArgs? e)
    {
        this.e = e;
    }

    public bool isPressed(string key)
    {
        key = key.ToLower();

        return Keys.TryGetValue(key, out var mappedKey) && mappedKey == e?.Key;
    }
}
