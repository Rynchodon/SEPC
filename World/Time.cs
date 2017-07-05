using System.Diagnostics;

using SEPC.Components;
using SEPC.Components.Attributes;
using SEPC.Logging;

namespace SEPC.World
{
    [IsSessionComponent(isStatic: true, order: int.MinValue)]
    public static class Time
    {

        //public const int UPDATES_PER_SECOND = 60;
        //public const float SECONDS_PER_UPDATE = 1f / UPDATES_PER_SECOND;
        //public const double TICKS_PER_UPDATE = (double)TimeSpan.TicksPerSecond / (double)UPDATES_PER_SECOND;

        private static long UpdatesStartedAt;
        private static long UpdatesStoppedAt;

        public static long UpdateTicks
        {
            get { return Stopwatch.GetTimestamp() - UpdatesStartedAt; }
        }

        //public static bool WorldClosed;
        //private static long StartedAt, LastUpdateAt, ClosedAt;
        //public static ulong UpdatesReceived;

        /// <summary>Simulation speed of game based on time between updates.</summary>
        //public static float SimSpeed = 1f;

        /// <summary>Elapsed time based on number of updates i.e. not incremented while paused.</summary>
        //public static TimeSpan ElapsedUpdateTime
        //{
        //    get { return new TimeSpan((long)(UpdatesReceived * TICKS_PER_UPDATE)); }
        //}

        [OnStaticSessionComponentInit]
        private static void SessionOpened()
        {
            Logger.Log("");
            UpdatesStartedAt = Stopwatch.GetTimestamp();
            //LastUpdateAt = StartedAt = Stopwatch.GetTimestamp();
            //UpdatesReceived = 0;
            //WorldClosed = false;
        }

        [OnSessionUpdate]
        public static void Update()
        {
            //LastUpdateAt = Stopwatch.GetTimestamp();
            //UpdatesReceived++;
            //float instantSimSpeed = UpdateDuration / (float)(DateTime.UtcNow - LastUpdateAt).TotalSeconds;
            //if (instantSimSpeed > 0.01f && instantSimSpeed < 1.1f)
            //    SimSpeed = SimSpeed * 0.9f + instantSimSpeed * 0.1f;
            //Log.DebugLog("instantSimSpeed: " + instantSimSpeed + ", SimSpeed: " + SimSpeed);
        }

        [OnSessionEvent(ComponentEventNames.UpdatingStopped)]
        public static void UpdatingStopped()
        {
            Logger.Log("");
            UpdatesStoppedAt = Stopwatch.GetTimestamp();
            //LastUpdateAt = Stopwatch.GetTimestamp();
            //UpdatesReceived++;
            //float instantSimSpeed = UpdateDuration / (float)(DateTime.UtcNow - LastUpdateAt).TotalSeconds;
            //if (instantSimSpeed > 0.01f && instantSimSpeed < 1.1f)
            //    SimSpeed = SimSpeed * 0.9f + instantSimSpeed * 0.1f;
            //Log.DebugLog("instantSimSpeed: " + instantSimSpeed + ", SimSpeed: " + SimSpeed);
        }

        [OnSessionEvent(ComponentEventNames.UpdatingResumed)]
        public static void UpdatingResumed()
        {
            Logger.Log("");
            long prevDuration = UpdatesStoppedAt - UpdatesStartedAt;
            UpdatesStartedAt = Stopwatch.GetTimestamp() - prevDuration;
            //LastUpdateAt = Stopwatch.GetTimestamp();
            //UpdatesReceived++;
            //float instantSimSpeed = UpdateDuration / (float)(DateTime.UtcNow - LastUpdateAt).TotalSeconds;
            //if (instantSimSpeed > 0.01f && instantSimSpeed < 1.1f)
            //    SimSpeed = SimSpeed * 0.9f + instantSimSpeed * 0.1f;
            //Log.DebugLog("instantSimSpeed: " + instantSimSpeed + ", SimSpeed: " + SimSpeed);
        }

        [OnSessionClose(order: int.MaxValue)]
        private static void SessionClosed()
        {
            Logger.Log("");
            //LastUpdateAt = ClosedAt = Stopwatch.GetTimestamp();
            //WorldClosed = true;
        }

    }
}
