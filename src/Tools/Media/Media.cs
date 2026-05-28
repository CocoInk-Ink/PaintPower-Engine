using System;

namespace PaintPower.Tools.Media;

public class Media
{
    byte[]? data = null; // Raw media data, loaded from file or other source.

    // Base class for media types, like: video, audio, etc.
    string? filePath = null;
    string? title = null;
    TimeSpan duration = TimeSpan.Zero;

    public Media(string? filePath = null)
    {
        if (filePath != null) this.filePath = filePath;
    }

    public string? FilePath
    {
        get => filePath;
        set => filePath = value;
    }

    public string? Title
    {
        get => title;
        set => title = value;
    }

    public TimeSpan Duration
    {
        get => duration;
        set => duration = value;
    }

    public void Load()
    {
        data = System.IO.File.ReadAllBytes(filePath ?? throw new InvalidOperationException("File path must be set to load media."));
        // Additional loading logic here (e.g., parsing metadata, etc.)
    }
}