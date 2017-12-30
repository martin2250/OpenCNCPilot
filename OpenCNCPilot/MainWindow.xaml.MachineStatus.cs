using OpenCNCPilot.Communication;
using OpenCNCPilot.GCode;
using OpenCNCPilot.Util;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;

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

			DoubleAnimation anim = new DoubleAnimation(0, new Duration(TimeSpan.FromSeconds(2)));
			anim.BeginTime = TimeSpan.FromSeconds(Properties.Settings.Default.ConsoleFadeTime);

			item.HorizontalContentAlignment = HorizontalAlignment.Left;
			item.VerticalContentAlignment = VerticalAlignment.Center;

			ListBoxHistory.Items.Add(item);

			item.BeginAnimation(OpacityProperty, anim);
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

		private void Machine_StatusReceived(string obj)
		{
			if (!Properties.Settings.Default.ShowStatusLines)
				return;

			ListBoxItem item = new ListBoxItem();
			item.Content = obj;
			item.FontSize = 14;

			item.Foreground = Brushes.Black;

			AddHistoryItem(item);
		}

		private void Machine_FilePositionChanged()
		{
			LabelFilePosition.Content = machine.FilePosition;

			if (ListViewFile.SelectedItem is TextBlock)
			{
				if(ListViewFile.SelectedIndex >= 0 && machine.PauseLines[ListViewFile.SelectedIndex])
					((TextBlock)ListViewFile.SelectedItem).Background = Brushes.YellowGreen;
				else
					((TextBlock)ListViewFile.SelectedItem).Background = Brushes.Transparent;
			}

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


			ListViewFile.Items.Clear();

			for(int line = 0; line < machine.File.Count; line++)
			{
				TextBlock tb = new TextBlock() { Text = $"{(line + 1).ToString(format)} : {machine.File[line]}" };

				if (machine.PauseLines[line])
					tb.Background = Brushes.YellowGreen;

				ListViewFile.Items.Add(tb);
			}

			if (ToolPath.ContainsMotion)
			{
				ModelFileBoundary.Points.Clear();
				Point3DCollection boundary = new Point3DCollection();

				Vector3 MinPoint = ToolPath.MinFeed;
				Vector3 MaxPoint = ToolPath.MaxFeed;

				for (int ax = 0; ax < 3; ax++)
				{
					for (int mask = 0; mask < 4; mask++)
					{
						Vector3 point = MinPoint;

						for (int i = 0; i < 2; i++)
						{
							// binary integer logic? hell yeah!
							if (((mask >> i) & 0x01) == 1)
							{
								point[(ax + i + 1) % 3] = MaxPoint[(ax + i + 1) % 3];
							}
						}

						boundary.Add(point.ToPoint3D());

						point[ax] = MaxPoint[ax];
						boundary.Add(point.ToPoint3D());
					}
				}

				ModelFileBoundary.Points = boundary;

				ModelTextMinPoint.Text = string.Format(Constants.DecimalOutputFormat, "({0:0.###}, {1:0.###}, {2:0.###})", MinPoint.X, MinPoint.Y, MinPoint.Z);
				ModelTextMaxPoint.Text = string.Format(Constants.DecimalOutputFormat, "({0:0.###}, {1:0.###}, {2:0.###})", MaxPoint.X, MaxPoint.Y, MaxPoint.Z);
				ModelTextMinPoint.Position = MinPoint.ToPoint3D();
				ModelTextMaxPoint.Position = MaxPoint.ToPoint3D();
				ModelFileBoundaryPoints.Points.Clear();
				ModelFileBoundaryPoints.Points.Add(MinPoint.ToPoint3D());
				ModelFileBoundaryPoints.Points.Add(MaxPoint.ToPoint3D());
			}
			else
			{
				ModelFileBoundary.Points.Clear();
				ModelFileBoundaryPoints.Points.Clear();
				ModelTextMinPoint.Text = "";
				ModelTextMaxPoint.Text = "";
			}
		}

		private void Machine_OperatingMode_Changed()
		{
			ButtonDistanceMode.IsEnabled = machine.Mode == Machine.OperatingMode.Manual;
			ButtonUnit.IsEnabled = machine.Mode == Machine.OperatingMode.Manual;
			ButtonArcPlane.IsEnabled = machine.Mode == Machine.OperatingMode.Manual;
			ButtonStatus.IsEnabled = machine.Mode == Machine.OperatingMode.Manual;
			ButtonFeedRateOvr.IsEnabled = machine.Mode != Machine.OperatingMode.Disconnected;

			ButtonFeedHold.IsEnabled = machine.Mode != Machine.OperatingMode.Disconnected;
			ButtonCycleStart.IsEnabled = machine.Mode != Machine.OperatingMode.Disconnected;
			ButtonSoftReset.IsEnabled = machine.Mode == Machine.OperatingMode.Manual;

			ButtonSettings.IsEnabled = machine.Mode == Machine.OperatingMode.Disconnected;

			ButtonFileOpen.IsEnabled = machine.Mode != Machine.OperatingMode.SendFile;
			ButtonFileSave.IsEnabled = machine.Mode != Machine.OperatingMode.SendFile;
			ButtonFileStart.IsEnabled = machine.Mode == Machine.OperatingMode.Manual;
			ButtonFilePause.IsEnabled = machine.Mode == Machine.OperatingMode.SendFile;
			ButtonFileGoto.IsEnabled = machine.Mode != Machine.OperatingMode.SendFile;
			ButtonFileClear.IsEnabled = machine.Mode != Machine.OperatingMode.SendFile;

			ButtonManualSend.IsEnabled = machine.Mode == Machine.OperatingMode.Manual;
			ButtonManualSetG10Zero.IsEnabled = machine.Mode == Machine.OperatingMode.Manual;
			ButtonManualSetG92Zero.IsEnabled = machine.Mode == Machine.OperatingMode.Manual;
			ButtonManualResetG10.IsEnabled = machine.Mode == Machine.OperatingMode.Manual;

			if (machine.Mode != Machine.OperatingMode.Manual)
				CheckBoxEnableJog.IsChecked = false;
			CheckBoxEnableJog.IsEnabled = machine.Mode == Machine.OperatingMode.Manual;

			ButtonEditSimplify.IsEnabled = machine.Mode != Machine.OperatingMode.SendFile;
			ButtonEditArcToLines.IsEnabled = machine.Mode != Machine.OperatingMode.SendFile;
			ButtonEditSplit.IsEnabled = machine.Mode != Machine.OperatingMode.SendFile;

			ModelTool.Visible = machine.Connected;

			ButtonSyncBuffer.IsEnabled = machine.Mode == Machine.OperatingMode.Manual;

			StackPanelOverrides.IsEnabled = machine.Mode != Machine.OperatingMode.Disconnected;

			UpdateProbeTabButtons();
		}

		private void Machine_ConnectionStateChanged()
		{
			ButtonConnect.Visibility = machine.Connected ? Visibility.Collapsed : Visibility.Visible;
			ButtonDisconnect.Visibility = machine.Connected ? Visibility.Visible : Visibility.Collapsed;

			ButtonSettings.IsEnabled = !machine.Connected;
		}

		private void Machine_OverrideChanged()
		{
			ButtonFeedRateOvr.Content = $"Feed: {machine.FeedOverride}%";
			LabelFeedOvr.Content = $"Feed: {machine.FeedOverride}%";
			LabelRapidOvr.Content = $"Rapid: {machine.RapidOverride}%";
		}
	}
}
