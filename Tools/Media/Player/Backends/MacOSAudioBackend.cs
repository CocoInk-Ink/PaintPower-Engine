using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace PaintPower.Tools.Media.Sound.Player.Backends;

public class MacOSAudioBackend : IAudioBackend
{
    private IntPtr audioQueue = IntPtr.Zero;
    private byte[]? pcm;
    private int channels;
    private int sampleRate;
    private int bits;
    private bool looping;
    private float volume = 1f;

    private GCHandle pcmHandle;
    private int position = 0;
    private const int BufferSize = 4096;

    public void LoadPcm(byte[] pcmData, int channels, int sampleRate, int bitsPerSample)
    {
        pcm = pcmData;
        this.channels = channels;
        this.sampleRate = sampleRate;
        this.bits = bitsPerSample;

        pcmHandle = GCHandle.Alloc(pcm, GCHandleType.Pinned);

        var format = new AudioStreamBasicDescription
        {
            SampleRate = sampleRate,
            FormatID = 0x6C70636D, // 'lpcm'
            FormatFlags = 0x29,    // signed int, packed
            BytesPerPacket = (uint)(channels * bitsPerSample / 8),
            FramesPerPacket = 1,
            BytesPerFrame = (uint)(channels * bitsPerSample / 8),
            ChannelsPerFrame = (uint)channels,
            BitsPerChannel = (uint)bitsPerSample
        };

        AudioQueue.AudioQueueNewOutput(
            ref format,
            OnBufferCallback,
            IntPtr.Zero,
            IntPtr.Zero,
            IntPtr.Zero,
            0,
            out audioQueue);

        SetVolume(volume);
    }

    public void Play()
    {
        if (audioQueue == IntPtr.Zero || pcm == null)
            return;

        position = 0;
        EnqueueNextBuffer();
        AudioQueue.AudioQueueStart(audioQueue, IntPtr.Zero);
    }

    public void Pause()
    {
        if (audioQueue == IntPtr.Zero) return;
        AudioQueue.AudioQueuePause(audioQueue);
    }

    public void Resume()
    {
        if (audioQueue == IntPtr.Zero) return;
        AudioQueue.AudioQueueStart(audioQueue, IntPtr.Zero);
    }

    public void Stop()
    {
        if (audioQueue == IntPtr.Zero) return;
        AudioQueue.AudioQueueStop(audioQueue, true);
        position = 0;
    }

    public void SetVolume(float vol)
    {
        volume = System.Math.Clamp(vol, 0f, 1f);
        if (audioQueue != IntPtr.Zero)
        {
            AudioQueue.AudioQueueSetParameter(audioQueue, 1, volume); // 1 = Volume
        }
    }

    public void SetLooping(bool loop)
    {
        looping = loop;
    }

    private void EnqueueNextBuffer()
    {
        if (pcm == null) return;

        AudioQueue.AudioQueueAllocateBuffer(audioQueue, BufferSize, out IntPtr buffer);

        int remaining = pcm.Length - position;
        int toCopy = System.Math.Min(BufferSize, remaining);

        unsafe
        {
            Buffer.MemoryCopy(
                source: (void*)(pcmHandle.AddrOfPinnedObject() + position),
                destination: (void*)(buffer + 32), // AudioQueue buffer header offset
                destinationSizeInBytes: BufferSize,
                sourceBytesToCopy: toCopy);
        }

        position += toCopy;

        AudioQueue.AudioQueueEnqueueBuffer(audioQueue, buffer, 0, IntPtr.Zero);

        if (position >= pcm.Length && looping)
            position = 0;
    }

    private void OnBufferCallback(IntPtr userData, IntPtr aq, IntPtr buffer)
    {
        if (pcm == null) return;

        if (position >= pcm.Length && !looping)
            return;

        EnqueueNextBuffer();
    }

    public void Dispose()
    {
        if (audioQueue != IntPtr.Zero)
        {
            AudioQueue.AudioQueueDispose(audioQueue, true);
            audioQueue = IntPtr.Zero;
        }

        if (pcmHandle.IsAllocated)
            pcmHandle.Free();
    }
}
