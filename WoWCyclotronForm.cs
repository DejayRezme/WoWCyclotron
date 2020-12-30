using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using CryptoTool;

namespace WoWCyclotron
{
	public class WoWCyclotronForm : Form
	{
		protected WoWCyclotronConfig config;
		protected WoWResizer resizer;

		protected TableLayoutPanel layoutPanel;

		protected NumericUpDown boxNumeric;
		protected ComboBox layoutComboBox;
		protected NumericUpDown pipLeftNumeric;
		protected NumericUpDown pipTopNumeric;
		protected NumericUpDown pipWidthNumeric;
		protected NumericUpDown pipHeightNumeric;
		protected CheckBox borderlessCheckBox;
		protected CheckBox alwaysOnTopCheckBox;
		protected CheckBox mouseFocusTrackingCheckBox;
		protected CheckBox taskbarAutohideCheckBox;
		protected CheckBox closeWoWWithAppCheckBox;
		protected Button closeWoWNowButton;
		protected TextBox swapWindowHotkeyTextBox;
		protected TextBox cycleFocusHotkeyTextBox;
		protected TextBox installPathTextBox;
		protected Button clipboardButton;
		protected Button launchButton;
		protected StatusStrip statusStrip;
		protected ToolStripStatusLabel statusStripLabel;

		private Timer windowSwitcherTimer;

		private Dictionary<String, int> hotkeys = new Dictionary<String, int>();

		public WoWCyclotronForm()
		{
			config = WoWCyclotronConfig.load();
			resizer = new WoWResizer(config);

			this.Text = "WoW Cyclotron";
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			//this.ClientSize = new System.Drawing.Size(800, 450);
			try
			{   // try setting the app icon from the launched executable
				this.Icon = Icon.ExtractAssociatedIcon(AppDomain.CurrentDomain.BaseDirectory + AppDomain.CurrentDomain.FriendlyName + ".exe");
			}
			catch (System.Exception)
			{
				this.ShowIcon = false;
			}

			//this.AutoSize = true;
			//this.AutoSizeMode = AutoSizeMode.GrowOnly;
			// add handler to close app on esc
			this.KeyPreview = true;
			this.KeyDown += new KeyEventHandler(OnAnyKeyDown);

			// NumberBox to select number of boxes
			// ComboBox to select layout
			// Preview of layout type
			// Checkbox for borderless border
			// Checkbox for alwaysOnTop for client boxes
			// Select wow install and retail or classic
			// Optional: Checkbox to enable mouse focus tracking (while running, disable on exit)
			// Optional: Checkbox to set Taskbar autohide
			// Checkbox to keep running and enable hotkey to maximize window

			layoutPanel = new TableLayoutPanel();
			layoutPanel.AutoSize = true;
			//layoutPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			layoutPanel.Dock = DockStyle.Fill;
			layoutPanel.Padding = new Padding(30);
			// layoutPanel.CellBorderStyle = TableLayoutPanelCellBorderStyle.InsetDouble;
			this.Controls.Add(layoutPanel);

			int row = 0;

			// NumberBox to select number of boxes
			boxNumeric = new NumericUpDown();
			boxNumeric.AutoSize = true;
			boxNumeric.Value = config.boxCount;
			boxNumeric.Maximum = config.maxBoxCount;
			boxNumeric.Anchor = AnchorStyles.Left;
			boxNumeric.ValueChanged += new EventHandler(OnBoxCountChanged);
			AddTableLabelControl("Number of boxes:", 0, row++, boxNumeric);

			// Preview of layout type?

			// ComboBox to select layout
			layoutComboBox = new ComboBox();
			layoutComboBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
			layoutComboBox.Items.AddRange(Enum.GetNames(typeof(MultiBoxLayouts)));
			layoutComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
			layoutComboBox.SelectedItem = config.layout.ToString();
			//layoutComboBox.Padding = new Padding(40);
			layoutComboBox.SelectedIndexChanged += new EventHandler(OnLayoutChanged);
			AddTableLabelControl("Select Layout:", 0, row++, layoutComboBox);

			pipLeftNumeric = new NumericUpDown();
			InitNumeric(pipLeftNumeric, config.PIPPosition.Left);
			pipTopNumeric = new NumericUpDown();
			InitNumeric(pipTopNumeric, config.PIPPosition.Top);
			AddTableLabelControl("PIP Position:", 0, row++, pipLeftNumeric, pipTopNumeric, 2);
			pipWidthNumeric = new NumericUpDown();
			InitNumeric(pipWidthNumeric, config.PIPPosition.Width);
			pipHeightNumeric = new NumericUpDown();
			InitNumeric(pipHeightNumeric, config.PIPPosition.Height);
			AddTableLabelControl("PIP Size:", 0, row++, pipWidthNumeric, pipHeightNumeric, 2);
			updatePIPSizeVisible();

			borderlessCheckBox = new CheckBox();
			borderlessCheckBox.AutoSize = true;
			borderlessCheckBox.Anchor = AnchorStyles.Left;
			borderlessCheckBox.Checked = config.borderless;
			borderlessCheckBox.CheckedChanged += new EventHandler(OnBorderlessChanged);
			AddTableLabelControl("Borderless WoW: ", 0, row++, borderlessCheckBox);

			alwaysOnTopCheckBox = new CheckBox();
			alwaysOnTopCheckBox.AutoSize = true;
			alwaysOnTopCheckBox.Anchor = AnchorStyles.Left;
			alwaysOnTopCheckBox.Checked = config.alwaysOnTop;
			alwaysOnTopCheckBox.CheckedChanged += new EventHandler(OnAlwaysOnTopChanged);
			AddTableLabelControl("AlwaysOnTop clients: ", 0, row++, alwaysOnTopCheckBox);

			mouseFocusTrackingCheckBox = new CheckBox();
			mouseFocusTrackingCheckBox.AutoSize = true;
			mouseFocusTrackingCheckBox.Anchor = AnchorStyles.Left;
			mouseFocusTrackingCheckBox.Checked = config.mouseFocusTracking;
			mouseFocusTrackingCheckBox.CheckedChanged += new EventHandler(OnMouseFocusTrackingChanged);
			AddTableLabelControl("WoW focus tracking: ", 0, row++, mouseFocusTrackingCheckBox);

			taskbarAutohideCheckBox = new CheckBox();
			taskbarAutohideCheckBox.AutoSize = true;
			taskbarAutohideCheckBox.Anchor = AnchorStyles.Left;
			taskbarAutohideCheckBox.Checked = config.taskbarAutohide;
			taskbarAutohideCheckBox.CheckedChanged += new EventHandler(OnTaskbarAutohideChanged);
			AddTableLabelControl("Autohide taskbar: ", 0, row++, taskbarAutohideCheckBox);

			closeWoWWithAppCheckBox = new CheckBox();
			closeWoWWithAppCheckBox.AutoSize = true;
			closeWoWWithAppCheckBox.Anchor = AnchorStyles.Left;
			closeWoWWithAppCheckBox.Checked = config.closeWoWWithApp;
			closeWoWWithAppCheckBox.CheckedChanged += new EventHandler(OnCloseWoWWithAppChanged);

			closeWoWNowButton = new Button();
			closeWoWNowButton.Text = "Close";
			closeWoWNowButton.AutoSize = true;
			closeWoWNowButton.Dock = DockStyle.Fill;
			closeWoWNowButton.Click += new EventHandler(OnCloseWoWNowClicked);
			AddTableLabelControl("Close WoW with app:", 0, row++, closeWoWWithAppCheckBox, closeWoWNowButton, 3);

			// Checkbox to keep running and enable hotkey to maximize window
			swapWindowHotkeyTextBox = new TextBox();
			swapWindowHotkeyTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
			swapWindowHotkeyTextBox.Text = config.swapWindowHotkey;
			swapWindowHotkeyTextBox.TextChanged += new EventHandler(OnSwapHotkeyChanged);
			AddTableLabelControl("Swap win hotkey: ", 0, row++, swapWindowHotkeyTextBox);
			OnSwapHotkeyChanged(null, null);
			
			// Checkbox to keep running and enable hotkey to maximize window
			cycleFocusHotkeyTextBox = new TextBox();
			cycleFocusHotkeyTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
			cycleFocusHotkeyTextBox.Text = config.cycleFocusHotkeys;
			cycleFocusHotkeyTextBox.TextChanged += new EventHandler(OnCycleFocusHotkeyChanged);
			AddTableLabelControl("Cycle focus hotkey: ", 0, row++, cycleFocusHotkeyTextBox);
			OnCycleFocusHotkeyChanged(null, null);
			
			// type in path to wow install. Fun!
			installPathTextBox = new TextBox();
			//installPathTextBox.Text = config.installPath;
			installPathTextBox.Text = String.Join(", ", config.installPaths);
			installPathTextBox.AutoSize = true;
			installPathTextBox.ReadOnly = true;
			installPathTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
			checkInstallPathSet();

			Button installFileSelectorButton = new Button();
			installFileSelectorButton.Text = "Browse";
			installFileSelectorButton.AutoSize = true;
			installFileSelectorButton.Dock = DockStyle.Fill;
			//installFileSelectorButton.Anchor = AnchorStyles.Left | AnchorStyles.Right;
			installFileSelectorButton.Click += new EventHandler(OnInstallFileSelectorClicked);

			layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 1));
			layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

			AddTableLabelControl("Install path wow.exe:", 0, row++, installPathTextBox, installFileSelectorButton, 3);

			// nothing to see here, move along
			if (config.unsecureClipboardString != null) 
			{
				clipboardButton = new Button();
				clipboardButton.Text = "Copy";
				clipboardButton.AutoSize = true;
				clipboardButton.Dock = DockStyle.Fill;
				clipboardButton.Click += new EventHandler(OnClipboardButtonClicked);
				AddTableLabelControl("Copy to clipboard:", 0, row++, clipboardButton);
			}

			// Button to launch wow
			launchButton = new Button();
			launchButton.Dock = DockStyle.Top;
			launchButton.Text = "Launch WoW / Apply changes";
			launchButton.AutoSize = true;
			//launchButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			launchButton.Click += new EventHandler(OnLaunchButtonClicked);
			//launchButton.Anchor = AnchorStyles.Left | AnchorStyles.Right;
			layoutPanel.Controls.Add(launchButton);
			layoutPanel.SetCellPosition(launchButton, new TableLayoutPanelCellPosition(0, row++));
			layoutPanel.SetColumnSpan(launchButton, 5);

			StatusStrip statusStrip = new StatusStrip();
			statusStrip.Dock = DockStyle.Bottom;
			statusStrip.LayoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow;
			this.Controls.Add(statusStrip);

			statusStripLabel = new ToolStripStatusLabel("Hello world of warcraft!");
			statusStrip.Items.Add(statusStripLabel);

			// set the minimum size for the form
			Size windowSize = new Size(this.PreferredSize.Width * 12 / 10, this.PreferredSize.Height);
			this.MinimumSize = windowSize;
			// position this form to the center of the cursor position
			this.StartPosition = FormStartPosition.Manual;
			Point startPosition = Cursor.Position;
			startPosition.Offset(-windowSize.Width / 2, -windowSize.Height / 2);
			this.Location = startPosition;

			windowSwitcherTimer = new Timer();
			windowSwitcherTimer.Interval = 5;
			windowSwitcherTimer.Tick += new EventHandler(OnWindowSwitcherTimer);
			windowSwitcherTimer.Start();

			int recovered = resizer.recoverAlreadyRunningWoWWindows();
			if (recovered > 0)
				statusStripLabel.Text = "Found " + recovered + " running WoW windows";
		}

		private void OnClipboardButtonClicked(object sender, EventArgs e)
		{
			String passphrase = "Wl84dfXzzgNzv8yS0v8Sewujz0IlOIJxhyD8cjQy";
			if (!config.unsecureClipboardString.StartsWith("crypto")) 
				config.unsecureClipboardString = "crypto" + Crypto.Encrypt(Crypto.GetMACAddress() + config.unsecureClipboardString, passphrase);
			String clipboardString = Crypto.Decrypt(config.unsecureClipboardString.Substring(6), passphrase).Substring(12);
			Clipboard.SetText(clipboardString);

			Timer timer = new Timer();
			timer.Interval = 60 * 1000;
			timer.Tick += new EventHandler(OnClipboardTimerTick);
			timer.Start();
		}

		private void OnClipboardTimerTick(object sender, EventArgs e)
		{
			Timer timer = (Timer) sender;
			timer.Stop();
			Clipboard.Clear();
		}

		private void OnInstallFileSelectorClicked(object sender, EventArgs e)
		{
			FolderBrowserDialog fileDialog = new FolderBrowserDialog();
			if (Directory.Exists(config.installPaths[0]))
				fileDialog.SelectedPath = config.installPaths[0];
			else
				fileDialog.SelectedPath = Directory.GetCurrentDirectory();
			DialogResult fileResult = fileDialog.ShowDialog();

			if (fileResult == DialogResult.OK)
			{
				config.installPaths = new String[] { fileDialog.SelectedPath };
				installPathTextBox.Text = fileDialog.SelectedPath;
				checkInstallPathSet();
			}
		}

		private bool checkInstallPathSet()
		{
			bool validPath = true;
			foreach (String path in config.installPaths)
				if (!File.Exists(path + "\\wow.exe") && !File.Exists(path + "\\wowClassic.exe"))
					validPath = false;
			installPathTextBox.BackColor = DefaultBackColor;
			installPathTextBox.ForeColor = validPath ? Color.Black : Color.Red;
			return validPath;
		}


		private void OnSwapHotkeyChanged(object sender, EventArgs e)
		{
			try
			{
				resizer.updateSwapWindowHotkeys(swapWindowHotkeyTextBox.Text);
				config.swapWindowHotkey = swapWindowHotkeyTextBox.Text;
				swapWindowHotkeyTextBox.ForeColor = Color.Black;
			}
			catch (Exception) 
			{ 
				swapWindowHotkeyTextBox.ForeColor = Color.Red;
			}
		}

		private void OnCycleFocusHotkeyChanged(object sender, EventArgs e)
		{
			try
			{
				resizer.updateCycleFocusHotkeys(cycleFocusHotkeyTextBox.Text);
				config.cycleFocusHotkeys = cycleFocusHotkeyTextBox.Text;
				cycleFocusHotkeyTextBox.ForeColor = Color.Black;
			}
			catch (Exception) 
			{ 
				cycleFocusHotkeyTextBox.ForeColor = Color.Red;
			}
		}

		bool wasScrollLock = true;
		private void setStatus(String status) 
		{
			this.statusStripLabel.Text = status;
		}

		private void OnWindowSwitcherTimer(object sender, EventArgs e)
		{
			var p = Cursor.Position;
			bool isScrollLock = IsKeyLocked(Keys.Scroll);
			resizer.checkKeyboardState(p.X, p.Y, isScrollLock);

			if (isScrollLock && !wasScrollLock)
				setStatus("Hotkeys enabled!");
			else if (!isScrollLock && wasScrollLock)
				setStatus("Hotkeys DISABLED toggle scroll lock!");
			wasScrollLock = isScrollLock;
		}

		protected void AddTableLabelControl(String text, int column, int row, Control control, Control control2 = null, int columnSpan = 4)
		{
			Label label = new Label();
			label.Text = text;
			label.AutoSize = true;
			label.Anchor = AnchorStyles.Right; // | AnchorStyles.Bottom;
			label.Padding = new Padding(10);
			layoutPanel.Controls.Add(label);
			layoutPanel.SetCellPosition(label, new TableLayoutPanelCellPosition(column, row));

			layoutPanel.Controls.Add(control);
			layoutPanel.SetCellPosition(control, new TableLayoutPanelCellPosition(column + 1, row));
			layoutPanel.SetColumnSpan(control, columnSpan);
			if (control2 != null)
			{
				//control2.Padding = new Padding(10);
				layoutPanel.Controls.Add(control2);
				layoutPanel.SetCellPosition(control2, new TableLayoutPanelCellPosition(column + 2, row));
				layoutPanel.SetColumnSpan(control2, 4 - columnSpan);
			}
		}

		private void InitNumeric(NumericUpDown pipSizeNumeric, int value)
		{
			pipSizeNumeric.AutoSize = true;
			pipSizeNumeric.Maximum = 10000;
			pipSizeNumeric.Value = value;
			pipSizeNumeric.Anchor = AnchorStyles.Left | AnchorStyles.Right;
			pipSizeNumeric.ValueChanged += new EventHandler(OnPIPSizeChanged);
			//pipSizeNumeric.Padding = new Padding(10);
			//layoutPanel.Controls.Add(pipSizeNumeric);
			//layoutPanel.SetCellPosition(pipSizeNumeric, new TableLayoutPanelCellPosition(column, row));
		}

		private void OnBoxCountChanged(object sender, EventArgs e)
		{
			config.boxCount = (int)boxNumeric.Value;
		}

		private void OnPIPSizeChanged(object sender, EventArgs e)
		{
			config.PIPPosition.Left = (int)pipLeftNumeric.Value;
			config.PIPPosition.Top = (int)pipTopNumeric.Value;
			config.PIPPosition.Width = (int)pipWidthNumeric.Value;
			config.PIPPosition.Height = (int)pipHeightNumeric.Value;
			//updateWoWClientsIfRunning();
		}

		private void updatePIPSizeVisible()
		{
			pipLeftNumeric.Enabled = config.layout == MultiBoxLayouts.PIPVertical;
			pipTopNumeric.Enabled = config.layout == MultiBoxLayouts.PIPVertical;
			pipWidthNumeric.Enabled = config.layout == MultiBoxLayouts.PIPVertical;
			pipHeightNumeric.Enabled = config.layout == MultiBoxLayouts.PIPVertical;
			if (config.layout == MultiBoxLayouts.CustomConfig)
				boxNumeric.Value = config.customLayout.Length;
			boxNumeric.Enabled = config.layout != MultiBoxLayouts.CustomConfig;
		}

		private void updateWoWClientsIfRunning()
		{
			if (resizer.isLaunched())
			{
				resizer.launchWoWClients();
				this.BringToFront();
			}
		}

		private void OnLayoutChanged(object sender, EventArgs e)
		{
			String layoutName = layoutComboBox.SelectedItem.ToString();
			config.layout = (MultiBoxLayouts)Enum.Parse(typeof(MultiBoxLayouts), layoutName);
			updatePIPSizeVisible();
			//updateWoWClientsIfRunning();
		}

		private void OnTaskbarAutohideChanged(object sender, EventArgs e)
		{
			config.taskbarAutohide = taskbarAutohideCheckBox.Checked;
			Win32Util.setTaskbarAutohide(config.taskbarAutohide);
		}

		private void OnCloseWoWWithAppChanged(object sender, EventArgs e)
		{
			config.closeWoWWithApp = closeWoWWithAppCheckBox.Checked;
		}

		private void OnCloseWoWNowClicked(object sender, EventArgs e)
		{
			resizer.closeWoWResizerNow();
		}

		private void OnMouseFocusTrackingChanged(object sender, EventArgs e)
		{
			config.mouseFocusTracking = mouseFocusTrackingCheckBox.Checked;
			// we use "software" mouse focus tracking now
			//Win32Util.setMouseFocusTracking(config.mouseFocusTracking);
		}

		private void OnAlwaysOnTopChanged(object sender, EventArgs e)
		{
			config.alwaysOnTop = alwaysOnTopCheckBox.Checked;
			//updateWoWClientsIfRunning();
		}

		private void OnBorderlessChanged(object sender, EventArgs e)
		{
			config.borderless = borderlessCheckBox.Checked;
			//updateWoWClientsIfRunning();
		}

		protected void OnLaunchButtonClicked(object sender, EventArgs e)
		{
			if (!checkInstallPathSet())
			{
				MessageBox.Show("Wow.exe not found! Please select an retail directory containing wow.exe or the classic directory containing wowClassic.exe!", "Wow.exe not found");
			}
			else
			{
				// set autohide if required, but don't disable it if not set (auto ignore)
				if (config.taskbarAutohide)
					Win32Util.setTaskbarAutohide(config.taskbarAutohide);

				// launch clients
				int launched = resizer.launchWoWClients();
				//this.BringToFront();
				statusStripLabel.Text = "Launched or updated all wow instances in " + launched + " milliseconds";
			}
		}

		protected void OnAnyKeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyData == Keys.Escape)
				this.Close();
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			config.save();
			if (config.taskbarAutohide && config.taskbarAutohideFalseOnClose)
				Win32Util.setTaskbarAutohide(false);

			if (config.closeWoWWithApp) 
				resizer.closeWoWResizerNow();
			base.OnClosing(e);
		}

		// protected override void WndProc(ref Message m)
		// {
		// 	base.WndProc(ref m);

		// 	if (m.Msg == 0x0312)
		// 	{
		// 		Keys key = (Keys)(((int)m.LParam >> 16) & 0xFFFF);
		// 		KeyModifier modifier = (KeyModifier)((int)m.LParam & 0xFFFF);
		// 		int id = m.WParam.ToInt32();

		// 		resizer.swapWindow(id == 1);
		// 		//MessageBox.Show("Hotkey has been pressed!" + id + " Key: " + key + " Modifier: " + modifier);
		// 	}
		// }
	}
}
