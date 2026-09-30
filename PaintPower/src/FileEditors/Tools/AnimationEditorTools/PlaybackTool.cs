using System;
using Avalonia.Threading;

namespace PaintPower.FileEditors.Tools.AnimationEditorTools;

public class PlaybackTool
{
    private readonly DispatcherTimer _timer = new();
    public int _currentFrame;

    public bool isPlaying = false;

    public event Action<int>? FrameChanged;
    public event Action? PlaybackStopped;

    public PlaybackTool()
    {
        _timer.Tick += (_, _) =>
        {
            isPlaying = true;
            _currentFrame++;
            FrameChanged?.Invoke(_currentFrame);
        };
    }

    public void SetFPS(int fps)
    {
        _timer.Interval = TimeSpan.FromMilliseconds(1000.0 / fps);
    }

    public void Play(int startFrame = 0)
    {
        _currentFrame = startFrame;
        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();
        PlaybackStopped?.Invoke();
        isPlaying = false;
    }
}
