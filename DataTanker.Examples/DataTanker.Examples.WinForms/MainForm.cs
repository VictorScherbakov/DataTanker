using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;

using DataTanker.Settings;
using DataTanker.Utils.Instrumentation;

namespace DataTanker.Examples.WinForms
{
    public partial class MainForm : Form
    {
        private static IKeyValueStorage<ComparableKeyOf<int>, ValueOf<string>> GetStorage()
        {
            var settings = BPlusTreeStorageSettings.Default(4); // use default settings with 4-bytes keys

            settings.CacheSettings.MaxCachedPages = 10000; // speedup massive insert operations
            settings.CacheSettings.MaxDirtyPages = 1000;   // by increasing cache size

            return new StorageFactory().CreateBPlusTreeStorage<int, string>(
                    BitConverter.GetBytes,               // key serialization
                    p => BitConverter.ToInt32(p, 0),     // key deserialization
                    p => Encoding.UTF8.GetBytes(p),      // value serialization
                    p => Encoding.UTF8.GetString(p),     // value deserialization
                    settings);
        }

        public MainForm()
        {
            InitializeComponent();
            _storage.OpenOrCreate(Directory.GetCurrentDirectory());
        }

        private readonly IKeyValueStorage<ComparableKeyOf<int>, ValueOf<string>> _storage = GetStorage();


        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _storageClosing = true;
            if(_storage != null)
                _storage.Close();
        }

        private readonly Random _random = new Random();
        private bool _storageClosing;

        private string RandomString(int size)
        {
            var builder = new StringBuilder();
            
            for (int i = 0; i < size; i++)
            {
                char ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * _random.NextDouble() + 65)));
                builder.Append(ch);
            }

            return builder.ToString();
        }

        private void btnStartGeneration_Click(object sender, EventArgs e)
        {
            int start = int.Parse(nudStartKey.Value.ToString(CultureInfo.InvariantCulture));
            int count = int.Parse(nudKeyCount.Value.ToString(CultureInfo.InvariantCulture));

            TimeMeasure.Start();
            try
            {
                for (int i = start; i < start + count; i++)
                {
                    if(_storageClosing) break;
                    _storage.Set(i, RandomString(_random.Next(500)));

                    if (i % 1000 == 0)
                    {
                        lblState.Text = string.Format("Inserting value {0}...", i);
                        Application.DoEvents();
                    }
                }

                if (!_storageClosing)
                {
                    lblState.Text = "Flushing...";
                    Application.DoEvents();
                    _storage.Flush();
                }
            }
            finally 
            {
                TimeMeasure.Stop();
            }

            lblState.Text =  string.Format("Done! Elapsed time: {0}", TimeMeasure.Result());
            TimeMeasure.Reset();
        }

        private int GetKey()
        {
            return int.Parse(nudKey.Value.ToString(CultureInfo.InvariantCulture));
        }

        private void btnGet_Click(object sender, EventArgs e)
        {
            var key = GetKey();
            tbValue.Text = _storage.Get(key);
        }

        private void btnSet_Click(object sender, EventArgs e)
        {
            var key = GetKey(); 
            _storage.Set(key, tbValue.Text);
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            var key = GetKey();
            _storage.Remove(key);
            tbValue.Text = string.Empty;
        }
    }
}
