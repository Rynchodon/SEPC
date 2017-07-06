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
        private bool SessionClosedAttached;
        private bool IsUpdatingStopped;

        #region MySessionComponentBase hooks

        public MySession() : base ()
        {
            Logger.DebugLog("App.MySession.Ctr()");

            // Set up resources that persist inside sessions
            ComponentSession.Open();
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            if (IsUpdatingStopped)
                UpdatingResumed();

            if (!SessionClosedAttached && MyAPIGateway.Entities != null)
            {
                Logger.DebugLog("Attaching SessionClosed");
                MyAPIGateway.Entities.OnCloseAll += SessionClosed;
                SessionClosedAttached = true;
            }

            ComponentSession.Update();
        }

        public override void UpdatingStopped()
        {
            base.UpdatingStopped();
            Logger.Log("App.MySession.UpdatingStopped()");
            ComponentSession.RaiseSessionEventImmediately(ComponentEventNames.UpdatingStopped);
            IsUpdatingStopped = true;
        }

        public override void SaveData()
        {
            base.SaveData();
            Logger.Log("App.MySession.SaveData()");
            ComponentSession.RaiseSessionEventImmediately(ComponentEventNames.SessionSave);
        }

        #endregion

        private void UpdatingResumed()
        {
            Logger.Log("App.MySession.UpdatingResumed()");
            ComponentSession.RaiseSessionEvent(ComponentEventNames.UpdatingResumed);
            IsUpdatingStopped = false;
        }

        private void SessionClosed()
        {
            Logger.Log("App.MySession.SessionClosed()");

            // Close resources that persist inside sessions
            ComponentSession.Close();
            Profiler.Close();
            Logger.Close();

            MyAPIGateway.Entities.OnCloseAll -= SessionClosed;
            SessionClosedAttached = false;
        }
    }
}
