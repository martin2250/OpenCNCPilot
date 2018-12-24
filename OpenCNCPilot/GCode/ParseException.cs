using System;

namespace OpenCNCPilot.GCode
{
	class ParseException : Exception
	{
		public int Line;
		public string Error;

		public ParseException(string error, int line)
		{
			Line = line;
			Error = error;
		}

		public override string Message
		{
			get
			{
				return $"Error while reading GCode File in Line {Line}:\n{Error}";
			}
		}
	}
}
