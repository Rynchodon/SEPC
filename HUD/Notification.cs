using System.Diagnostics;

using Sandbox.ModAPI;
using VRage.Game;

using SEPC.Logging;
using SEPC.Threading;

namespace SEPC.HUD
{
    class Notification
    {
        /// <summary>
        /// Display a HUD notification, thread-safe and error-handled.
        /// </summary>
        public static void Notify(string message, int disappearTimeMs = 2000, string font = MyFontEnum.White)
        {
            MainThread.TryOnMainThread(() => 
                MyAPIGateway.Utilities.ShowNotification(message, disappearTimeMs, font)
            );
        }

        /// <summary>
        /// Display a HUD notification, thread-safe and error-handled.
        /// </summary>
        public static void Notify(string message, int disappearTimeMs = 2000, Severity.Level level = Severity.Level.TRACE)
        {
            Notify(message, disappearTimeMs, Severity.FontForLevel(level));
        }

        /// <summary>
        /// Display a HUD notification, thread-safe and error-handled.
        /// Conditional on DEBUG.
        /// </summary>
        [Conditional("DEBUG")]
        public static void DebugNotify(string message, int disappearTimeMs = 2000, Severity.Level level = Severity.Level.TRACE)
        {
            Notify(message, disappearTimeMs, level);
        }
    }
}
