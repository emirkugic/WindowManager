using System;
using System.Windows.Forms;
using System.Drawing;

namespace WindowManager
{
    internal class MainForm : Form
    {
        private HotkeyManager _hotkeyManager;
        private WindowManager _windowManager;
        private OverlayForm _overlayForm;
        private NotifyIcon _notifyIcon;
        private Label _statusLabel;
        private Button _testButton;

        public MainForm()
        {
            InitializeComponents();

            // Initialize managers
            _windowManager = new WindowManager();
            _overlayForm = new OverlayForm(_windowManager);

            // Set up hotkey manager
            _hotkeyManager = new HotkeyManager();
            _hotkeyManager.HotkeyActivated += HotkeyManager_HotkeyActivated;
            _hotkeyManager.HotkeyDeactivated += HotkeyManager_HotkeyDeactivated;
            _hotkeyManager.KeyStateChanged += HotkeyManager_KeyStateChanged;

            // Show the form initially for debugging
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
        }

        private void InitializeComponents()
        {
            this.Text = "Window Manager (Ctrl+Shift)";
            this.Size = new Size(400, 250);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // Add status label
            _statusLabel = new Label();
            _statusLabel.Text = "Ctrl: ❌ Shift: ❌";
            _statusLabel.AutoSize = true;
            _statusLabel.Font = new Font("Segoe UI", 12);
            _statusLabel.Location = new Point(20, 20);
            this.Controls.Add(_statusLabel);

            // Add a test button
            _testButton = new Button();
            _testButton.Text = "Test Manager";
            _testButton.Size = new Size(120, 30);
            _testButton.Location = new Point(20, 60);
            _testButton.Click += (s, e) =>
            {
                _windowManager.StartWindowManagement();
                _overlayForm.Activate();
                MessageBox.Show("Press OK to end test and restore windows", "Test Active",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                _overlayForm.Deactivate();
                _windowManager.EndWindowManagement();
            };
            this.Controls.Add(_testButton);

            // Create notify icon
            _notifyIcon = new NotifyIcon();
            _notifyIcon.Icon = SystemIcons.Application;
            _notifyIcon.Text = "Window Manager";
            _notifyIcon.Visible = true;

            // Create context menu
            ContextMenuStrip contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Show", null, (s, e) =>
            {
                this.Show();
                this.WindowState = FormWindowState.Normal;
                this.Activate();
            });
            contextMenu.Items.Add("Exit", null, (s, e) => Application.Exit());
            _notifyIcon.ContextMenuStrip = contextMenu;

            // Show form when clicked on notify icon
            _notifyIcon.DoubleClick += (s, e) =>
            {
                this.Show();
                this.WindowState = FormWindowState.Normal;
                this.Activate();
            };
        }

        private void HotkeyManager_KeyStateChanged(object sender, EventArgs e)
        {
            UpdateStatusLabel();
        }

        private void UpdateStatusLabel()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(UpdateStatusLabel));
                return;
            }

            _statusLabel.Text = $"Ctrl: {(_hotkeyManager.IsCtrlKeyDown ? "✅" : "❌")} " +
                               $"Shift: {(_hotkeyManager.IsShiftKeyDown ? "✅" : "❌")}";
        }

        private void HotkeyManager_HotkeyActivated(object sender, EventArgs e)
        {
            _statusLabel.Text = "Hotkey Activated!";
            _windowManager.StartWindowManagement();
            _overlayForm.Activate();
        }

        private void HotkeyManager_HotkeyDeactivated(object sender, EventArgs e)
        {
            _statusLabel.Text = "Hotkey Deactivated!";
            _overlayForm.Deactivate();
            _windowManager.EndWindowManagement();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
                return;
            }

            base.OnFormClosing(e);

            if (_hotkeyManager != null)
                _hotkeyManager.Dispose();

            if (_notifyIcon != null)
                _notifyIcon.Dispose();
        }
    }
}