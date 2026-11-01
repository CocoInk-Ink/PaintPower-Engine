using Avalonia;
using System;
using System.Runtime.InteropServices;
using Toolbox.Sessions;

namespace PaintPower;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) {
		#if DEBUG
			Session.InitializeSession<PaintPowerApp>(args, "Testing", "PaintPower-Engine");
		#else
        	Session.InitializeSession<PaintPowerApp>(args, PaintPower_Engine.Version, "PaintPower-Engine");
		#endif
    }


    [DllImport("kernel32.dll")]
    private static extern bool AllocConsole();

}
