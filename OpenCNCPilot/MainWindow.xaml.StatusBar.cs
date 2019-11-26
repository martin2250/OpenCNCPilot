using System.Windows;

namespace OpenCNCPilot
{
	partial class MainWindow
	{
		private void ButtonFeedHold_Click(object sender, RoutedEventArgs e)
		{
			machine.FeedHold();
            machine.SendLine("M5");
        }

		private void ButtonCycleStart_Click(object sender, RoutedEventArgs e)
		{
            machine.SendLine("M3");
            //pause thread for 2 seconds to allow the spindle kickstart
            System.Threading.Thread.Sleep(2000);
            machine.CycleStart();
        }

		private void ButtonSoftReset_Click(object sender, RoutedEventArgs e)
		{
			machine.SoftReset();
		}
	}
}
