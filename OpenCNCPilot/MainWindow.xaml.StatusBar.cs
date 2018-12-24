using System.Windows;

namespace OpenCNCPilot
{
	partial class MainWindow
	{
		private void ButtonFeedHold_Click(object sender, RoutedEventArgs e)
		{
			machine.FeedHold();
		}

		private void ButtonCycleStart_Click(object sender, RoutedEventArgs e)
		{
			machine.CycleStart();
		}

		private void ButtonSoftReset_Click(object sender, RoutedEventArgs e)
		{
			machine.SoftReset();
		}
	}
}
