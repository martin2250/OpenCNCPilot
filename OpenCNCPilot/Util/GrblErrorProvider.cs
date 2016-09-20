using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;

namespace OpenCNCPilot.Util
{
	static class GrblErrorProvider
	{
		static Dictionary<int, string> Errors;

		static GrblErrorProvider()
		{
			Console.WriteLine("Loading GRBL Error Database");

			Errors = new Dictionary<int, string>();

			if (!File.Exists(Constants.FilePathErrors))
			{
				Console.WriteLine("File Missing: {0}", Constants.FilePathErrors);
				return;
			}

			string ErrorFile;

			try
			{
				ErrorFile = File.ReadAllText(Constants.FilePathErrors);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				return;
			}

			Regex LineParser = new Regex(@"([0-9]+)\t([^\n^\r]*)");     //test here https://www.regex101.com/r/hO5zI1/2

			MatchCollection mc = LineParser.Matches(ErrorFile);

			foreach (Match m in mc)
			{
				int errorNo = int.Parse(m.Groups[1].Value);

				Errors.Add(errorNo, m.Groups[2].Value);
			}

			Console.WriteLine("Loaded GRBL Error Database");
		}

		public static string GetErrorMessage(int errorCode)
		{
			if (Errors.ContainsKey(errorCode))
				return Errors[errorCode];
			else
				return $"Unknown Error: {errorCode}";
		}

		static Regex ErrorExp = new Regex(@"Invalid gcode ID:(\d+)");
		private static string ErrorMatchEvaluator(Match m)
		{
			return GetErrorMessage(int.Parse(m.Groups[1].Value));
		}

		public static string ExpandError(string error)
		{
			return ErrorExp.Replace(error, ErrorMatchEvaluator);
		}
	}
}
