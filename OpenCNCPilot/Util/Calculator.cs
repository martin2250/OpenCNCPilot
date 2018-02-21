using OpenCNCPilot.Communication;
using System;
using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;

namespace OpenCNCPilot.Util
{
	class Calculator
	{
		private Machine machine;
		private DataTable table = new DataTable();

		public Calculator(Machine machine)
		{
			this.machine = machine;
			CultureInfo c = new CultureInfo("en-US");
			table.Locale = c;
		}


		private string ExpressionEvaluator(Match m)
		{
			try
			{
				string expression = m.Groups[1].Value;

				expression = expression.Replace("WX", $"({machine.WorkPosition.X:N3})");
				expression = expression.Replace("WY", $"({machine.WorkPosition.Y:N3})");
				expression = expression.Replace("WZ", $"({machine.WorkPosition.Z:N3})");

				expression = expression.Replace("MX", $"({machine.MachinePosition.X:N3})");
				expression = expression.Replace("MY", $"({machine.MachinePosition.Y:N3})");
				expression = expression.Replace("MZ", $"({machine.MachinePosition.Z:N3})");

				expression = expression.Replace("PWX", $"({machine.LastProbePosWork.X:N3})");
				expression = expression.Replace("PWY", $"({machine.LastProbePosWork.Y:N3})");
				expression = expression.Replace("PWZ", $"({machine.LastProbePosWork.Z:N3})");

				expression = expression.Replace("PMX", $"({machine.LastProbePosMachine.X:N3})");
				expression = expression.Replace("PMY", $"({machine.LastProbePosMachine.Y:N3})");
				expression = expression.Replace("PMZ", $"({machine.LastProbePosMachine.Z:N3})");

				return table.Compute(expression, "").ToString();
			}
			catch(Exception ex)
			{
				Console.WriteLine(ex.Message);
				return "(ERR)";
			}
		}

		private static Regex regexExpression = new Regex(@"(\([^)]*\))");
		public string Evaluate(string input)
		{
			return regexExpression.Replace(input, ExpressionEvaluator);
		}
	}
}
