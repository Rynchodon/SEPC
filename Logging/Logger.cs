using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

using Sandbox.ModAPI;
using VRage;
using VRage.Utils;

using SEPC.Collections;
using SEPC.Extensions;
using SEPC.Files;
using SEPC.HUD;
using SEPC.Threading;

namespace SEPC.Logging
{
    /// <summary>
    /// Provides thread-safe, conditional, annotated, rotated, mod-seggregated logging.
    /// </summary>
    /// <remarks>
    /// Applies the below Log4J format, which can be parsed by GamutLogViewer: 
    /// [%date][%level][%Thread][%Context][%FileName][%Member][%Line][%PriState][%SecState]%Message
    /// </remarks>
    public static class Logger
    {

        private struct LogItem
        {
            public readonly Assembly Assembly;
            public readonly string Context;
            public readonly string FileName;
            public readonly Severity.Level Level;
            public readonly int Line;
            public readonly string Member;
            public string Message; // Can be overwritten for LogItem reuse when breaking up a multiline message 
            public readonly string PrimaryState;
            public readonly string SecondaryState;
            public readonly string Thread;
            public readonly DateTime Time;

            public LogItem(
                string message, Severity.Level level,
                string context, string primaryState, string secondaryState,
                Assembly assembly, string filePath, string member, int line
            )
            {
                Assembly = assembly;
                Context = (context != null) ? context : String.Empty;
                FileName = Path.GetFileName(filePath);
                Level = level;
                Line = line;
                Member = (member != null) ? member : String.Empty;
                Message = (message != null) ? message : String.Empty;
                PrimaryState = (primaryState != null) ? primaryState : String.Empty;
                SecondaryState = (secondaryState != null) ? secondaryState : String.Empty;
                Thread = (ThreadTracker.ThreadName != null) ? ThreadTracker.ThreadName : ThreadTracker.ThreadNumber.ToString();
                Time = DateTime.Now;
            }
        }

        private class ModLog
        {
            public uint Lines;

            private TextWriter Writer;

            public ModLog(Assembly assembly)
            {
                Writer = new ModFile("log", "txt", assembly, 10).GetTextWriter();
            }

            public void WriteLine(StringBuilder line)
            {
                Writer.WriteLine(line);
                Writer.Flush();
                Lines++;
            }

            public void Close()
            {
                Writer.Flush();
                Writer.Close();
                Writer = null;
            }
        }

        private const int MAX_LINES_PER_FILE = 1000000;
        private static readonly char[] NEWLINE_CHARS = new char[] { '\n', '\r' };

        private static LockedDeque<LogItem> LogItems = new LockedDeque<LogItem>();
        private static LockedDictionary<Assembly, ModLog> ModLogs = new LockedDictionary<Assembly, ModLog>();
        private static StringBuilder StringCache = new StringBuilder();
        private static FastResourceLock Lock = new FastResourceLock();

        /// <summary>
        /// Closes all existing logs.
        /// Should be called at the end of each session and when game is closing.
        /// </summary>
        public static void Close()
        {
            Lock.AcquireExclusive();

            foreach (var assembly in ModLogs.Keys)
                Log("Closing log", Severity.Level.INFO, "SEPC.Logging", assembly: assembly);

            WriteItems();

            foreach (ModLog modLog in ModLogs.Values)
                modLog.Close();

            LogItems.Clear();
            ModLogs.Clear();
            StringCache.Clear();

            Lock.ReleaseExclusive();
        }

        #region General Logging

        [Conditional("TRACE")]
        public static void TraceLog(
            string message, 
            Severity.Level level = Severity.Level.TRACE, 
            string context = null, 
            string primaryState = null, 
            string secondaryState = null, 
            bool condition = true,
            Assembly assembly = null,
            [CallerFilePath] string filePath = null, 
            [CallerMemberName] string member = null, 
            [CallerLineNumber] int line = 0
        ) {
            assembly = (assembly != null) ? assembly : Assembly.GetCallingAssembly();
            Log(message, level, context, primaryState, secondaryState, condition, assembly, filePath, member, line);
        }

        [Conditional("DEBUG")]
        public static void DebugLog(
            string message,
            Severity.Level level = Severity.Level.TRACE,
            string context = null,
            string primaryState = null,
            string secondaryState = null,
            bool condition = true,
            Assembly assembly = null,
            [CallerFilePath] string filePath = null,
            [CallerMemberName] string member = null,
            [CallerLineNumber] int line = 0
        ) {
            assembly = (assembly != null) ? assembly : Assembly.GetCallingAssembly();
            Log(message, level, context, primaryState, secondaryState, condition, assembly, filePath, member, line);
        }

        [Conditional("PROFILE")]
        public static void ProfileLog(
            string message,
            Severity.Level level = Severity.Level.TRACE,
            string context = null,
            string primaryState = null,
            string secondaryState = null,
            bool condition = true,
            Assembly assembly = null,
            [CallerFilePath] string filePath = null,
            [CallerMemberName] string member = null,
            [CallerLineNumber] int line = 0
        ) {
            assembly = (assembly != null) ? assembly : Assembly.GetCallingAssembly();
            Log(message, level, context, primaryState, secondaryState, condition, assembly, filePath, member, line);
        }

        public static void Log(
            string message,
            Severity.Level level = Severity.Level.TRACE,
            string context = null,
            string primaryState = null,
            string secondaryState = null,
            bool condition = true,
            Assembly assembly = null,
            [CallerFilePath] string filePath = null,
            [CallerMemberName] string member = null,
            [CallerLineNumber] int line = 0
        ) {
            assembly = (assembly != null) ? assembly : Assembly.GetCallingAssembly();
            if (condition)
                Log(new LogItem(message, level, context, primaryState, secondaryState, assembly, filePath, member, line));
        }

        #endregion
        #region Error Logging

        /// <summary>
        /// Append the relevant portion of the stack to a StringBuilder.
        /// </summary>
        public static void AppendStack(StringBuilder builder, StackTrace stackTrace, params Type[] skipTypes)
        {
            builder.AppendLine("   Stack:");
            int totalFrames = stackTrace.FrameCount, frame = 0;
            while (true)
            {
                if (frame >= totalFrames)
                {
                    builder.AppendLine("Failed to skip frames, dumping all");
                    builder.Append(stackTrace);
                    builder.AppendLine();
                    return;
                }
                Type declaringType = stackTrace.GetFrame(frame).GetMethod().DeclaringType;

                foreach (Type t in skipTypes)
                    if (declaringType == t)
                    {
                        frame++;
                        continue;
                    }

                break;
            }

            bool appendedFrame = false;
            while (frame < totalFrames)
            {
                MethodBase method = stackTrace.GetFrame(frame).GetMethod();
                if (!method.DeclaringType.Namespace.StartsWith("Rynchodon"))
                    break;
                appendedFrame = true;
                builder.Append("   at ");
                builder.Append(method.DeclaringType);
                builder.Append('.');
                builder.Append(method);
                builder.AppendLine();
                frame++;
            }

            if (!appendedFrame)
            {
                builder.AppendLine("Did not append any frames, dumping all");
                builder.Append(stackTrace);
                builder.AppendLine();
                return;
            }
        }

        [Conditional("DEBUG")]
        public static void DebugLogCallStack(
            Severity.Level level = Severity.Level.TRACE,
            string context = null,
            string primaryState = null,
            string secondaryState = null,
            bool condition = true,
            Assembly assembly = null,
            [CallerFilePath] string filePath = null,
            [CallerMemberName] string member = null,
            [CallerLineNumber] int line = 0
        ) {
            if (condition)
            {
                StringBuilder builder = new StringBuilder(255);
                AppendStack(builder, new StackTrace(), typeof(Logger));
                var message = builder.ToString();

                assembly = (assembly != null) ? assembly : Assembly.GetCallingAssembly();
                Log(new LogItem(message, level, context, primaryState, secondaryState, assembly, filePath, member, line));
            }
        }

        #endregion
        #region Logging Internals

        private static void AppendWithBrackets(string append)
        {
            append = (append != null) ? append.Replace('[', '{').Replace(']', '}') : String.Empty;
            StringCache.Append('[');
            StringCache.Append(append);
            StringCache.Append(']');
        }

        private static ModLog GetModLog(Assembly assembly)
        {
            ModLog result;
            if (!ModLogs.TryGetValue(assembly, out result))
                ModLogs[assembly] = result = new ModLog(assembly);
            return result;
        }

        private static void Log(LogItem item) {
            if (item.Level <= Severity.Level.WARNING && item.Assembly.IsDebugDefined())
                Notification.Notify(item.Assembly.GetName().Name + " " + item.Level, 2000, item.Level);

            LogItems.AddTail(item);

            MyAPIGateway.Parallel?.StartBackground(Loop);
        }

        private static void Loop()
        {
            if (Lock.TryAcquireExclusive())
            {
                WriteItems();
                Lock.ReleaseExclusive();
            }
        }

        private static void WriteItems()
        {
            try
            {
                LogItem item;
                while (LogItems.TryPopHead(out item))
                    WriteItem(ref item);
            }
            catch (Exception error)
            {
                // is MyLog.Default thread-safe?
                //MainThread.TryOnMainThread(() => 
                //  MyLog.Default.WriteLine("SEPC - ERROR encountered while logging:");
                //  MyLog.Default.WriteLine(error);
                //);
                MyLog.Default.WriteLine("SEPC - ERROR encountered while logging:");
                MyLog.Default.WriteLine(error);
                StringCache.Clear();
            }
        }

        private static void WriteItem(ref LogItem item)
        {
            var modLog = GetModLog(item.Assembly);
            if (modLog.Lines >= MAX_LINES_PER_FILE)
                return;

            // Break at newlines
            if (item.Message.IndexOfAny(NEWLINE_CHARS) != -1)
            {
                var lines = item.Message.Split(NEWLINE_CHARS, StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines)
                {
                    item.Message = line;
                    WriteItem(ref item);
                }
                return;
            }

            // Build Line
            AppendWithBrackets(item.Time.ToString("yyyy-MM-dd HH:mm:ss,fff"));
            AppendWithBrackets(item.Level.ToString());
            AppendWithBrackets(item.Thread);
            AppendWithBrackets(item.Context);
            AppendWithBrackets(item.FileName.Substring(0, item.FileName.Length - 3));
            AppendWithBrackets(item.Member);
            AppendWithBrackets(item.Line.ToString());
            AppendWithBrackets(item.PrimaryState);
            AppendWithBrackets(item.SecondaryState);
            StringCache.Append(item.Message);

            // Write
            modLog.WriteLine(StringCache);
            StringCache.Clear();
        }

        #endregion

    }
}
