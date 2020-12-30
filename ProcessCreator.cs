using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace WoWCyclotron
{
	public class ProcessCreator
	{
		public static int createProcess(String Application, String CommandLine)
		{
			PROCESS_INFORMATION pInfo = new PROCESS_INFORMATION();
			STARTUPINFO sInfo = new STARTUPINFO();
			SECURITY_ATTRIBUTES pSec = new SECURITY_ATTRIBUTES();
			SECURITY_ATTRIBUTES tSec = new SECURITY_ATTRIBUTES();
			pSec.nLength = Marshal.SizeOf(pSec);
			tSec.nLength = Marshal.SizeOf(tSec);

			bool retValue = CreateProcess(Application, CommandLine,
				ref pSec, ref tSec, false, NORMAL_PRIORITY_CLASS,
				IntPtr.Zero, null, ref sInfo, out pInfo);

			return pInfo.dwProcessId;
		}

		public const uint DEBUG_PROCESS = 0x00000001;
		public const uint DEBUG_ONLY_THIS_PROCESS = 0x00000002;
		public const uint CREATE_SUSPENDED = 0x00000004;
		public const uint DETACHED_PROCESS = 0x00000008;
		public const uint CREATE_NEW_CONSOLE = 0x00000010;
		public const uint NORMAL_PRIORITY_CLASS = 0x00000020;
		public const uint IDLE_PRIORITY_CLASS = 0x00000040;
		public const uint HIGH_PRIORITY_CLASS = 0x00000080;
		public const uint REALTIME_PRIORITY_CLASS = 0x00000100;
		public const uint CREATE_NEW_PROCESS_GROUP = 0x00000200;
		public const uint CREATE_UNICODE_ENVIRONMENT = 0x00000400;
		public const uint CREATE_SEPARATE_WOW_VDM = 0x00000800;
		public const uint CREATE_SHARED_WOW_VDM = 0x00001000;
		public const uint CREATE_FORCEDOS = 0x00002000;
		public const uint BELOW_NORMAL_PRIORITY_CLASS = 0x00004000;
		public const uint ABOVE_NORMAL_PRIORITY_CLASS = 0x00008000;
		public const uint INHERIT_PARENT_AFFINITY = 0x00010000;
		public const uint INHERIT_CALLER_PRIORITY = 0x00020000;
		public const uint CREATE_PROTECTED_PROCESS = 0x00040000;
		public const uint EXTENDED_STARTUPINFO_PRESENT = 0x00080000;
		public const uint PROCESS_MODE_BACKGROUND_BEGIN = 0x00100000;
		public const uint PROCESS_MODE_BACKGROUND_END = 0x00200000;
		public const uint CREATE_BREAKAWAY_FROM_JOB = 0x01000000;
		public const uint CREATE_PRESERVE_CODE_AUTHZ_LEVEL = 0x02000000;
		public const uint CREATE_DEFAULT_ERROR_MODE = 0x04000000;
		public const uint CREATE_NO_WINDOW = 0x08000000;
		public const uint PROFILE_USER = 0x10000000;
		public const uint PROFILE_KERNEL = 0x20000000;
		public const uint PROFILE_SERVER = 0x40000000;
		public const uint CREATE_IGNORE_SYSTEM_DEFAULT = 0x80000000;

		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		static extern bool CreateProcess(
			string lpApplicationName,
			string lpCommandLine,
			ref SECURITY_ATTRIBUTES lpProcessAttributes,
			ref SECURITY_ATTRIBUTES lpThreadAttributes,
			bool bInheritHandles,
			uint dwCreationFlags,
			IntPtr lpEnvironment,
			string lpCurrentDirectory,
			[In] ref STARTUPINFO lpStartupInfo,
			out PROCESS_INFORMATION lpProcessInformation);

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		struct STARTUPINFOEX
		{
			public STARTUPINFO StartupInfo;
			public IntPtr lpAttributeList;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		struct STARTUPINFO
		{
			public Int32 cb;
			public string lpReserved;
			public string lpDesktop;
			public string lpTitle;
			public Int32 dwX;
			public Int32 dwY;
			public Int32 dwXSize;
			public Int32 dwYSize;
			public Int32 dwXCountChars;
			public Int32 dwYCountChars;
			public Int32 dwFillAttribute;
			public Int32 dwFlags;
			public Int16 wShowWindow;
			public Int16 cbReserved2;
			public IntPtr lpReserved2;
			public IntPtr hStdInput;
			public IntPtr hStdOutput;
			public IntPtr hStdError;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct PROCESS_INFORMATION
		{
			public IntPtr hProcess;
			public IntPtr hThread;
			public int dwProcessId;
			public int dwThreadId;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct SECURITY_ATTRIBUTES
		{
			public int nLength;
			public IntPtr lpSecurityDescriptor;
			public int bInheritHandle;
		}

		//////////////////////////////////////////////////////////
		// debugging stuff
		//////////////////////////////////////////////////////////

		public static int debugProcess(String Application, String CommandLine)
		{
			PROCESS_INFORMATION pInfo = new PROCESS_INFORMATION();
			STARTUPINFO sInfo = new STARTUPINFO();
			SECURITY_ATTRIBUTES pSec = new SECURITY_ATTRIBUTES();
			SECURITY_ATTRIBUTES tSec = new SECURITY_ATTRIBUTES();
			pSec.nLength = Marshal.SizeOf(pSec);
			tSec.nLength = Marshal.SizeOf(tSec);

			bool retValue = CreateProcess(Application, CommandLine,
				ref pSec, ref tSec, false, DEBUG_ONLY_THIS_PROCESS,
				IntPtr.Zero, null, ref sInfo, out pInfo);

			DEBUG_EVENT debug_event = new DEBUG_EVENT();
			while (true)
			{
				if (!WaitForDebugEvent(ref debug_event, INFINITE))
					return 0;
				ContinueStatus continueStatus = ContinueStatus.DBG_CONTINUE;
				// ProcessDebugEvent(&debug_event);  // User-defined function, not API
				if (debug_event.dwDebugEventCode == DebugEventType.EXIT_PROCESS_DEBUG_EVENT)
					break;
				if (debug_event.dwDebugEventCode == DebugEventType.EXCEPTION_DEBUG_EVENT)
					continueStatus = ContinueStatus.DBG_EXCEPTION_NOT_HANDLED;
				ContinueDebugEvent(debug_event.dwProcessId, debug_event.dwThreadId, continueStatus);
			}
			// stop debugging
			retValue = DebugActiveProcessStop(pInfo.dwProcessId);
			Console.WriteLine("DebugActiveProcessStop returned: " + retValue);
			int wfso = (int)WaitForSingleObject(pInfo.hProcess, INFINITE);
			Console.WriteLine("Waitfor single object returned: " + $"0x{wfso:X}");

			// terminate the debuggee
			retValue = TerminateProcess(pInfo.hProcess, 0);
			Console.WriteLine("TerminateProcess returned: " + retValue);

			retValue = CloseHandle(pInfo.hProcess);
			Console.WriteLine("CloseHandle hProcess returned: " + retValue);
			retValue = CloseHandle(pInfo.hThread);
			Console.WriteLine("CloseHandle hThread returned: " + retValue);

			return 0;
		}

		[DllImport("kernel32.dll", EntryPoint = "WaitForDebugEvent")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool WaitForDebugEvent(ref DEBUG_EVENT lpDebugEvent, uint dwMilliseconds);

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool ContinueDebugEvent(int dwProcessId, int dwThreadId, ContinueStatus dwContinueStatus);

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool TerminateProcess(IntPtr hProcess, uint uExitCode);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool DebugActiveProcessStop(int dwProcessId);

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool CloseHandle(IntPtr hObject);

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern UInt32 WaitForSingleObject(IntPtr hHandle, UInt32 dwMilliseconds);
		const UInt32 INFINITE = 0xFFFFFFFF;
		const UInt32 WAIT_ABANDONED = 0x00000080;
		const UInt32 WAIT_OBJECT_0 = 0x00000000;
		const UInt32 WAIT_TIMEOUT = 0x00000102;

		public enum ContinueStatus : uint
		{
			DBG_CONTINUE = 0x00010002,
			DBG_EXCEPTION_NOT_HANDLED = 0x80010001,
			DBG_REPLY_LATER = 0x40010001
		}

		public enum DebugEventType : uint
		{
			RIP_EVENT = 9,
			OUTPUT_DEBUG_STRING_EVENT = 8,
			UNLOAD_DLL_DEBUG_EVENT = 7,
			LOAD_DLL_DEBUG_EVENT = 6,
			EXIT_PROCESS_DEBUG_EVENT = 5,
			EXIT_THREAD_DEBUG_EVENT = 4,
			CREATE_PROCESS_DEBUG_EVENT = 3,
			CREATE_THREAD_DEBUG_EVENT = 2,
			EXCEPTION_DEBUG_EVENT = 1,
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct DEBUG_EVENT
		{
			public DebugEventType dwDebugEventCode;
			public int dwProcessId;
			public int dwThreadId;

			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 86, ArraySubType = UnmanagedType.U1)]
			byte[] debugInfo;

			// public EXCEPTION_DEBUG_INFO Exception
			// {
			// 	get { return GetDebugInfo<EXCEPTION_DEBUG_INFO>(); }
			// }

			// public CREATE_THREAD_DEBUG_INFO CreateThread
			// {
			// 	get { return GetDebugInfo<CREATE_THREAD_DEBUG_INFO>(); }
			// }

			// public CREATE_PROCESS_DEBUG_INFO CreateProcessInfo
			// {
			// 	get { return GetDebugInfo<CREATE_PROCESS_DEBUG_INFO>(); }
			// }

			// public EXIT_THREAD_DEBUG_INFO ExitThread
			// {
			// 	get { return GetDebugInfo<EXIT_THREAD_DEBUG_INFO>(); }
			// }

			// public EXIT_PROCESS_DEBUG_INFO ExitProcess
			// {
			// 	get { return GetDebugInfo<EXIT_PROCESS_DEBUG_INFO>(); }
			// }

			// public LOAD_DLL_DEBUG_INFO LoadDll
			// {
			// 	get { return GetDebugInfo<LOAD_DLL_DEBUG_INFO>(); }
			// }

			// public UNLOAD_DLL_DEBUG_INFO UnloadDll
			// {
			// 	get { return GetDebugInfo<UNLOAD_DLL_DEBUG_INFO>(); }
			// }

			// public OUTPUT_DEBUG_STRING_INFO DebugString
			// {
			// 	get { return GetDebugInfo<OUTPUT_DEBUG_STRING_INFO>(); }
			// }

			// public RIP_INFO RipInfo
			// {
			// 	get { return GetDebugInfo<RIP_INFO>(); }
			// }

			private T GetDebugInfo<T>() where T : struct
			{
				var structSize = Marshal.SizeOf(typeof(T));
				var pointer = Marshal.AllocHGlobal(structSize);
				Marshal.Copy(debugInfo, 0, pointer, structSize);

				var result = Marshal.PtrToStructure(pointer, typeof(T));
				Marshal.FreeHGlobal(pointer);
				return (T)result;
			}
		}
	}
}