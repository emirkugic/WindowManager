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

        // Track both left and right Ctrl keys separately
        public bool IsLeftCtrlKeyDown { get; private set; }
        public bool IsRightCtrlKeyDown { get; private set; }
        public bool IsYKeyDown { get; private set; }
        public bool IsBKeyDown { get; private set; }

        // Property that checks if either Ctrl key is down
        public bool IsCtrlKeyDown => IsLeftCtrlKeyDown || IsRightCtrlKeyDown;

        public event EventHandler HotkeyActivated;
        public event EventHandler HotkeyDeactivated;
        public event EventHandler KeyStateChanged;
        public event EventHandler ExitHotkeyPressed; // New event for Ctrl+B

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
                WindowsAPI.KBDLLHOOKSTRUCT keyInfo = (WindowsAPI.KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(WindowsAPI.KBDLLHOOKSTRUCT));

                bool isKeyDown = wParam == (IntPtr)WindowsAPI.WM_KEYDOWN ||
                                wParam == (IntPtr)WindowsAPI.WM_SYSKEYDOWN;
                bool isKeyUp = wParam == (IntPtr)WindowsAPI.WM_KEYUP ||
                               wParam == (IntPtr)WindowsAPI.WM_SYSKEYUP;

                bool oldLeftCtrlState = IsLeftCtrlKeyDown;
                bool oldRightCtrlState = IsRightCtrlKeyDown;
                bool oldYState = IsYKeyDown;
                bool oldBState = IsBKeyDown;

                // Update Left Ctrl key state
                if (keyInfo.vkCode == (uint)WindowsAPI.VK_LCONTROL && isKeyDown)
                    IsLeftCtrlKeyDown = true;
                else if (keyInfo.vkCode == (uint)WindowsAPI.VK_LCONTROL && isKeyUp)
                    IsLeftCtrlKeyDown = false;

                // Update Right Ctrl key state
                if (keyInfo.vkCode == (uint)WindowsAPI.VK_RCONTROL && isKeyDown)
                    IsRightCtrlKeyDown = true;
                else if (keyInfo.vkCode == (uint)WindowsAPI.VK_RCONTROL && isKeyUp)
                    IsRightCtrlKeyDown = false;

                // Also check for generic CONTROL key (some keyboards might report this)
                if (keyInfo.vkCode == (uint)WindowsAPI.VK_CONTROL && isKeyDown)
                {
                    // Check extended flag to determine left/right
                    if ((keyInfo.flags & 0x01) == 0x01)
                        IsRightCtrlKeyDown = true;
                    else
                        IsLeftCtrlKeyDown = true;
                }
                else if (keyInfo.vkCode == (uint)WindowsAPI.VK_CONTROL && isKeyUp)
                {
                    // Check extended flag to determine left/right
                    if ((keyInfo.flags & 0x01) == 0x01)
                        IsRightCtrlKeyDown = false;
                    else
                        IsLeftCtrlKeyDown = false;
                }

                // Update Y key state
                if (keyInfo.vkCode == (uint)WindowsAPI.VK_Y && isKeyDown)
                    IsYKeyDown = true;
                else if (keyInfo.vkCode == (uint)WindowsAPI.VK_Y && isKeyUp)
                    IsYKeyDown = false;

                // Update B key state
                if (keyInfo.vkCode == (uint)WindowsAPI.VK_B && isKeyDown)
                    IsBKeyDown = true;
                else if (keyInfo.vkCode == (uint)WindowsAPI.VK_B && isKeyUp)
                    IsBKeyDown = false;

                // If key state changed, trigger the event
                if (oldLeftCtrlState != IsLeftCtrlKeyDown ||
                    oldRightCtrlState != IsRightCtrlKeyDown ||
                    oldYState != IsYKeyDown ||
                    oldBState != IsBKeyDown)
                {
                    KeyStateChanged?.Invoke(this, EventArgs.Empty);
                }

                // Check for Ctrl+B exit shortcut
                if (IsCtrlKeyDown && IsBKeyDown && (!oldBState || !IsCtrlKeyDown))
                {
                    ExitHotkeyPressed?.Invoke(this, EventArgs.Empty);
                }

                // Check for hotkey activation (Ctrl+Y)
                bool wasActive = (oldLeftCtrlState || oldRightCtrlState) && oldYState;
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