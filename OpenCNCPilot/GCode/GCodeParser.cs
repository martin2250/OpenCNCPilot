using OpenCNCPilot.GCode.GCodeCommands;
using OpenCNCPilot.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace OpenCNCPilot.GCode
{
	public enum ParseDistanceMode
	{
		Absolute,
		Incremental
	}

	public enum ParseUnit
	{
		Metric,
		Imperial
	}

	class ParserState
	{
		public Vector3 Position;
		public bool[] PositionValid;    // true if the position for this coordinate was previously specified in absolute terms, to prevent the start point of (0, 0, 0) to influence the output file
		public ArcPlane Plane;
		public double Feed;
		public ParseDistanceMode DistanceMode;
		public ParseDistanceMode ArcDistanceMode;
		public ParseUnit Unit;
		public int LastMotionMode;

		public ParserState()
		{
			Position = Vector3.MinValue;
			PositionValid = new bool[] { false, false, false };
			Plane = ArcPlane.XY;
			Feed = 0;
			DistanceMode = ParseDistanceMode.Absolute;
			ArcDistanceMode = ParseDistanceMode.Incremental;
			Unit = ParseUnit.Metric;
			LastMotionMode = -1;
		}
	}

	struct Word
	{
		public char Command;
		public double Parameter;

		public override string ToString()
		{
			return $"{Command}{Parameter}";
		}
	}

	static class GCodeParser
	{
		public static ParserState State;

		public static Regex GCodeSplitter = new Regex(@"([A-Z])\s*(\-?\d+\.?\d*)", RegexOptions.Compiled);
		private static double[] MotionCommands = new double[] { 0, 1, 2, 3 };
		private static string ValidWords = "GMXYZIJKFRSP";
		private static string IgnoreAxes = "ABC";
		public static List<Command> Commands;
		public static List<string> Warnings;

		public static void Reset()
		{
			State = new ParserState();
			Commands = new List<Command>(); //don't reuse, might be used elsewhere
			Warnings = new List<string>();
		}

		static GCodeParser()
		{
			Reset();
		}

		public static void ParseFile(string path)
		{
			Parse(File.ReadLines(path));
		}

		public static void Parse(IEnumerable<string> file)
		{
			int i = 0;

			var sw = System.Diagnostics.Stopwatch.StartNew();

			foreach (string linei in file)
			{
				i++;
				string line = CleanupLine(linei, i);

				if (string.IsNullOrWhiteSpace(line))
					continue;

				Parse(line.ToUpper(), i);
			}

			sw.Stop();

			Console.WriteLine("parsing the G code file took {0} ms", sw.ElapsedMilliseconds);
		}

		static string CleanupLine(string line, int lineNumber)
		{
			int commentIndex = line.IndexOf(';');

			if (commentIndex > -1)
				line = line.Remove(commentIndex);

			int start = -1;

			while ((start = line.IndexOf('(')) != -1)
			{
				int end = line.IndexOf(')');

				if (end < start)
					throw new ParseException("mismatched parentheses", lineNumber);

				line = line.Remove(start, end - start);
			}

			return line;
		}

		static void Parse(string line, int lineNumber)
		{
			MatchCollection matches = GCodeSplitter.Matches(line);

			List<Word> Words = new List<Word>(matches.Count);

			foreach (Match match in matches)
			{
				Words.Add(new Word() { Command = match.Groups[1].Value[0], Parameter = double.Parse(match.Groups[2].Value, Constants.DecimalParseFormat) });
			}

			for (int i = 0; i < Words.Count; i++)
			{
				if (Words[i].Command == 'N')
				{
					Words.RemoveAt(i--);
					continue;
				}

				if (IgnoreAxes.Contains(Words[i].Command) && Properties.Settings.Default.IgnoreAdditionalAxes)
				{
					Words.RemoveAt(i--);
					continue;
				}

				if (!ValidWords.Contains(Words[i].Command))
				{
					Warnings.Add($"ignoring unknown word (letter): \"{Words[i]}\". (line {lineNumber})");
					Words.RemoveAt(i--);
					continue;
				}

				if (Words[i].Command != 'F')
					continue;

				State.Feed = Words[i].Parameter;
				if (State.Unit == ParseUnit.Imperial)
					State.Feed *= 25.4;
				Words.RemoveAt(i--);
				continue;
			}

			for (int i = 0; i < Words.Count; i++)
			{
				if (Words[i].Command == 'M')
				{
					int param = (int)Words[i].Parameter;

					if (param != Words[i].Parameter || param < 0)
						throw new ParseException("M code can only have positive integer parameters", lineNumber);

					Commands.Add(new MCode() { Code = param, LineNumber = lineNumber });

					Words.RemoveAt(i);
					i--;
					continue;
				}

				if (Words[i].Command == 'S')
				{
					double param = Words[i].Parameter;

					if (param < 0)
						Warnings.Add($"spindle speed must be positive. (line {lineNumber})");

					Commands.Add(new Spindle() { Speed = Math.Abs(param), LineNumber = lineNumber });

					Words.RemoveAt(i);
					i--;
					continue;
				}

				if (Words[i].Command == 'G' && !MotionCommands.Contains(Words[i].Parameter))
				{
					#region UnitPlaneDistanceMode

					double param = Words[i].Parameter;

					if (param == 90)
					{
						State.DistanceMode = ParseDistanceMode.Absolute;
						Words.RemoveAt(i);
						i--;
						continue;
					}
					if (param == 91)
					{
						State.DistanceMode = ParseDistanceMode.Incremental;
						Words.RemoveAt(i);
						i--;
						continue;
					}
					if (param == 90.1)
					{
						State.ArcDistanceMode = ParseDistanceMode.Absolute;
						Words.RemoveAt(i);
						continue;
					}
					if (param == 91.1)
					{
						State.ArcDistanceMode = ParseDistanceMode.Incremental;
						Words.RemoveAt(i);
						i--;
						continue;
					}
					if (param == 21)
					{
						State.Unit = ParseUnit.Metric;
						Words.RemoveAt(i);
						i--;
						continue;
					}
					if (param == 20)
					{
						State.Unit = ParseUnit.Imperial;
						Words.RemoveAt(i);
						i--;
						continue;
					}
					if (param == 17)
					{
						State.Plane = ArcPlane.XY;
						Words.RemoveAt(i);
						i--;
						continue;
					}
					if (param == 18)
					{
						State.Plane = ArcPlane.ZX;
						Words.RemoveAt(i);
						i--;
						continue;
					}
					if (param == 19)
					{
						State.Plane = ArcPlane.YZ;
						Words.RemoveAt(i);
						i--;
						continue;
					}
					if (param == 4)
					{
						if (Words.Count >= 2 && Words[i + 1].Command == 'P')
						{
							if (Words[i + 1].Parameter < 0)
								Warnings.Add($"dwell time must be positive. (line {lineNumber})");

							Commands.Add(new Dwell() { Seconds = Math.Abs(Words[i + 1].Parameter), LineNumber = lineNumber });
							Words.RemoveAt(i + 1);
							Words.RemoveAt(i);
							i--;
							continue;
						}
					}

					Warnings.Add($"ignoring unknown command G{param}. (line {lineNumber})");
					Words.RemoveAt(i--);
					#endregion
				}
			}

			if (Words.Count == 0)
				return;

			int MotionMode = State.LastMotionMode;

			if (Words.First().Command == 'G')
			{
				MotionMode = (int)Words.First().Parameter;
				State.LastMotionMode = MotionMode;
				Words.RemoveAt(0);
			}

			if (MotionMode < 0)
				throw new ParseException("no motion mode active", lineNumber);

			double UnitMultiplier = (State.Unit == ParseUnit.Metric) ? 1 : 25.4;

			Vector3 EndPos = State.Position;

			var StartValid = State.PositionValid.All(isValid => isValid);

			if (State.DistanceMode == ParseDistanceMode.Incremental && !StartValid)
			{
				throw new ParseException("incremental motion is only allowed after an absolute position has been established (eg. with \"G90 G0 X0 Y0 Z5\")", lineNumber);
			}

			if ((MotionMode == 2 || MotionMode == 3) && !StartValid)
			{
				throw new ParseException("arcs (G2/G3) are only allowed after an absolute position has been established (eg. with \"G90 G0 X0 Y0 Z5\")", lineNumber);
			}


			#region FindEndPos
			{
				int Incremental = (State.DistanceMode == ParseDistanceMode.Incremental) ? 1 : 0;

				for (int i = 0; i < Words.Count; i++)
				{
					if (Words[i].Command != 'X')
						continue;
					EndPos.X = Words[i].Parameter * UnitMultiplier + Incremental * EndPos.X;
					Words.RemoveAt(i);
					State.PositionValid[0] = true;
					break;
				}

				for (int i = 0; i < Words.Count; i++)
				{
					if (Words[i].Command != 'Y')
						continue;
					EndPos.Y = Words[i].Parameter * UnitMultiplier + Incremental * EndPos.Y;
					Words.RemoveAt(i);
					State.PositionValid[1] = true;
					break;
				}

				for (int i = 0; i < Words.Count; i++)
				{
					if (Words[i].Command != 'Z')
						continue;
					EndPos.Z = Words[i].Parameter * UnitMultiplier + Incremental * EndPos.Z;
					Words.RemoveAt(i);
					State.PositionValid[2] = true;
					break;
				}
			}
			#endregion

			if (MotionMode != 0 && State.Feed <= 0)
			{
				throw new ParseException("feed rate undefined", lineNumber);
			}

			if (MotionMode == 1 && !StartValid)
			{
				Warnings.Add($"a feed move is used before an absolute position is established, height maps will not be applied to this motion. (line {lineNumber})");
			}

			if (MotionMode <= 1)
			{
				if (Words.Count > 0)
					Warnings.Add($"motion command must be last in line (ignoring unused words {string.Join(" ", Words)} in block). (line {lineNumber})");

				Line motion = new Line();
				motion.Start = State.Position;
				motion.End = EndPos;
				motion.Feed = State.Feed;
				motion.Rapid = MotionMode == 0;
				motion.LineNumber = lineNumber;
				motion.StartValid = StartValid;
				State.PositionValid.CopyTo(motion.PositionValid, 0);

				Commands.Add(motion);
				State.Position = EndPos;
				return;
			}

			double U, V;

			bool IJKused = false;

			switch (State.Plane)
			{
				default:
					U = State.Position.X;
					V = State.Position.Y;
					break;
				case ArcPlane.YZ:
					U = State.Position.Y;
					V = State.Position.Z;
					break;
				case ArcPlane.ZX:
					U = State.Position.Z;
					V = State.Position.X;
					break;
			}

			#region FindIJK
			{
				int ArcIncremental = (State.ArcDistanceMode == ParseDistanceMode.Incremental) ? 1 : 0;

				for (int i = 0; i < Words.Count; i++)
				{
					if (Words[i].Command != 'I')
						continue;

					switch (State.Plane)
					{
						case ArcPlane.XY:
							U = Words[i].Parameter * UnitMultiplier + ArcIncremental * State.Position.X;
							break;
						case ArcPlane.YZ:
							throw new ParseException("current plane is YZ, I word is invalid", lineNumber);
						case ArcPlane.ZX:
							V = Words[i].Parameter * UnitMultiplier + ArcIncremental * State.Position.X;
							break;
					}

					IJKused = true;
					Words.RemoveAt(i);
					break;
				}

				for (int i = 0; i < Words.Count; i++)
				{
					if (Words[i].Command != 'J')
						continue;

					switch (State.Plane)
					{
						case ArcPlane.XY:
							V = Words[i].Parameter * UnitMultiplier + ArcIncremental * State.Position.Y;
							break;
						case ArcPlane.YZ:
							U = Words[i].Parameter * UnitMultiplier + ArcIncremental * State.Position.Y;
							break;
						case ArcPlane.ZX:
							throw new ParseException("current plane is ZX, J word is invalid", lineNumber);
					}

					IJKused = true;
					Words.RemoveAt(i);
					break;
				}

				for (int i = 0; i < Words.Count; i++)
				{
					if (Words[i].Command != 'K')
						continue;

					switch (State.Plane)
					{
						case ArcPlane.XY:
							throw new ParseException("current plane is XY, K word is invalid", lineNumber);
						case ArcPlane.YZ:
							V = Words[i].Parameter * UnitMultiplier + ArcIncremental * State.Position.Z;
							break;
						case ArcPlane.ZX:
							U = Words[i].Parameter * UnitMultiplier + ArcIncremental * State.Position.Z;
							break;
					}

					IJKused = true;
					Words.RemoveAt(i);
					break;
				}
			}
			#endregion

			#region ResolveRadius
			for (int i = 0; i < Words.Count; i++)
			{
				if (Words[i].Command != 'R')
					continue;

				if (IJKused)
					throw new ParseException("both IJK and R notation used", lineNumber);

				if (State.Position == EndPos)
					throw new ParseException("arcs in R-notation must have non-coincident start and end points", lineNumber);

				double Radius = Words[i].Parameter * UnitMultiplier;

				if (Radius == 0)
					throw new ParseException("radius can't be zero", lineNumber);

				double A, B;

				switch (State.Plane)
				{
					default:
						A = EndPos.X;
						B = EndPos.Y;
						break;
					case ArcPlane.YZ:
						A = EndPos.Y;
						B = EndPos.Z;
						break;
					case ArcPlane.ZX:
						A = EndPos.Z;
						B = EndPos.X;
						break;
				}

				A -= U;     //(AB) = vector from start to end of arc along the axes of the current plane
				B -= V;

				//see grbl/gcode.c
				double h_x2_div_d = 4.0 * (Radius * Radius) - (A * A + B * B);
				if (h_x2_div_d < 0)
				{
					throw new ParseException("arc radius too small to reach both ends", lineNumber);
				}

				h_x2_div_d = -Math.Sqrt(h_x2_div_d) / Math.Sqrt(A * A + B * B);

				if (MotionMode == 3 ^ Radius < 0)
				{
					h_x2_div_d = -h_x2_div_d;
				}

				U += 0.5 * (A - (B * h_x2_div_d));
				V += 0.5 * (B + (A * h_x2_div_d));

				Words.RemoveAt(i);
				break;
			}
			#endregion

			if (Words.Count > 0)
				Warnings.Add($"motion command must be last in line (ignoring unused words {string.Join(" ", Words)} in block). (line {lineNumber})");

			Arc arc = new Arc();
			arc.Start = State.Position;
			arc.End = EndPos;
			arc.Feed = State.Feed;
			arc.Direction = (MotionMode == 2) ? ArcDirection.CW : ArcDirection.CCW;
			arc.U = U;
			arc.V = V;
			arc.LineNumber = lineNumber;
			arc.Plane = State.Plane;

			Commands.Add(arc);
			State.Position = EndPos;
			return;
		}
	}
}
