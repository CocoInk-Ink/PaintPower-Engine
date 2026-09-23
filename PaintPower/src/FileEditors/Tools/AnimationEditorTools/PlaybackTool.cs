using System;
using Avalonia.Threading;

namespace PaintPower.FileEditors.Tools.AnimationEditorTools;

public class PlaybackTool
{
    private readonly DispatcherTimer _timer = new();
    private int _currentFrame;

    public event Action<int>? FrameChanged;
    public event Action? PlaybackStopped;

    public PlaybackTool()
    {
        _timer.Tick += (_, _) =>
        {
            _currentFrame++;
            FrameChanged?.Invoke(_currentFrame);
        };
    }

    public void SetFPS(int fps)
    {
        _timer.Interval = TimeSpan.FromMilliseconds(1000.0 / fps);
    }

    public void Play()
    {
        _currentFrame = 0;
        _timer.Start();
    }

    public void Stop()
    {
        PlaybackStopped?.Invoke();
        _timer.Stop();
    }
}
