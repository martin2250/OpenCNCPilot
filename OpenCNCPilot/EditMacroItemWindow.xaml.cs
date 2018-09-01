using System.Linq;
using System.Windows;

namespace OpenCNCPilot
{
	public partial class EditMacroItemWindow : Window
	{
		public string MacroName { get; set; }
		public string Commands { get; set; }
		public bool UseMacros { get; set; }
		public bool Ok { get; set; } = false;

		public EditMacroItemWindow(string name, string commands, bool useMacros)
		{
			MacroName = name;
			Commands = commands;
			UseMacros = useMacros;

			InitializeComponent();
		}

		private void ButtonOk_Click(object sender, RoutedEventArgs e)
		{
			if (MacroName.Contains(':') || MacroName.Contains(';') || Commands.Contains(':') || Commands.Contains(';'))
			{
				MessageBox.Show("Name and Commands can't include ':' or ';'");
				return;
			}
			Ok = true;
			Close();
		}

		private void ButtonCancel_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}
