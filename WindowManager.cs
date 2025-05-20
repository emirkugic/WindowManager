using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;

namespace WindowManager
{
    internal class WindowManager
    {
        private IntPtr _activeWindow;
        private WindowsAPI.RECT _originalWindowRect;
        private WindowsAPI.WINDOWPLACEMENT _originalWindowPlacement;
        private bool _wasMaximized;
        private Dictionary<string, Rectangle> _presets;
        private List<IntPtr> _minimizedWindows;

        public IntPtr GetActiveWindow() => _activeWindow;

        public WindowManager()
        {
            _presets = new Dictionary<string, Rectangle>();
            _minimizedWindows = new List<IntPtr>();
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
            // Clear any previous list
            _minimizedWindows.Clear();

            // Store the active window
            _activeWindow = WindowsAPI.GetForegroundWindow();

            // Check if window is maximized
            _wasMaximized = WindowsAPI.IsZoomed(_activeWindow);

            // Save original window placement (includes state and position)
            _originalWindowPlacement = new WindowsAPI.WINDOWPLACEMENT();
            _originalWindowPlacement.length = Marshal.SizeOf(_originalWindowPlacement);
            WindowsAPI.GetWindowPlacement(_activeWindow, ref _originalWindowPlacement);

            // Store the original window rect
            WindowsAPI.GetWindowRect(_activeWindow, out _originalWindowRect);

            // Minimize other visible application windows
            MinimizeOtherWindows();

            // If maximized, restore it to normal state first
            if (_wasMaximized)
            {
                WindowsAPI.ShowWindow(_activeWindow, WindowsAPI.SW_RESTORE);
            }

            // Show preview of the active window
            ShowWindowPreview();
        }

        public void EndWindowManagement()
        {
            // Restore minimized windows
            RestoreMinimizedWindows();
        }

        public void CancelWindowManagement()
        {
            // Restore the original window state and position
            RestoreOriginalWindowState();

            // Restore minimized windows
            RestoreMinimizedWindows();
        }

        private void RestoreOriginalWindowState()
        {
            // Reset the window placement to the original state
            WindowsAPI.SetWindowPlacement(_activeWindow, ref _originalWindowPlacement);

            // If it was maximized, make sure it's maximized again
            if (_wasMaximized)
            {
                WindowsAPI.ShowWindow(_activeWindow, WindowsAPI.SW_MAXIMIZE);
            }
        }

        private void MinimizeOtherWindows()
        {
            WindowsAPI.EnumWindows(EnumWindowsCallback, IntPtr.Zero);
        }

        private bool EnumWindowsCallback(IntPtr hWnd, IntPtr lParam)
        {
            // Skip the current active window
            if (hWnd == _activeWindow)
                return true;

            // Skip invisible windows
            if (!WindowsAPI.IsWindowVisible(hWnd))
                return true;

            // Get window title
            StringBuilder sb = new StringBuilder(256);
            WindowsAPI.GetWindowText(hWnd, sb, 256);

            // Skip windows with empty titles (often system windows)
            if (sb.Length == 0)
                return true;

            // Skip windows that are already minimized
            WindowsAPI.WINDOWPLACEMENT placement = new WindowsAPI.WINDOWPLACEMENT();
            placement.length = Marshal.SizeOf(placement);
            WindowsAPI.GetWindowPlacement(hWnd, ref placement);
            if (placement.showCmd == WindowsAPI.SW_SHOWMINIMIZED)
                return true;

            // Check if window has a visible frame (i.e., it's a regular application window)
            long style = WindowsAPI.GetWindowLong(hWnd, WindowsAPI.GWL_STYLE);
            bool hasFrame = (style & WindowsAPI.WS_CAPTION) != 0;

            if (hasFrame && sb.Length > 0)
            {
                // Add to our list of windows we're minimizing
                _minimizedWindows.Add(hWnd);

                // Minimize the window
                WindowsAPI.ShowWindow(hWnd, WindowsAPI.SW_MINIMIZE);
            }

            return true;
        }

        private void RestoreMinimizedWindows()
        {
            // Only restore windows that we minimized
            foreach (IntPtr hWnd in _minimizedWindows)
            {
                WindowsAPI.ShowWindow(hWnd, WindowsAPI.SW_RESTORE);
            }

            // Clear the list
            _minimizedWindows.Clear();
        }

        private void ShowWindowPreview()
        {
            // Get current dimensions (might be different than original if we've restored from maximized)
            WindowsAPI.RECT currentRect;
            WindowsAPI.GetWindowRect(_activeWindow, out currentRect);

            // Calculate a slightly smaller size for preview
            int previewWidth = (int)(currentRect.Width * 0.9);
            int previewHeight = (int)(currentRect.Height * 0.9);

            // Center the preview
            int previewX = currentRect.Left + (currentRect.Width - previewWidth) / 2;
            int previewY = currentRect.Top + (currentRect.Height - previewHeight) / 2;

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
            RestoreOriginalWindowState();
        }
    }
}