using System;
using System.IO;
using NAudio.Wave;
using Toolbox.Logging;
using Toolbox.Media;
using Toolbox.Media.Player;

namespace Toolbox.Media.Sound.Player;

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

    public override async Task LoadMedia(Media media)
    {
        await base.LoadMedia(media);

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

    public override async Task Play()
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

    public override async Task Pause()
    {
        if (output == null)
            return;

        output.Pause();
        isPaused = true;
    }

    public override async Task Resume()
    {
        if (output == null || !isPaused)
            return;

        output.Play();
        isPaused = false;
    }

    public override async Task Stop()
    {
        if (output == null)
            return;

        output.Stop();
        isPaused = false;
    }

    public override async Task Seek(TimeSpan position)
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
