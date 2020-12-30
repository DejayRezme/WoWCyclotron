using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace WoWCyclotron
{
	static class Program
	{
		[STAThread]
		static void Main(string[] args)
		{
			bool beingDebugged = Win32Util.IsDebuggerPresent();
			bool doNotRelaunch = Array.Exists(args, s => s.Contains("doNotRelaunch"));;
			Console.WriteLine("We are " + (beingDebugged ? "" : "NOT ") + "being debugged and doNotRelaunch argument is " + (doNotRelaunch ? "" : "NOT ") + "present");
			if (!beingDebugged && !doNotRelaunch)
			{
				// if we're not being debugged, simply launch ourselves again
				Console.WriteLine("Launching myself again in debug mode");
				// AppDomain.CurrentDomain.BaseDirectory +  //@"M:\Games\WoWTools\WoWFocusRecycler\bin\Debug\net5.0-windows\win-x64\publish\" + 
				String exePath = AppDomain.CurrentDomain.BaseDirectory + AppDomain.CurrentDomain.FriendlyName + ".exe";
				bool exeExists = File.Exists(exePath);
				Console.WriteLine("Our executable: " + exePath + (exeExists ? " exists! " : " DOES NOT EXIST!"));
				if (exeExists)
				{
					int pid = ProcessCreator.debugProcess(null, exePath + " doNotRelaunch");
					Process s = Process.GetProcessById(pid);
					Console.WriteLine("Finished debugging process: " + s.ToString());
				}
				Console.WriteLine("Exiting");
				return;
			}

			Application.SetHighDpiMode(HighDpiMode.SystemAware);
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new WoWCyclotronForm());
		}
	}
}
