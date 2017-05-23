using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MLGenerator
{
	public static class Settings
	{
		public const string ResolvePostUrl = @"http://dmbx.acapela-group.com/DemoHTML5Form_V2.php?langdemo=Powered+by+%3Ca+href%3D%22http%3A%2F%2Fwww.acapela-vaas.com%22%3EAcapela+Voice+as+a+Service%3C%2Fa%3E.+For+demo+and+evaluation+purpose+only%2C+for+commercial+use+of+generated+sound+files+please+go+to+%3Ca+href%3D%22http%3A%2F%2Fwww.acapela-box.com%22%3Ewww.acapela-box.com%3C%2Fa%3E";
		public const string ResolvePostBody = @"MyLanguages=sonid10&0=Leila&1=Laia&2=Eliska&3=Mette&4=Zoe&5=Jasmijn&6=Tyler&7=Deepa&8=Rhona&9=Rachel&MySelectedVoice=Ryan&11=Hanna&12=Sanna&13=Manon-be&14=Louise&15=Manon&16=Claudia&17=Dimitris&18=Fabiana&19=Sakura&20=Minji&21=Lulu&22=Bente&23=Monika&24=Marcia&25=Celia&26=Alyona&27=Biera&28=Ines&29=Rodrigo&30=Elin&31=Samuel&32=Kal&33=Mia&34=Ipek&MyTextForTTS={{INPUT}}&agreeterms=on&t=1&SendToVaaS=";
	}

	public static class TextProcessor
	{
		public static List<string> GetSentences(string input)
		{
			int totChars = 0;
			var erg = new List<string>();
			var sentences = input.Replace("\r", "").Replace("\n", "").Split('.');
			foreach (var item in sentences)
			{
				if (item.Length >= 300) //Split into tinier parts
				{
					var tmpList = new List<string>();
					foreach (var itWord in item.Split(' '))
					{
						if (totChars + itWord.Length >= 300)
						{
							erg.Add(string.Join(" ", tmpList.ToArray()));

							totChars = 0;
							tmpList.Clear();
						}

						tmpList.Add(item);
						totChars += itWord.Length + 1;//+1 wg space
					}
					if (tmpList.Any())
					{
						erg.Add(string.Join(" ", tmpList.ToArray()) + ".");
					}
					else
					{
						if (erg.Any())
						{
							erg[erg.Count - 1] += ".";

						}
					}

				}
				else
				{
					if (!string.IsNullOrEmpty(item))
					{
						erg.Add(item + ".");
					}
				}
			}
			return erg;
		}
	}

	public class MLGVoice
	{
		private CookieContainer cc;
		private Regex rgxMp3 = new Regex(@"'(http:\/\/.*?)'", RegexOptions.ECMAScript | RegexOptions.Compiled);

		public MLGVoice()
		{
			Init();
		}

		public async void Init()
		{
			if (cc == null)
			{
				var cookie = new CookieWebClient();
				cookie.Proxy = null;
				await cookie.DownloadStringTaskAsync("http://www.acapela-group.com/");
				cc = cookie.CookieContainer;
			}
		}

		public async Task<string> ResolveLinkAsync(string textToSpeak)
		{
			var res = await PostAsyncTask(textToSpeak);
			if (rgxMp3.IsMatch(res))
			{
				return rgxMp3.Match(res).Groups[1].Value;
			}
			else
			{
				return null;
			}
		}

		private async Task<string> PostAsyncTask(string input)
		{
			var contenta = Settings.ResolvePostBody.Replace("{{INPUT}}", input);
			var request = (HttpWebRequest)WebRequest.Create(Settings.ResolvePostUrl);

			request.CookieContainer = cc;
			request.Proxy = null;

			var data = Encoding.ASCII.GetBytes(contenta);

			request.Method = "POST";
			request.ContentType = "application/x-www-form-urlencoded";
			request.ContentLength = data.Length;

			using (var stream = await request.GetRequestStreamAsync())
			{
				stream.Write(data, 0, data.Length);
			}

			var response = await (request.GetResponseAsync());

			return await (new StreamReader(response.GetResponseStream())).ReadToEndAsync();
		}
	}

	public class CookieWebClient : WebClient
	{
		public static CookieWebClient GetNew()
		{
			return new CookieWebClient();
		}

		public CookieWebClient()
		{
			this.container = new CookieContainer();
		}

		public CookieWebClient(CookieContainer container)
		{
			this.container = container;
		}

		public CookieContainer CookieContainer
		{
			get { return container; }
			set { container = value; }
		}

		private CookieContainer container = new CookieContainer();

		protected override WebRequest GetWebRequest(Uri address)
		{
			WebRequest r = base.GetWebRequest(address);
			if (r is HttpWebRequest)
			{
				((HttpWebRequest)r).CookieContainer = container;
			}
			return r;
		}

		private Uri _responseUri;

		public Uri ResponseUri
		{
			get { return _responseUri; }
		}

		protected override WebResponse GetWebResponse(WebRequest request, IAsyncResult result)
		{
			request.Headers["Cache-Control"] = "max-age=0";
			//request.Headers["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
			//request.Headers["User-agent"] = "Mozilla/5.0 (Windows NT 6.1; Win64; x64; rv:50.0) Gecko/20100101 Firefox/50.0";
			request.Headers["Accept-Language"] = "en-US,en;q=0.8";

			WebResponse response = base.GetWebResponse(request, result);
			_responseUri = response.ResponseUri;
			ReadCookies(response);
			return response;
		}

		protected override WebResponse GetWebResponse(WebRequest request)
		{
			request.Headers["Cache-Control"] = "max-age=0";
			//request.Headers["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
			//request.Headers["User-agent"] = "Mozilla/5.0 (Windows NT 6.1; Win64; x64; rv:50.0) Gecko/20100101 Firefox/50.0";
			request.Headers["Accept-Language"] = "en-US,en;q=0.8";

			WebResponse response = base.GetWebResponse(request);
			_responseUri = response.ResponseUri;
			ReadCookies(response);
			return response;
		}

		private void ReadCookies(WebResponse r)
		{
			var response = r as HttpWebResponse;
			if (response != null)
			{
				CookieCollection cookies = response.Cookies;
				container.Add(cookies);
			}
		}
	}
}