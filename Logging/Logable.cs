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
        public void Trace(string toLog, Severity.Level level = Severity.Level.TRACE, bool condition = true, [CallerFilePath] string filePath = null, [CallerMemberName] string member = null, [CallerLineNumber] int lineNumber = 0)
        {
            LogWithContext(toLog, level, condition, filePath, member, lineNumber);
        }

        [Conditional("TRACE")]
        public void Trace(Func<string> toLog, Severity.Level level = Severity.Level.TRACE, bool condition = true, [CallerFilePath] string filePath = null, [CallerMemberName] string member = null, [CallerLineNumber] int lineNumber = 0)
        {
            LogWithContext(toLog.Invoke(), level, condition, filePath, member, lineNumber);
        }

        [Conditional("TRACE")]
        public void Entered([CallerFilePath] string filePath = null, [CallerMemberName] string member = null, [CallerLineNumber] int lineNumber = 0)
        {
            LogWithContext("Entered", Severity.Level.TRACE, true, filePath, member, lineNumber);
        }

        [Conditional("TRACE")]
        public void Leaving([CallerFilePath] string filePath = null, [CallerMemberName] string member = null, [CallerLineNumber] int lineNumber = 0)
        {
            LogWithContext("Leaving", Severity.Level.TRACE, true, filePath, member, lineNumber);
        }

        [Conditional("DEBUG")]
        public void Debug(string toLog, Severity.Level level = Severity.Level.TRACE, bool condition = true, [CallerFilePath] string filePath = null, [CallerMemberName] string member = null, [CallerLineNumber] int lineNumber = 0)
        {
            LogWithContext(toLog, level, condition, filePath, member, lineNumber);
        }

        [Conditional("DEBUG")]
        public void Debug(Func<string> toLog, Severity.Level level = Severity.Level.DEBUG, bool condition = true, [CallerFilePath] string filePath = null, [CallerMemberName] string member = null, [CallerLineNumber] int lineNumber = 0)
        {
            LogWithContext(toLog.Invoke(), level, condition, filePath, member, lineNumber);
        }

        [Conditional("PROFILE")]
        public void Profile(string toLog, Severity.Level level = Severity.Level.TRACE, bool condition = true, [CallerFilePath] string filePath = null, [CallerMemberName] string member = null, [CallerLineNumber] int lineNumber = 0)
        {
            LogWithContext(toLog, level, condition, filePath, member, lineNumber);
        }

        public void Log(string toLog, Severity.Level level = Severity.Level.INFO, bool condition = true, [CallerFilePath] string filePath = null, [CallerMemberName] string member = null, [CallerLineNumber] int lineNumber = 0)
        {
            LogWithContext(toLog, level, condition, filePath, member, lineNumber);
        }

        public void Error(string toLog, bool condition = true, [CallerFilePath] string filePath = null, [CallerMemberName] string member = null, [CallerLineNumber] int lineNumber = 0)
        {
            LogWithContext(toLog, Severity.Level.ERROR, condition, filePath, member, lineNumber);
        }

        public void Error(Exception error, bool condition = true, [CallerFilePath] string filePath = null, [CallerMemberName] string member = null, [CallerLineNumber] int lineNumber = 0)
        {
            Error(error.ToString(), condition, filePath, member, lineNumber);
        }

        private void LogWithContext(string message, Severity.Level level, bool condition, string filePath, string member, int lineNumber)
        {
            Logger.Log(message, level, Context, PrimaryState, SecondaryState, condition, CallerAssembly, filePath, member, lineNumber);
        }
    }
}