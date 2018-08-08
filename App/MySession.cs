using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Utils;

using SEPC.Components;
using SEPC.Diagnostics;
using SEPC.Logging;

namespace SEPC.App
{
    /// <summary>
    /// Loaded with a session and persists until it's closed.
    /// Sets up and tears down resources that exist inside sessions.
    /// Detects session events and sends them to ComponentSession.
    /// </summary>
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    class MySession : MySessionComponentBase
    {
        private static Logable Log = new Logable("SEPC.App");

        private bool SessionClosedAttached;
        private bool IsUpdatingStopped;
        private bool IsComponentSessionOpened;

        #region MySessionComponentBase hooks

        public MySession() : base ()
        {
            Log.Entered();
            // Move this to delayed initialization, seems to happen multiple times at start
            // ComponentSession.Open();
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            // Do delayed initialization, seems like only one of the created components receives updates
            if (!IsComponentSessionOpened)
            {
                ComponentSession.Open();
                IsComponentSessionOpened = true;
            }

            if (IsUpdatingStopped)
                UpdatingResumed();

            if (!SessionClosedAttached && MyAPIGateway.Entities != null)
            {
                Log.Trace("Attaching SessionClosed");
                MyAPIGateway.Entities.OnCloseAll += SessionClosed;
                SessionClosedAttached = true;
            }

            ComponentSession.Update();
        }

        public override void UpdatingStopped()
        {
            base.UpdatingStopped();
            Log.Entered();
            ComponentSession.RaiseSessionEventImmediately(ComponentEventNames.UpdatingStopped);
            IsUpdatingStopped = true;
        }

        public override void SaveData()
        {
            base.SaveData();
            Log.Entered();
            ComponentSession.RaiseSessionEventImmediately(ComponentEventNames.SessionSave);
        }

        #endregion

        private void UpdatingResumed()
        {
            Log.Entered();
            ComponentSession.RaiseSessionEvent(ComponentEventNames.UpdatingResumed);
            IsUpdatingStopped = false;
        }

        private void SessionClosed()
        {
            Log.Entered();
            ComponentSession.Close();

            // Send close event to ComponentSession dependencies
            Profiler.Close();
            Logger.Close();

            MyAPIGateway.Entities.OnCloseAll -= SessionClosed;
            SessionClosedAttached = false;
        }
    }
}
