using OpenCNCPilot.Util;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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

		public Vector2 ToolPathMin = LastMin;
		public Vector2 ToolPathMax = LastMax;

		public double GCodeMargin { get; set; } = 0;

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

			UpdateTextboxes();

			EventManager.RegisterClassHandler(typeof(System.Windows.Controls.TextBox), GotKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(OnGotKeyboardFocus));

			FocusManager.SetFocusedElement(this, TextBoxMinX);
			TextBoxMinX.SelectAll();
		}

		public NewHeightMapWindow() : this(LastMin, LastMax)
		{

		}

		void UpdateTextboxes()
		{
			foreach (TextBox tb in new TextBox[] { TextBoxMinX, TextBoxMaxX, TextBoxMinY, TextBoxMaxY, TextBoxGridSize })
			{
				tb.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
			}
		}

		void OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
		{
			var textBox = sender as TextBox;

			if (textBox != null && !textBox.IsReadOnly && (e.KeyboardDevice.IsKeyDown(Key.Tab) || e.KeyboardDevice.IsKeyDown(Key.Enter)))
				textBox.SelectAll();
		}

		private void ButtonOK_Click(object sender, RoutedEventArgs e)
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

		private void ButtonSizeFromCode_Click(object sender, RoutedEventArgs e)
		{
			if (GCodeMargin < 0)
			{
				MessageBox.Show("Margin must be greater than or equal to zero");
				return;
			}

			MinX = Math.Floor((ToolPathMin.X - GCodeMargin) * 100) / 100.0;
			MinY = Math.Floor((ToolPathMin.Y - GCodeMargin) * 100) / 100.0;

			MaxX = Math.Ceiling((ToolPathMax.X + GCodeMargin) * 100) / 100.0;
			MaxY = Math.Ceiling((ToolPathMax.Y + GCodeMargin) * 100) / 100.0;

			UpdateTextboxes();
		}

		private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			TextBox textBox = (TextBox)sender;
			string newText = e.Text;

			if (newText == "\r")
			{
				var request = new TraversalRequest(FocusNavigationDirection.Next);
				request.Wrapped = true;
				((TextBox)sender).MoveFocus(request);
			}

			if (textBox.Text.Contains(",") || newText.Contains(","))
			{
				int cursorPos = textBox.SelectionStart;
				textBox.Text = textBox.Text.Replace(',', '.') + newText.Replace(',', '.');
				textBox.SelectionStart = cursorPos + 1;
				e.Handled = true;
			}
		}
	}
}
