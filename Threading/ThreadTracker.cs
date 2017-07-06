using System;

using VRage;

namespace SEPC.Threading
{
    /// <summary>
    /// Identifies threads by a name and a number for logging and debugging.
    /// </summary>
    public static class ThreadTracker
    {
        public const ushort GAME_THREAD_NUMBER = 1;
        public const string GAME_THREAD_NAME = "Game";

        private static ushort LastThreadNumber = GAME_THREAD_NUMBER;
        private static FastResourceLock LastThreadNumberLock = new FastResourceLock();

        [ThreadStatic]
        public static string ThreadName;

        [ThreadStatic]
        private static ushort value_ThreadNumber;

        public static bool IsGameThread
        {
            get { return value_ThreadNumber == GAME_THREAD_NUMBER; }
        }

        public static ushort ThreadNumber
        {
            get
            {
                if (!IsNumberAssigned())
                    AssignNumber();
                return value_ThreadNumber;
            }
        }

        /// <summary>
        /// Must be called early, before any accessors for the thread are called (including logging!).
        /// </summary>
        public static void SetGameThread()
        {
            value_ThreadNumber = GAME_THREAD_NUMBER;
            ThreadName = GAME_THREAD_NAME;
        }

        private static void AssignNumber()
        {
            using (LastThreadNumberLock.AcquireExclusiveUsing())
                value_ThreadNumber = ++LastThreadNumber;
        }

        private static bool IsNumberAssigned()
        {
            return value_ThreadNumber != 0;
        }
    }
}
