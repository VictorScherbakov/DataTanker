using System;
using System.IO;
using System.Text;
using DataTanker.Settings;

namespace DataTanker.Examples.Console
{
    class Program
    {
        private static IKeyValueStorage<ComparableKeyOf<int>, ValueOf<string>> GetStorage()
        {
            var settings = BPlusTreeStorageSettings.Default(4); // use default settings with 4-bytes keys
            settings.AutoFlushTimeout = TimeSpan.FromMilliseconds(50);

            return new StorageFactory().CreateBPlusTreeStorage<int, string>( 
                    BitConverter.GetBytes,               // key serialization
                    p => BitConverter.ToInt32(p, 0),     // key deserialization
                    p => Encoding.UTF8.GetBytes(p),      // value serialization
                    p => Encoding.UTF8.GetString(p),     // value deserialization
                    settings);
        }

        private static bool GetInteger(string line, out int result)
        {
            result = -1;
            var tokens = line.Split(new[]{' '}, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length < 2)
            {
                WriteError("Missing integer key");
                return false;
            }

            var intToken = tokens[1];
            if (int.TryParse(intToken, out result))
                return true;

            WriteError("Invalid integer key");
            return false;
        }

        private static void WriteMessage(string message)
        {
            System.Console.ForegroundColor = ConsoleColor.DarkGray;
            System.Console.WriteLine(message);
            System.Console.ForegroundColor = ConsoleColor.White;
        }

        private static void WriteError(string message)
        {
            System.Console.ForegroundColor = ConsoleColor.DarkRed;
            System.Console.WriteLine(message);
            System.Console.ForegroundColor = ConsoleColor.White;
        }

        static void Main(string[] args)
        {
            WriteMessage("DataTanker console example - interactive key-value storage");
            WriteMessage("with integer keys and string values");
            WriteMessage("");
            WriteMessage("Using:");
            WriteMessage(" set <integer key> <string value>");
            WriteMessage(" get <integer key>");
            WriteMessage(" remove <integer key>");
            WriteMessage(" exists <integer key>");
            WriteMessage(" exit");
            WriteMessage("");

            using (var storage = GetStorage())
            {
                storage.OpenOrCreate(Directory.GetCurrentDirectory());
                while (true)
                {
                    var line = System.Console.ReadLine();

                    if(line == null) continue;
                    if(line.Trim() == "exit") break;

                    if (line.Trim() == "cls")
                    {
                        System.Console.Clear();
                        continue;
                    }

                    int key;

                    if (line.StartsWith("set "))
                    {
                        if (GetInteger(line, out key))
                        {
                            var startIndex = line.IndexOf(' ', 5);
                            if (startIndex > -1)
                            {
                                var value = line.Substring(startIndex).Trim();
                                storage.Set(key, value);
                            }
                        }
                        continue;
                    } 
                    
                    if (line.StartsWith("get "))
                    {
                        if (GetInteger(line, out key))
                        {
                            var value = storage.Get(key);
                            WriteMessage(value);
                        }
                        continue;
                    } 
                    
                    if (line.StartsWith("remove "))
                    {
                        if (GetInteger(line, out key))
                        {
                            storage.Remove(key);
                            WriteMessage("Done!");
                        }
                        continue;
                    } 
                    
                    if (line.StartsWith("exists "))
                    {
                        if (GetInteger(line, out key))
                            WriteMessage(storage.Exists(key) ? "Yes" : "No");
                        continue;
                    } 

                    WriteError("Unknown command");
                }
            }
        }
    }
}
