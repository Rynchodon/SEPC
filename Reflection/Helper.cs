using System;
using System.Linq;
using System.Reflection;

using Sandbox.Game.World;
using Sandbox.ModAPI;
using VRage.Collections;
using VRage.Game.Components;

using SEPC.Logging;

namespace SEPC.Reflection
{
    public static class ReflectionHelper
    {
        public static MySessionComponentBase FindModSessionComponent(string modName, ulong publishedId, string modScriptsFolder, string typeName)
        {
            var qualifyingMods = MyAPIGateway.Session.Mods.Where(mod => mod.PublishedFileId == publishedId || mod.Name == modName);
            if (qualifyingMods.Count() == 0)
            {
                Logger.Log($"No mods exist with name {modName} or publishedId {publishedId}.", Severity.Level.ERROR);
                return null;
            }

            MySessionComponentBase result;
            foreach (var mod in qualifyingMods)
            {
                //Logger.DebugLog("Iterating mod - FriendlyName: " + mod.FriendlyName + ", Name: " + mod.Name + ", Published ID: " + mod.PublishedFileId);
                result = FindSessionComponentInLoadedMod(mod.Name, modScriptsFolder, typeName);
                if (result != null)
                    return result;
            }

            Logger.Log($"Failed to find component for {typeName} in {qualifyingMods.Count()} qualifying mods.", Severity.Level.ERROR);
            return null;
        }

        private static MySessionComponentBase FindSessionComponentInLoadedMod(string modName, string modScriptsFolder, string typeName)
        {
            string assemblyName = $"{modName}_{modScriptsFolder}, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";

            var sessionComponents = (CachingDictionary<Type, MySessionComponentBase>)GetInstanceField(MySession.Static, "m_sessionComponents");
            if (sessionComponents == null)
            {
                Logger.Log("Failed to get m_sessionComponents", Severity.Level.ERROR);
                return null;
            }

            // Type.GetType(string typeName) with the fully qualified name doesn't seem to work, maybe it's something to do with CodeDOM
            // there can be more than one assembly with the right name
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies().Where(x => x.FullName == assemblyName))
            {
                Type componentType = assembly.GetType(typeName);
                if (componentType == null)
                {
                    Logger.DebugLog($"Type: {typeName} not present in Assembly: {assemblyName}, ");
                    continue;
                }

                MySessionComponentBase component;
                if (!sessionComponents.TryGetValue(componentType, out component))
                {
                    Logger.DebugLog($"Type: {typeName} is not loaded into Session Components");
                    continue;
                }

                return component;
            }

            return null;
        }

        public static object GetInstanceField(object instance, string fieldName)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null)
                throw new Exception($"{fieldName} is not defined.");
            return field.GetValue(instance);
        }

        public static void SetInstanceField(object instance, string fieldName, object value)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null)
                throw new Exception($"{fieldName} is not defined.");
            field.SetValue(instance, value);
        }
    }
}
