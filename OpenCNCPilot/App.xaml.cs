using OpenCNCPilot.Properties;
using System.Windows;

namespace OpenCNCPilot
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		private void Application_Startup(object sender, StartupEventArgs e)
		{
			if (Settings.Default.SettingsUpdateRequired)
			{
				Settings.Default.Upgrade();
				Settings.Default.SettingsUpdateRequired = false;
				Settings.Default.Save();
			}
		}
	}
}
