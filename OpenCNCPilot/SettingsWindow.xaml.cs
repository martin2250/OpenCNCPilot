using System;
using System.Windows;
using System.Windows.Controls;

namespace OpenCNCPilot
{
	/// <summary>
	/// Interaction logic for SettingsWindow.xaml
	/// </summary>
	public partial class SettingsWindow : Window
	{
		public SettingsWindow()
		{
			InitializeComponent();
		}

		private void ComboBox_DropDownOpened(object sender, EventArgs e)
		{
			((ComboBox)sender).Items.Clear();

			foreach (string port in System.IO.Ports.SerialPort.GetPortNames())
				((ComboBox)sender).Items.Add(port);
		}

		private void Window_Closed(object sender, EventArgs e)
		{
			Properties.Settings.Default.Save();
		}
	}
}
