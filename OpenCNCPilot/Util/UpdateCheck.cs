using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace OpenCNCPilot.Util
{
	static class UpdateCheck
	{
		static WebClient client;
		static Regex versionRegex = new Regex("\"tag_name\":\\s*\"v([0-9\\.]+)\",");
		static Regex releaseRegex = new Regex("\"html_url\":\\s*\"([^\"]*)\",");

		public static void CheckForUpdate()
		{
			client = new WebClient();
			client.Headers["User-Agent"] = "Mozilla/5.0 (Windows; U; Windows NT 6.1; en-US; rv:1.9.2.15) Gecko/20110303 Firefox/3.6.15";
			client.Proxy = null;
			client.DownloadStringCompleted += Client_DownloadStringCompleted;
			client.DownloadStringAsync(new Uri("https://api.github.com/repos/martin2250/OpenCNCPilot/releases/latest"));
		}

		private static void Client_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
		{
			try
			{
				if (e.Error != null)
				{
					Console.WriteLine("Error while checking for new version:");
					Console.WriteLine(e.Error.Message);
					return;
				}

				Match m = versionRegex.Match(e.Result);

				if (!m.Success)
				{
					Console.WriteLine("No matching tag_id found");
					return;
				}

				Version latest;

				if (!Version.TryParse(m.Groups[1].Value, out latest))
				{
					Console.WriteLine($"Error while parsing version string <{m.Groups[1].Value}>");
					return;
				}

				Console.WriteLine($"Latest version on GitHub: {latest}");

				if (System.Reflection.Assembly.GetEntryAssembly().GetName().Version < latest)
				{
					Match urlMatch = releaseRegex.Match(e.Result);

					string url = "https://github.com/martin2250/OpenCNCPilot/releases";

					if (urlMatch.Success)
					{
						url = urlMatch.Groups[1].Value;
					}

					if (MessageBox.Show("There is an update available!\nOpen in browser?", "Update", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
						System.Diagnostics.Process.Start(url);
				}
			}
			catch { }   //update check is non-critical and should never interrupt normal application operation
		}
	}
}
