using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace WindowManager
{
    internal class WindowManager
    {
        private IntPtr _activeWindow;
        private WindowsAPI.RECT _originalWindowRect;
        private Dictionary<string, Rectangle> _presets;

        public WindowManager()
        {
            _presets = new Dictionary<string, Rectangle>();
            InitializeDefaultPresets();
        }

        private void InitializeDefaultPresets()
        {
            // Get screen dimensions
            Rectangle screen = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;

            // Top-left corner
            _presets.Add("TopLeft", new Rectangle(0, 0, screen.Width / 2, screen.Height / 2));

            // Top-right corner
            _presets.Add("TopRight", new Rectangle(screen.Width / 2, 0, screen.Width / 2, screen.Height / 2));

            // Bottom-left corner
            _presets.Add("BottomLeft", new Rectangle(0, screen.Height / 2, screen.Width / 2, screen.Height / 2));

            // Bottom-right corner
            _presets.Add("BottomRight", new Rectangle(screen.Width / 2, screen.Height / 2, screen.Width / 2, screen.Height / 2));
        }

        public void StartWindowManagement()
        {
            // Store the active window
            _activeWindow = WindowsAPI.GetForegroundWindow();

            // Store the original window rect
            WindowsAPI.GetWindowRect(_activeWindow, out _originalWindowRect);

            // Hide other windows
            HideOtherWindows();

            // Show preview of the active window
            ShowWindowPreview();
        }

        public void EndWindowManagement()
        {
            // Show all windows again
            ShowAllWindows();
        }

        private void HideOtherWindows()
        {
            WindowsAPI.EnumWindows(EnumWindowsCallback, IntPtr.Zero);
        }

        private bool EnumWindowsCallback(IntPtr hWnd, IntPtr lParam)
        {
            // Skip the current active window
            if (hWnd == _activeWindow)
                return true;

            // Check if the window is visible
            if (WindowsAPI.IsWindowVisible(hWnd))
            {
                StringBuilder sb = new StringBuilder(256);
                WindowsAPI.GetWindowText(hWnd, sb, 256);

                // Skip windows with empty titles (often system windows)
                if (sb.Length > 0)
                {
                    // Hide the window
                    WindowsAPI.ShowWindow(hWnd, 0); // SW_HIDE = 0
                }
            }

            return true;
        }

        private void ShowAllWindows()
        {
            WindowsAPI.EnumWindows((hWnd, lParam) =>
            {
                if (hWnd != _activeWindow && WindowsAPI.IsWindowVisible(hWnd) == false)
                {
                    StringBuilder sb = new StringBuilder(256);
                    WindowsAPI.GetWindowText(hWnd, sb, 256);

                    // Skip windows with empty titles
                    if (sb.Length > 0)
                    {
                        // Show the window
                        WindowsAPI.ShowWindow(hWnd, 1); // SW_SHOWNORMAL = 1
                    }
                }
                return true;
            }, IntPtr.Zero);
        }

        private void ShowWindowPreview()
        {
            // Calculate a slightly smaller size for preview
            int previewWidth = (int)(_originalWindowRect.Width * 0.9);
            int previewHeight = (int)(_originalWindowRect.Height * 0.9);

            // Center the preview
            int previewX = _originalWindowRect.Left + (_originalWindowRect.Width - previewWidth) / 2;
            int previewY = _originalWindowRect.Top + (_originalWindowRect.Height - previewHeight) / 2;

            // Set the new window position and size
            WindowsAPI.SetWindowPos(
                _activeWindow,
                (IntPtr)WindowsAPI.HWND_TOP,
                previewX,
                previewY,
                previewWidth,
                previewHeight,
                WindowsAPI.SWP_SHOWWINDOW
            );
        }

        public void MoveWindowToCorner(string corner)
        {
            if (_presets.TryGetValue(corner, out Rectangle preset))
            {
                WindowsAPI.SetWindowPos(
                    _activeWindow,
                    (IntPtr)WindowsAPI.HWND_TOP,
                    preset.X,
                    preset.Y,
                    preset.Width,
                    preset.Height,
                    WindowsAPI.SWP_SHOWWINDOW
                );
            }
        }

        public void MoveWindowTo(int x, int y, int width, int height)
        {
            WindowsAPI.SetWindowPos(
                _activeWindow,
                (IntPtr)WindowsAPI.HWND_TOP,
                x,
                y,
                width,
                height,
                WindowsAPI.SWP_SHOWWINDOW
            );
        }

        public string GetCornerFromPosition(Point position)
        {
            // Get screen dimensions
            Rectangle screen = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;

            // Check which quadrant the position is in
            if (position.X < screen.Width / 2)
            {
                if (position.Y < screen.Height / 2)
                    return "TopLeft";
                else
                    return "BottomLeft";
            }
            else
            {
                if (position.Y < screen.Height / 2)
                    return "TopRight";
                else
                    return "BottomRight";
            }
        }

        public void RestoreWindowPosition()
        {
            WindowsAPI.SetWindowPos(
                _activeWindow,
                (IntPtr)WindowsAPI.HWND_TOP,
                _originalWindowRect.Left,
                _originalWindowRect.Top,
                _originalWindowRect.Width,
                _originalWindowRect.Height,
                WindowsAPI.SWP_SHOWWINDOW
            );
        }
    }
}