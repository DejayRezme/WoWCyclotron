using System;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using Microsoft.Win32;

namespace WoWCyclotron
{
	public enum MultiBoxLayouts
	{
		BottomRow,
		BottomAndRight,
		BottomDoubleRow,
		PictureInPicture,
		CustomConfig,
	}

	public enum MultiBoxOrientation
	{
		Horizontal,
		Vertical,
		Both,
	}

	public enum MultiBoxMainWindow
	{
		TopLeft,
		TopRight,
		BottomLeft,
		BottomRight,
	}

	public struct Rectangle
	{
		public int Left, Top, Width, Height;
		public Rectangle(int left, int top, int width, int height) { Left = left; Top = top; Width = width; Height = height; }
		public bool isInside(int x, int y) 
		{
			return x >= Left && y >= Top && x < (Left + Width) && y < (Top + Height);
		}
	}

	public class WoWCyclotronConfig
	{
		public int boxCount = 5;
		public int maxBoxCount = 10;
		public MultiBoxLayouts layout = MultiBoxLayouts.BottomRow;
		public MultiBoxMainWindow layoutMain = MultiBoxMainWindow.TopLeft;
		public MultiBoxOrientation layoutOrientation = MultiBoxOrientation.Horizontal;
		public bool borderless = true;
		public bool alwaysOnTop = false;
		private String installPath = null;
		public String[] installPaths = null;
		public bool mouseFocusTracking = true;
		public bool taskbarAutohide = false;
		public bool subtractTaskbarHeight = false;
		public bool closeWoWWithApp = false;
		public bool taskbarAutohideFalseOnClose = true;
		public String swapWindowHotkey = "Control-Tab";
		public String cycleFocusHotkeys = "D1, D2, D3, D4, D5, Oem102, Y, X, C, V";
		public int focusSnapbackDelay = 350;
		public String unsecureClipboardString;
		public Rectangle PIPPosition = new Rectangle(800, 800, 240, 135);
		public Rectangle[] customLayout;

		public Rectangle previousAppPosition = new Rectangle();

		public const String configFileName = "WoWCyclotron.json";

		public static WoWCyclotronConfig load()
		{
			WoWCyclotronConfig config;

			try
			{
				JsonSerializerOptions options = new JsonSerializerOptions();
				options.IncludeFields = true;
				String jsonString = File.ReadAllText(configFileName);
				config = JsonSerializer.Deserialize<WoWCyclotronConfig>(jsonString, options);
			}
			catch (FileNotFoundException)
			{
				// ignore if not found
				config = new WoWCyclotronConfig();
			}
			catch (Exception e)
			{
				MessageBox.Show("Error while reading config file \"" + configFileName + "\", try fixing errors or deleting it. Error: " + e.Message);
				throw;
			}

			// initialize empty config
			if (config.installPaths == null)
			{
				if (config.installPath != null)
					config.installPaths = new String[] { config.installPath };
				else
				{
					String wowInstall = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Blizzard Entertainment\World of Warcraft", "InstallPath", null).ToString();
					config.installPaths = new String[] { wowInstall };
				}
			}
			config.installPath = null;

			if (config.customLayout == null)
			{
				config.customLayout = new Rectangle[5];
				config.customLayout[0] = new Rectangle(0, 0, 1920, 1080);
				for (int i = 1; i < config.customLayout.Length; i++)
					config.customLayout[i] = new Rectangle(i * 320, 0, 320, 180); 
			}

			return config;
		}

		public void save()
		{
			try
			{
				JsonSerializerOptions options = new JsonSerializerOptions();
				options.IncludeFields = true;
				options.WriteIndented = true;
				String jsonString = JsonSerializer.Serialize(this, options);
				File.WriteAllText(configFileName, jsonString);
			}
			catch (Exception e)
			{
				MessageBox.Show("Error while writing config file: " + configFileName + e.ToString());
			}
		}
	}
}
