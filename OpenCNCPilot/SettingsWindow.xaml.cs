using System;
using System.Collections.Generic;
using System.IO.Ports;
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

			Dictionary<string, string> ports = new Dictionary<string, string>();

			try
			{
				ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_SerialPort");
				foreach (ManagementObject queryObj in searcher.Get())
				{
					string id = queryObj["DeviceID"] as string;
					string name = queryObj["Name"] as string;

					ports.Add(id, name);
				}
			}
			catch (ManagementException ex)
			{
				MessageBox.Show("An error occurred while querying for WMI data: " + ex.Message);
			}

			// fix error of some boards not being listed properly
			foreach (string port in SerialPort.GetPortNames())
			{
				if (!ports.ContainsKey(port))
				{
					ports.Add(port, port);
				}
			}

			foreach (var port in ports)
			{
				ComboBoxSerialPort.Items.Add(new ComboBoxItem() { Content = port.Value, Tag = port.Key });
			}
		}

		private void Window_Closed(object sender, EventArgs e)
		{
			Properties.Settings.Default.Save();
		}
	}
}
