using System;
using PaintPower.Tools.Media.Sound.Player;

namespace PaintPower.Tools.SoundEffects;

public class SoundEffect
{
    public SoundPlayer Player { get; private set; }

    public SoundEffect(string name)
    {
        Player = new SoundPlayer();
        var media = new Media.Media("Assets/Sounds/" + name + ".wav");
        media.Load();
        Player.LoadMedia(media);
    }

    public void Play()
    {
        if (Player != null)
            Player.Play();
    }

    public void Stop()
    {
        if (Player != null)
            Player.Stop();
    }
}