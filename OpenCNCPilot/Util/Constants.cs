using System.Globalization;

namespace OpenCNCPilot.Util
{
	public class Constants
	{
		public static NumberFormatInfo DecimalParseFormat = new NumberFormatInfo() { NumberDecimalSeparator = "."};

        public static NumberFormatInfo DecimalOutputFormat
		{
			get
			{
				return new NumberFormatInfo() { NumberDecimalSeparator = ".", NumberDecimalDigits = 3};
			}
		}

		public static string FileFilterGCode = "GCode|*.tap;*.nc;*.ngc|All Files|*.*";
		public static string FileFilterHeightMap = "Height Maps|*.hmap|All Files|*.*";

		public static string FilePathErrors = "Resources\\GrblErrors.txt";
		public static string FilePathWebsite = "Resources\\index.html";

		public static string LogFile = "log.txt";

		public static char[] NewLines = new char[] {'\n', '\r'};

		static Constants()
		{
			
		}
	}
}
