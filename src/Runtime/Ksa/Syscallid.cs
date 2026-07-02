// File: PaintPower.Runtime.Ksa/SyscallId.cs
namespace PaintPower.Runtime.Ksa
{
    public enum SyscallId : int
    {
        Print = 0x000,
        Log = 0x001,

        // Sprite operations
        SpriteSay = 0x100,
        SpriteCenter = 0x210,
        SpriteGlide = 0x211,
        SpriteSetPos = 0x213,
        SwitchSkin = 0x220,
        GetSkinChild = 0x221,
        GetVideo = 0x222,
        VideoPlay = 0x223,
        GetImage = 0x224,

        // Timing / events
        WaitMs = 0x300,
        WaitUntilVarTrue = 0x301,

        // Messaging
        Broadcast = 0x400,
        BroadcastAndWait = 0x401,
        MessageExists = 0x402,

        // Object model
        AllocObject = 0x500,
        GetField = 0x501,
        SetField = 0x502,

        ExitThread = 0xFFF
    }
}
