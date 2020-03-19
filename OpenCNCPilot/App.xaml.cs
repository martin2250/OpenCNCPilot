using OpenCNCPilot.Properties;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace OpenCNCPilot
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		// command line args
		public static string[] Args;

		public const int WM_COPYDATA = 0x004A;

		[DllImport("user32", EntryPoint = "SendMessageA")]
		private static extern int SendMessage(IntPtr Hwnd, int wMsg, IntPtr wParam, IntPtr lParam);

		[StructLayout(LayoutKind.Sequential)]
		public struct COPYDATASTRUCT
		{
			public IntPtr dwData;    // Any value the sender chooses.  Perhaps its main window handle?
			public int cbData;       // The count of bytes in the message.
			public IntPtr lpData;    // The address of the message.
		}

		void SendMessage(IntPtr hWnd, byte[] array, int startIndex, int length)
		{
			IntPtr ptr = Marshal.AllocHGlobal(IntPtr.Size * 3 + length);
			Marshal.WriteIntPtr(ptr, 0, IntPtr.Zero);
			Marshal.WriteIntPtr(ptr, IntPtr.Size, (IntPtr)length);
			IntPtr dataPtr = new IntPtr(ptr.ToInt64() + IntPtr.Size * 3);
			Marshal.WriteIntPtr(ptr, IntPtr.Size * 2, dataPtr);
			Marshal.Copy(array, startIndex, dataPtr, length);
			int result = SendMessage(hWnd, WM_COPYDATA, IntPtr.Zero, ptr);
			Marshal.FreeHGlobal(ptr);
		}


		private void Application_Startup(object sender, StartupEventArgs e)
		{
			// check if already running
			Process _currentProcess = Process.GetCurrentProcess();
			Process _other = null;
			foreach (Process p in Process.GetProcessesByName(_currentProcess.ProcessName))
			{
				if (p.Id == _currentProcess.Id)
					continue;
				_other = p;
				break;
			}

			if (_other != null)
			{
				if (e.Args.Length > 0)
				{
					byte[] data = Encoding.Unicode.GetBytes(e.Args[0]);
					SendMessage(_other.MainWindowHandle, data, 0, data.Length);
				}
				else
				{
					MessageBox.Show("OpenCNCPilot is already running.");
				}
				Shutdown();
			}

			Args = e.Args;

			// upgrade settings after a new version was installed
			if (Settings.Default.SettingsUpdateRequired)
			{
				Settings.Default.Upgrade();
				Settings.Default.SettingsUpdateRequired = false;
				Settings.Default.Save();
			}
		}
	}
}
