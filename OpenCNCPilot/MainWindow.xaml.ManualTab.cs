using OpenCNCPilot.Communication;
using OpenCNCPilot.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace OpenCNCPilot
{
	partial class MainWindow
	{
		private List<string> ManualCommands = new List<string>();	//pos 0 is the last command sent, pos1+ are older
		private int ManualCommandIndex = -1;

		void ManualSend()
		{
			if (machine.Mode != Machine.OperatingMode.Manual)
				return;

			machine.SendLine(TextBoxManual.Text);

			ManualCommands.Insert(0, TextBoxManual.Text);
			ManualCommandIndex = -1;

			TextBoxManual.Text = "";
		}

		private void ButtonManualSend_Click(object sender, RoutedEventArgs e)
		{
			ManualSend();
		}

		private void TextBoxManual_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (e.Key == System.Windows.Input.Key.Enter)
			{
				e.Handled = true;
				ManualSend();
			}
			else if (e.Key == System.Windows.Input.Key.Down)
			{
				e.Handled = true;

				if (ManualCommandIndex == 0)
				{
					TextBoxManual.Text = "";
					ManualCommandIndex = -1;
				}
				else if (ManualCommandIndex > 0)
				{
					ManualCommandIndex--;
					TextBoxManual.Text = ManualCommands[ManualCommandIndex];
					TextBoxManual.SelectionStart = TextBoxManual.Text.Length;
				}
			}
			else if (e.Key == System.Windows.Input.Key.Up)
			{
				e.Handled = true;

				if (ManualCommands.Count > ManualCommandIndex + 1)
				{
					ManualCommandIndex++;
					TextBoxManual.Text = ManualCommands[ManualCommandIndex];
					TextBoxManual.SelectionStart = TextBoxManual.Text.Length;
				}
			}
		}

		private void ButtonManualSetG10Zero_Click(object sender, RoutedEventArgs e)
		{
			if (machine.Mode != Machine.OperatingMode.Manual)
				return;

			TextBoxManual.Text = $"G10 L2 P0 X{machine.WorkPosition.X.ToString(Constants.DecimalOutputFormat)} Y{machine.WorkPosition.Y.ToString(Constants.DecimalOutputFormat)} Z{machine.WorkPosition.Z.ToString(Constants.DecimalOutputFormat)}";
		}

		private void ButtonManualSetG92Zero_Click(object sender, RoutedEventArgs e)
		{
			if (machine.Mode != Machine.OperatingMode.Manual)
				return;

			TextBoxManual.Text = "G92 X0 Y0 Z0";
        }

		private void ButtonManualResetG10_Click(object sender, RoutedEventArgs e)
		{
			if (machine.Mode != Machine.OperatingMode.Manual)
				return;

			TextBoxManual.Text = "G10 L2 P0 X0 Y0 Z0";
        }
	}
}
