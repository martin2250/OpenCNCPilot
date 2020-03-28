using OpenCNCPilot.Communication;
using OpenCNCPilot.GCode;
using OpenCNCPilot.Util;
using System;
using System.Windows;

namespace OpenCNCPilot
{
	partial class MainWindow
	{
		void UpdateProbeTabButtons()
		{
			ButtonHeightMapCreateNew.IsEnabled = Map == null;
			ButtonHeightMapLoad.IsEnabled = Map == null;
			ButtonHeightMapSave.IsEnabled = machine.Mode != Machine.OperatingMode.Probe && Map != null;
			ButtonHeightMapClear.IsEnabled = machine.Mode != Machine.OperatingMode.Probe && Map != null;

			GridProbingControls.Visibility = Map != null ? Visibility.Visible : Visibility.Collapsed;

			ButtonHeightMapStart.IsEnabled = machine.Mode == Machine.OperatingMode.Manual && Map != null && Map.NotProbed.Count > 0;
			ButtonHeightMapPause.IsEnabled = machine.Mode == Machine.OperatingMode.Probe;

			ButtonEditApplyHeightMap.IsEnabled = machine.Mode != Machine.OperatingMode.SendFile && Map != null && Map.NotProbed.Count == 0;
		}

		NewHeightMapWindow NewHeightMapDialog;

		private void Map_MapUpdated()
		{
			Map.GetModel(ModelHeightMap);
			LabelHeightMapProgress.Content = Map.Progress + "/" + Map.TotalPoints;

			if (Map.MinHeight != double.MaxValue)
				LabelHeightMapMinZ.Content = Map.MinHeight.ToString("N", Constants.DecimalOutputFormat);
			else
				LabelHeightMapMinZ.Content = "~";

			if (Map.MaxHeight != double.MinValue)
				LabelHeightMapMaxZ.Content = Map.MaxHeight.ToString("N", Constants.DecimalOutputFormat);
			else
				LabelHeightMapMaxZ.Content = "~";

		}

		private void ButtonHeightmapCreateNew_Click(object sender, RoutedEventArgs e)
		{
			if (machine.Mode == Machine.OperatingMode.Probe || Map != null)
				return;

			NewHeightMapDialog = new NewHeightMapWindow();

			if (ToolPath.ContainsMotion)
			{
				NewHeightMapDialog.ToolPathMin = ToolPath.MinFeed.GetXY();
				NewHeightMapDialog.ToolPathMax = ToolPath.MaxFeed.GetXY();
			}

			NewHeightMapDialog.Owner = this;

			NewHeightMapDialog.Size_Ok += NewHeightMapDialog_Size_Ok;
			NewHeightMapDialog.SelectedSizeChanged += NewHeightMapDialog_SelectedSizeChanged;
			NewHeightMapDialog.Closed += NewHeightMapDialog_Closed;

			NewHeightMapDialog.Show();

			NewHeightMapDialog_SelectedSizeChanged();
		}

		private void NewHeightMapDialog_Closed(object sender, EventArgs e)
		{
			if (NewHeightMapDialog.Ok)
				return;

			ModelHeightMapBoundary.Points.Clear();
			ModelHeightMapPoints.Points.Clear();
		}

		private void NewHeightMapDialog_SelectedSizeChanged()
		{
			HeightMap.GetPreviewModel(NewHeightMapDialog.Min, NewHeightMapDialog.Max, NewHeightMapDialog.GridSize, ModelHeightMapBoundary, ModelHeightMapPoints);
		}

		private void NewHeightMapDialog_Size_Ok()
		{
			if (machine.Mode == Machine.OperatingMode.Probe || Map != null)
				return;

			if (NewHeightMapDialog.Min.X == NewHeightMapDialog.Max.X || NewHeightMapDialog.Min.Y == NewHeightMapDialog.Max.Y)
				return;

			try
			{
				Map = new HeightMap(NewHeightMapDialog.GridSize, NewHeightMapDialog.Min, NewHeightMapDialog.Max);

				if (NewHeightMapDialog.GenerateTestPattern)
				{
					try
					{
						Map.FillWithTestPattern(NewHeightMapDialog.TestPattern);
						Map.NotProbed.Clear();
					}
					catch { MessageBox.Show("Error in test pattern"); }
				}

				Map.MapUpdated += Map_MapUpdated;
				UpdateProbeTabButtons();
				Map_MapUpdated();
			}
			catch (Exception ex)
			{
				Machine_Info(ex.Message);
			}
		}

		private void SaveFileDialogHeightMap_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (machine.Mode == Machine.OperatingMode.Probe || Map == null)
				return;

			try
			{
				Map.Save(saveFileDialogHeightMap.FileName);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void OpenFileDialogHeightMap_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
		{
			OpenHeightMap(openFileDialogHeightMap.FileName);
		}

		private void OpenHeightMap(string filepath)
		{
			if (machine.Mode == Machine.OperatingMode.Probe || Map != null)
				return;

			try
			{
				Map = HeightMap.Load(filepath);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}

			Map.MapUpdated += Map_MapUpdated;

			Map.GetPreviewModel(ModelHeightMapBoundary, ModelHeightMapPoints);

			UpdateProbeTabButtons();
			Map_MapUpdated();
		}

		private void ButtonHeightmapLoad_Click(object sender, RoutedEventArgs e)
		{
			if (machine.Mode == Machine.OperatingMode.Probe || Map != null)
				return;

			openFileDialogHeightMap.ShowDialog();
		}

		private void ButtonHeightmapSave_Click(object sender, RoutedEventArgs e)
		{
			if (machine.Mode == Machine.OperatingMode.Probe || Map == null)
				return;

			saveFileDialogHeightMap.FileName = $"map{(int)Map.Delta.X}x{(int)Map.Delta.Y}.hmap";
			saveFileDialogHeightMap.ShowDialog();
		}

		private void ButtonHeightmapClear_Click(object sender, RoutedEventArgs e)
		{
			if (machine.Mode == Machine.OperatingMode.Probe || Map == null)
				return;

			Map = null;

			LabelHeightMapProgress.Content = "0/0";

			ModelHeightMap.MeshGeometry = new System.Windows.Media.Media3D.MeshGeometry3D();
			ModelHeightMapBoundary.Points.Clear();
			ModelHeightMapPoints.Points.Clear();

			UpdateProbeTabButtons();
		}

		private void HeightMapProbeNextPoint()
		{
			if (machine.Mode != Machine.OperatingMode.Probe)
				return;

			if (!machine.Connected || Map == null || Map.NotProbed.Count == 0)
			{
				machine.ProbeStop();
				return;
			}

			Map.NotProbed.Sort(
				delegate (Tuple<int, int> a, Tuple<int, int> b)
				{
					Vector2 va = Map.GetCoordinates(a) - machine.WorkPosition.GetXY();
					va.X *= Properties.Settings.Default.ProbeXAxisWeight;
					Vector2 vb = Map.GetCoordinates(b) - machine.WorkPosition.GetXY();
					vb.X *= Properties.Settings.Default.ProbeXAxisWeight;
					return va.Magnitude.CompareTo(vb.Magnitude);
				});

			Vector2 nextPoint = Map.GetCoordinates(Map.NotProbed[0].Item1, Map.NotProbed[0].Item2);

			machine.SendLine($"G0X{nextPoint.X.ToString("0.###", Constants.DecimalOutputFormat)}Y{nextPoint.Y.ToString("0.###", Constants.DecimalOutputFormat)}");

			machine.SendLine($"G38.3Z-{Properties.Settings.Default.ProbeMaxDepth.ToString("0.###", Constants.DecimalOutputFormat)}F{Properties.Settings.Default.ProbeFeed.ToString("0.#", Constants.DecimalOutputFormat)}");

			machine.SendLine("G91");
			machine.SendLine($"G0Z{Properties.Settings.Default.ProbeMinimumHeight.ToString("0.###", Constants.DecimalOutputFormat)}");
			machine.SendLine("G90");
		}

		private void Machine_ProbeFinished(Vector3 position, bool success)
		{
			if (machine.Mode != Machine.OperatingMode.Probe)
				return;

			if (!machine.Connected || Map == null || Map.NotProbed.Count == 0)
			{
				machine.ProbeStop();
				return;
			}

			if (!success && Properties.Settings.Default.AbortOnProbeFail)
			{
				MessageBox.Show("Probe Failed! aborting");

				machine.ProbeStop();
				return;
			}

			Tuple<int, int> lastPoint = Map.NotProbed[0];
			Map.NotProbed.RemoveAt(0);

			Map.AddPoint(lastPoint.Item1, lastPoint.Item2, position.Z);

			if (Map.NotProbed.Count == 0)
			{
				machine.SendLine($"G0Z{Math.Max(Properties.Settings.Default.ProbeSafeHeight, position.Z).ToString(Constants.DecimalOutputFormat)}");
				machine.ProbeStop();
				Machine_Info("HeightMap complete!");

				if (Properties.Settings.Default.BackupHeightMap)
				{
					try
					{
						string exepath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
						string backupdir = System.IO.Path.Combine(exepath, "HeightMapBackup");
						string filepath = System.IO.Path.Combine(backupdir, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".hmap");

						System.IO.Directory.CreateDirectory(backupdir);

						Map.Save(filepath);
					}
					catch
					{
						Machine_NonFatalException("Could not save backup of HeightMap");
					}
				}
				return;
			}

			HeightMapProbeNextPoint();
		}

		private void ButtonHeightMapStart_Click(object sender, RoutedEventArgs e)
		{
			if (!machine.Connected || machine.Mode != Machine.OperatingMode.Manual || Map == null)
				return;

			if (Map.Progress == Map.TotalPoints)
				return;

			machine.ProbeStart();

			if (machine.Mode != Machine.OperatingMode.Probe)
				return;

			machine.SendLine("G90");
			machine.SendLine($"G0Z{Properties.Settings.Default.ProbeSafeHeight.ToString("0.###", Constants.DecimalOutputFormat)}");

			HeightMapProbeNextPoint();
		}

		private void ButtonHeightMapPause_Click(object sender, RoutedEventArgs e)
		{
			if (machine.Mode != Machine.OperatingMode.Probe)
				return;

			machine.ProbeStop();
		}
	}
}
