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
			var parts = TextProcessor.GetSentences(textBox1.Text);

			Task[] dlTaskList = new Task[parts.Count];

			for (int i = 0; i < parts.Count; i++)
			{
				if (string.IsNullOrEmpty(parts[i]))
					continue;

				Text = $"Resolving part {i} of {parts.Count}";
				string audioLink = null;
				while (audioLink == null)
				{
					audioLink = await mgv.ResolveLinkAsync(parts[i]);
					if (audioLink == null)
					{
						Debug.WriteLine($"Resolve for {i} failed, retrying...");
					}
				}

				var audioLocation = getnext(@"temp\output", "mp3");
				var wc = new CustomWebClient() { Proxy = null };


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
			foreach (var item in dlTaskList)
			{
				if (item != null)
					await item;
			}
			Text = $"Combining...";
			using (FileStream fs = File.Create(getnext(@"finished\output", "mp3")))
			{
				Combine(downloadContainer.Select(m => m.Location), fs);
			}
			Text = $"Cleanup";

			foreach (var item in downloadContainer)
			{
				if (item != null && File.Exists(item.Location))
					File.Delete(item.Location);
			}
			Text = $"Ready";
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