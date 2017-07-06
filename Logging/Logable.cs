using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

using VRage.Game.ModAPI;
using VRage.ModAPI;

using SEPC.Extensions;

namespace SEPC.Logging
{

    /// <summary>
    /// Classes define a property that generates a context-aware Logable
    /// </summary>
    public struct Logable
    {

        public readonly string Context, PrimaryState, SecondaryState;
        public readonly Assembly CallerAssembly;

        public Logable(string context, string primaryState = null, string secondaryState = null)
        {
            CallerAssembly = Assembly.GetCallingAssembly();
            Context = context;
            PrimaryState = primaryState;
            SecondaryState = secondaryState;
        }

        public Logable(IMyEntity entity)
        {
            CallerAssembly = Assembly.GetCallingAssembly();

            if (entity == null)
            {
                Context = PrimaryState = SecondaryState = null;
            }
            else if (entity is IMyCubeBlock)
            {
                IMyCubeBlock block = (IMyCubeBlock)entity;
                Context = block.CubeGrid.NameWithId();
                PrimaryState = block.DefinitionDisplayNameText;
                SecondaryState = block.NameWithId();
            }
            else
            {
                Context = entity.NameWithId();
                PrimaryState = SecondaryState = null;
            }
        }

        public Logable(IMyEntity entity, string secondaryState)
        {
            CallerAssembly = Assembly.GetCallingAssembly();
            SecondaryState = secondaryState;

            if (entity == null)
            {
                Context = PrimaryState = null;
            }
            else if (entity is IMyCubeBlock)
            {
                IMyCubeBlock block = (IMyCubeBlock)entity;
                Context = block.CubeGrid.NameWithId();
                PrimaryState = block.NameWithId();
            }
            else
            {
                Context = entity.NameWithId();
                PrimaryState = null;
            }
        }

        [Conditional("TRACE")]
        public void TraceLog(string toLog, Severity.Level level = Severity.Level.TRACE, bool condition = true, [CallerFilePath] string filePath = null, [CallerMemberName] string member = null, [CallerLineNumber] int lineNumber = 0)
        {
            LogWithContext(toLog, level, condition, filePath, member, lineNumber);
        }

        [Conditional("TRACE")]
        public void TraceLog(Func<string> toLog, Severity.Level level = Severity.Level.TRACE, bool condition = true, [CallerFilePath] string filePath = null, [CallerMemberName] string member = null, [CallerLineNumber] int lineNumber = 0)
        {
            LogWithContext(toLog.Invoke(), level, condition, filePath, member, lineNumber);
        }

        [Conditional("DEBUG")]
        public void DebugLog(string toLog, Severity.Level level = Severity.Level.TRACE, bool condition = true, [CallerFilePath] string filePath = null, [CallerMemberName] string member = null, [CallerLineNumber] int lineNumber = 0)
        {
            LogWithContext(toLog, level, condition, filePath, member, lineNumber);
        }

        [Conditional("DEBUG")]
        public void DebugLog(Func<string> toLog, Severity.Level level = Severity.Level.TRACE, bool condition = true, [CallerFilePath] string filePath = null, [CallerMemberName] string member = null, [CallerLineNumber] int lineNumber = 0)
        {
            LogWithContext(toLog.Invoke(), level, condition, filePath, member, lineNumber);
        }

        [Conditional("PROFILE")]
        public void ProfileLog(string toLog, Severity.Level level = Severity.Level.TRACE, bool condition = true, [CallerFilePath] string filePath = null, [CallerMemberName] string member = null, [CallerLineNumber] int lineNumber = 0)
        {
            LogWithContext(toLog, level, condition, filePath, member, lineNumber);
        }

        public void AlwaysLog(string toLog, Severity.Level level = Severity.Level.TRACE, bool condition = true, [CallerFilePath] string filePath = null, [CallerMemberName] string member = null, [CallerLineNumber] int lineNumber = 0)
        {
            LogWithContext(toLog, level, condition, filePath, member, lineNumber);
        }

        [Conditional("TRACE")]
        public void Entered([CallerFilePath] string filePath = null, [CallerMemberName] string member = null, [CallerLineNumber] int lineNumber = 0)
        {
            LogWithContext("entered", Severity.Level.TRACE, true, filePath, member, lineNumber);
        }

        [Conditional("TRACE")]
        public void Leaving([CallerFilePath] string filePath = null, [CallerMemberName] string member = null, [CallerLineNumber] int lineNumber = 0)
        {
            LogWithContext("leaving", Severity.Level.TRACE, true, filePath, member, lineNumber);
        }

        private void LogWithContext(string message, Severity.Level level, bool condition, string filePath, string member, int lineNumber)
        {
            Logger.Log(message, level, Context, PrimaryState, SecondaryState, condition, CallerAssembly, filePath, member, lineNumber);
        }

    }
}