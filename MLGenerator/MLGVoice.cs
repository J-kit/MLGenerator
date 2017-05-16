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
	internal class MLGVoice
	{
		private CookieContainer cc;

		public MLGVoice()
		{
			if (cc == null)
			{
				var cookie = new CookieWebClient();
				cookie.Proxy = null;
				cookie.DownloadString("http://www.acapela-group.com/");
				cc = cookie.CookieContainer;
			}
		}

		private Regex rgxMp3 = new Regex(@"'(http:\/\/.*?)'", RegexOptions.ECMAScript | RegexOptions.Compiled);

		public string[] process(string input)
		{
			var tskAll = new List<Task<string>>();

			List<string> lstErg = new List<string>();
			List<string> tmpLst = new List<string>();
			int totChars = 0;
			foreach (var item in input.Split(' '))
			{
				if (totChars + item.Length >= 300)
				{
					Debug.WriteLine("Requesting chunk " + totChars);
					var strReq = string.Join(" ", tmpLst.ToArray());
					tskAll.Add(Task.Run<string>(() =>
					{ return GetMp3(strReq); }
					));

					totChars = 0;
					tmpLst.Clear();
				}

				tmpLst.Add(item);
				totChars += item.Length + 1;//+1 wg space
			}
			_all = tmpLst.Count;
			if (tmpLst.Count != 0)
			{
				//lstErg.Add(GetMp3(string.Join(" ", tmpLst.ToArray())));
				var strReq = string.Join(" ", tmpLst.ToArray());
				tskAll.Add(Task.Run<string>(() =>
				{
					return GetMp3(strReq);
				}));
			}

			Task.WaitAll(tskAll.ToArray());

			return tskAll.Select(m => m.Result).ToArray();
		}

		private static int counter = 0;

		private long _all = 0;

		private string GetMp3(string input)
		{
			var ERG = PostIt(input);
			var all = Interlocked.Read(ref _all);
			Debug.WriteLine(Interlocked.Increment(ref counter) + "/" + all);

			if (rgxMp3.IsMatch(ERG))
			{
				return rgxMp3.Match(ERG).Groups[1].Value;
			}
			else
			{
				return null;
			}
		}

		private string PostIt(string input)
		{
			//http://dmbx.acapela-group.com/DemoHTML5Form_V2.php?langdemo=Powered+by+%3Ca+href%3D%22http%3A%2F%2Fwww.acapela-vaas.com%22%3EAcapela+Voice+as+a+Service%3C%2Fa%3E.+For+demo+and+evaluation+purpose+only%2C+for+commercial+use+of+generated+sound+files+please+go+to+%3Ca+href%3D%22http%3A%2F%2Fwww.acapela-box.com%22%3Ewww.acapela-box.com%3C%2Fa%3E
			var url = @"http://dmbx.acapela-group.com/DemoHTML5Form_V2.php?langdemo=Powered+by+%3Ca+href%3D%22http%3A%2F%2Fwww.acapela-vaas.com%22%3EAcapela+Voice+as+a+Service%3C%2Fa%3E.+For+demo+and+evaluation+purpose+only%2C+for+commercial+use+of+generated+sound+files+please+go+to+%3Ca+href%3D%22http%3A%2F%2Fwww.acapela-box.com%22%3Ewww.acapela-box.com%3C%2Fa%3E";
			//var url = @"http://www.acapela-group.com/demo-tts/DemoHTML5Form_V2.php?langdemo=Powered+by+%3Ca+href%3D%22http%3A%2F%2Fwww.acapela-vaas.com%22%3EAcapela+Voice+as+a+Service%3C%2Fa%3E.+For+demo+and+evaluation+purpose+only%2C+for+commercial+use+of+generated+sound+files+please+go+to+%3Ca+href%3D%22http%3A%2F%2Fwww.acapela-box.com%22%3Ewww.acapela-box.com%3C%2Fa%3E";

			var contenta = @"MyLanguages=sonid10" +
				"&0=Leila&1=Laia&2=Eliska&3=Mette&4=Zoe&5=Jasmijn&6=Tyler&7=Deepa&8=Rhona&9=Rachel&MySelectedVoice=Ryan&11=Hanna&12=Sanna&13=Manon-be&14=Louise&15=Manon&16=Claudia&17=Dimitris&18=Fabiana&19=Sakura&20=Minji&21=Lulu&22=Bente&23=Monika&24=Marcia&25=Celia&26=Alyona&27=Biera&28=Ines&29=Rodrigo&30=Elin&31=Samuel&32=Kal&33=Mia&34=Ipek" + "" +
				"&MyTextForTTS=" + input + "&agreeterms=on&t=1&SendToVaaS=";

			var request = (HttpWebRequest)WebRequest.Create(url);

			request.CookieContainer = cc;
			request.Proxy = null;

			var data = Encoding.ASCII.GetBytes(contenta);

			request.Method = "POST";
			request.ContentType = "application/x-www-form-urlencoded";
			request.ContentLength = data.Length;

			using (var stream = request.GetRequestStream())
			{
				stream.Write(data, 0, data.Length);
			}

			var response = (HttpWebResponse)request.GetResponse();

			return new StreamReader(response.GetResponseStream()).ReadToEnd();

			//  CookieWebClient wc = new CookieWebClient();
			//wc.Proxy = null;
			//wc.DownloadString("http://www.acapela-group.com/");
			//var url = @"http://www.acapela-group.com/demo-tts/DemoHTML5Form_V2.php?langdemo=Powered+by+%3Ca+href%3D%22http%3A%2F%2Fwww.acapela-vaas.com%22%3EAcapela+Voice+as+a+Service%3C%2Fa%3E.+For+demo+and+evaluation+purpose+only%2C+for+commercial+use+of+generated+sound+files+please+go+to+%3Ca+href%3D%22http%3A%2F%2Fwww.acapela-box.com%22%3Ewww.acapela-box.com%3C%2Fa%3E";

			//var contenta = @"MyLanguages=sonid10" +
			//    "&0=Leila&1=Laia&2=Eliska&3=Mette&4=Zoe&5=Jasmijn&6=Tyler&7=Deepa&8=Rhona&9=Rachel&MySelectedVoice=Ryan&11=Hanna&12=Sanna&13=Manon-be&14=Louise&15=Manon&16=Claudia&17=Dimitris&18=Fabiana&19=Sakura&20=Minji&21=Lulu&22=Bente&23=Monika&24=Marcia&25=Celia&26=Alyona&27=Biera&28=Ines&29=Rodrigo&30=Elin&31=Samuel&32=Kal&33=Mia&34=Ipek" + "" +
			//    "&MyTextForTTS=sssdd&agreeterms=on&t=1&SendToVaaS=";

			//string HtmlResult = wc.UploadString(url, contenta);

			//return HtmlResult;
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
			if (r is HttpWebRequest request)
			{
				request.CookieContainer = container;
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