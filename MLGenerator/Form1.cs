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
			Directory.CreateDirectory("temp");
			Directory.CreateDirectory("finished");

			var downloadContainer = new List<DownloadInfo>();
			var parts = TextProcessor.Process(textBox1.Text);
			Task[] dlTaskList = new Task[parts.Count];
			for (int i = 0; i < parts.Count; i++)
			{
				Text = $"Resolving part {i} of {parts.Count}";

				var audioLink = await mgv.ResolveLinkAsync(parts[i]);
				var audioLocation = getnext(@"temp\output", "mp3");
				var wc = new CustomWebClient() { Proxy = null };

				//wc.DownloadProgressChanged += (a, x) =>
				//{

				//};

				//wc.DownloadFileCompleted += (a, x) =>
				//{

				//};
				var dlInfo = new DownloadInfo()
				{
					Downloader = wc,
					Link = audioLink,
					Location = audioLocation,
					Part = i,
					Text = parts[i]
				};

				wc.CustStatObject = dlInfo;
				//wc.DownloadFileAsync(new Uri(audioLink), audioLocation);
				downloadContainer.Add(dlInfo);
				dlTaskList[i] = wc.DownloadFileTaskAsync(new Uri(audioLink), audioLocation);
			}
			//Wait for all downloads to finish
			//Combine
			//etc etc
			Text = $"Waiting for {dlTaskList.Length} download Tasks";
			Task.WaitAll(dlTaskList);
			Text = $"Combining...";
			using (FileStream fs = File.Create(getnext(@"finished\output", "mp3")))
			{
				Combine(downloadContainer.Select(m=>m.Location), fs);
			}
			Text = $"Ready";
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
					File.WriteAllText(nextFil, "");
					ToConCat.Add(nextFil);
					var wc = new WebClient();
					wc.Proxy = null;

					dlProc.Add(Task.Run(() =>
					{
						wc.DownloadFile(curi, nextFil);
					}));
				}
				this.Invoke(uptAct, "Waiting for downloads to finish (" + eg.Length + ")");
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

		public static void Combine(IEnumerable<string> inputFiles, Stream output)
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

	public class DownloadInfo
	{
		public int Part { get; set; }
		public string Text { get; set; }
		public string Link { get; set; }
		public string Location { get; set; }
		public CustomWebClient Downloader { get; set; }
	}

	public class CustomWebClient : WebClient
	{
		public object CustStatObject { get; set; }
	}
}