using System;
using System.Runtime.InteropServices;

namespace PaintPower.Tools.Media.Sound.Player.Backends;

internal static class AudioQueue
{
    private const string AudioToolboxLib = "/System/Library/Frameworks/AudioToolbox.framework/AudioToolbox";

    [DllImport(AudioToolboxLib)]
    public static extern int AudioQueueNewOutput(
        ref AudioStreamBasicDescription format,
        AudioQueueCallback callback,
        IntPtr userData,
        IntPtr runLoop,
        IntPtr runLoopMode,
        uint flags,
        out IntPtr outAQ);

    [DllImport(AudioToolboxLib)]
    public static extern int AudioQueueStart(IntPtr aq, IntPtr reserved);

    [DllImport(AudioToolboxLib)]
    public static extern int AudioQueuePause(IntPtr aq);

    [DllImport(AudioToolboxLib)]
    public static extern int AudioQueueStop(IntPtr aq, bool immediate);

    [DllImport(AudioToolboxLib)]
    public static extern int AudioQueueDispose(IntPtr aq, bool immediate);

    [DllImport(AudioToolboxLib)]
    public static extern int AudioQueueSetParameter(IntPtr aq, int param, float value);

    [DllImport(AudioToolboxLib)]
    public static extern int AudioQueueAllocateBuffer(IntPtr aq, uint bufferSize, out IntPtr buffer);

    [DllImport(AudioToolboxLib)]
    public static extern int AudioQueueEnqueueBuffer(IntPtr aq, IntPtr buffer, uint numPacketDescs, IntPtr descs);
}

[StructLayout(LayoutKind.Sequential)]
public struct AudioStreamBasicDescription
{
    public double SampleRate;
    public uint FormatID;
    public uint FormatFlags;
    public uint BytesPerPacket;
    public uint FramesPerPacket;
    public uint BytesPerFrame;
    public uint ChannelsPerFrame;
    public uint BitsPerChannel;
    public uint Reserved;
}

public delegate void AudioQueueCallback(
    IntPtr userData,
    IntPtr aq,
    IntPtr buffer);
