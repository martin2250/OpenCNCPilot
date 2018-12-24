using OpenCNCPilot.Communication;
using System;
using System.Windows;

namespace OpenCNCPilot
{
	partial class MainWindow
	{
		private void ButtonEditSimplify_Click(object sender, RoutedEventArgs e)
		{
			if (machine.Mode == Machine.OperatingMode.SendFile)
				return;

			machine.SetFile(ToolPath.GetGCode().ToArray());
		}

		private void ButtonEditApplyHeightMap_Click(object sender, RoutedEventArgs e)
		{
			if (machine.Mode == Machine.OperatingMode.SendFile)
				return;

			if (Map == null || Map.NotProbed.Count > 0)
			{
				MessageBox.Show("HeightMap is not ready");
				return;
			}

			try
			{
				machine.SetFile(ToolPath.ApplyHeightMap(Map).GetGCode());
				Machine_OperatingMode_Changed();
			}
			catch (IndexOutOfRangeException)
			{
				MessageBox.Show("The Toolpath is not contained in the HeightMap");
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void ButtonEditArcToLines_Click(object sender, RoutedEventArgs e)
		{
			if (machine.Mode == Machine.OperatingMode.SendFile)
				return;

			EnterNumberWindow enw = new EnterNumberWindow(Properties.Settings.Default.ArcToLineSegmentLength);
			enw.Title = "Arc Segment Length";
			enw.Owner = this;
			enw.User_Ok += Enw_User_Ok_ArcToLines;
			enw.Show();
		}

		private void Enw_User_Ok_ArcToLines(double value)
		{
			Properties.Settings.Default.ArcToLineSegmentLength = value;

			if (machine.Mode == Machine.OperatingMode.SendFile)
				return;

			machine.SetFile(ToolPath.ArcsToLines(value).GetGCode());
		}

		private void ButtonEditSplit_Click(object sender, RoutedEventArgs e)
		{
			if (machine.Mode == Machine.OperatingMode.SendFile)
				return;

			EnterNumberWindow enw = new EnterNumberWindow(Properties.Settings.Default.SplitSegmentLength);
			enw.Title = "Segment Length";
			enw.Owner = this;
			enw.User_Ok += Enw_User_Ok_Split;
			enw.Show();
		}

		private void ButtonEditRotateCW_Click(object sender, RoutedEventArgs e)
		{
			if (machine.Mode == Machine.OperatingMode.SendFile)
				return;

			try
			{
				machine.SetFile(ToolPath.RotateCW().GetGCode());
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void Enw_User_Ok_Split(double value)
		{
			Properties.Settings.Default.SplitSegmentLength = value;

			if (machine.Mode == Machine.OperatingMode.SendFile)
				return;

			machine.SetFile(ToolPath.Split(value).GetGCode());
		}
	}
}
