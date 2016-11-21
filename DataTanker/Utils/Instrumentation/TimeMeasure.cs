namespace DataTanker.Utils.Instrumentation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;

    /// <summary>
    /// Simple thread-safe time measuring tool.
    /// <para>
    /// Usage: 
    /// TimeMeasure.Start("measure1"); 
    /// //do something 
    /// TimeMeasure.Stop("measure1"); 
    /// Console.WriteLine(TimeMeasure.Result("measure1"));
    /// </para>
    /// </summary>
    public static class TimeMeasure
    {
        [ThreadStatic]
        private static Stopwatch _unnamedWatch;

        private static Stopwatch UnnamedWatch => _unnamedWatch ?? (_unnamedWatch = new Stopwatch());

        [ThreadStatic]
        private static Dictionary<string, Stopwatch> Watches;

        private static readonly Dictionary<string, long> Ticks = new Dictionary<string, long>();

        private static long UnnamedTicks;

        /// <summary>
        /// Gets the names of the performed measures
        /// </summary>
        public static string[] MeasureNames => Ticks.Keys.ToArray();

        /// <summary>
        /// Gets the result of unnamed measure.
        /// </summary>
        /// <returns>The result of measure</returns>
        public static TimeSpan Result()
        {
            return TimeSpan.FromSeconds((double)UnnamedTicks / Stopwatch.Frequency);
        }

        /// <summary>
        /// Gets the result of named measure.
        /// </summary>
        /// <param name="measureName">The name of measure</param>
        /// <returns>The result of measure</returns>
        public static TimeSpan Result(string measureName)
        {
            return TimeSpan.FromSeconds((double)Ticks[measureName] / Stopwatch.Frequency);
        }

        /// <summary>
        /// Starts the measure with the specified name
        /// </summary>
        /// <param name="measureName">The name of measure</param>
        public static void Start(string measureName)
        {
            if (Watches == null)
                Watches = new Dictionary<string, Stopwatch>();

            if (!Watches.ContainsKey(measureName))
            {
                Watches.Add(measureName, new Stopwatch());
                lock (Ticks)
                    if (!Ticks.ContainsKey(measureName))
                        Ticks.Add(measureName, 0);
            }

            Watches[measureName].Restart();
        }

        /// <summary>
        /// Stops the measure with the specified name
        /// </summary>
        /// <param name="measureName">The name of measure</param>
        public static void Stop(string measureName)
        {
            // do not perform any checks

            var w = Watches[measureName];
            w.Stop();

            lock (Ticks)
                Ticks[measureName] += w.ElapsedTicks;
        }

        /// <summary>
        /// Resets the measure result
        /// </summary>
        /// <param name="measureName">The name of measure</param>
        public static void Reset(string measureName)
        {
            lock (Ticks)
                Ticks[measureName] = 0; 
        }

        /// <summary>
        /// Resets the measure result
        /// </summary>
        public static void Reset()
        {
            UnnamedTicks = 0;
        }

        /// <summary>
        /// Resets all measures
        /// </summary>
        public static void ResetAll()
        {
            lock (Ticks)
                Ticks.Keys.ToList().ForEach(key => Ticks[key] = 0);
        }

        /// <summary>
        /// Starts unnamed measure
        /// </summary>
        public static void Start()
        {
            UnnamedWatch.Restart();
        }

        /// <summary>
        /// Stops unnamed measure
        /// </summary>
        public static void Stop()
        {
            UnnamedWatch.Stop();
            Interlocked.Add(ref UnnamedTicks, UnnamedWatch.ElapsedTicks);
        }

        /// <summary>
        /// Clears all named measures and resets unnamed measure.
        /// </summary>
        public static void Clear()
        {
            lock (Ticks)
            {
                Ticks.Clear();
                Watches.Clear();
                UnnamedTicks = 0;
                _unnamedWatch = null;
            }
        }
    }
}
