// A class for a cached timer.

namespace PaintPower.Time;

public class CachedTimer
{
    private static Date cachedTime = null;
    private static bool dirty = true; // Whether the cache is dirty and needs updating.

    // Get the current time, updating the cache if necessary.
    public static Date GetCachedTimer()
    {
        return dirty ? getFreshTime() : cachedTime;
    }

    public static void clearCachedTimer()
    {
        dirty = true;
    }

    private static Date getFreshTime()
    {
        cachedTime = new Date();
        dirty = false;
        return cachedTime;
    }
}