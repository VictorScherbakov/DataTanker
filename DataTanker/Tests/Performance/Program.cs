using System;
using System.IO;
using System.Linq;
using DataTanker.Utils.Instrumentation;

namespace Performance
{
    class Program
    {
        private static string _storagePath = "storage";

        private static void Cleanup()
        {
            string[] files = Directory.GetFiles(_storagePath);
            foreach (string file in files)
                File.Delete(file);
        }

        public static void ClearCurrentConsoleLine()
        {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentLineCursor);
        }

        private static void WriteInfo(string info)
        {
            ClearCurrentConsoleLine();
            Console.Write(info);
            Console.CursorLeft = 0;
        }

        private static void RunTest(string testName, Action<Action<string>> test)
        {
            Console.WriteLine("Running {0}", testName);

            if (!Directory.Exists(_storagePath))
                Directory.CreateDirectory(_storagePath);

            TimeMeasure.Start(testName);
            try
            {
                test(message =>
                         {
                             TimeMeasure.Stop(testName);
                             WriteInfo($"{TimeMeasure.Result(testName):hh\\:mm\\:ss\\:fff} {message}");
                             TimeMeasure.Start(testName);
                         });
            }
            finally
            {
                TimeMeasure.Stop(testName);
                Cleanup();
            }
        }

        private static void PrintResults()
        {
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Results");

            foreach (var name in TimeMeasure.MeasureNames.OrderBy(TimeMeasure.Result))
                Console.WriteLine("{0} time is {1:hh\\:mm\\:ss\\:fff}", name, TimeMeasure.Result(name));

            Console.WriteLine();

            foreach (var name in Counter.CounterNames.OrderBy(Counter.Result))
                Console.WriteLine("{0} counter is {1}", name, Counter.Result(name));

            Console.WriteLine();
        }

        static void Main(string[] args)
        {
            Console.Title = "DataTanker Performance Tests";
            Console.WriteLine("Press Enter to run");
            Console.ReadLine();

            Console.WriteLine("BPlusTree tests");

            BPlusTreeStorageTests.StoragePath = _storagePath;

            TimeMeasure.Start("Overall");

            RunTest("BPlusTree: InsertAndReadMillionRecords", BPlusTreeStorageTests.InsertAndReadMillionRecords);
            RunTest("BPlusTree: InsertLargeValues", BPlusTreeStorageTests.InsertLargeValues);
            RunTest("BPlusTree: RandomOperations", BPlusTreeStorageTests.RandomOperations);

            TimeMeasure.Stop("Overall");
            PrintResults();
            TimeMeasure.Clear();

            Console.WriteLine("RadixTree tests");

            RadixTreeStorageTests.StoragePath = _storagePath;

            TimeMeasure.Start("Overall");

            RunTest("RadixTree: EnglishWords", RadixTreeStorageTests.EnglishWords);

            TimeMeasure.Stop("Overall");
            PrintResults();

            Console.ReadLine();
        }
    }
}
