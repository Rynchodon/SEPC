using System.Diagnostics;

using SEPC.Components;
using SEPC.Components.Attributes;
using SEPC.Logging;

namespace SEPC.World
{
    [IsSessionComponent]
    public class Updates
    {

        //public const int UPDATES_PER_SECOND = 60;
        //public const float SECONDS_PER_UPDATE = 1f / UPDATES_PER_SECOND;
        //public const double TICKS_PER_UPDATE = (double)TimeSpan.TicksPerSecond / (double)UPDATES_PER_SECOND;

        public static Updates Static;

        private static Logable Log = new Logable("SEPC.World");

        private long Received;
        private long StartedAt = Stopwatch.GetTimestamp();
        private long StoppedAt;

        public Updates()
        {
            Static = this;
        }

        public long UpdateTicks
        {
            get {

                long result = (StoppedAt != 0) ? StoppedAt - StartedAt : Stopwatch.GetTimestamp() - StartedAt;
                //Log.Debug("World.Time.UpdateTime: " + result / Stopwatch.Frequency);
                return result;
            }
        }

        public long UpdatesReceived
        {
            get { return Received; }
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

        [OnSessionUpdate]
        private void Update()
        {
            Received++;
            //LastUpdateAt = Stopwatch.GetTimestamp();
            //float instantSimSpeed = UpdateDuration / (float)(DateTime.UtcNow - LastUpdateAt).TotalSeconds;
            //if (instantSimSpeed > 0.01f && instantSimSpeed < 1.1f)
            //    SimSpeed = SimSpeed * 0.9f + instantSimSpeed * 0.1f;
            //Log.DebugLog("instantSimSpeed: " + instantSimSpeed + ", SimSpeed: " + SimSpeed);
        }

        [OnSessionEvent(ComponentEventNames.UpdatingStopped)]
        private void UpdatingStopped()
        {
            StoppedAt = Stopwatch.GetTimestamp();
            //Log.Debug("World.Time.UpdatingStopped() at " + UpdatesStoppedAt / Stopwatch.Frequency);
            //LastUpdateAt = Stopwatch.GetTimestamp();
            //UpdatesReceived++;
            //float instantSimSpeed = UpdateDuration / (float)(DateTime.UtcNow - LastUpdateAt).TotalSeconds;
            //if (instantSimSpeed > 0.01f && instantSimSpeed < 1.1f)
            //    SimSpeed = SimSpeed * 0.9f + instantSimSpeed * 0.1f;
            //Log.DebugLog("instantSimSpeed: " + instantSimSpeed + ", SimSpeed: " + SimSpeed);
        }

        [OnSessionEvent(ComponentEventNames.UpdatingResumed)]
        private void UpdatingResumed()
        {
            var resumedAt = Stopwatch.GetTimestamp();
            //Log.Debug("World.Time.UpdatingResumed() at " + resumedAt / Stopwatch.Frequency);
            long prevDuration = StoppedAt - StartedAt;
            //Log.Debug("prevDuration: " + prevDuration / Stopwatch.Frequency);
            StartedAt = Stopwatch.GetTimestamp() - prevDuration;
            //Log.Debug("New UpdatesStartedAt: " + UpdatesStartedAt / Stopwatch.Frequency);
            StoppedAt = 0;
            //LastUpdateAt = Stopwatch.GetTimestamp();
            //UpdatesReceived++;
            //float instantSimSpeed = UpdateDuration / (float)(DateTime.UtcNow - LastUpdateAt).TotalSeconds;
            //if (instantSimSpeed > 0.01f && instantSimSpeed < 1.1f)
            //    SimSpeed = SimSpeed * 0.9f + instantSimSpeed * 0.1f;
            //Log.DebugLog("instantSimSpeed: " + instantSimSpeed + ", SimSpeed: " + SimSpeed);
        }

        [OnSessionClose(order: int.MaxValue)]
        private void SessionClosed()
        {
            Log.Entered();
            //LastUpdateAt = ClosedAt = Stopwatch.GetTimestamp();
            //WorldClosed = true;
        }

    }
}
