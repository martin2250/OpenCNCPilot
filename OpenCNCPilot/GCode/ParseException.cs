using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
