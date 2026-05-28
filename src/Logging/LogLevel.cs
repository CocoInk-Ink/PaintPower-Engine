using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaintPower.Logging;
public class LogLevel {
    public static int WARN = 0;
    public static int ERROR = 1;
    public static int INFO = 2;
    public static int TRACE = 3;
    public static int FATAL = 4;
    public static int SecurityError = 5;
    public static int SecurityFatal = 6;
    public static int SecurityWarning = 7;
    public static int InternetError = 8;
    public static int InternetFatal = 9;
    public static int InternetWarning = 10;
    public static int InternetInfo = 11;
    public static int InternetTrace = 12;
}