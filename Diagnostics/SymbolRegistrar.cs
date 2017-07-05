using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

//using VRage.Utils;

namespace SEPC.Diagnostics
{
    /// <summary>
    /// Persists while game is open, through multiple sessions.
    /// Holds compilation symbols from loaded assemblies.
    /// Assemblies must inform the registrar of their symbols using the provided methods.
    /// </summary>
    public static class SymbolRegistrar
    {
        private static readonly HashSet<Assembly> AssembliesToDebug = new HashSet<Assembly>();
        private static readonly HashSet<Assembly> AssembliesToProfile = new HashSet<Assembly>();

        /// <summary>
        /// Should be called early, before IsDebugDefined is requested.
        /// </summary>
        [Conditional("DEBUG")]
        public static void SetDebugIfDefined()
        {
            AssembliesToDebug.Add(Assembly.GetCallingAssembly());
            //MyLog.Default.WriteLine($"SEPC - SetDebugIfDefined - {Assembly.GetCallingAssembly().GetName().Name}");
        }

        /// <summary>
        /// Should be called early, before IsProfileDefined is requested.
        /// </summary>
        [Conditional("PROFILE")]
        public static void SetProfileIfDefined()
        {
            AssembliesToProfile.Add(Assembly.GetCallingAssembly());
            //MyLog.Default.WriteLine($"SEPC - SetProfileIfDefined - {Assembly.GetCallingAssembly().GetName().Name}");
        }

        public static bool IsDebugDefined(Assembly assembly)
        {
            //MyLog.Default.WriteLine($"AssembliesToDebug.Contains({assembly.GetName().Name}) ? {AssembliesToDebug.Contains(assembly)}");
            return AssembliesToDebug.Contains(assembly);
        }

        public static bool IsProfileDefined(Assembly assembly)
        {
            //MyLog.Default.WriteLine($"AssembliesToProfile.Contains({assembly.GetName().Name}) ? {AssembliesToProfile.Contains(assembly)}");
            return AssembliesToProfile.Contains(assembly);
        }
    }
}
