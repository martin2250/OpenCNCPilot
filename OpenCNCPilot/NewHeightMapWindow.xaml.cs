using OpenCNCPilot.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace OpenCNCPilot
{
	/// <summary>
	/// Interaction logic for NewHeightMapWindow.xaml
	/// </summary>
	public partial class NewHeightMapWindow : Window
	{
		static Vector2 LastMin = new Vector2();
		static Vector2 LastMax = new Vector2(100, 50);
		static string LastTestPattern = "(x * x + y * y) / 1000.0";
		static double LastGridSize = 5;
		static bool LastGenTestPattern = false;

		public event Action SelectedSizeChanged;
		public event Action Size_Ok;

		public Vector2 Min;
		public Vector2 Max;

		public bool Ok { get; set; } = false;
		public bool GenerateTestPattern { get; set; } = LastGenTestPattern;
		public string TestPattern { get; set; } = LastTestPattern;

		public double MinX
		{
			get { return Min.X; }
			set
			{
				if (Min.X == value)
					return;

				Min.X = value;

				if (SelectedSizeChanged != null)
					SelectedSizeChanged.Invoke();
			}
		}

		public double MinY
		{
			get { return Min.Y; }
			set
			{
				if (Min.Y == value)
					return;
				Min.Y = value;
				if (SelectedSizeChanged != null)
					SelectedSizeChanged.Invoke();
			}
		}

		public double MaxX
		{
			get { return Max.X; }
			set
			{
				if (Max.X == value)
					return;
				Max.X = value;
				if (SelectedSizeChanged != null)
					SelectedSizeChanged.Invoke();
			}
		}

		public double MaxY
		{
			get { return Max.Y; }
			set
			{
				if (Max.Y == value)
					return;
				Max.Y = value;
				if (SelectedSizeChanged != null)
					SelectedSizeChanged.Invoke();
			}
		}

		private double _gridSize = LastGridSize;
		public double GridSize
		{
			get { return _gridSize; }
			set
			{
				if (_gridSize == value)
					return;
				if (value == 0)
					return;
				_gridSize = Math.Abs(value);

				if (SelectedSizeChanged != null)
					SelectedSizeChanged.Invoke();
			}
		}

		public NewHeightMapWindow(Vector2 min, Vector2 max)
		{
			Min = min;
			Max = max;

			InitializeComponent();
		}

		public NewHeightMapWindow() : this(LastMin, LastMax)
		{

		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			if (Min.X > Max.X)
			{
				double a = Min.X;
				Min.X = Max.X;
				Max.X = a;
			}

			if (Min.Y > Max.Y)
			{
				double a = Min.Y;
				Min.Y = Max.Y;
				Max.Y = a;
			}

			Ok = true;

			LastMin = Min;
			LastMax = Max;
			LastGridSize = GridSize;
			LastTestPattern = TestPattern;
			LastGenTestPattern = GenerateTestPattern;

			if (Size_Ok != null)
				Size_Ok.Invoke();

			Close();
		}
	}
}
