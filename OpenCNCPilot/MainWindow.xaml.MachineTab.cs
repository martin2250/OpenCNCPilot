using System;
using System.Windows;

namespace OpenCNCPilot
{
	partial class MainWindow
	{
		private void ButtonSettings_Click(object sender, RoutedEventArgs e)
		{
			if (machine.Mode != Communication.Machine.OperatingMode.Disconnected)
				return;

			new SettingsWindow().ShowDialog();
		}

		private void ButtonConnect_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				machine.Connect();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void ButtonDisconnect_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				machine.Disconnect();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			Properties.Settings.Default.Save();

			if (machine.Connected)
			{
				MessageBox.Show("Can't close while connected!");
				e.Cancel = true;
				return;
			}

			settingsWindow.Close();
			Application.Current.Shutdown();
		}

		private void ButtonSyncBuffer_Click(object sender, RoutedEventArgs e)
		{
			if (machine.Mode != Communication.Machine.OperatingMode.Manual)
				return;

			machine.SyncBuffer = true;
		}

		private void ShowGrblSettings_Click(object sender, RoutedEventArgs e)
		{
			if (machine.Mode != Communication.Machine.OperatingMode.Manual)
				return;

			machine.SendLine("$$");
			settingsWindow.ShowDialog();
		}

		private void ButtonMachineHome_Click(object sender, RoutedEventArgs e)
		{
			if (machine.Mode != Communication.Machine.OperatingMode.Manual)
				return;

			machine.SendLine("$H");
		}
	}
}
