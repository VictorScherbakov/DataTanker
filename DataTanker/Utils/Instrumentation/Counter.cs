namespace DataTanker.Utils.Instrumentation
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    /// <summary>
    /// Simple thread-safe counting tool.
    /// <para>
    /// Usage: 
    /// Counter.Inc("counter1"); 
    /// or
    /// Counter.Inc("counter1", value); 
    /// Console.WriteLine(Counter.Result("counter1"));
    /// </para>
    /// </summary>
    public static class Counter
    {
        private static readonly Dictionary<string, long> Counters = new Dictionary<string, long>();

        private static long UnnamedCounter;

        /// <summary>
        /// Gets the names of counters
        /// </summary>
        public static string[] CounterNames
        {
            get { return Counters.Keys.ToArray(); }
        }

        /// <summary>
        /// Gets the result of unnamed measure.
        /// </summary>
        /// <returns>The result of measure</returns>
        public static long Result()
        {
            return UnnamedCounter;
        }

        /// <summary>
        /// Gets the result of named measure.
        /// </summary>
        /// <param name="counterName">The name of measure</param>
        /// <returns>The result of measure</returns>
        public static long Result(string counterName)
        {
            return Counters[counterName];
        }

        /// <summary>
        /// Increases named counter by one
        /// </summary>
        /// <param name="counterName">The name of the counter</param>
        public static void Inc(string counterName)
        {
            if (!Counters.ContainsKey(counterName))
            {
                lock (Counters)
                    if (!Counters.ContainsKey(counterName))
                        Counters.Add(counterName, 0);
            }

            lock (Counters)
                Counters[counterName]++;
        }

        /// <summary>
        /// Increases named counter by specified value
        /// </summary>
        /// <param name="counterName">The name of the counter</param>
        /// <param name="value">Increment value</param>
        public static void Inc(string counterName, long value)
        {
            if (!Counters.ContainsKey(counterName))
            {
                lock (Counters)
                    if (!Counters.ContainsKey(counterName))
                        Counters.Add(counterName, 0);
            }

            lock (Counters)
                Counters[counterName] += value;
        }

        /// <summary>
        /// Increases unnamed counter by specified value
        /// </summary>
        /// <param name="value">Increment value</param>
        public static void Inc(long value)
        {
            Interlocked.Add(ref UnnamedCounter, value);
        }

        /// <summary>
        /// Increases unnamed counter by one
        /// </summary>
        public static void Inc()
        {
            Interlocked.Increment(ref UnnamedCounter);
        }

        /// <summary>
        /// Resets the counter
        /// </summary>
        /// <param name="counterName">The name of measure</param>
        public static void Reset(string counterName)
        {
            lock (Counters)
                Counters[counterName] = 0;
        }

        /// <summary>
        /// Resets the unnamed counter
        /// </summary>
        public static void Reset()
        {
            UnnamedCounter = 0;
        }

        /// <summary>
        /// Resets all counters
        /// </summary>
        public static void ResetAll()
        {
            lock (Counters)
                Counters.Keys.ToList().ForEach(key => Counters[key] = 0);
        }

        /// <summary>
        /// Starts unnamed counter
        /// </summary>
        public static void Start()
        {
            UnnamedCounter = 0;
        }

        /// <summary>
        /// Clears all named counters and resets unnamed counter.
        /// </summary>
        public static void Clear()
        {
            lock (Counters)
            {
                Counters.Clear();
                UnnamedCounter = 0;
            }
        }
    }
}
