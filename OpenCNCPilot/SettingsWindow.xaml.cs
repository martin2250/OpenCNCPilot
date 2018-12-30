using System;
using System.Management;
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

			ComboBoxSerialPort_DropDownOpened(null, null);
		}

		private void ComboBoxSerialPort_DropDownOpened(object sender, EventArgs e)
		{
			ComboBoxSerialPort.Items.Clear();

			try
			{
				ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_SerialPort");
				foreach (ManagementObject queryObj in searcher.Get())
				{
					string id = queryObj["DeviceID"] as string;
					string name = queryObj["Name"] as string;

					ComboBoxSerialPort.Items.Add(new ComboBoxItem() { Content = name, Tag = id });
				}
			}
			catch (ManagementException ex)
			{
				MessageBox.Show("An error occurred while querying for WMI data: " + ex.Message);
			}
		}

		private void Window_Closed(object sender, EventArgs e)
		{
			Properties.Settings.Default.Save();
		}
	}
}
