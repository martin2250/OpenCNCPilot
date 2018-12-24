using HelixToolkit.Wpf;
using OpenCNCPilot.Util;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Media3D;
using System.Xml;

namespace OpenCNCPilot.GCode
{
	public class HeightMap
	{
		public double?[,] Points { get; private set; }
		public int SizeX { get; private set; }
		public int SizeY { get; private set; }

		public int Progress { get { return TotalPoints - NotProbed.Count; } }
		public int TotalPoints { get { return SizeX * SizeY; } }

		public List<Tuple<int, int>> NotProbed { get; private set; } = new List<Tuple<int, int>>();

		public Vector2 Min { get; private set; }
		public Vector2 Max { get; private set; }

		public Vector2 Delta { get { return Max - Min; } }

		public double MinHeight { get; private set; } = double.MaxValue;
		public double MaxHeight { get; private set; } = double.MinValue;

		public event Action MapUpdated;

		public double GridX { get { return (Max.X - Min.X) / (SizeX - 1); } }
		public double GridY { get { return (Max.Y - Min.Y) / (SizeY - 1); } }


		public HeightMap(double gridSize, Vector2 min, Vector2 max)
		{
			if (min.X == max.X || min.Y == max.Y)
				throw new Exception("Height map can't be infinitely narrow");

			int pointsX = (int)Math.Ceiling((max.X - min.X) / gridSize) + 1;
			int pointsY = (int)Math.Ceiling((max.Y - min.Y) / gridSize) + 1;

			if (pointsX < 2 || pointsY < 2)
				throw new Exception("Height map must have at least 4 points");

			Points = new double?[pointsX, pointsY];

			if (max.X < min.X)
			{
				double a = min.X;
				min.X = max.X;
				max.X = a;
			}

			if (max.Y < min.Y)
			{
				double a = min.Y;
				min.Y = max.Y;
				max.Y = a;
			}

			Min = min;
			Max = max;

			SizeX = pointsX;
			SizeY = pointsY;


			for (int x = 0; x < SizeX; x++)
			{
				for (int y = 0; y < SizeY; y++)
					NotProbed.Add(new Tuple<int, int>(x, y));
			}
		}

		public double InterpolateZ(double x, double y)
		{
			if (x > Max.X || x < Min.X || y > Max.Y || y < Min.Y)
				return MaxHeight;

			x -= Min.X;
			y -= Min.Y;

			x /= GridX;
			y /= GridY;

			int iLX = (int)Math.Floor(x);   //lower integer part
			int iLY = (int)Math.Floor(y);

			int iHX = (int)Math.Ceiling(x); //upper integer part
			int iHY = (int)Math.Ceiling(y);

			double fX = x - iLX;             //fractional part
			double fY = y - iLY;

			double linUpper = Points[iHX, iHY].Value * fX + Points[iLX, iHY].Value * (1 - fX);       //linear immediates
			double linLower = Points[iHX, iLY].Value * fX + Points[iLX, iLY].Value * (1 - fX);

			return linUpper * fY + linLower * (1 - fY);     //bilinear result
		}

		public Vector2 GetCoordinates(int x, int y)
		{
			return new Vector2(x * (Delta.X / (SizeX - 1)) + Min.X, y * (Delta.Y / (SizeY - 1)) + Min.Y);
		}

		public Vector2 GetCoordinates(Tuple<int, int> index)
		{
			return GetCoordinates(index.Item1, index.Item2);
		}

		private HeightMap()
		{

		}

		public void AddPoint(int x, int y, double height)
		{
			Points[x, y] = height;

			if (height > MaxHeight)
				MaxHeight = height;
			if (height < MinHeight)
				MinHeight = height;

			if (MapUpdated != null)
				MapUpdated();
		}

		public static HeightMap Load(string path)
		{
			HeightMap map = new HeightMap();

			XmlReader r = XmlReader.Create(path);

			while (r.Read())
			{
				if (!r.IsStartElement())
					continue;

				switch (r.Name)
				{
					case "heightmap":
						map.Min = new Vector2(double.Parse(r["MinX"], Constants.DecimalParseFormat), double.Parse(r["MinY"], Constants.DecimalParseFormat));
						map.Max = new Vector2(double.Parse(r["MaxX"], Constants.DecimalParseFormat), double.Parse(r["MaxY"], Constants.DecimalParseFormat));
						map.SizeX = int.Parse(r["SizeX"]);
						map.SizeY = int.Parse(r["SizeY"]);
						map.Points = new double?[map.SizeX, map.SizeY];
						break;
					case "point":
						int x = int.Parse(r["X"]), y = int.Parse(r["Y"]);
						double height = double.Parse(r.ReadInnerXml(), Constants.DecimalParseFormat);

						map.Points[x, y] = height;

						if (height > map.MaxHeight)
							map.MaxHeight = height;
						if (height < map.MinHeight)
							map.MinHeight = height;

						break;
				}
			}

			r.Dispose();

			for (int x = 0; x < map.SizeX; x++)
			{
				for (int y = 0; y < map.SizeY; y++)
					if (!map.Points[x, y].HasValue)
						map.NotProbed.Add(new Tuple<int, int>(x, y));
			}

			return map;
		}

		public void Save(string path)
		{
			XmlWriterSettings set = new XmlWriterSettings();
			set.Indent = true;
			XmlWriter w = XmlWriter.Create(path, set);
			w.WriteStartDocument();
			w.WriteStartElement("heightmap");
			w.WriteAttributeString("MinX", Min.X.ToString(Constants.DecimalParseFormat));
			w.WriteAttributeString("MinY", Min.Y.ToString(Constants.DecimalParseFormat));
			w.WriteAttributeString("MaxX", Max.X.ToString(Constants.DecimalParseFormat));
			w.WriteAttributeString("MaxY", Max.Y.ToString(Constants.DecimalParseFormat));
			w.WriteAttributeString("SizeX", SizeX.ToString(Constants.DecimalParseFormat));
			w.WriteAttributeString("SizeY", SizeY.ToString(Constants.DecimalParseFormat));

			for (int x = 0; x < SizeX; x++)
			{
				for (int y = 0; y < SizeY; y++)
				{
					if (!Points[x, y].HasValue)
						continue;

					w.WriteStartElement("point");
					w.WriteAttributeString("X", x.ToString());
					w.WriteAttributeString("Y", y.ToString());
					w.WriteString(Points[x, y].Value.ToString(Constants.DecimalParseFormat));
					w.WriteEndElement();
				}
			}
			w.WriteEndElement();
			w.Close();
		}

		public void GetModel(MeshGeometryVisual3D mesh)
		{
			MeshBuilder mb = new MeshBuilder(false, true);

			double Hdelta = MaxHeight - MinHeight;

			for (int x = 0; x < SizeX - 1; x++)
			{
				for (int y = 0; y < SizeY - 1; y++)
				{
					if (!Points[x, y].HasValue || !Points[x, y + 1].HasValue || !Points[x + 1, y].HasValue || !Points[x + 1, y + 1].HasValue)
						continue;

					mb.AddQuad(
						new Point3D(Min.X + (x + 1) * Delta.X / (SizeX - 1), Min.Y + (y) * Delta.Y / (SizeY - 1), Points[x + 1, y].Value),
						new Point3D(Min.X + (x + 1) * Delta.X / (SizeX - 1), Min.Y + (y + 1) * Delta.Y / (SizeY - 1), Points[x + 1, y + 1].Value),
						new Point3D(Min.X + (x) * Delta.X / (SizeX - 1), Min.Y + (y + 1) * Delta.Y / (SizeY - 1), Points[x, y + 1].Value),
						new Point3D(Min.X + (x) * Delta.X / (SizeX - 1), Min.Y + (y) * Delta.Y / (SizeY - 1), Points[x, y].Value),
						new Point(0, (Points[x + 1, y].Value - MinHeight) * Hdelta),
						new Point(0, (Points[x + 1, y + 1].Value - MinHeight) * Hdelta),
						new Point(0, (Points[x, y + 1].Value - MinHeight) * Hdelta),
						new Point(0, (Points[x, y].Value - MinHeight) * Hdelta)
						);
				}
			}

			mesh.MeshGeometry = mb.ToMesh();
		}

		public void GetPreviewModel(LinesVisual3D border, PointsVisual3D pointv)
		{
			GetPreviewModel(Min, Max, SizeX, SizeY, border, pointv);
		}

		public void FillWithTestPattern(string pattern)
		{
			martin2250.Calculator.Expression expr = martin2250.Calculator.Expression.Parse(pattern);

			for (int x = 0; x < SizeX; x++)
			{
				for (int y = 0; y < SizeY; y++)
				{
					Dictionary<string, double> variables = new Dictionary<string, double>();

					variables.Add("X", (x * (Max.X - Min.X)) / (SizeX - 1) + Min.X);
					variables.Add("Y", (y * (Max.Y - Min.Y)) / (SizeY - 1) + Min.Y);

					AddPoint(x, y, expr.GetValue(variables));
				}
			}
		}

		public static void GetPreviewModel(Vector2 min, Vector2 max, double gridSize, LinesVisual3D border, PointsVisual3D pointv)
		{
			Vector2 min_temp = new Vector2(Math.Min(min.X, max.X), Math.Min(min.Y, max.Y));
			Vector2 max_temp = new Vector2(Math.Max(min.X, max.X), Math.Max(min.Y, max.Y));

			min = min_temp;
			max = max_temp;

			if ((max.X - min.X) == 0 || (max.Y - min.Y) == 0)
			{
				pointv.Points.Clear();
				border.Points.Clear();
				return;
			}

			int pointsX = (int)Math.Ceiling((max.X - min.X) / gridSize) + 1;
			int pointsY = (int)Math.Ceiling((max.Y - min.Y) / gridSize) + 1;

			GetPreviewModel(min, max, pointsX, pointsY, border, pointv);
		}

		public static void GetPreviewModel(Vector2 min, Vector2 max, int pointsX, int pointsY, LinesVisual3D border, PointsVisual3D pointv)
		{
			Vector2 min_temp = new Vector2(Math.Min(min.X, max.X), Math.Min(min.Y, max.Y));
			Vector2 max_temp = new Vector2(Math.Max(min.X, max.X), Math.Max(min.Y, max.Y));

			min = min_temp;
			max = max_temp;

			double gridX = (max.X - min.X) / (pointsX - 1);
			double gridY = (max.Y - min.Y) / (pointsY - 1);

			Point3DCollection points = new Point3DCollection();

			for (int x = 0; x < pointsX; x++)
			{
				for (int y = 0; y < pointsY; y++)
				{
					points.Add(new Point3D(min.X + x * gridX, min.Y + y * gridY, 0));
				}
			}

			pointv.Points.Clear();
			pointv.Points = points;

			Point3DCollection b = new Point3DCollection();
			b.Add(new Point3D(min.X, min.Y, 0));
			b.Add(new Point3D(min.X, max.Y, 0));
			b.Add(new Point3D(min.X, max.Y, 0));
			b.Add(new Point3D(max.X, max.Y, 0));
			b.Add(new Point3D(max.X, max.Y, 0));
			b.Add(new Point3D(max.X, min.Y, 0));
			b.Add(new Point3D(max.X, min.Y, 0));
			b.Add(new Point3D(min.X, min.Y, 0));

			border.Points.Clear();
			border.Points = b;
		}
	}
}
