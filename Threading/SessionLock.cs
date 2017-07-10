/*
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;

using SEPC.Components.Attributes;
using SEPC.Collections;


namespace SEPC.Threading
{
    /// <summary>
    /// Provides methods to perform work using the main thread during a session.
    /// </summary>
    [IsSessionComponent(order: SessionOpenedOrder, isStatic: true)]
    public class SessionLock
    {
        class DummyDispoable : IDisposable
        {
            public void Dispose() { }
        }

        /// <summary> Dependent components in SEPC should be opened after this.</summary>
        public const int SessionOpenedOrder = int.MinValue;

        /// <summary> Dependent components anywhere should be closed before this.</summary>
        public const int SessionClosedOrder = int.MaxValue;

        static readonly FastResourceLock MainThreadLock = new FastResourceLock();
        static readonly DummyDispoable DummyUsing = new DummyDispoable();
        static readonly LockedDeque<Action> ToDo = new LockedDeque<Action>();

        /// <summary>
        /// Acquire a shared using lock on the session thread.
        /// Consumers should ensure the session is still open when the lock is acquired.
        /// </summary>
        public static IDisposable AcquireSharedUsing()
        {
            if (ThreadTracker.IsGameThread)
                return DummyUsing;
            else
                return MainThreadLock.AcquireSharedUsing();
        }

        /// <summary>
        /// Invoke an action on the session thread.
        /// </summary>
        public static void InvokeOnMainThread(Action action)
        {
            using (AcquireSharedUsing())
                action.Invoke();
        }

        #region Instance

        SessionLock()
        {
            MainThreadLock.AcquireExclusive();
        }

        [OnSessionUpdate]
        void Update()
        {
            MainThreadLock.ReleaseExclusive();

            // Does all pending work

            MainThreadLock.AcquireExclusive();
        }

        [OnSessionClose(order: SessionClosedOrder)]
        void Close()
        {
            MainThreadLock.ReleaseExclusive();
        }

        #endregion
    }
}
*/