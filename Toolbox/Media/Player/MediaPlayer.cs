/*
    MediaPlayer.cs
    Base class for media players, like: video, audio, etc.
*/

using System;
using System.Threading.Tasks;

namespace Toolbox.Media.Player;

public class MediaPlayer
{

    Media? media = null;

    public MediaPlayer()
    {
        // Initialize media player resources here
        
    }

    public virtual async Task Play() {}

    public virtual async Task Stop() {}
    
    public virtual async Task Pause() {}

    public virtual async Task Resume() {}

    public virtual async Task Seek(TimeSpan position) {}

    public virtual async Task LoadMedia(Media media)
    {
        this.media = media;
        media.Load();
        // Additional logic to prepare media for playback
    }
}