using System.IO;
using NUnit.Framework;

namespace Tests
{
    public class FileSystemStorageTestBase
    {
        protected string StoragePath { get; set; }

        [SetUp]
        public void Init()
        {
            if (!string.IsNullOrEmpty(StoragePath) && !Directory.Exists(StoragePath))
                Directory.CreateDirectory(StoragePath);
        }

        [TearDown]
        public void Cleanup()
        {
            if (Directory.Exists(StoragePath))
            {
                string[] files = Directory.GetFiles(StoragePath);
                foreach (string file in files)
                    File.Delete(file);
            }
        }
    }
}