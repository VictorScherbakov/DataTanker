using System;
using System.Globalization;
using System.IO;
using System.Text;
using DataTanker;
using DataTanker.Settings;

namespace Performance
{
    class RadixTreeStorageTests
    {
        public static string StoragePath { get; set; }

        private static IKeyValueStorage<KeyOf<string>, ValueOf<byte[]>> GetByteArrayStorage()
        {
            var factory = new StorageFactory();

            return factory.CreateRadixTreeByteArrayStorage<string>(
                p => Encoding.UTF8.GetBytes(p),
                p => Encoding.UTF8.GetString(p),
                RadixTreeStorageSettings.Default());
        }

        public static void EnglishWords(Action<string> writeInfo)
        {
            using (var fileStream = new FileStream("..\\..\\..\\Data\\words.txt", FileMode.Open))
            {
                using (var textReader = new StreamReader(fileStream))
                {
                    using (var storage = GetByteArrayStorage())
                    {
                        storage.CreateNew(StoragePath);

                        int i = 0;
                        var r = new Random();

                        while (!textReader.EndOfStream)
                        {
                            var line = textReader.ReadLine();
                            var bytes = new byte[20];
                            r.NextBytes(bytes);
                            storage.Set(line, bytes);
                            if (i % 1000 == 0)
                                writeInfo("insert " + i.ToString(CultureInfo.InvariantCulture));
                            i++;
                        }
                    }

                    fileStream.Position = 0;
                    using (var storage = GetByteArrayStorage())
                    {
                        storage.OpenExisting(StoragePath);

                        int i = 0;
                        while (!textReader.EndOfStream)
                        {
                            var line = textReader.ReadLine();
                            storage.Get(line);
                            if (i % 1000 == 0)
                                writeInfo("check " + i.ToString(CultureInfo.InvariantCulture));

                            i++;
                        }
                    }
                }
            }

        }
    }
}