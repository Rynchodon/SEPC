using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

using VRage;
using VRage.Library.Utils;

using SEPC.Collections;
using SEPC.Files;
using SEPC.Logging;
using SEPC.World;

namespace SEPC.Diagnostics
{
    /// <summary>
    /// Measures the execution time of methods.
    /// Keep in mind that this will include time spent waiting on a lock.
    /// Sum counts the time spent in the outermost blocks.
    /// </summary>
    public class Profiler
    {
        private struct Block
        {
            public Assembly CallerAssembly; // tells us which mod storage to use when writing the result
            public string CallerMemberName, CallerFilePath; // tells us if it's closed from the same location
            public string ProfiledMemberName, ProfiledFilePath; // identifies the profiled location
            public long StartedAt; // the entire reason the block exists

            public string ProfiledId
            {
                get { return Path.GetFileName(ProfiledFilePath) + ',' + ProfiledMemberName; }
            }
        }

        private class BlockResult
        {
            public long Invokes;
            public MyTimeSpan TimeSpent = MyTimeSpan.Zero;
            public MyTimeSpan WorstTime = MyTimeSpan.Zero;

            public void BlockClosed(MyTimeSpan timeSpent, bool addTimeSpent = true)
            {
                Invokes++;
                if (addTimeSpent)
                    TimeSpent += timeSpent;
                if (timeSpent > WorstTime)
                    WorstTime = timeSpent;
            }

            public string ToCSV(double totalProfileTicks, double totalUpdateTicks)
            {
                return String.Join(",",
                    TimeSpent.Seconds,
                    Invokes,
                    TimeSpent.Seconds / Invokes,
                    WorstTime.Seconds,
                    TimeSpent.Ticks / totalProfileTicks,
                    TimeSpent.Ticks / totalUpdateTicks
                );
            }
        }

        private class ModProfiler
        {
            private readonly Assembly Assembly;
            private readonly Dictionary<string, BlockResult> ResultsByName = new Dictionary<string, BlockResult>();
            private readonly BlockResult ResultsTotal = new BlockResult();
            private readonly FastResourceLock ResultsLock = new FastResourceLock();

            public ModProfiler(Assembly assembly)
            {
                Assembly = assembly;
            }

            public void BlockClosed(Block block, MyTimeSpan timeSpent, bool lastBlockOfStack)
            {
                using (ResultsLock.AcquireExclusiveUsing())
                {
                    BlockResult blockResult;
                    if (!ResultsByName.TryGetValue(block.ProfiledId, out blockResult))
                    {
                        blockResult = new BlockResult();
                        ResultsByName.Add(block.ProfiledId, blockResult);
                    }
                    blockResult.BlockClosed(timeSpent);
                    ResultsTotal.BlockClosed(timeSpent, addTimeSpent: lastBlockOfStack);
                }
            }

            public void Write()
            {
                using (TextWriter writer = new ModFile("Profiler", "csv", Assembly, 10).GetTextWriter())
                {
                    using (ResultsLock.AcquireExclusiveUsing())
                    {
                        var profileTicks = ResultsTotal.TimeSpent.Ticks;
                        var updateTicks = Time.UpdateTicks;

                        writer.WriteLine("Class Name, Method Name, Seconds, Invokes, Seconds per Invoke, Worst Time, Ratio of Sum, Ratio of Game Time");
                        WriteRow(writer, "Game Time,", new BlockResult() { TimeSpent = new MyTimeSpan(updateTicks) }, profileTicks, updateTicks);
                        WriteRow(writer, "Sum,", ResultsTotal, profileTicks, updateTicks);
                        foreach (var pair in ResultsByName)
                            WriteRow(writer, pair.Key, pair.Value, profileTicks, updateTicks);
                    }
                }
            }

            private static void WriteRow(TextWriter writer, string blockName, BlockResult result, double totalProfileTicks, double totalUpdateTicks)
            {
                writer.WriteLine(blockName + "," + result.ToCSV(totalProfileTicks, totalUpdateTicks));
            }
        }

        private static readonly LockedDictionary<Assembly, ModProfiler> ModProfilers = new LockedDictionary<Assembly, ModProfiler>();

        [ThreadStatic]
        private static readonly Stack<Block> BlockStack = new Stack<Block>();

        #region Thread-safe accessors

        /// <summary>
        /// Profiles an action.
        /// </summary>
        [Conditional("PROFILE")]
        public static void ProfileActionConditional(Action action, Assembly callerAssembly = null)
        {
            callerAssembly = (callerAssembly != null) ? callerAssembly : Assembly.GetCallingAssembly();
            ProfileAction(action, callerAssembly);
        }

        /// <summary>
        /// Profiles an action.
        /// </summary>
        public static void ProfileAction(Action action, Assembly callerAssembly = null)
        {
            callerAssembly = (callerAssembly != null) ? callerAssembly : Assembly.GetCallingAssembly();
            StartProfileBlockForAction(action, callerAssembly);
            action.Invoke();
            EndProfileBlock();
        }

        /// <summary>
        /// Start a profiling block. Must be ended.
        /// </summary>
        [Conditional("PROFILE")]
        public static void StartProfileBlockConditional(
            Assembly callerAssembly = null,
            [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null,
            [CallerMemberName] string profiledMemberName = null, [CallerFilePath] string profiledFilePath = null
        )
        {
            callerAssembly = (callerAssembly != null) ? callerAssembly : Assembly.GetCallingAssembly();
            StartProfileBlock(callerAssembly, callerMemberName, callerFilePath, profiledMemberName, profiledFilePath);
        }

        public static void StartProfileBlockForAction(
            Action action, Assembly callerAssembly = null,
            [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
        {
            callerAssembly = (callerAssembly != null) ? callerAssembly : Assembly.GetCallingAssembly();
            StartProfileBlock(callerAssembly, callerMemberName, callerFilePath, action.Method.Name, action.Target == null ? "N/A" : action.Target.GetType().Name);
        }

        /// <summary>
        /// Start a profiling block. Must be ended.
        /// </summary>
        public static void StartProfileBlock(
            Assembly callerAssembly = null,
            [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null,
            [CallerMemberName] string profiledMemberName = null, [CallerFilePath] string profiledFilePath = null
        )
        {
            callerAssembly = (callerAssembly != null) ? callerAssembly : Assembly.GetCallingAssembly();

            if (BlockStack.Count > 1000)
            {
                Logger.Log("BlockStack is too large:");
                foreach (var item in BlockStack)
                    Logger.Log("Item: " + item.ProfiledId, Severity.Level.ERROR);
                throw new OverflowException("BlockStack is too large.");
            }

            BlockStack.Push(new Block()
            {
                CallerAssembly = callerAssembly,
                CallerFilePath = callerFilePath,
                CallerMemberName = callerMemberName,
                ProfiledFilePath = profiledFilePath,
                ProfiledMemberName = profiledMemberName,
                StartedAt = Stopwatch.GetTimestamp(),
            });
        }

        /// <summary>
        /// End a profiling block, must be invoked even if an exception is thrown after block started.
        /// </summary>
        [Conditional("PROFILE")]
        public static void EndProfileBlockConditional([CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
        {
            EndProfileBlock(callerMemberName, callerFilePath);
        }

        /// <summary>
        /// End a profiling block, must be invoked even if an exception is thrown after block started.
        /// </summary>
        public static void EndProfileBlock([CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
        {
            var block = BlockStack.Pop();
            var elapsed = new MyTimeSpan(Stopwatch.GetTimestamp() - block.StartedAt);

            string startedIn = Path.GetFileName(block.CallerFilePath) + ',' + block.CallerMemberName;
            string endingIn = Path.GetFileName(callerFilePath) + ',' + callerMemberName;
            if (startedIn != endingIn)
                throw new Exception($"Block was started in {startedIn} but ended in {endingIn}");

            ModProfiler profiler;
            if (!ModProfilers.TryGetValue(block.CallerAssembly, out profiler))
            {
                profiler = new ModProfiler(block.CallerAssembly);
                ModProfilers.Add(block.CallerAssembly, profiler);
            }

            profiler.BlockClosed(block, elapsed, BlockStack.Count == 0);
        }

        #endregion

        /// <summary>
        /// Write all the profile data to .csv files.
        /// Should be called at the end of every session and when the game closes.
        /// </summary>
        public static void Close()
        {
            foreach (var modResult in ModProfilers.Values)
                modResult.Write();
            ModProfilers.Clear();
        }
    }
}