using OpenCNCPilot.Communication;
using OpenCNCPilot.GCode;
using OpenCNCPilot.Util;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace OpenCNCPilot
{
	partial class MainWindow
	{
		private void Machine_PlaneChanged()
		{
			ButtonArcPlane.Content = machine.Plane.ToString() + "-Plane";
		}

		private void Machine_UnitChanged()
		{
			ButtonUnit.Content = machine.Unit.ToString();
		}

		private void Machine_DistanceModeChanged()
		{
			ButtonDistanceMode.Content = machine.DistanceMode.ToString();
		}

		private void Machine_StatusChanged()
		{
			ButtonStatus.Content = machine.Status;

			if (machine.Status == "Alarm")
				ButtonStatus.Foreground = Brushes.Red;
			else if(machine.Status == "Hold")
				ButtonStatus.Foreground = Brushes.Yellow;
			else if (machine.Status == "Run")
				ButtonStatus.Foreground = Brushes.Green;
			else
				ButtonStatus.Foreground = Brushes.Black;
		}

		private void Machine_PositionUpdateReceived()
		{
			ModelTool.Point1 = (machine.WorkPosition + new Vector3(0, 0, 10)).ToPoint3D();
			ModelTool.Point2 = machine.WorkPosition.ToPoint3D();

			var nfi = Constants.DecimalOutputFormat;

			LabelPosX.Content = machine.WorkPosition.X.ToString(nfi);
			LabelPosY.Content = machine.WorkPosition.Y.ToString(nfi);
			LabelPosZ.Content = machine.WorkPosition.Z.ToString(nfi);

			LabelPosMX.Content = machine.MachinePosition.X.ToString(nfi);
			LabelPosMY.Content = machine.MachinePosition.Y.ToString(nfi);
			LabelPosMZ.Content = machine.MachinePosition.Z.ToString(nfi);
		}

		private void Machine_BufferStateChanged()
		{
			ProgressBarBufferCapacity.Value = machine.BufferState;
			LabelBufferState.Content = machine.BufferState;
		}

		private void ButtonDistanceMode_Click(object sender, RoutedEventArgs e)
		{
			if (machine.Mode != Machine.OperatingMode.Manual)
				return;

			if (machine.DistanceMode == GCode.ParseDistanceMode.Absolute)
				machine.SendLine("G91");
			else
				machine.SendLine("G90");
		}

		private void ButtonArcPlane_Click(object sender, RoutedEventArgs e)
		{
			if (machine.Mode != Machine.OperatingMode.Manual)
				return;

			if(machine.Plane != GCode.GCodeCommands.ArcPlane.XY)
				machine.SendLine("G17");
		}

		private void ButtonUnit_Click(object sender, RoutedEventArgs e)
		{
			if (machine.Mode != Machine.OperatingMode.Manual)
				return;

			if (machine.Unit == GCode.ParseUnit.Metric)
				machine.SendLine("G20");
			else
				machine.SendLine("G21");
		}

		private void ButtonStatus_Click(object sender, RoutedEventArgs e)
		{
			if (machine.Mode != Machine.OperatingMode.Manual)
				return;

			machine.SendLine("$X");
		}

		private void AddHistoryItem(ListBoxItem item)
		{
			if (ListBoxHistory.Items.Count > 8)
				ListBoxHistory.Items.RemoveAt(0);

			ListBoxHistory.Items.Add(item);
		}

		private void Machine_NonFatalException(string obj)
		{
			ListBoxItem item = new ListBoxItem();
			item.Content = obj;
			item.Foreground = Brushes.Red;
			item.FontSize = 18;

			AddHistoryItem(item);
		}

		private void Machine_Info(string obj)
		{
			ListBoxItem item = new ListBoxItem();
			item.Content = obj;
			item.Foreground = Brushes.OrangeRed;
			item.FontSize = 14;

			AddHistoryItem(item);
		}

		private void Machine_LineSent(string obj)
		{
			ListBoxItem item = new ListBoxItem();
			item.Content = obj;
			item.Foreground = Brushes.Black;
			item.FontSize = 14;

			AddHistoryItem(item);
		}

		private void Machine_LineReceived(string obj)
		{
			ListBoxItem item = new ListBoxItem();
			item.Content = obj;
			item.FontSize = 14;

			if (obj.StartsWith("error"))
				item.Foreground = Brushes.Red;
			else
				item.Foreground = Brushes.Green;

			AddHistoryItem(item);
		}

		private void Machine_FilePositionChanged()
		{
			LabelFilePosition.Content = machine.FilePosition;

			if (ListViewFile.SelectedItem is TextBlock)
				((TextBlock)ListViewFile.SelectedItem).Background = Brushes.Transparent;

			ListViewFile.SelectedIndex = machine.FilePosition;

			if (ListViewFile.SelectedItem is TextBlock)
				((TextBlock)ListViewFile.SelectedItem).Background = Brushes.Gray;

			ListViewFile.ScrollIntoView(ListViewFile.SelectedItem);
		}

		private void Machine_FileChanged()
		{
			try
			{
				ToolPath = GCodeFile.FromList(machine.File);
				GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced); // prevents considerable increase in memory usage
			}
			catch(Exception ex)
			{
				MessageBox.Show("Could not parse GCode File, no preview/editing available\nrun this file at your own risk\n" + ex.Message);
			}

			if(Properties.Settings.Default.EnableCodePreview)
				ToolPath.GetModel(ModelLine, ModelRapid, ModelArc);

			LabelFileLength.Content = machine.File.Count;

			int digits = (int)Math.Ceiling(Math.Log10(machine.File.Count));

			string format = "D" + digits;

			int i = 1;

			ListViewFile.Items.Clear();
			foreach (string line in machine.File)
			{
				ListViewFile.Items.Add(new TextBlock() { Text = $"{i++.ToString(format)} : {line}" });
			}

		}

		private void UpdateAllButtons()
		{
			ButtonDistanceMode.IsEnabled = machine.Mode == Machine.OperatingMode.Manual;
			ButtonUnit.IsEnabled = machine.Mode == Machine.OperatingMode.Manual;
			ButtonArcPlane.IsEnabled = machine.Mode == Machine.OperatingMode.Manual;
			ButtonStatus.IsEnabled = machine.Mode == Machine.OperatingMode.Manual;

			ButtonFeedHold.IsEnabled = machine.Mode != Machine.OperatingMode.Disconnected;
			ButtonCycleStart.IsEnabled = machine.Mode != Machine.OperatingMode.Disconnected;
			ButtonSoftReset.IsEnabled = machine.Mode == Machine.OperatingMode.Manual;

			ButtonSettings.IsEnabled = machine.Mode == Machine.OperatingMode.Disconnected;

			ButtonFileOpen.IsEnabled = machine.Mode != Machine.OperatingMode.SendFile;
			ButtonFileStart.IsEnabled = machine.Mode == Machine.OperatingMode.Manual;
			ButtonFilePause.IsEnabled = machine.Mode == Machine.OperatingMode.SendFile;
			ButtonFileGoto.IsEnabled = machine.Mode != Machine.OperatingMode.SendFile;
			ButtonFileClear.IsEnabled = machine.Mode != Machine.OperatingMode.SendFile;

			ButtonManualSend.IsEnabled = machine.Mode == Machine.OperatingMode.Manual;
			ButtonManualSetG10Zero.IsEnabled = machine.Mode == Machine.OperatingMode.Manual;
			ButtonManualSetG92Zero.IsEnabled = machine.Mode == Machine.OperatingMode.Manual;
			ButtonManualResetG10.IsEnabled = machine.Mode == Machine.OperatingMode.Manual;

			ButtonEditSimplify.IsEnabled = machine.Mode != Machine.OperatingMode.SendFile;
			ButtonEditArcToLines.IsEnabled = machine.Mode != Machine.OperatingMode.SendFile;
			ButtonEditSplit.IsEnabled = machine.Mode != Machine.OperatingMode.SendFile;

			ModelTool.Visible = machine.Connected;

			UpdateProbeTabButtons();
		}

		private void Machine_ConnectionStateChanged()
		{
			ButtonConnect.Visibility = machine.Connected ? Visibility.Collapsed : Visibility.Visible;
			ButtonDisconnect.Visibility = machine.Connected ? Visibility.Visible : Visibility.Collapsed;

			ButtonSettings.IsEnabled = !machine.Connected;
		}
	}
}
