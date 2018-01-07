using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;

namespace OpenCNCPilot.Util
{
	static class GrblCodeTranslator
	{
		static Dictionary<int, string> Errors = new Dictionary<int, string>();
		static Dictionary<int, string> Alarms = new Dictionary<int, string>();
		static Dictionary<int, string> Settings = new Dictionary<int, string>();

		private static void Load(Dictionary<int, string> dict, string path)
		{
			if (!File.Exists(path))
			{
				Console.WriteLine("File Missing: {0}", path);
				return;
			}

			string FileContents;

			try
			{
				FileContents = File.ReadAllText(path);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				return;
			}

			Regex LineParser = new Regex(@"""([0-9]+)"",""[^\n\r""]*"",""([^\n\r""]*)""");     //test here https://regex101.com/r/hO5zI1/4

			MatchCollection mc = LineParser.Matches(FileContents);

			foreach (Match m in mc)
			{
				int number = int.Parse(m.Groups[1].Value);

				dict.Add(number, m.Groups[2].Value);
			}
		}

		static GrblCodeTranslator()
		{
			Console.WriteLine("Loading GRBL Code Database");

			Load(Errors, "Resources\\error_codes_en_US.csv");
			Load(Alarms, "Resources\\alarm_codes_en_US.csv");
			Load(Settings, "Resources\\setting_codes_en_US.csv");

			Console.WriteLine("Loaded GRBL Code Database");
		}

		public static string GetErrorMessage(int errorCode, bool alarm = false)
		{
			if(!alarm)
			{
				if (Errors.ContainsKey(errorCode))
					return Errors[errorCode];
				else
					return $"Unknown Error: {errorCode}";
			}
			else
			{
				if (Alarms.ContainsKey(errorCode))
					return Alarms[errorCode];
				else
					return $"Unknown Alarm: {errorCode}";
			}
		}

		static Regex ErrorExp = new Regex(@"error:(\d+)");
		private static string ErrorMatchEvaluator(Match m)
		{
			return GetErrorMessage(int.Parse(m.Groups[1].Value));
		}

		static Regex AlarmExp = new Regex(@"ALARM:(\d+)");
		private static string AlarmMatchEvaluator(Match m)
		{
			return GetErrorMessage(int.Parse(m.Groups[1].Value), true);
		}

		public static string ExpandError(string error)
		{
			string ret = ErrorExp.Replace(error, ErrorMatchEvaluator);
			return AlarmExp.Replace(ret, AlarmMatchEvaluator);
		}
	}
}
