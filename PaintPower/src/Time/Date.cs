using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaintPower.Time;

public class Date
{
    public int Year { get; }
    public int Month { get; }
    public int Day { get; }
    public int Hour { get; }
    public int Minute { get; }
    public int Second { get; }
    public int Millisecond { get; }
    public Date()
    {
        var now = DateTime.Now;
        Year = now.Year;
        Month = now.Month;
        Day = now.Day;
        Hour = now.Hour;
        Minute = now.Minute;
        Second = now.Second;
        Millisecond = now.Millisecond;
    }

    public override string ToString()
    {
        return $"{Year}-{Month:D2}-{Day:D2} {Hour:D2}:{Minute:D2}:{Second:D2}.{Millisecond:D3}";
    }

    public string getBuildTimestamp()
    {

        #if BUILD_TIME
            return BUILD_TIME.ToString();
        #else
            return "Unknown";
        #endif

    }
}
