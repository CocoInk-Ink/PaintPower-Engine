using System;

namespace Toolbox.Time;

public class Time
{
    public Date Now;
    public int getHour() {
        return Now.Hour;
    }
    
    public int getMinute() {
        return Now.Minute;
    }
    public int getSecond() {
        return Now.Second;
    }

    public int getMillisecond() {
        return Now.Millisecond;
    }

    public Time() {
        Now = CachedTimer.GetCachedTimer();
    }
}