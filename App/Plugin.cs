using System;
using System.Reflection;

using Sandbox.ModAPI;
using Sandbox.Game.World;
using VRage.Plugins;

using SEPC.Components;
using SEPC.Diagnostics;
using SEPC.Threading;
using SEPC.Logging;

namespace SEPC.App
{
    /// <summary>
    /// Loaded with the game and persists until it's closed.
    /// Sets up and tears down resources that exist outside of sessions.
    /// Registers MySession each time a new session is opened.
    /// </summary>
    public class Plugin : IPlugin
    {
        private bool SessionAttached;

        /// <summary>
        /// Called when game starts
        /// </summary>
        public void Init(object gameInstance)
        {
            // Register our compilation symbol state
            SymbolRegistrar.SetDebugIfDefined();
            SymbolRegistrar.SetProfileIfDefined();

            // Set up resources that persist outside of sessions
            ThreadTracker.SetGameThread();

            Logger.DebugLog("SEPC.Plugin.Init()");

            // Register our SEPC-managed SessionComponents
            ComponentRegistrar.AddComponents(Assembly.GetExecutingAssembly());
            ComponentRegistrar.LoadOnInit(0, Assembly.GetExecutingAssembly());
        }

        public void Update()
        {
            // Detect when the session has started and re-register MySession
            if (!SessionAttached)
                TryAttachSession();
        }

        /// <summary>
        /// Called when game exits
        /// </summary>
        public void Dispose()
        {
            Logger.DebugLog("SEPC.Plugin.Dispose()");

            // Close resources that persist outside of sessions
            Profiler.Close();
            Logger.Close();
        }

        private void TryAttachSession()
        {
            if (Sandbox.Game.World.MySession.Static == null || MyAPIGateway.Entities == null)
                return;

            Logger.DebugLog("Attaching MySession");
            Sandbox.Game.World.MySession.Static.RegisterComponentsFromAssembly(Assembly.GetExecutingAssembly(), true);
            MyAPIGateway.Entities.OnCloseAll += DetachSession;
            SessionAttached = true;
        }

        private void DetachSession()
        {
            Logger.DebugLog("Marking MySession detatched");
            MyAPIGateway.Entities.OnCloseAll -= DetachSession;
            SessionAttached = false;
        }
    }
}
