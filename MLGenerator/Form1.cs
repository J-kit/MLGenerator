using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MLGenerator
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            mgv = new MLGVoice();
        }

		private MLGVoice mgv;

		private async void button2_Click(object sender, EventArgs e)
		{
			var links = new List<string>();
			var parts = TextProcessor.Process(textBox1.Text);
			foreach (var part in parts)
			{
				links.Add(await mgv.ResolveLinkAsync(part));

				var wc = new WebClient() { Proxy = null };
				wc.DownloadProgressChanged += (a, x) =>
				{

				};
				wc.DownloadFileCompleted += (a, x) =>
				{

				};
			}
			//Wait for all downloads to finish
			//Combine
			//etc etc
		}
		private void button1_ClickAsync(object sender, EventArgs e)
        {
            var text = textBox1.Text;

            Thread th = new Thread(() =>
            {
                Directory.CreateDirectory("temp");
                Directory.CreateDirectory("finished");
                List<string> ToConCat = new List<string>();

                Action<string> uptAct = new Action<string>((a) =>
                {
                    this.Text = a;
                });
                this.Invoke(uptAct, "Resolving mp3 files");

                var eg = mgv.process(text);
                var dlProc = new List<Task>();

                this.Invoke(uptAct, "Starting download (" + eg.Length + ")");
                foreach (var item in eg)
                {
                    var curi = new Uri(item);
                    var nextFil = getnext(@"temp\output", "mp3");
                    File.WriteAllText(nextFil,"");
                    ToConCat.Add(nextFil);
                    var wc = new WebClient();
                    wc.Proxy = null;

                    dlProc.Add(Task.Run(() =>
                    {
                        wc.DownloadFile(curi, nextFil);
                    }));
                }
                this.Invoke(uptAct, "Waiting for downloads to finish ("+ eg.Length + ")");
                Task.WaitAll(dlProc.ToArray());
                this.Invoke(uptAct, "Combine files");
                using (FileStream fs = File.Create(getnext(@"finished\output", "mp3")))
                {
                    Combine(ToConCat.ToArray(), fs);
                }
                this.Invoke(uptAct, "Finished");
                foreach (var item in ToConCat)
                {
                    File.Delete(item);
                }
                MessageBox.Show("Finished!");
            });
            th.Name = "ResolverThread";
            th.IsBackground = true;
            th.Start();
        }

        public static void Combine(string[] inputFiles, Stream output)
        {
            foreach (string file in inputFiles)
            {
                Mp3FileReader reader = new Mp3FileReader(file);
                if ((output.Position == 0) && (reader.Id3v2Tag != null))
                {
                    output.Write(reader.Id3v2Tag.RawData, 0, reader.Id3v2Tag.RawData.Length);
                }
                Mp3Frame frame;
                while ((frame = reader.ReadNextFrame()) != null)
                {
                    output.Write(frame.RawData, 0, frame.RawData.Length);
                }
                reader.Close();
            }
        }

        private string getnext(string name, string ext)
        {
            int num = 0;
            do
            {
                if (!File.Exists(name + num + "." + ext))
                    return name + num + "." + ext;
                else
                    num++;
            } while (true);
        }

	
	}
}