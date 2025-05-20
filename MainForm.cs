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
        private bool _managementActive = false;

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
            _hotkeyManager.ExitHotkeyPressed += HotkeyManager_ExitHotkeyPressed;

            // Handle escape key for cancellation
            Application.AddMessageFilter(new EscapeKeyMessageFilter(this));

            // Show the form initially for debugging
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;

            // Create a debug timer to continuously check for Ctrl+Y directly
            Timer keyCheckTimer = new Timer();
            keyCheckTimer.Interval = 100; // Check every 100ms
            keyCheckTimer.Tick += KeyCheckTimer_Tick;
            keyCheckTimer.Start();
        }

        // Message filter to handle Escape key
        private class EscapeKeyMessageFilter : IMessageFilter
        {
            private MainForm _mainForm;

            public EscapeKeyMessageFilter(MainForm mainForm)
            {
                _mainForm = mainForm;
            }

            public bool PreFilterMessage(ref Message m)
            {
                // Check for escape key
                if (m.Msg == 0x0100 && (int)m.WParam == 0x1B) // WM_KEYDOWN and VK_ESCAPE
                {
                    if (_mainForm._managementActive)
                    {
                        _mainForm.CancelWindowManagement();
                        return true; // Message handled
                    }
                }
                return false; // Continue processing message
            }
        }

        private void CancelWindowManagement()
        {
            if (_managementActive)
            {
                _statusLabel.Text = "Operation Cancelled - Window Restored";
                _overlayForm.Deactivate();
                _previewForm.Deactivate();
                _windowManager.CancelWindowManagement();
                _managementActive = false;
            }
        }

        // For debugging - directly check key states
        private void KeyCheckTimer_Tick(object sender, EventArgs e)
        {
            bool ctrlPressed = (WindowsAPI.GetAsyncKeyState(WindowsAPI.VK_CONTROL) & 0x8000) != 0 ||
                              (WindowsAPI.GetAsyncKeyState(WindowsAPI.VK_LCONTROL) & 0x8000) != 0 ||
                              (WindowsAPI.GetAsyncKeyState(WindowsAPI.VK_RCONTROL) & 0x8000) != 0;

            bool yPressed = (WindowsAPI.GetAsyncKeyState(WindowsAPI.VK_Y) & 0x8000) != 0;

            if (ctrlPressed && yPressed && !_hotkeyManager.CheckHotkeyState())
            {
                // Keys are pressed according to GetAsyncKeyState but not according to our hook
                _statusLabel.Text = "Direct detection: Ctrl+Y active but hook not detecting!";
            }
        }

        private void InitializeComponents()
        {
            this.Text = "Window Manager (Ctrl+Y)";
            this.Size = new Size(400, 320);
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
                    _managementActive = true;
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
                    _managementActive = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error during test: {ex.Message}", "Test Error",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _managementActive = false;
                }
            };
            this.Controls.Add(_testButton);

            // Add debug buttons for direct activation
            Button activateButton = new Button();
            activateButton.Text = "Force Activate";
            activateButton.Size = new Size(120, 30);
            activateButton.Location = new Point(150, 60);
            activateButton.Click += (s, e) =>
            {
                HotkeyManager_HotkeyActivated(this, EventArgs.Empty);
            };
            this.Controls.Add(activateButton);

            Button deactivateButton = new Button();
            deactivateButton.Text = "Force Deactivate";
            deactivateButton.Size = new Size(120, 30);
            deactivateButton.Location = new Point(280, 60);
            deactivateButton.Click += (s, e) =>
            {
                HotkeyManager_HotkeyDeactivated(this, EventArgs.Empty);
            };
            this.Controls.Add(deactivateButton);

            // Add cancel button to restore original window position
            Button cancelButton = new Button();
            cancelButton.Text = "Cancel (ESC)";
            cancelButton.Size = new Size(120, 30);
            cancelButton.Location = new Point(150, 100);
            cancelButton.Click += (s, e) => CancelWindowManagement();
            this.Controls.Add(cancelButton);

            // Add help text
            Label helpLabel = new Label();
            helpLabel.Text = "Instructions:\n" +
                            "1. Hold Ctrl+Y to activate window management\n" +
                            "2. A miniature preview will be centered on your cursor\n" +
                            "3. Move mouse to any corner to position the active window\n" +
                            "4. Release Ctrl+Y to confirm position\n" +
                            "5. Press ESC to cancel and restore window\n" +
                            "6. Press Ctrl+B to exit application\n\n" +
                            "If hotkeys don't work, use the Force buttons.";
            helpLabel.Font = new Font("Segoe UI", 9);
            helpLabel.AutoSize = true;
            helpLabel.Location = new Point(20, 140);
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

            _statusLabel.Text = $"LCtrl: {(_hotkeyManager.IsLeftCtrlKeyDown ? "✅" : "❌")} " +
                               $"RCtrl: {(_hotkeyManager.IsRightCtrlKeyDown ? "✅" : "❌")} " +
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
                _managementActive = true;
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
                _managementActive = false;
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
                _managementActive = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deactivating window management: {ex.Message}",
                                "Deactivation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _managementActive = false;
            }
        }

        private void HotkeyManager_ExitHotkeyPressed(object sender, EventArgs e)
        {
            try
            {
                if (InvokeRequired)
                {
                    Invoke(new Action<object, EventArgs>(HotkeyManager_ExitHotkeyPressed), sender, e);
                    return;
                }

                // First make sure we're not in the middle of window management
                if (_managementActive)
                {
                    CancelWindowManagement();
                }

                // Then exit the application
                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exiting application: {ex.Message}",
                                "Exit Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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