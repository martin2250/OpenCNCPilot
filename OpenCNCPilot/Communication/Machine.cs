using System;
using System.Collections;
using System.Threading.Tasks;
using OpenCNCPilot.Util;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Windows;
using System.Text.RegularExpressions;
using OpenCNCPilot.GCode;
using OpenCNCPilot.GCode.GCodeCommands;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace OpenCNCPilot.Communication
{
	enum ConnectionType
	{
		Serial
	}

	class Machine
	{
		public enum OperatingMode
		{
			Manual,
			SendFile,
			Probe,
			Disconnected
		}

		public event Action<Vector3, bool> ProbeFinished;
		public event Action<string> NonFatalException;
		public event Action<string> Info;
		public event Action<string> LineReceived;
		public event Action<string> LineSent;
		public event Action ConnectionStateChanged;
		public event Action PositionUpdateReceived;
		public event Action StatusChanged;
		public event Action DistanceModeChanged;
		public event Action UnitChanged;
		public event Action PlaneChanged;
		public event Action BufferStateChanged;
		public event Action OperatingModeChanged;
		public event Action FileChanged;
		public event Action FilePositionChanged;

		public Vector3 MachinePosition { get; private set; } = new Vector3();   //No events here, the parser triggers a single event for both
		public Vector3 WorkOffset { get; private set; } = new Vector3();
		public Vector3 WorkPosition { get { return MachinePosition - WorkOffset; } }

		private ReadOnlyCollection<string> _file = new ReadOnlyCollection<string>(new string[0]);
		public ReadOnlyCollection<string> File
		{
			get { return _file; }
			set
			{
				_file = value;
				FilePosition = 0;
				RaiseEvent(FileChanged);
			}
		}

		private int _filePosition = 0;
		public int FilePosition
		{
			get { return _filePosition; }
			private set
			{
				_filePosition = value;
				RaiseEvent(FilePositionChanged);
			}
		}

		private OperatingMode _mode = OperatingMode.Disconnected;
		public OperatingMode Mode
		{
			get { return _mode; }
			private set
			{
				if (_mode == value)
					return;

				_mode = value;
				RaiseEvent(OperatingModeChanged);
			}
		}

		#region Status
		private string _status = "Disconnected";
		public string Status
		{
			get { return _status; }
			private set
			{
				if (_status == value)
					return;
				_status = value;

				RaiseEvent(StatusChanged);
			}
		}

		private ParseDistanceMode _distanceMode = ParseDistanceMode.Absolute;
		public ParseDistanceMode DistanceMode
		{
			get { return _distanceMode; }
			private set
			{
				if (_distanceMode == value)
					return;
				_distanceMode = value;

				RaiseEvent(DistanceModeChanged);
			}
		}

		private ParseUnit _unit = ParseUnit.Metric;
		public ParseUnit Unit
		{
			get { return _unit; }
			private set
			{
				if (_unit == value)
					return;
				_unit = value;

				RaiseEvent(UnitChanged);
			}
		}

		private ArcPlane _plane = ArcPlane.XY;
		public ArcPlane Plane
		{
			get { return _plane; }
			private set
			{
				if (_plane == value)
					return;
				_plane = value;

				RaiseEvent(PlaneChanged);
			}
		}

		private bool _connected = false;
		public bool Connected
		{
			get { return _connected; }
			private set
			{
				if (value == _connected)
					return;

				_connected = value;

				if (!Connected)
					Mode = OperatingMode.Disconnected;

				RaiseEvent(ConnectionStateChanged);
			}
		}

		private int _bufferState;
		public int BufferState
		{
			get { return _bufferState; }
			private set
			{
				if (_bufferState == value)
					return;

				_bufferState = value;

				RaiseEvent(BufferStateChanged);
			}
		}
		#endregion Status

		private Stream Connection;
		private Thread WorkerThread;

		public Machine()
		{

		}

		Queue Sent = Queue.Synchronized(new Queue());
		Queue ToSend = Queue.Synchronized(new Queue());
		Queue ToSendPriority = Queue.Synchronized(new Queue()); //contains characters (for soft reset, feed hold etc)

		private void Work()
		{
			try
			{
				StreamReader reader = new StreamReader(Connection);
				StreamWriter writer = new StreamWriter(Connection);

				int StatusPollInterval = Properties.Settings.Default.StatusPollInterval;

				int ControllerBufferSize = Properties.Settings.Default.ControllerBufferSize;
				BufferState = 0;

				TimeSpan WaitTime = TimeSpan.FromMilliseconds(0.5);
				DateTime LastStatusPoll = DateTime.Now + TimeSpan.FromSeconds(0.5);
				DateTime StartTime = DateTime.Now;

				writer.Write("\n$G\n");
				writer.Flush();

				while (true)
				{
					Task<string> lineTask = reader.ReadLineAsync();

					while (!lineTask.IsCompleted)
					{
						if (!Connected)
						{
							return;
						}

						while (ToSendPriority.Count > 0)
						{
							writer.Write((char)ToSendPriority.Dequeue());
							writer.Flush();
						}
						if (Mode == OperatingMode.SendFile)
						{
							if (File.Count > FilePosition && (File[FilePosition].Length + 1) < (ControllerBufferSize - BufferState))
							{
								string send_line = File[FilePosition++];

								writer.Write(send_line);
								writer.Write('\n');
								writer.Flush();

								RaiseEvent(UpdateStatus, send_line);
								RaiseEvent(LineSent, send_line);

								BufferState += send_line.Length + 1;

								Sent.Enqueue(send_line);

								if(FilePosition >= File.Count)
								{
									Mode = OperatingMode.Manual;
								}

								continue;
							}
						}
						else
						{
							if (ToSend.Count > 0 && (((string)ToSend.Peek()).Length + 1) < (ControllerBufferSize - BufferState))
							{
								string send_line = (string)ToSend.Peek();

								writer.Write(send_line);
								writer.Write('\n');
								writer.Flush();

								RaiseEvent(UpdateStatus, send_line);
								RaiseEvent(LineSent, send_line);

								BufferState += send_line.Length + 1;

								Sent.Enqueue(send_line);
								ToSend.Dequeue();

								continue;
							}
						}

						DateTime Now = DateTime.Now;

						if ((Now - LastStatusPoll).TotalMilliseconds > StatusPollInterval)
						{
							writer.Write('?');
							writer.Flush();
							LastStatusPoll = Now;
						}

						Thread.Sleep(WaitTime);
					}

					string line = lineTask.Result;

					if (line == "ok")
					{
						if (Sent.Count != 0)
						{
							BufferState -= ((string)Sent.Dequeue()).Length + 1;
						}
						else
						{
							Console.WriteLine("Received OK without anything in the Sent Buffer");
							BufferState = 0;
						}
					}
					else
					{
						if (line.StartsWith("error:"))
						{
							if (Sent.Count != 0)
							{
								string errorline = (string)Sent.Dequeue();

								RaiseEvent(ReportError, $"{line}: {errorline}");
								BufferState -= errorline.Length + 1;
							}
							else
							{
								if ((DateTime.Now - StartTime).TotalMilliseconds > 200)
									RaiseEvent(ReportError, $"Received <{line}> without anything in the Sent Buffer");

								BufferState = 0;
							}

							Mode = OperatingMode.Manual;
						}
						else if (line.StartsWith("<"))
							RaiseEvent(ParseStatus, line);
						else if (line.StartsWith("[PRB:"))
						{
							RaiseEvent(ParseProbe, line);
							RaiseEvent(LineReceived, line);
						}
						else if (line.StartsWith("["))
						{
							RaiseEvent(UpdateStatus, line);
							RaiseEvent(LineReceived, line);
						}
						else if (line.StartsWith("ALARM"))
						{
							RaiseEvent(NonFatalException, line);
							Mode = OperatingMode.Manual;
						}
						else if (line.Length > 0)
							RaiseEvent(LineReceived, line);
					}
				}
			}
			catch (Exception ex)
			{
				RaiseEvent(ReportError, $"Fatal Error: {ex.Message}");
				Disconnect();
			}
		}

		public void Connect()
		{
			if (Connected)
				throw new Exception("Can't Connect: Already Connected");

			switch (Properties.Settings.Default.ConnectionType)
			{
				case ConnectionType.Serial:
					SerialPort port = new SerialPort(Properties.Settings.Default.SerialPortName, Properties.Settings.Default.SerialPortBaud);
					port.Open();
					Connection = port.BaseStream;
					break;
				default:
					throw new Exception("Invalid Connection Type");
			}

			Connected = true;

			ToSend.Clear();
			Sent.Clear();

			Mode = OperatingMode.Manual;

			if (PositionUpdateReceived != null)
				PositionUpdateReceived.Invoke();

			WorkerThread = new Thread(Work);
			WorkerThread.Priority = ThreadPriority.AboveNormal;
			WorkerThread.Start();
		}

		public void Disconnect()
		{
			if (!Connected)
				throw new Exception("Can't Disconnect: Not Connected");

			Connected = false;

			WorkerThread.Join();

			Connection.Close();
			Connection.Dispose();
			Connection = null;

			Mode = OperatingMode.Disconnected;

			MachinePosition = new Vector3();
			WorkOffset = new Vector3();

			if (PositionUpdateReceived != null)
				PositionUpdateReceived.Invoke();

			Status = "Disconnected";
			DistanceMode = ParseDistanceMode.Absolute;
			Unit = ParseUnit.Metric;
			Plane = ArcPlane.XY;
			BufferState = 0;

			ToSend.Clear();
			ToSendPriority.Clear();
			Sent.Clear();
		}

		public void SendLine(string line)
		{
			if (!Connected)
			{
				RaiseEvent(Info, "Not Connected");
				return;
			}

			if (Mode != OperatingMode.Manual && Mode != OperatingMode.Probe)
			{
				RaiseEvent(Info, "Not in Manual Mode");
				return;
			}

			ToSend.Enqueue(line);
		}

		public void SoftReset()
		{
			if (!Connected)
			{
				RaiseEvent(Info, "Not Connected");
				return;
			}

			Mode = OperatingMode.Manual;

			ToSend.Clear();
			ToSendPriority.Clear();
			Sent.Clear();
			ToSendPriority.Enqueue((char)0x18);

			BufferState = 0;
		}

		public void FeedHold()
		{
			if (!Connected)
			{
				RaiseEvent(Info, "Not Connected");
				return;
			}

			ToSendPriority.Enqueue('!');
		}

		public void CycleStart()
		{
			if (!Connected)
			{
				RaiseEvent(Info, "Not Connected");
				return;
			}

			ToSendPriority.Enqueue('~');
		}

		public void SetFile(IList<string> file)
		{
			if (Mode == OperatingMode.SendFile)
			{
				RaiseEvent(Info, "Can't change file while active");
				return;
			}

			File = new ReadOnlyCollection<string>(file);
			FilePosition = 0;
		}

		public void ClearFile()
		{
			if (Mode == OperatingMode.SendFile)
			{
				RaiseEvent(Info, "Can't change file while active");
				return;
			}

			File = new ReadOnlyCollection<string>(new string[0]);
			FilePosition = 0;
		}

		public void FileStart()
		{
			if (!Connected)
			{
				RaiseEvent(Info, "Not Connected");
				return;
			}

			if (Mode != OperatingMode.Manual)
			{
				RaiseEvent(Info, "Not in Manual Mode");
				return;
			}

			Mode = OperatingMode.SendFile;
		}

		public void FilePause()
		{
			if (!Connected)
			{
				RaiseEvent(Info, "Not Connected");
				return;
			}

			if (Mode != OperatingMode.SendFile)
			{
				RaiseEvent(Info, "Not in SendFile Mode");
				return;
			}

			Mode = OperatingMode.Manual;
		}

		public void ProbeStart()
		{
			if (!Connected)
			{
				RaiseEvent(Info, "Not Connected");
				return;
			}

			if (Mode != OperatingMode.Manual)
			{
				RaiseEvent(Info, "Can't start probing while running!");
				return;
			}

			Mode = OperatingMode.Probe;
		}

		public void ProbeStop()
		{
			if (!Connected)
			{
				RaiseEvent(Info, "Not Connected");
				return;
			}

			if (Mode != OperatingMode.Probe)
			{
				RaiseEvent(Info, "Not in Probe mode");
				return;
			}

			Mode = OperatingMode.Manual;
		}

		public void FileGoto(int lineNumber)
		{
			if (Mode == OperatingMode.SendFile)
				return;
			
			if(lineNumber >= File.Count || lineNumber < 0)
			{
				RaiseEvent(NonFatalException, "Line Number outside of file length");
				return;
			}

			FilePosition = lineNumber;
		}

		public void ClearQueue()
		{
			if (Mode != OperatingMode.Manual)
			{
				RaiseEvent(Info, "Not in Manual mode");
				return;
			}

			ToSend.Clear();
		}

		private static Regex GCodeSplitter = new Regex(@"(G)\s*(\-?\d+\.?\d*)", RegexOptions.Compiled);

		/// <summary>
		/// Updates Status info from each line sent
		/// </summary>
		/// <param name="line"></param>
		private void UpdateStatus(string line)
		{
			if (!Connected)
				return;

			//we use a Regex here so G91.1 etc don't get recognized as G91

			foreach(Match m in GCodeSplitter.Matches(line))
			{
				if (m.Groups[1].Value != "G")
					continue;

				float code = float.Parse(m.Groups[2].Value);

				if (code == 17)
					Plane = ArcPlane.XY;
				if (code == 18)
					Plane = ArcPlane.YZ;
				if (code == 19)
					Plane = ArcPlane.ZX;

				if (code == 20)
					Unit = ParseUnit.Imperial;
				if (code == 21)
					Unit = ParseUnit.Metric;

				if (code == 90)
					DistanceMode = ParseDistanceMode.Absolute;
				if (code == 91)
					DistanceMode = ParseDistanceMode.Incremental;
			}
		}

		//	https://regex101.com/r/4r3Ukj/3
		private static Regex StatusEx = new Regex(@"(?<=[<|])(\w+):?(([0-9\.-]*),?([0-9\.-]*)?,?([0-9\.,-]*)?)?(?=[|>])", RegexOptions.Compiled);

		/// <summary>
		/// Parses a recevied status report (answer to '?')
		/// </summary>
		private void ParseStatus(string line)
		{
			MatchCollection statusMatch = StatusEx.Matches(line);

			if (statusMatch.Count == 0)
			{
				NonFatalException.Invoke(string.Format("Received Bad Status: '{0}'", line));
				return;
			}

			bool posUpdate = false;

			foreach(Match m in statusMatch)
			{
				if(m.Index == 1)
				{
					Status = m.Groups[1].Value;
					continue;
				}

				if (m.Groups[1].Value == "WCO")
				{
					try
					{
						WorkOffset = Vector3.Parse(m.Groups[2].Value);
						posUpdate = true;
					}
					catch { NonFatalException.Invoke(string.Format("Received Bad Status: '{0}'", line)); }
				}
            }

			//run this later to catch work offset changes before parsing position
			Vector3 NewMachinePosition = MachinePosition;

			foreach (Match m in statusMatch)
			{
                if (m.Groups[1].Value == "MPos" || m.Groups[1].Value == "WPos")
				{
					try
					{
						NewMachinePosition = Vector3.Parse(m.Groups[2].Value);

						if (m.Groups[1].Value == "WPos")
							NewMachinePosition += WorkOffset;

						if (NewMachinePosition != MachinePosition)
						{
							posUpdate = true;
							MachinePosition = NewMachinePosition;
						}
					}
					catch { NonFatalException.Invoke(string.Format("Received Bad Status: '{0}'", line)); }
				}

			}

			if (posUpdate && Connected && PositionUpdateReceived != null)
				PositionUpdateReceived.Invoke();
		}

		private static Regex ProbeEx = new Regex(@"\[PRB:(?'Pos'[-0-9\.]*,[-0-9\.]*,[-0-9\.]*):(?'Success'0|1)\]", RegexOptions.Compiled);

		/// <summary>
		/// Parses a recevied probe report
		/// </summary>
		private void ParseProbe(string line)
		{
			if (ProbeFinished == null)
				return;

			Match probeMatch = ProbeEx.Match(line);

			Group pos = probeMatch.Groups["Pos"];
			Group success = probeMatch.Groups["Success"];

			if (!probeMatch.Success || !(pos.Success & success.Success))
			{
				NonFatalException.Invoke($"Received Bad Probe: '{line}'");
				return;
			}

			Vector3 ProbePos = Vector3.Parse(pos.Value);

			ProbePos -= WorkOffset;     //Mpos, Wpos only get updated by the same dispatcher, so this should be thread safe

			bool ProbeSuccess = success.Value == "1";

			ProbeFinished.Invoke(ProbePos, ProbeSuccess);
		}

		/// <summary>
		/// Reports error. This is there to offload the ExpandError function from the "Real-Time" worker thread to the application thread
		/// </summary>
		private void ReportError(string error)
		{
			if (NonFatalException != null)
				NonFatalException.Invoke(GrblErrorProvider.ExpandError(error));
		}

		private void RaiseEvent(Action<string> action, string param)
		{
			if (action == null)
				return;

			Application.Current.Dispatcher.BeginInvoke(action, param);
		}

		private void RaiseEvent(Action action)
		{
			if (action == null)
				return;
			
			Application.Current.Dispatcher.BeginInvoke(action);
		}
	}
}
