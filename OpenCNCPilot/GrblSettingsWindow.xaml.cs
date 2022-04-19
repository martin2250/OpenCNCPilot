using Microsoft.Win32;
using OpenCNCPilot.Util;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace OpenCNCPilot
{
	public partial class GrblSettingsWindow : Window
	{
        OpenFileDialog openFileDialogSettings = new OpenFileDialog() { Filter = Constants.FileFilterSettings };
        SaveFileDialog saveFileDialogSettings = new SaveFileDialog() { Filter = Constants.FileFilterSettings };
        Dictionary<int, double> CurrentSettings = new Dictionary<int, double>();
		Dictionary<int, TextBox> SettingsBoxes = new Dictionary<int, TextBox>();
		private bool noupdate = false;

		public event Action<string> SendLine;

		public GrblSettingsWindow()
		{
			InitializeComponent();
		}

		static Regex settingParser = new Regex(@"\$([0-9]+)=([0-9\.]+)");
		public void LineReceived(string line)
		{
			if (!line.StartsWith("$"))
				return;

			try
			{
				Match m = settingParser.Match(line);
				int number = int.Parse(m.Groups[1].Value);
				double value = double.Parse(m.Groups[2].Value, Util.Constants.DecimalParseFormat);

				if (!CurrentSettings.ContainsKey(number))
				{
					RowDefinition rowDef = new RowDefinition();
					rowDef.Height = new GridLength(25);
					gridMain.RowDefinitions.Add(rowDef);

					TextBox valBox = new TextBox
					{
						Text = value.ToString(Util.Constants.DecimalOutputFormat),
						VerticalAlignment = VerticalAlignment.Center
					};
					Grid.SetRow(valBox, gridMain.RowDefinitions.Count - 1);
					Grid.SetColumn(valBox, 1);
					gridMain.Children.Add(valBox);

					TextBlock num = new TextBlock
					{
						Text = $"${number}=",
						HorizontalAlignment = HorizontalAlignment.Right,
						VerticalAlignment = VerticalAlignment.Center
					};
					Grid.SetRow(num, gridMain.RowDefinitions.Count - 1);
					Grid.SetColumn(num, 0);
					gridMain.Children.Add(num);

					if (Util.GrblCodeTranslator.Settings.ContainsKey(number))
					{
						Tuple<string, string, string> labels = Util.GrblCodeTranslator.Settings[number];

						TextBlock name = new TextBlock
						{
							Text = labels.Item1,
							VerticalAlignment = VerticalAlignment.Center
						};
						Grid.SetRow(name, gridMain.RowDefinitions.Count - 1);
						Grid.SetColumn(name, 0);
						gridMain.Children.Add(name);

						TextBlock unit = new TextBlock
						{
							Text = labels.Item2,
							VerticalAlignment = VerticalAlignment.Center
						};
						Grid.SetRow(unit, gridMain.RowDefinitions.Count - 1);
						Grid.SetColumn(unit, 2);
						gridMain.Children.Add(unit);

						valBox.ToolTip = $"{labels.Item1} ({labels.Item2}):\n{labels.Item3}";
					}

					if (!noupdate) {
						CurrentSettings.Add(number, value);
					}
					SettingsBoxes.Add(number, valBox);
				}
				else
				{
					SettingsBoxes[number].Text = value.ToString(Util.Constants.DecimalOutputFormat);
					if (!noupdate)
					{
						CurrentSettings[number] = value;
					}
				}
			}
			catch { }
		}

		private async void ButtonApply_Click(object sender, RoutedEventArgs e)
		{
			List<Tuple<int, double>> ToSend = new List<Tuple<int, double>>();

			foreach (KeyValuePair<int, double> kvp in CurrentSettings)
			{
				double newval;

				if (!double.TryParse(SettingsBoxes[kvp.Key].Text, System.Globalization.NumberStyles.Float, Util.Constants.DecimalParseFormat, out newval))
				{
					MessageBox.Show($"Value \"{SettingsBoxes[kvp.Key].Text}\" is invalid for Setting \"{Util.GrblCodeTranslator.Settings[kvp.Key].Item1}\"");
					return;
				}

				if (newval == kvp.Value)
					continue;

				ToSend.Add(new Tuple<int, double>(kvp.Key, newval));
			}

			if (SendLine == null)
				return;

			foreach (Tuple<int, double> setting in ToSend)
			{
				SendLine.Invoke($"${setting.Item1}={setting.Item2.ToString(Util.Constants.DecimalOutputFormat)}");
				CurrentSettings[setting.Item1] = setting.Item2;
				await Task.Delay(Properties.Settings.Default.SettingsSendDelay);
			}
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			e.Cancel = true;
			Hide();
		}

        private void ButtonImport_Click(object sender, RoutedEventArgs e)
        {
			if((bool)openFileDialogSettings.ShowDialog())
            {
				string fileName = openFileDialogSettings.FileName;

				string[] settings = System.IO.File.ReadAllLines(fileName);

				foreach (string setting in settings)
                {
					this.noupdate = true;
					LineReceived(setting);
					this.noupdate = false;
				}
			}

		}

        private void ButtonExport_Click(object sender, RoutedEventArgs e)
        {
			List<Tuple<int, double>> ToSend = new List<Tuple<int, double>>();
			string fileOutput = "";

			foreach (KeyValuePair<int, double> kvp in CurrentSettings)
			{
				double newval;

				if (!double.TryParse(SettingsBoxes[kvp.Key].Text, System.Globalization.NumberStyles.Float, Util.Constants.DecimalParseFormat, out newval))
				{
					MessageBox.Show($"Value \"{SettingsBoxes[kvp.Key].Text}\" is invalid for Setting \"{Util.GrblCodeTranslator.Settings[kvp.Key].Item1}\"");
					return;
				}

				fileOutput += "$" + kvp.Key.ToString() + "=" + newval.ToString() + Environment.NewLine;
			}

			if((bool)saveFileDialogSettings.ShowDialog())
            {
				System.IO.File.WriteAllText(saveFileDialogSettings.FileName, fileOutput);
			}
		}
    }
}
