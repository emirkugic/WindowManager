using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WindowManager
{
    internal class HotkeyManager : IDisposable
    {
        private IntPtr _hookID = IntPtr.Zero;
        private WindowsAPI.LowLevelKeyboardProc _proc;

        // Changed to Ctrl+Y
        public bool IsCtrlKeyDown { get; private set; }
        public bool IsYKeyDown { get; private set; }

        public event EventHandler HotkeyActivated;
        public event EventHandler HotkeyDeactivated;
        public event EventHandler KeyStateChanged;

        public HotkeyManager()
        {
            _proc = HookCallback;
            _hookID = SetHook(_proc);

            // Debug message to confirm hook was set
            if (_hookID == IntPtr.Zero)
            {
                MessageBox.Show("Failed to set keyboard hook. The application may not work correctly.",
                    "Hook Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private IntPtr SetHook(WindowsAPI.LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return WindowsAPI.SetWindowsHookEx(WindowsAPI.WH_KEYBOARD_LL, proc,
                    WindowsAPI.GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);

                bool isKeyDown = wParam == (IntPtr)WindowsAPI.WM_KEYDOWN ||
                                wParam == (IntPtr)WindowsAPI.WM_SYSKEYDOWN;
                bool isKeyUp = wParam == (IntPtr)WindowsAPI.WM_KEYUP ||
                               wParam == (IntPtr)WindowsAPI.WM_SYSKEYUP;

                bool oldCtrlState = IsCtrlKeyDown;
                bool oldYState = IsYKeyDown;

                // Update Ctrl key state
                if (vkCode == WindowsAPI.VK_CONTROL && isKeyDown)
                    IsCtrlKeyDown = true;
                else if (vkCode == WindowsAPI.VK_CONTROL && isKeyUp)
                    IsCtrlKeyDown = false;

                // Update Y key state
                if (vkCode == WindowsAPI.VK_Y && isKeyDown)
                    IsYKeyDown = true;
                else if (vkCode == WindowsAPI.VK_Y && isKeyUp)
                    IsYKeyDown = false;

                // If key state changed, trigger the event
                if (oldCtrlState != IsCtrlKeyDown || oldYState != IsYKeyDown)
                {
                    KeyStateChanged?.Invoke(this, EventArgs.Empty);
                }

                // Check for hotkey activation (Ctrl+Y)
                bool wasActive = oldCtrlState && oldYState;
                bool isActive = IsCtrlKeyDown && IsYKeyDown;

                if (isActive && !wasActive)
                    HotkeyActivated?.Invoke(this, EventArgs.Empty);
                else if (!isActive && wasActive)
                    HotkeyDeactivated?.Invoke(this, EventArgs.Empty);
            }

            return WindowsAPI.CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        public bool CheckHotkeyState()
        {
            return IsCtrlKeyDown && IsYKeyDown;
        }

        public void Dispose()
        {
            if (_hookID != IntPtr.Zero)
            {
                WindowsAPI.UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
            }
        }
    }
}