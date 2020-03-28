using OpenCNCPilot.Communication;
using OpenCNCPilot.GCode;
using System;
using System.Windows;

namespace OpenCNCPilot
{
	partial class MainWindow
	{
		private string _currentFileName = "";

		public string CurrentFileName
		{
			get => _currentFileName;
			set
			{
				_currentFileName = value;
				GetBindingExpression(Window.TitleProperty).UpdateTarget();
			}
		}

		private void OpenFileDialogGCode_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (machine.Mode == Machine.OperatingMode.SendFile)
				return;

			CurrentFileName = "";
			ToolPath = GCodeFile.Empty;

			openFileDialogGCode.InitialDirectory = System.IO.Path.GetDirectoryName(openFileDialogGCode.FileName);

			try
			{
				machine.SetFile(System.IO.File.ReadAllLines(openFileDialogGCode.FileName));
				CurrentFileName = System.IO.Path.GetFileName(openFileDialogGCode.FileName);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void SaveFileDialogGCode_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (machine.Mode == Machine.OperatingMode.SendFile)
				return;

			try
			{
				ToolPath.Save(saveFileDialogGCode.FileName);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void ButtonOpen_Click(object sender, RoutedEventArgs e)
		{
			if (machine.Mode == Machine.OperatingMode.SendFile)
				return;

			// prevent exception when the drive letter doesn't exist
			if (!System.IO.Directory.Exists(openFileDialogGCode.InitialDirectory))
			{
				openFileDialogGCode.InitialDirectory = "";
			}
			openFileDialogGCode.ShowDialog();
		}

		private void ButtonSave_Click(object sender, RoutedEventArgs e)
		{
			if (machine.Mode == Machine.OperatingMode.SendFile)
				return;

			saveFileDialogGCode.ShowDialog();
		}

		private void ButtonClear_Click(object sender, RoutedEventArgs e)
		{
			if (machine.Mode == Machine.OperatingMode.SendFile)
				return;

			machine.ClearFile();
			CurrentFileName = "";
		}

		private void ButtonFileStart_Click(object sender, RoutedEventArgs e)
		{
			machine.FileStart();
		}

		private void ButtonFilePause_Click(object sender, RoutedEventArgs e)
		{
			machine.FilePause();
		}

		private void ButtonFileGoto_Click(object sender, RoutedEventArgs e)
		{
			if (machine.Mode == Machine.OperatingMode.SendFile)
				return;

			EnterNumberWindow enw = new EnterNumberWindow(machine.FilePosition + 1);
			enw.Title = "Enter new line number";
			enw.Owner = this;
			enw.User_Ok += Enw_User_Ok_Goto;
			enw.Show();
		}

		private void Enw_User_Ok_Goto(double value)
		{
			if (machine.Mode == Machine.OperatingMode.SendFile)
				return;

			machine.FileGoto((int)value - 1);
		}
	}
}
