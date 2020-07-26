using HelixToolkit.Wpf;
using OpenCNCPilot.GCode.GCodeCommands;
using OpenCNCPilot.Properties;
using OpenCNCPilot.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Media.Media3D;

namespace OpenCNCPilot.GCode
{
	class GCodeFile
	{
		public ReadOnlyCollection<Command> Toolpath;
		public string FileName = string.Empty;

		public Vector3 Min { get; private set; }
		public Vector3 Max { get; private set; }
		public Vector3 Size { get; private set; }

		public Vector3 MinFeed { get; private set; }
		public Vector3 MaxFeed { get; private set; }
		public Vector3 SizeFeed { get; private set; }

		public bool ContainsMotion { get; private set; } = false;

		public double TravelDistance { get; private set; } = 0;
		public TimeSpan TotalTime { get; private set; } = TimeSpan.Zero;

		public List<string> Warnings = new List<string>();

		private GCodeFile(List<Command> toolpath)
		{
			for (int i = 0; i < toolpath.Count; i++)
			{
				Command c = toolpath[i];

				if (c is Motion)
				{
					Motion m = (Motion)c;

					if (m.Start == m.End)
					{
						Warnings.Add($"ignoring zero-length move from line number {m.LineNumber}");
						toolpath.RemoveAt(i--);
					}
				}
			}

			Toolpath = new ReadOnlyCollection<Command>(toolpath);

			Vector3 min = Vector3.MaxValue, max = Vector3.MinValue;
			Vector3 minfeed = Vector3.MaxValue, maxfeed = Vector3.MinValue;

			foreach (Command c in Toolpath)
			{
				if (c is Line)
				{
					Line l = (Line)c;
					// StartValid should be sufficient, keep PositionValid just to be sure
					if (!l.StartValid || l.PositionValid.Any(isValid => !isValid))
						continue;
				}

				if (c is Motion)
				{
					ContainsMotion = true;

					Motion m = (Motion)c;

					TravelDistance += m.Length;

					if (m is Line && !((Line)m).Rapid && ((Line)m).Feed > 0.0)
						TotalTime += TimeSpan.FromMinutes(m.Length / m.Feed);

					min = Vector3.ElementwiseMin(min, m.End);
					max = Vector3.ElementwiseMax(max, m.End);
					min = Vector3.ElementwiseMin(min, m.Start);
					max = Vector3.ElementwiseMax(max, m.Start);

					if (m is Line && (m as Line).Rapid)
						continue;

					minfeed = Vector3.ElementwiseMin(minfeed, m.End);
					maxfeed = Vector3.ElementwiseMax(maxfeed, m.End);
					minfeed = Vector3.ElementwiseMin(minfeed, m.Start);
					maxfeed = Vector3.ElementwiseMax(maxfeed, m.Start);
				}
			}

			Max = max;
			Min = min;
			Vector3 size = Max - Min;

			for (int i = 0; i < 3; i++)
			{
				if (size[i] < 0)
					size[i] = 0;
			}

			Size = size;

			MaxFeed = maxfeed;
			MinFeed = minfeed;
			Vector3 sizefeed = MaxFeed - MinFeed;

			for (int i = 0; i < 3; i++)
			{
				if (sizefeed[i] < 0)
					sizefeed[i] = 0;
			}

			SizeFeed = sizefeed;
		}

		public static GCodeFile Load(string path)
		{
			GCodeParser.Reset();
			GCodeParser.ParseFile(path);

			GCodeFile gcodeFile = new GCodeFile(GCodeParser.Commands) { FileName = path.Substring(path.LastIndexOf('\\') + 1) };
			gcodeFile.Warnings.InsertRange(0, GCodeParser.Warnings);
			return gcodeFile;
		}

		public static GCodeFile FromList(IEnumerable<string> file)
		{
			GCodeParser.Reset();
			GCodeParser.Parse(file);

			GCodeFile gcodeFile = new GCodeFile(GCodeParser.Commands) { FileName = "output.nc" };
			gcodeFile.Warnings.InsertRange(0, GCodeParser.Warnings);
			return gcodeFile;
		}

		public static GCodeFile Empty
		{
			get
			{
				return new GCodeFile(new List<Command>());
			}
		}

		public void Save(string path)
		{
			File.WriteAllLines(path, GetGCode());
		}

		public void GetModel(LinesVisual3D line, LinesVisual3D rapid, LinesVisual3D arc)
		{
			var sw = System.Diagnostics.Stopwatch.StartNew();

			Point3DCollection linePoints = new Point3DCollection();
			Point3DCollection rapidPoints = new Point3DCollection();
			Point3DCollection arcPoints = new Point3DCollection();

			foreach (Command c in Toolpath)
			{
				var l = c as Line;

				if (l != null)
				{
					if (!l.StartValid)
						continue;

					if (l.Rapid)
					{
						rapidPoints.Add(l.Start.ToPoint3D());
						rapidPoints.Add(l.End.ToPoint3D());
					}
					else
					{
						linePoints.Add(l.Start.ToPoint3D());
						linePoints.Add(l.End.ToPoint3D());
					}

					continue;
				}

				var a = c as Arc;

				if (a != null)
				{
					foreach (Motion sub in a.Split(Settings.Default.ViewportArcSplit))
					{
						arcPoints.Add(sub.Start.ToPoint3D());
						arcPoints.Add(sub.End.ToPoint3D());
					}
				}
			}

			line.Points = linePoints;
			rapid.Points = rapidPoints;
			arc.Points = arcPoints;

			sw.Stop();
			Console.WriteLine("Generating the toolpath model took {0} ms", sw.ElapsedMilliseconds);
		}

		public List<string> GetGCode()
		{
			List<string> GCode = new List<string>(Toolpath.Count + 1) { "G90 G91.1 G21 G17" };

			NumberFormatInfo nfi = new NumberFormatInfo();
			nfi.NumberDecimalSeparator = ".";   //prevent problems with international versions of windows (eg Germany would write 25.4 as 25,4 which is not compatible with standard GCode)

			ParserState State = new ParserState();
			var xyz = "XYZ";

			foreach (Command c in Toolpath)
			{
				if (c is Motion)
				{
					Motion m = c as Motion;

					if (m.Feed != State.Feed)
					{
						GCode.Add(string.Format(nfi, "F{0:0.###}", m.Feed));

						State.Feed = m.Feed;
					}
				}

				if (c is Line)
				{
					Line l = c as Line;

					string code = l.Rapid ? "G0" : "G1";

					for (int i = 0; i < 3; i++)
					{
						if (!l.PositionValid[i])
							continue;
						if (!l.StartValid || State.Position[i] != l.End[i])
							code += string.Format(nfi, " {0}{1:0.###}", xyz[i], l.End[i]);
					}

					GCode.Add(code);

					State.Position = l.End;

					continue;
				}

				if (c is Arc)
				{
					Arc a = c as Arc;

					if (State.Plane != a.Plane)
					{
						switch (a.Plane)
						{
							case ArcPlane.XY:
								GCode.Add("G17");
								break;
							case ArcPlane.YZ:
								GCode.Add("G19");
								break;
							case ArcPlane.ZX:
								GCode.Add("G18");
								break;
						}
						State.Plane = a.Plane;
					}

					string code = a.Direction == ArcDirection.CW ? "G2" : "G3";

					if (State.Position.X != a.End.X)
						code += string.Format(nfi, " X{0:0.###}", a.End.X);
					if (State.Position.Y != a.End.Y)
						code += string.Format(nfi, " Y{0:0.###}", a.End.Y);
					if (State.Position.Z != a.End.Z)
						code += string.Format(nfi, " Z{0:0.###}", a.End.Z);

					Vector3 Center = new Vector3(a.U, a.V, 0).RollComponents((int)a.Plane) - State.Position;

					if (Center.X != 0 && a.Plane != ArcPlane.YZ)
						code += string.Format(nfi, " I{0:0.###}", Center.X);
					if (Center.Y != 0 && a.Plane != ArcPlane.ZX)
						code += string.Format(nfi, " J{0:0.###}", Center.Y);
					if (Center.Z != 0 && a.Plane != ArcPlane.XY)
						code += string.Format(nfi, " K{0:0.###}", Center.Z);

					GCode.Add(code);
					State.Position = a.End;

					continue;
				}

				if (c is MCode)
				{
					int code = ((MCode)c).Code;
					if (!Settings.Default.GCodeIncludeMEnd)
					{
						if (code == 2 || code == 30)
							continue;
					}
					GCode.Add($"M{code}");

					continue;
				}

				if (c is Spindle)
				{
					if (!Settings.Default.GCodeIncludeSpindle)
						continue;

					GCode.Add(string.Format(nfi, "S{0}", ((Spindle)c).Speed));

					continue;
				}

				if (c is Dwell)
				{
					if (!Settings.Default.GCodeIncludeDwell)
						continue;

					GCode.Add(string.Format(nfi, "G4 P{0}", ((Dwell)c).Seconds));

					continue;
				}
			}

			return GCode;
		}

		public GCodeFile Split(double length)
		{
			List<Command> newFile = new List<Command>();

			foreach (Command c in Toolpath)
			{
				if (c is Motion)
				{
					newFile.AddRange(((Motion)c).Split(length));
				}
				else
				{
					newFile.Add(c);
				}
			}

			return new GCodeFile(newFile);
		}

		public GCodeFile ArcsToLines(double length)
		{
			List<Command> newFile = new List<Command>();

			foreach (Command c in Toolpath)
			{
				if (c is Arc)
				{
					foreach (Arc segment in ((Arc)c).Split(length).Cast<Arc>())
					{
						Line l = new Line();
						l.Start = segment.Start;
						l.End = segment.End;
						l.Feed = segment.Feed;
						l.Rapid = false;
						l.PositionValid = new bool[] { true, true, true };
						l.StartValid = true;
						newFile.Add(l);
					}
				}
				else
				{
					newFile.Add(c);
				}
			}

			return new GCodeFile(newFile);
		}

		public GCodeFile ApplyHeightMap(HeightMap map)
		{
			double segmentLength = Math.Min(map.GridX, map.GridY);

			List<Command> newToolPath = new List<Command>();

			foreach (Command command in Toolpath)
			{
				if (command is Motion)
				{
					Motion m = (Motion)command;

					if (m is Arc)
					{
						Arc a = m as Arc;
						if (a.Plane != ArcPlane.XY)
							throw new Exception("GCode contains arcs in YZ or XZ plane (G18/19), can't apply height map. Use 'Arcs to Lines' if you really need this.");
					}

					if (m is Line)
					{
						Line l = (Line)m;

						// do not split up or modify any lines that are rapid or not fully defined
						if (!l.StartValid || l.PositionValid.Any(isValid => !isValid) || l.Rapid)
						{
							newToolPath.Add(l);
							continue;
						}
					}

					foreach (Motion subMotion in m.Split(segmentLength))
					{
						subMotion.Start.Z += map.InterpolateZ(subMotion.Start.X, subMotion.Start.Y);
						subMotion.End.Z += map.InterpolateZ(subMotion.End.X, subMotion.End.Y);

						newToolPath.Add(subMotion);
					}
				}
				else
				{
					newToolPath.Add(command);
					continue;
				}
			}

			return new GCodeFile(newToolPath);
		}

		public GCodeFile RotateCW()
		{
			List<Command> newFile = new List<Command>();

			foreach (Command oldCommand in Toolpath)
			{
				if (oldCommand is Motion)
				{
					Motion oldMotion = (Motion)oldCommand;
					Motion newMotion;

					if (oldCommand is Arc)
					{
						Arc oldArc = (Arc)oldMotion;
						Arc newArc = new Arc();

						// would be possible, but I'm too lazy to implement this properly
						if (oldArc.Plane != ArcPlane.XY)
							throw new Exception("GCode contains arcs in YZ or XZ plane (G18/19), can't rotate gcode. Use 'Arcs to Lines' if you really need this.");

						newArc.Direction = oldArc.Direction;
						newArc.Plane = oldArc.Plane;
						newArc.U = oldArc.V;
						newArc.V = -oldArc.U;
						newMotion = newArc;
					}
					else if (oldCommand is Line)
					{
						Line oldLine = (Line)oldMotion;
						Line newLine = new Line();
						newLine.Rapid = oldLine.Rapid;
						newLine.PositionValid[0] = oldLine.PositionValid[1];
						newLine.PositionValid[1] = oldLine.PositionValid[0];
						newLine.PositionValid[2] = oldLine.PositionValid[2];
						newLine.StartValid = oldLine.StartValid;
						newMotion = newLine;
					}
					else
						throw new Exception("this shouldn't happen, please contact the autor on GitHub");

					newMotion.Start = oldMotion.Start;
					newMotion.End = oldMotion.End;
					newMotion.Start.X = oldMotion.Start.Y;
					newMotion.Start.Y = -oldMotion.Start.X;
					newMotion.End.X = oldMotion.End.Y;
					newMotion.End.Y = -oldMotion.End.X;

					newMotion.Feed = oldMotion.Feed;

					newFile.Add(newMotion);
				}
				else
				{
					newFile.Add(oldCommand);
				}
			}

			return new GCodeFile(newFile);
		}
	}
}
