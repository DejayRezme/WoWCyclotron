using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace WoWCyclotron
{
	// Class to launch, configure, resize and layout the wow windows
	// Should be able to do:
	// * Launch all wow windows
	// * Relaunch a wow window if it's missing or if number of boxes is increased
	// * Change layout if different layout is selected
	// * Global hotkey to maximize PIPs or smaller WoW windows or cycle through followers
	// * 
	public class WoWResizer
	{
		protected WoWCyclotronConfig config;

		protected int selectedWindow = 0;
		protected List<Rectangle> layouts = new List<Rectangle>();
		//protected List<Process> wowProcesses = new List<Process>(100);
		protected List<WoWBoxState> wow = new List<WoWBoxState>(40);
		protected int currentMaximized = 0;
		protected int recentlyLaunched = 0;
		Stopwatch stopwatch;
		//Process myProcess;

		protected int windowBorderSize = 0;
		protected int windowCaptionSize = 0;

		private Hotkey[] cycleFocusHotkeys = {};
		private Hotkey[] swapWindowsHotkeys = {};

		public class WoWBoxState
		{
			public Process process = null;
			public bool isBorderless = false;
			public bool isAlwaysOnTop = false;
			public Rectangle position = new Rectangle(-1, -1, 0, 0);
		}

		public class Hotkey
		{
			public int id;       // function of key
			public int modifier; // combination of Keys.Control | Keys.Alt | Keys.Shift
			public Keys key;	 // 
			public bool wasPressed; // record last state
		}

		public WoWResizer(WoWCyclotronConfig config)
		{
			this.config = config;
			for (int i = 0; i < 40; i++)
				wow.Add(new WoWBoxState());
			//myProcess = Process.GetCurrentProcess();

			// set the foreground locktimeout to 0. This doesn't seem to change the registry on windows 10
			Win32Util.SystemParametersInfo(Win32Util.SPI_SETFOREGROUNDLOCKTIMEOUT, 0, new IntPtr(0), Win32Util.SPIF_SENDWININICHANGE);
			//Win32Util.SystemParametersInfo(Win32Util.SPI_SETFOREGROUNDFLASHCOUNT, 0, new IntPtr(0), Win32Util.SPIF_SENDWININICHANGE);

			// start a stopwatch for focus snapback timeout
			stopwatch = new Stopwatch();
			stopwatch.Start();
		}

		public void LaunchWoW(int boxNumber)
		{
			Win32Util.AllowSetForegroundWindow(Process.GetCurrentProcess().Id); //Win32Util.ASFW_ANY

			// first check if the process isn't already running
			Process wowProcess = wow[boxNumber].process;
			if (wowProcess == null || wowProcess.HasExited)
			{
				ProcessStartInfo startInfo = new ProcessStartInfo();

				// get the appropriate install path of which directory to launch for this box
				int installPathIndex = Math.Min(boxNumber, config.installPaths.Length - 1);
				String installPath = config.installPaths[installPathIndex];

				// check if wow.exe or wowClassic.exe exists	
				String wowExePath;
				if (File.Exists(installPath + "\\Wow.exe"))
					wowExePath = installPath + "\\Wow.exe";
				else if (File.Exists(installPath + "\\WowClassic.exe"))
					wowExePath = installPath + "\\WowClassic.exe";
				else
					throw new Exception("Wow.exe or WowClassic.exe not found in " + installPath);

				startInfo.UseShellExecute = false;
				startInfo.FileName = wowExePath;
				startInfo.WorkingDirectory = installPath;
				recentlyLaunched++;

				// check to see if special config<N>.wtf exists and use it. Otherwise don't pass argument
				String configWTF = "config" + (boxNumber + 1) + ".wtf";
				if (File.Exists(installPath + "\\WTF\\" + configWTF))
					startInfo.Arguments = "-config " + configWTF;

				//int processId = ProcessCreator.run(startInfo.FileName, null);
				//wow[boxNumber].process = Process.GetProcessById(processId);

				wow[boxNumber].process = Process.Start(startInfo);
				wow[boxNumber].isBorderless = false;
				wow[boxNumber].isAlwaysOnTop = false;
				//bool r = Win32Util.AllowSetForegroundWindow(wow[boxNumber].process.Id);

				// wait for the process to create a window handle, otherwise order in taskbar is screwed up
				//wow[boxNumber].process.WaitForInputIdle(5000);
				stopwatch.Restart();
				while (wow[boxNumber].process.MainWindowHandle == IntPtr.Zero && stopwatch.ElapsedMilliseconds < 1000)
					Thread.Sleep(10);
			}
		}

		public void GenerateWoWLayout()
		{
			layouts.Clear();

			Rectangle screen;
			if (config.subtractTaskbarHeight)
			{
				var vs = SystemInformation.VirtualScreen;
				vs = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;
				screen = new Rectangle(vs.Left, vs.Top, vs.Width, vs.Height);
			}
			else
				screen = new Rectangle(0, 0, SystemInformation.PrimaryMonitorSize.Width, SystemInformation.PrimaryMonitorSize.Height);

			if (config.layout == MultiBoxLayouts.BottomRow)
			{
				// put all them pips on the bottom row, divide screen by boxCount-1
				int gridN = Math.Max(3, config.boxCount - 1);
				int pipWidth = screen.Width / gridN;
				int pipHeight = (pipWidth / 16) * 9;
				int mainHeight = screen.Height - pipHeight;
				int mainWidth = mainHeight / 9 * 16;

				layouts.Add(new Rectangle(screen.Left, screen.Top, mainWidth, mainHeight));
				for (int i = 0; i < config.boxCount - 1; i++)
				{   // we can fit gridN windows at the bottom
					layouts.Add(new Rectangle(i * pipWidth, mainHeight, pipWidth, pipHeight));
				}
			}
			else if (config.layout == MultiBoxLayouts.BottomDoubleRow)
			{
				// put all them pips on the bottom row, divide screen by boxCount-1
				int gridN = Math.Max(4, (int)Math.Ceiling((config.boxCount - 1) / 2.0));
				int pipWidth = screen.Width / gridN;
				int pipHeight = (pipWidth / 16) * 9;
				int mainHeight = screen.Height - pipHeight * 2;
				int mainWidth = mainHeight / 9 * 16;

				layouts.Add(new Rectangle(screen.Left, screen.Top, mainWidth, mainHeight));
				// we can fit equal windows on both rows
				int firstRow = ((config.boxCount - 1) / 2);
				for (int i = 0; i < config.boxCount - 1; i++)
				{
					if (i < firstRow)
						layouts.Add(new Rectangle(i * pipWidth, mainHeight, pipWidth, pipHeight));
					else
						layouts.Add(new Rectangle((i - firstRow) * pipWidth, mainHeight + pipHeight, pipWidth, pipHeight));
				}
			}
			else if (config.layout == MultiBoxLayouts.BottomAndRight)
			{   // Main window topleft with windows arranted in L shape around bottom right
				// how many screens fit into an L shape in an N*N grid: 1 + N + N-1 = 2*N
				// Calculate grid needed for LType layout: MB / 2
				int gridN = Math.Max(3, (int)Math.Ceiling(config.boxCount / 2.0));
				int pipWidth = screen.Width / gridN;
				//pipWidth = Math.Floor(pipWidth / 16) * 16;
				int pipHeight = (pipWidth / 16) * 9;
				int mainHeight = screen.Height - pipHeight;
				//mainHeight = Math.Floor(mainHeight / 9) * 9;
				int mainWidth = pipWidth * (gridN - 1);

				layouts.Add(new Rectangle(screen.Left, screen.Top, mainWidth, mainHeight));
				for (int i = 0; i < gridN; i++)
				{   // we can fit gridN windows at the bottom
					layouts.Add(new Rectangle(i * pipWidth, mainHeight, pipWidth, pipHeight));
				}
				for (int i = 0; i < (config.boxCount - 1 - gridN); i++)
				{   // we can fit the rest at the right
					layouts.Add(new Rectangle(mainWidth, mainHeight - (i + 1) * pipHeight, pipWidth, pipHeight));
				}
			}
			else if (config.layout == MultiBoxLayouts.PictureInPicture)
			{
				layouts.Add(screen);
				Rectangle pip = config.PIPPosition;
				// put the pips according to config orientation
				for (int i = 0; i < config.boxCount - 1; i++)
				{
					if (config.layoutOrientation == MultiBoxOrientation.Horizontal)
						layouts.Add(new Rectangle(pip.Left + i * pip.Width, pip.Top, pip.Width, pip.Height));
					else
						layouts.Add(new Rectangle(pip.Left, pip.Top + i * pip.Height, pip.Width, pip.Height));
				}

			}
			else if (config.layout == MultiBoxLayouts.CustomConfig)
			{
				for (int i = 0; i < config.boxCount; i++)
				{   // put custom configs in there, repeat last one if not enough
					layouts.Add(config.customLayout[Math.Min(i, config.customLayout.Length - 1)]);
				}
			}
		}

		public void LayoutWoWWindows()
		{
			currentMaximized = 0;
			for (int i = 0; i < config.boxCount; i++)
			{
				bool alwaysOnTop = config.alwaysOnTop && (i > 0);
				LayoutWoWWindow(i, i, alwaysOnTop);
			}
		}

		public void LayoutWoWWindow(int boxNumber, int layoutNumer, bool alwaysOnTop)
		{
			WoWBoxState wowState = wow[boxNumber];
			wow[boxNumber].process.WaitForInputIdle(5000);
			IntPtr wowHandle = wow[boxNumber].process.MainWindowHandle;

			// update the window style to borderless and captionless
			if (wowState.isBorderless != config.borderless) {
				Win32Util.setBorderless(wowHandle, config.borderless);
				wowState.isBorderless = config.borderless;
			}

			int x = layouts[layoutNumer].Left;
			int y = layouts[layoutNumer].Top;
			int w = layouts[layoutNumer].Width;
			int h = layouts[layoutNumer].Height;

			// if this is our first window, get the border size by comparing window and client size
			if (windowBorderSize == 0)
			{
				RECT windowRect, clientRect;
				Win32Util.GetWindowRect(wowHandle, out windowRect);
				Win32Util.GetClientRect(wowHandle, out clientRect);
				windowBorderSize = ((windowRect.Right - windowRect.Left) - clientRect.Right) / 2;
				windowCaptionSize = ((windowRect.Bottom - windowRect.Top) - clientRect.Bottom - windowBorderSize);
			}

			// fix window size for borders with shadow
			if (!config.borderless)
			{
				x -= windowBorderSize;
				y -= windowCaptionSize;
				w += windowBorderSize * 2;
				h += windowCaptionSize + windowBorderSize;
			}

			// move and resize the window as needed. MoveWindow seems to be faster than PositionWindow
			Win32Util.MoveWindow(wowHandle, x, y, w, h, false);
			//Win32Util.PositionWindow(wowHandle, x, y, w, h, alwaysOnTop);

			// resizing can invalidate always on top setting
			bool isResize = (wowState.position.Width != w || wowState.position.Height != h);
			wowState.position = new Rectangle(x, y, w, h);

			// update the always on top flag if needed or on resize
			if (wowState.isAlwaysOnTop != alwaysOnTop || isResize) {
				Win32Util.setAlwaysOnTop(wowHandle, alwaysOnTop);
				wowState.isAlwaysOnTop = alwaysOnTop;
			}
		}

		public void swapToWindow(int nextMaximized)
		{
			// reset currentlyMaximized (soon previously maximized) unless it's 0
			if (currentMaximized != 0)
				LayoutWoWWindow(currentMaximized, currentMaximized, config.alwaysOnTop);
			// put the 0 to where the nextMaximized is
			LayoutWoWWindow(0, nextMaximized, config.alwaysOnTop);
			// maximize the nextMaximized
			LayoutWoWWindow(nextMaximized, 0, false);

			// this sets the maximized window as the foreground window but messes with switching back and fourth
			Win32Util.SetForegroundWindow(wow[nextMaximized].process.MainWindowHandle);

			currentMaximized = nextMaximized;
		}

		public void swapWindow(bool tabForward)
		{
			// get the current foreground window
			IntPtr forergoundWin = Win32Util.GetForegroundWindow();

			// check if it's one of our wow windows
			for (int i = 0; i < config.boxCount; i++)
			{
				Process wp = wow[i].process;
				if (wp != null && !wp.HasExited && wp.MainWindowHandle == forergoundWin)
				{
					// either we pressed Ctrl+Tab above the maximized or main window and want to cycle next
					// or we Ctrl+Tab above above one of the PIP windows and want to maximize that one
					if (i == currentMaximized)
					{
						int nextMaximized = (currentMaximized + 1) % config.boxCount;
						swapToWindow(nextMaximized);
					}
					else
					{
						swapToWindow(i);
					}
					// break from this loop
					return;
				}
			}
		}

		static String[] hotkeyReplacements = {"0", "1", "2", "3", "4", "5", "6", "7", "8", "9"};
		static String[] hotkeySubstitution = {"D0", "D1", "D2", "D3", "D4", "D5", "D6", "D7", "D8", "D9"};
		static List<String> hotkeyReplacementsList = new List<String>(hotkeyReplacements);

		public Hotkey[] parseHotkeys(String hotkeyConfigString) 
		{
			// try to read the hotkey definition of the config string
			String[] hotkeysSplit = hotkeyConfigString.Split(',');
			Hotkey[] hotkeys = new Hotkey[hotkeysSplit.Length];

			for (int i = 0; i < hotkeysSplit.Length; i++)
			{
				String hotkeyString = hotkeysSplit[i];
				String[] hotkeyParts = hotkeyString.Split('-');
				hotkeys[i] = new Hotkey();
				for (int p = 0; p < hotkeyParts.Length; p++)
				{
					String hotkeyPart = hotkeyParts[p].Trim();

					// check if we have number keys like "1" to be replaced by "D1"
					int match = hotkeyReplacementsList.IndexOf(hotkeyPart);
					if (match != -1)
						hotkeyPart = hotkeySubstitution[match];
					
					// find the matching Keys enum 
					Keys key = Enum.Parse<Keys>(hotkeyPart);
					if (key == Keys.Control)
						hotkeys[i].modifier |= (int)Keys.Control;
					else if (key == Keys.Alt)
						hotkeys[i].modifier |= (int)Keys.Alt;
					else if (key == Keys.Shift)
						hotkeys[i].modifier |= (int)Keys.Shift;
					else
						hotkeys[i].key = key;

				}
			}
			return hotkeys;
		}

		public void updateCycleFocusHotkeys(String hotkeysString)
		{
			cycleFocusHotkeys = parseHotkeys(hotkeysString);
		}

		public void updateSwapWindowHotkeys(String hotkeysString)
		{
			swapWindowsHotkeys = parseHotkeys(hotkeysString);
		}

		// private void registerHotkey(bool enableHotkey, String hotkeyString, int idCode)
		// {
		// 		bool isHotkeyRegistered = hotkeys.ContainsKey(hotkeyString);
		// 		if (enableHotkey && !isHotkeyRegistered)
		// 		{   // hotkey should be active but hasn't been registered
		// 			Win32Util.RegisterHotKey(this.Handle, idCode, keyModifier, key);
		// 			hotkeys.Add(hotkeyString, idCode);
		// 		}
		// 		else if (!enableHotkey && isHotkeyRegistered)
		// 		{   // hotkey shouldn't be active but still is registered
		// 			Win32Util.UnregisterHotKey(this.Handle, idCode);
		// 			hotkeys.Remove(hotkeyString);
		// 		}
		// }

		public bool isKeyPressed(Keys key) 
		{
			int keyState = Win32Util.GetAsyncKeyState(key.GetHashCode());
			return (keyState & 0x80000000) != 0;
		}

		public bool checkKeyboardState(Hotkey[] hotkeys, int modifierState) 
		{
			// check the key state of all the keys we're watching, plus all the modifiers
			for (int i = 0; i < hotkeys.Length; i++) 
			{
				// check if key was pressed since last we checked. We need to check regardless of keymodifier to avoid it popping up later
				int keyState = Win32Util.GetAsyncKeyState(hotkeys[i].key.GetHashCode());
				if ((keyState & 0x1) != 0 && modifierState == hotkeys[i].modifier) 
					hotkeys[i].wasPressed = true;
				if ((keyState & 0x80000000) == 0 && hotkeys[i].wasPressed && modifierState == hotkeys[i].modifier)
				{
					hotkeys[i].wasPressed = false;
					return true;
				}
			}
			return false;
		}

		public void checkKeyboardState(int mouseX, int mouseY, bool enableHotkeys)
		{
			// get foreground window and wow process. 
			IntPtr forergoundWin = Win32Util.GetForegroundWindow();
			// check if it's one of our wow windows
			int foregroundWoW = -1;
			for (int i = 0; i < config.boxCount; i++)
				if (wow[i].process != null && wow[i].process.MainWindowHandle == forergoundWin)
				{
					foregroundWoW = i;
					break;
				}
			int og = foregroundWoW;

			int modifierState = 0;
			if (isKeyPressed(Keys.ControlKey))
				modifierState |= (int)Keys.Control;
			if (isKeyPressed(Keys.Menu))
				modifierState |= (int)Keys.Alt;
			if (isKeyPressed(Keys.ShiftKey))
				modifierState |= (int)Keys.Shift;

			// We only do anything if foreground window is wow
			if (foregroundWoW != -1)
			{
				// if the timeout for focus switching has elapsed, snap focus to targeted window
				if (stopwatch.ElapsedMilliseconds > config.focusSnapbackDelay && config.mouseFocusTracking)
				{
					// check the mouse position and set targeted wow window to foreground. Go backwards for PIPs
					for (int i = layouts.Count - 1; i >= 0; i--) 
					{
						if (isWoWRunning(i) && layouts[i].isInside(mouseX, mouseY)) 
						{
							foregroundWoW = i;
							if (currentMaximized != 0)
								if (foregroundWoW == currentMaximized)
									foregroundWoW = 0;
								else if (foregroundWoW == 0)
									foregroundWoW = currentMaximized;

							if (foregroundWoW != og) 
							{
								//Win32Util.AllowSetForegroundWindow(wow[foregroundWoW].process.Id);
								//Win32Util.SetForegroundWindow(myProcess.MainWindowHandle);
								Win32Util.SetForegroundWindow(wow[foregroundWoW].process.MainWindowHandle);
								//Win32Util.SetFocus(wow[foregroundWoW].process.MainWindowHandle);
							}
							break;
						}
					}
				}

				// only do something if hotkeys are enabled with scrollLock
				if (enableHotkeys)
				{
					// check if the swap hotkey has been pressed
					bool swapHotkeyPressed = checkKeyboardState(swapWindowsHotkeys, modifierState);
					if (swapHotkeyPressed)
						swapWindow(true);

					// check if a cycle focus hotkey has been pressed
					bool cycleFocusHotkeyPressed = checkKeyboardState(cycleFocusHotkeys, modifierState);
					if (cycleFocusHotkeyPressed)
					{
						// cycle to next foreground wow window
						foregroundWoW = (foregroundWoW + 1) % config.boxCount;
						if (isWoWRunning(foregroundWoW))
							Win32Util.SetForegroundWindow(wow[foregroundWoW].process.MainWindowHandle);
						stopwatch.Restart();
					}
				}
			}
		}

		public void WaitForWoWReady() 
		{
			if (recentlyLaunched > 0) 
			{
				for (int i = 0; i < config.boxCount; i++)
					wow[i].process.WaitForInputIdle(10000);
				Thread.Sleep(250);
				recentlyLaunched = 0;
			}
		}

		public bool isWoWRunning(int boxNumber) 
		{
			return wow[boxNumber].process != null && !wow[boxNumber].process.HasExited && wow[boxNumber].process.MainWindowHandle != IntPtr.Zero;
		}

		public int recoverAlreadyRunningWoWWindows() 
		{
			GenerateWoWLayout();

			// see if we can / want to recover previous wow instances
			int runningWoWs = 0;
			int orphanCount = 0;			
			for (int i = 0; i < config.boxCount; i++)
				if (isWoWRunning(i))
					runningWoWs++;

			if (runningWoWs < config.boxCount) 
			{
				// find orphaned wow processes that are not in our list and sort them in dictionary
				SortedDictionary<long, Process> orphans = new SortedDictionary<long, Process>();
				Process[] list = Process.GetProcesses();
				foreach (Process p in list) 
				{
					if (p.MainWindowTitle.StartsWith("World of Warcraft") && p.MainModule.ModuleName.Equals("Wow.exe")) 
					{
						bool isOrphaned = true;
						for (int i = 0; i < config.boxCount; i++)
							if (isWoWRunning(i) && wow[i].process.MainWindowHandle == p.MainWindowHandle)
								isOrphaned = false;
						if (isOrphaned) 
							orphans.Add(p.StartTime.Ticks, p);
					}
				}
				orphanCount = orphans.Count;

				// go through each wow and assign the oldest orphan
				List<Process> orphansList = new List<Process>(orphans.Values);
				//orphansList.Reverse(); // why no reverse? Weird lol
				for (int i = 0; i < config.boxCount; i++)
					if (!isWoWRunning(i) && orphansList.Count > 0)
					{
						wow[i].process = orphansList[0];
						orphansList.RemoveAt(0);
					}
			}
			return orphanCount;
		}

		public int launchWoWClients()
		{
			Stopwatch stoppy = new Stopwatch();
			stoppy.Start();

			currentMaximized = 0;
			GenerateWoWLayout();
			// first make sure all are launched
			for (int i = 0; i < config.boxCount; i++)
				LaunchWoW(i);
			// wait for them to be all ready
			WaitForWoWReady();

			//System.Console.WriteLine("Elapsed time since starting: " + stoppy.ElapsedMilliseconds);
			// the rack 'em and stack em
			for (int i = 0; i < config.boxCount; i++)
				LayoutWoWWindow(i, i, config.alwaysOnTop && (i > 0));
			
			Win32Util.SystemParametersInfo(Win32Util.SPI_SETFOREGROUNDLOCKTIMEOUT, 0, new IntPtr(0), Win32Util.SPIF_SENDWININICHANGE);

			return (int)stoppy.ElapsedMilliseconds;
		}

		public bool isLaunched()
		{
			return (wow[0].process != null && !wow[0].process.HasExited);
		}

		public void closeWoWResizerNow()
		{
			for (int i = 0; i < wow.Count; i++)
			{
				if (wow[i].process != null && !wow[i].process.HasExited)
					wow[i].process.CloseMainWindow();
			}
		}
	}
}