using System;
using System.Collections.Generic;
using PaintPower.Tools.Media.Sound.Player;

namespace PaintPower.Tools.SoundEffects;

public class SoundEffect
{
    private readonly List<Media.Media> MediaList = new();

    public SoundEffect(params string[] names)
    {
        foreach (var name in names)
        {
            try
            {
                var media = new Media.Media("Assets/Sounds/" + name + ".wav");
                media.Load();
                MediaList.Add(media);
            }
            catch { }
        }
    }

    public void Play()
    {
        foreach (var media in MediaList)
        {
            // Create a NEW player for each sound
            var player = new SoundPlayer(media);

            // Fire and forget
            player.Play();
        }
    }

    public void Stop()
    {
        // Optional: stop all players if you track them
    }
}
