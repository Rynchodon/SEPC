using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using SEPC.Components.Descriptions;
using SEPC.Logging;

namespace SEPC.Components
{
    /// <summary>
    /// Persists while game is open, through multiple sessions.
    /// Holds descriptions of the Components defined in loaded assemblies.
    /// Assemblies can specify groups to load on session init and if events should be profiled / debugged.
    /// </summary>
    public static class ComponentRegistrar
    {
        private static readonly Dictionary<Assembly, ComponentDescriptionCollection> ComponentsByAssembly = new Dictionary<Assembly, ComponentDescriptionCollection>();
        private static readonly Dictionary<Assembly, int> InitGroupsByAssembly = new Dictionary<Assembly, int>();

        #region Registration

        /// <summary>
        /// Defines a particular group to load from the given assembly when the session starts.
        /// Should be called once within game instance before a session is loaded, e.g. within IPlugin.Init().
        /// Assembly defaults to CallingAssembly, but that's PluginLoader inside a released IPlugin so you usually must provide it.
        /// </summary>
        public static void LoadOnInit(int groupId, Assembly assembly = null)
        {
            assembly = (assembly != null) ? assembly : Assembly.GetCallingAssembly();
            Logger.Log($"LoadOnInit group: {groupId}, assembly: {Assembly.GetCallingAssembly().GetName().FullName}");
            InitGroupsByAssembly[Assembly.GetCallingAssembly()] = groupId;
        }

        /// <summary>
        /// Defines all the components within the given assembly and stores them for use within a session.
        /// Should be called once within game instance before a session is loaded, e.g. within IPlugin.Init().
        /// Assembly defaults to CallingAssembly, but that's PluginLoader inside a released IPlugin so you usually must provide it.
        /// </summary>
        public static void AddComponents(Assembly assembly = null)
        {
            assembly = (assembly != null) ? assembly : Assembly.GetCallingAssembly();
            Logger.Log($"AddComponents assembly: {assembly.GetName().FullName}");
            var collection = ComponentDescriptionCollection.FromAssembly(assembly);
            ComponentsByAssembly.Add(assembly, collection);
        }

        #endregion
        #region Query

        /// <summary>
        /// Gets a particular group registered from an Assembly.
        /// Used by classes that instantiate and manage components, i.e. UpdateManager.
        /// Allows plugins to delay initializing groups of components until their dependencies are ready.
        /// </summary>
        public static ComponentDescriptionCollection GetComponents(Assembly assembly, int groupId)
        {
            Logger.Log($"GetComponents group: {groupId}, assembly: {assembly.GetName().FullName}");
            ComponentDescriptionCollection components;
            if (!ComponentsByAssembly.TryGetValue(assembly, out components))
                throw new Exception("No components registered for " + assembly.GetName().Name);
            return components.SelectGroup(groupId);
        }

        /// <summary>
        /// Gets all component groups whose Assemblies have marked them to LoadOnInit.
        /// Used by classes that instantiate and manage components, i.e. UpdateManager.
        /// </summary>
        public static List<ComponentDescriptionCollection> GetInitComponents()
        {
            return InitGroupsByAssembly.Select((kvp) => GetComponents(kvp.Key, kvp.Value)).ToList();
        }

        #endregion
    }
}
