using System;

using Sandbox.ModAPI;
using VRage.Utils;


namespace SEPC.Threading
{
    public static class MainThread
    {
        /// <remarks>
        /// User by Logger. Don't use it.
        /// </remarks>
        public static void TryOnMainThread(Action action)
        {
            MyAPIGateway.Utilities?.InvokeOnGameThread(() =>
            {
                try { action.Invoke(); }
                catch (Exception ex) { MyLog.Default?.WriteLine("SEPC - ERROR invoking on game thread: " + ex); }
            });
        }
    }
}
