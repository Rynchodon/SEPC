using System;
using System.Collections.Generic;
using System.Reflection;

using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;

using SEPC.Collections;
using SEPC.Components.Stores;
using SEPC.Logging;

namespace SEPC.Components
{
    /// <summary>
    /// Holds a ComponentStore and propagates events, updates, and entity changes to it.
    /// Provides thread-safe component registration and event raising.
    /// Receives events from MySession.
    /// </summary>
    public class ComponentSession
    {
        private enum SessionStatus : byte { NotInitialized, Initialized, Terminated }

        private static ComponentCollectionStore ComponentStore;
        private static LockedDeque<Action> ExternalRegistrations;
        private static SessionStatus Status;

        #region Registration and event raising

        /// <param name="unregisterOnClosing">Leave as null if you plan on manually unregistering</param>
        public static void RegisterUpdateHandler(uint frequency, Action toInvoke, IMyEntity unregisterOnClosing = null)
        {
            var assembly = Assembly.GetCallingAssembly();
            ExternalRegistrations?.AddTail(() => {
                ComponentStore.AddUpdateHandler(frequency, toInvoke, assembly);
                if (unregisterOnClosing != null)
                    unregisterOnClosing.OnClosing += (entity) => ComponentStore.RemoveUpdateHandler(frequency, toInvoke);
            });
        }

        public static void UnregisterUpdateHandler(uint frequency, Action toInvoke)
        {
            ExternalRegistrations?.AddTail(() => {
                ComponentStore.RemoveUpdateHandler(frequency, toInvoke);
            });
        }

        public static void RegisterComponentGroup(int group)
        {
            var collection = ComponentRegistrar.GetComponents(Assembly.GetCallingAssembly(), group);
            ExternalRegistrations?.AddTail(() => {
                ComponentStore.TryAddCollection(collection);
            });
        }

        public static void RaiseSessionEvent(string eventName)
        {
            ExternalRegistrations?.AddTail(() => {
                ComponentStore.RaiseSessionEvent(eventName); ;
            });
        }

        /// <summary>
        /// NOT thread-safe, but allows us to immediately raise an event instead of deferring it to the next frame.
        /// This is useful for events that would be propagated too late if delayed til the next frame, like UpdatingStopped (next frame comes after UpdatingResumed).
        /// </summary>
        /// <param name="eventName"></param>
        public static void RaiseSessionEventImmediately(string eventName)
        {
            ComponentStore.RaiseSessionEvent(eventName);
        }

        public static void RaiseEntityEvent(string eventName, IMyEntity entity)
        {
            ExternalRegistrations?.AddTail(() => {
                ComponentStore.RaiseEntityEvent(eventName, entity); ;
            });
        }

        #endregion
        #region Lifecycle

        /// <summary>
        /// Resets the status for a new session.
        /// Called by SEPC.App.MySession only.
        /// </summary>
        public static void Open()
        {
            Logger.DebugLog("Components.Session.SessionOpened()");
            Status = SessionStatus.NotInitialized;
        }

        /// <summary>
        /// Initializes once MySession is ready, runs external actions, and propagates updates to the store.
        /// Called by SEPC.App.MySession only.
        /// </summary>
        public static void Update()
        {
            try
            {
                if (Status == SessionStatus.Terminated)
                    return;

                if (Status == SessionStatus.NotInitialized)
                {
                    TryInitialize();
                    return;
                }

                if (ExternalRegistrations.Count != 0)
                    ExternalRegistrations.PopHeadInvokeAll();

                ComponentStore.Update();
            }
            catch (Exception error)
            {
                Logger.Log("Error: " + error, Severity.Level.FATAL);
                Status = SessionStatus.Terminated;
            }
        }

        /// <summary>
        /// Releases all resources and marks as terminated.
        /// Called by SEPC.App.MySession only.
        /// </summary>
        public static void Close()
        {
            Logger.DebugLog("Terminating");

            if (ComponentStore != null)
                ComponentStore.RaiseSessionEvent(ComponentEventNames.SessionClose);

            MyAPIGateway.Entities.OnEntityAdd -= EntityAdded;

            // clear fields in case SE doesn't clean up properly
            ComponentStore = null;
            ExternalRegistrations = null;

            Status = SessionStatus.Terminated;
        }

        private static void TryInitialize()
        {
            // return unless session ready
            if (MyAPIGateway.CubeBuilder == null || MyAPIGateway.Entities == null || MyAPIGateway.Multiplayer == null || MyAPIGateway.Parallel == null
                || MyAPIGateway.Players == null || MyAPIGateway.Session == null || MyAPIGateway.TerminalActionsHelper == null || MyAPIGateway.Utilities == null ||
                (!MyAPIGateway.Multiplayer.IsServer && MyAPIGateway.Session.Player == null))
                return;

            var runningOn = !MyAPIGateway.Multiplayer.MultiplayerActive ? RunLocation.Both : (MyAPIGateway.Multiplayer.IsServer ? RunLocation.Server : RunLocation.Client);

            Logger.DebugLog($"Initializing. RunningOn: {runningOn}, SessionName: {MyAPIGateway.Session.Name}, SessionPath: {MyAPIGateway.Session.CurrentPath}");

            ComponentStore = new ComponentCollectionStore(runningOn);
            ExternalRegistrations = new LockedDeque<Action>();

            MyAPIGateway.Entities.OnEntityAdd += EntityAdded;

            var entities = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(entities);
            foreach (IMyEntity entity in entities)
                EntityAdded(entity);

            foreach (var collection in ComponentRegistrar.GetInitComponents())
                ComponentStore.TryAddCollection(collection);

            Status = SessionStatus.Initialized;
        }

        #endregion
        #region Entity Add/Remove Handlers

        private static void EntityAdded(IMyEntity entity)
        {
            ComponentStore.AddEntity(entity);
            entity.OnClosing += EntityRemoved;

            // CubeBlocks aren't included in Entities.OnEntityAdd
            IMyCubeGrid asGrid = entity as IMyCubeGrid;
            if (asGrid != null && asGrid.Save)
            {
                var blocksInGrid = new List<IMySlimBlock>();
                asGrid.GetBlocks(blocksInGrid, slim => slim.FatBlock != null);
                foreach (IMySlimBlock slim in blocksInGrid)
                    BlockAdded(slim);

                asGrid.OnBlockAdded += BlockAdded;
            }
        }

        private static void BlockAdded(IMySlimBlock entity)
        {
            IMyCubeBlock asBlock = entity.FatBlock;
            if (asBlock != null)
                EntityAdded(asBlock);
        }

        private static void EntityRemoved(IMyEntity entity)
        {
            // Attached to entities themselves, so can be called after terminated
            if (ComponentStore != null)
                ComponentStore.RemoveEntity(entity);

            entity.OnClosing -= EntityRemoved;

            // CubeBlocks aren't included in Entities.OnEntityAdd
            IMyCubeGrid asGrid = entity as IMyCubeGrid;
            if (asGrid != null)
                asGrid.OnBlockAdded -= BlockAdded;
        }

        #endregion
    }
}
