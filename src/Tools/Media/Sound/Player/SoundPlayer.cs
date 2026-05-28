using System;
using System.IO;
using NAudio.Wave;
using PaintPower.Logging;
using PaintPower.Tools.Media;
using PaintPower.Tools.Media.Player;

namespace PaintPower.Tools.Media.Sound.Player;

public class SoundPlayer : MediaPlayer, IDisposable
{
    private IWavePlayer? output;
    private AudioFileReader? reader;

    public bool isPaused = false;
    private bool loop = false;

    public float Volume
    {
        get => reader?.Volume ?? 1f;
        set
        {
            if (reader != null)
                reader.Volume = System.Math.Clamp(value, 0f, 1f);
        }
    }

    public bool Loop
    {
        get => loop;
        set => loop = value;
    }

    public SoundPlayer(Media? media = null) : base()
    {
        if (media != null)
            LoadMedia(media);
    }

    public override void LoadMedia(Media media)
    {
        base.LoadMedia(media);

        if (media.FilePath == null)
            throw new InvalidOperationException("Sound must have a file path.");

        try {
        output?.Stop();
        output?.Dispose();
        reader?.Dispose();

        reader = new AudioFileReader(media.FilePath);
        output = new WaveOutEvent();
        output.Init(reader);

        output.PlaybackStopped += (s, e) =>
        {
            if (loop && reader != null)
            {
                reader.Position = 0;
                output?.Play();
            }
        };
        } catch (Exception ex)
        {
            Log.Error(new Exception("Failed to load sound: " + ex.Message));
            output = null;
            reader = null;
        }
    }

    public override void Play()
    {
        Log.Info("Playing sound.");

        if (output == null || reader == null) {
            Log.QuickLog("No sound loaded to play.");
            return;
        }

        Log.Info("Sound loaded, starting playback.");

        reader.Position = 0;
        output.Play();
        isPaused = false;
    }

    public override void Pause()
    {
        if (output == null)
            return;

        output.Pause();
        isPaused = true;
    }

    public override void Resume()
    {
        if (output == null || !isPaused)
            return;

        output.Play();
        isPaused = false;
    }

    public override void Stop()
    {
        if (output == null)
            return;

        output.Stop();
        isPaused = false;
    }

    public override void Seek(TimeSpan position)
    {
        if (reader == null)
            return;

        reader.CurrentTime = position;
    }

    public void Dispose()
    {
        output?.Stop();
        output?.Dispose();
        reader?.Dispose();
    }
}
