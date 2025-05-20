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
        private PreviewForm _previewForm;
        private NotifyIcon _notifyIcon;
        private Label _statusLabel;
        private Button _testButton;

        public MainForm()
        {
            InitializeComponents();

            // Initialize managers and forms
            _windowManager = new WindowManager();
            _overlayForm = new OverlayForm(_windowManager);
            _previewForm = new PreviewForm(_windowManager);

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
            this.Text = "Window Manager (Ctrl+Y)";
            this.Size = new Size(400, 300);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // Add status label
            _statusLabel = new Label();
            _statusLabel.Text = "Ctrl: ❌ Y: ❌";
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
                try
                {
                    _windowManager.StartWindowManagement();

                    // Create preview of active window
                    _previewForm.SetWindowPreview(_windowManager.GetActiveWindow());
                    _previewForm.Activate();

                    // Show overlay
                    _overlayForm.Activate();

                    MessageBox.Show("Move your mouse to different corners to position the window.\n\nPress OK to end test and restore windows",
                        "Test Active", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    _overlayForm.Deactivate();
                    _previewForm.Deactivate();
                    _windowManager.EndWindowManagement();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error during test: {ex.Message}", "Test Error",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            this.Controls.Add(_testButton);

            // Add help text
            Label helpLabel = new Label();
            helpLabel.Text = "Instructions:\n" +
                            "1. Hold Ctrl+Y to activate window management\n" +
                            "2. A miniature preview will follow your cursor\n" +
                            "3. Move mouse to any corner to position the active window\n" +
                            "4. Release Ctrl+Y to confirm position\n\n" +
                            "This will minimize other windows while active.";
            helpLabel.Font = new Font("Segoe UI", 9);
            helpLabel.AutoSize = true;
            helpLabel.Location = new Point(20, 110);
            this.Controls.Add(helpLabel);

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
                               $"Y: {(_hotkeyManager.IsYKeyDown ? "✅" : "❌")}";
        }

        private void HotkeyManager_HotkeyActivated(object sender, EventArgs e)
        {
            try
            {
                if (InvokeRequired)
                {
                    Invoke(new Action<object, EventArgs>(HotkeyManager_HotkeyActivated), sender, e);
                    return;
                }

                _statusLabel.Text = "Hotkey Activated!";
                _windowManager.StartWindowManagement();

                // Create preview of active window
                _previewForm.SetWindowPreview(_windowManager.GetActiveWindow());
                _previewForm.Activate();

                // Show overlay
                _overlayForm.Activate();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error activating window management: {ex.Message}",
                                "Activation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void HotkeyManager_HotkeyDeactivated(object sender, EventArgs e)
        {
            try
            {
                if (InvokeRequired)
                {
                    Invoke(new Action<object, EventArgs>(HotkeyManager_HotkeyDeactivated), sender, e);
                    return;
                }

                _statusLabel.Text = "Hotkey Deactivated!";
                _overlayForm.Deactivate();
                _previewForm.Deactivate();
                _windowManager.EndWindowManagement();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deactivating window management: {ex.Message}",
                                "Deactivation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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

            if (_previewForm != null)
                _previewForm.Dispose();

            if (_overlayForm != null)
                _overlayForm.Dispose();
        }
    }
}