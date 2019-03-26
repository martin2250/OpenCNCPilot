using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace OpenCNCPilot
{
	public partial class WarningWindow : Window
	{
		public WarningWindow(string header, IEnumerable<string> warnings)
		{
			InitializeComponent();

			StringBuilder b = new StringBuilder(header);
			int i = 1;

			foreach (string warning in warnings)
			{
				b.Append(i++);
				b.Append(" > ");
				b.AppendLine(warning);
			}

			TextBlockWarnings.Text = b.ToString();
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}
