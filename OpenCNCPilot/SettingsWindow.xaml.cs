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
	}
}
