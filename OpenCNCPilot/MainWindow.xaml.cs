using System;
using System.Windows;
using OpenCNCPilot.Communication;
using OpenCNCPilot.Util;
using Microsoft.Win32;
using OpenCNCPilot.GCode;

namespace OpenCNCPilot
{
	public partial class MainWindow : Window
	{
		Machine machine = new Machine();

		OpenFileDialog openFileDialogGCode = new OpenFileDialog() { InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), Filter = Constants.FileFilterGCode };
		OpenFileDialog openFileDialogHeightMap = new OpenFileDialog() { InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), Filter = Constants.FileFilterHeightMap };
		SaveFileDialog saveFileDialogHeightMap = new SaveFileDialog() { InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), Filter = Constants.FileFilterHeightMap };

        GCodeFile ToolPath { get; set; } = GCodeFile.Empty;
		HeightMap Map { get; set; }

		public MainWindow()
		{
			InitializeComponent();

			openFileDialogGCode.FileOk += OpenFileDialogGCode_FileOk;
			openFileDialogHeightMap.FileOk += OpenFileDialogHeightMap_FileOk;
			saveFileDialogHeightMap.FileOk += SaveFileDialogHeightMap_FileOk;

			machine.ConnectionStateChanged += Machine_ConnectionStateChanged;

			machine.NonFatalException += Machine_NonFatalException;
			machine.Info += Machine_Info;
			machine.LineReceived += Machine_LineReceived;
			machine.LineSent += Machine_LineSent;

			machine.PositionUpdateReceived += Machine_PositionUpdateReceived;
			machine.StatusChanged += Machine_StatusChanged;
			machine.DistanceModeChanged += Machine_DistanceModeChanged;
			machine.UnitChanged += Machine_UnitChanged;
			machine.PlaneChanged += Machine_PlaneChanged;
			machine.BufferStateChanged += Machine_BufferStateChanged;
			machine.OperatingModeChanged += UpdateAllButtons;
			machine.FileChanged += Machine_FileChanged;
			machine.FilePositionChanged += Machine_FilePositionChanged;
			machine.ProbeFinished += Machine_ProbeFinished;

			UpdateAllButtons();
		}


	}
}
