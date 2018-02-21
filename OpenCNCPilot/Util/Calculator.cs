using OpenCNCPilot.Communication;
using System;
using System.Text.RegularExpressions;
using NCalc;
using System.Text;
using System.Threading.Tasks;
using Jace;
using System.Collections.Generic;
using Hef.Math;

namespace OpenCNCPilot.Util
{
	class Calculator
	{
		private Machine machine;
		private bool Success = true;
		CalculationEngine engine = new CalculationEngine();
		Interpreter interpreter = new Interpreter();

		public Calculator(Machine machine)
		{
			this.machine = machine;
		}

		private static string[] Axes = new string[] {"X", "Y", "Z"};
		private string ExpressionEvaluator(string input)
		{
			try
			{/*
				Expression exp = new Expression(input);

				for(int i = 0; i < 3; i++)
				{
					exp.Parameters.Add("M" + Axes[i], machine.MachinePosition[i]);
					exp.Parameters.Add("W" + Axes[i], machine.WorkPosition[i]);
					exp.Parameters.Add("PM" + Axes[i], machine.LastProbePosMachine[i]);
					exp.Parameters.Add("PW" + Axes[i], machine.LastProbePosWork[i]);
				}

				exp.Parameters.Add("TLO", machine.CurrentTLO);


				object o = "ERR";

				var task = Task.Run(() => { o = exp.Evaluate(); });
				if (!task.Wait(TimeSpan.FromSeconds(0.005)))
				{ 
					throw new Exception("Timed out");
				}

				double res = Convert.ToDouble(o);
				return res.ToString("0.###", Constants.DecimalOutputFormat);	*/
				
				Dictionary<string, double> variables = new Dictionary<string, double>();
				for (int i = 0; i < 3; i++)
				{
					interpreter.SetVar("M" + Axes[i], machine.MachinePosition[i]);
					interpreter.SetVar("W" + Axes[i], machine.WorkPosition[i]);
					interpreter.SetVar("PM" + Axes[i], machine.LastProbePosMachine[i]);
					interpreter.SetVar("PW" + Axes[i], machine.LastProbePosWork[i]);
				}

				interpreter.SetVar("TLO", machine.CurrentTLO);

				return interpreter.Calculate(input).ToString("0.###", Constants.DecimalOutputFormat);

			}
			catch(Exception ex)
			{
				Success = false;
				Console.WriteLine(ex.Message);
				return "[ERR]";
			}
		}

		private static Regex regexExpression = new Regex(@"\[([^\]]*)\]");
		public string Evaluate(string input, out bool success)
		{
			Success = true;

			try
			{
				int depth = 0;
				int start = 0;

				StringBuilder output = new StringBuilder(input.Length);

				for (int i = 0; i < input.Length; i++)
				{
					if (input[i] == '(')
					{
						if (depth == 0)
							start = i + 1;
						depth++;
					}
					else if (input[i] == ')')
					{
						depth--;
						if (depth == 0)
						{
							if (i - start > 0)
								output.Append(ExpressionEvaluator(input.Substring(start, i - start)));
						}
						else if (depth == -1)
						{
							Success = false;
							depth = 0;
						}
					}
					else if (depth == 0)
						output.Append(input[i]);
				}

				if (depth != 0)
					Success = false;

				success = Success;
				return output.ToString();
			}
			catch
			{
				success = false;
				return "ERROR";
			}
		}
	}
}
