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
	/// Interaction logic for EnterNumberWindow.xaml
	/// </summary>
	public partial class EnterNumberWindow : Window
	{
		public double Value { get; set; }
		public bool Ok { get; private set; } = false;
		public event Action<double> User_Ok;
		public event Action User_Cancel;

		public EnterNumberWindow(double defaultValue)
		{
			Value = defaultValue;
			InitializeComponent();
			textBox.SelectAll();
		}

		private void buttonCancel_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void buttonOk_Click(object sender, RoutedEventArgs e)
		{
			Ok = true;

			if (User_Ok != null)
				User_Ok.Invoke(Value);

			Close();
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (!Ok && User_Cancel != null)
				User_Cancel.Invoke();
		}

		private void textBox_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				e.Handled = true;
				((TextBox)sender).GetBindingExpression(TextBox.TextProperty).UpdateSource();
				buttonOk_Click(null, null);
			}
			else if (e.Key == Key.Escape)
			{
				e.Handled = true;
				buttonCancel_Click(null, null);
			}
		}
	}
}
