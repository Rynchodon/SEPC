using System.Reflection;

using SEPC.Diagnostics;

namespace SEPC.Extensions
{
    public static class AssemblyExtensions
    {
        public static bool IsDebugDefined(this Assembly assembly)
        {
            return SymbolRegistrar.IsDebugDefined(assembly);
        }

        public static bool IsProfileDefined(this Assembly assembly)
        {
            return SymbolRegistrar.IsProfileDefined(assembly);
        }
    }
}
