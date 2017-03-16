namespace Uml.Robotics.Ros.Tests {
    public class Utils {
        public static void WaitForDebugger() {
            System.Console.WriteLine("Unit Test is halted until the debugger is attached to following PID:");
            System.Console.WriteLine($"Process ID: {System.Diagnostics.Process.GetCurrentProcess().Id}");
            while (!System.Diagnostics.Debugger.IsAttached) ;
        }
    }
}