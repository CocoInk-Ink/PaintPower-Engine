// File: PaintPower.Runtime.Ksa/SyscallId.cs

// User defined runtime variables start with 0x12300000,
// Example: 0x12377304

namespace PaintPower.Runtime.Ksa
{
    public enum SyscallId : int
    {
        // System operations 0x000
        Print = 0x000,
        Log = 0x001,
        Error = 0x002,
        Breakpoint = 0x003,

        // Sprite operations 0x200

        // Sprite motion 20x
        SpriteSay = 0x200,
        SpriteGlide = 0x201,
        SpriteSetPos = 0x202,
        SpriteTurn = 0x203,
        SpriteSetVisible = 0x204,
        SpriteDirectionSet = 0x205,

        // Sprite skins 21x
        SwitchSkin = 0x210,
        GetSkinChild = 0x211,

        // Timing / events
        WaitMs = 0x300,
        WaitUntilVarTrue = 0x301,
        Await = 0x302,

        // Messaging
        Broadcast = 0x400,
        BroadcastAndWait = 0x401,
        MessageExists = 0x402,

        // Object model
        AllocObject = 0x500,
        GetField = 0x501,
        SetField = 0x502,

        // Pointers and memory
        Pointer = 0x6001,
        Push = 0x6002,
        Pull = 0x6003,

        ExitThread = 0xFFF
    }
}
