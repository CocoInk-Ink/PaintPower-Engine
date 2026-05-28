/*
    MediaPlayer.cs
    Base class for media players, like: video, audio, etc.
*/

using System;

namespace PaintPower.Tools.Media.Player;

public class MediaPlayer
{

    Media? media = null;

    public MediaPlayer()
    {
        // Initialize media player resources here
        
    }

    public virtual void Play() {}

    public virtual void Stop() {}

    public virtual void Pause() {}

    public virtual void Resume() {}

    public virtual void Seek(TimeSpan position) {}

    public virtual void LoadMedia(Media media)
    {
        this.media = media;
        media.Load();
        // Additional logic to prepare media for playback
    }
}