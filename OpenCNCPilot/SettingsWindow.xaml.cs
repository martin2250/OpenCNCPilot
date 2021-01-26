using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.IO;
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
			if(Properties.Settings.Default.ConnectionType == OpenCNCPilot.Communication.ConnectionType.Serial){
				SerialType.IsChecked = true;
				EthernetSettings.Visibility = Visibility.Hidden;
				SerialSettings.Visibility = Visibility.Visible;
			}
			if (Properties.Settings.Default.ConnectionType == OpenCNCPilot.Communication.ConnectionType.Ethernet)
			{
				EthernetType.IsChecked = true;
				EthernetSettings.Visibility = Visibility.Visible;
				SerialSettings.Visibility = Visibility.Hidden;
			}
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

        private void SerialType_Checked(object sender, RoutedEventArgs e)
        {
			Properties.Settings.Default.ConnectionType = OpenCNCPilot.Communication.ConnectionType.Serial;
			EthernetSettings.Visibility = Visibility.Hidden;
			SerialSettings.Visibility = Visibility.Visible;
		}

        private void EthernetType_Checked(object sender, RoutedEventArgs e)
        {
			Properties.Settings.Default.ConnectionType = OpenCNCPilot.Communication.ConnectionType.Ethernet;
			EthernetSettings.Visibility = Visibility.Visible;
			SerialSettings.Visibility = Visibility.Hidden;
		}

    }
}
