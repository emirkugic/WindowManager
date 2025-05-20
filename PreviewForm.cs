using System;
using System.Drawing;
using System.Windows.Forms;

namespace WindowManager
{
    internal class PreviewForm : Form
    {
        private WindowManager _windowManager;
        private Bitmap _previewImage;
        private string _currentCorner = "";
        private float _previewScale = 0.25f; // 25% of original size

        public PreviewForm(WindowManager windowManager)
        {
            _windowManager = windowManager;

            // Set up form properties
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.BackColor = Color.White;

            // Make the form follow the mouse cursor
            Timer cursorTimer = new Timer();
            cursorTimer.Interval = 10; // Update every 10ms
            cursorTimer.Tick += CursorTimer_Tick;
            cursorTimer.Start();
        }

        private void CursorTimer_Tick(object sender, EventArgs e)
        {
            if (this.Visible)
            {
                WindowsAPI.GetCursorPos(out WindowsAPI.POINT cursorPos);

                // Get current corner
                Point cursorPoint = new Point(cursorPos.X, cursorPos.Y);
                string corner = _windowManager.GetCornerFromPosition(cursorPoint);

                // If corner changed, update preview position
                if (corner != _currentCorner)
                {
                    _currentCorner = corner;
                    UpdatePreviewBasedOnCorner(corner);
                }

                // Position the preview so the cursor is centered in it
                // Instead of following offset, center the preview on the cursor
                this.Left = cursorPos.X - (this.Width / 2);
                this.Top = cursorPos.Y - (this.Height / 2);
            }
        }

        public void SetWindowPreview(IntPtr hWnd)
        {
            try
            {
                // Get window bounds
                WindowsAPI.GetWindowRect(hWnd, out WindowsAPI.RECT rect);

                // Capture window contents
                int width = rect.Width;
                int height = rect.Height;

                if (width <= 0 || height <= 0)
                {
                    MessageBox.Show("Invalid window dimensions", "Preview Error",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Create a bitmap to hold the screenshot
                using (Bitmap windowBitmap = new Bitmap(width, height))
                {
                    using (Graphics g = Graphics.FromImage(windowBitmap))
                    {
                        // Get the device context of the window
                        IntPtr hdcWindow = User32.GetDC(hWnd);

                        // Get the device context of our bitmap
                        IntPtr hdcBitmap = g.GetHdc();

                        // Copy from the window to the bitmap
                        WindowsAPI.BitBlt(hdcBitmap, 0, 0, width, height, hdcWindow, 0, 0, WindowsAPI.SRCCOPY);

                        // Release the device contexts
                        g.ReleaseHdc(hdcBitmap);
                        User32.ReleaseDC(hWnd, hdcWindow);
                    }

                    // Create a scaled version for the preview
                    int previewWidth = (int)(width * _previewScale);
                    int previewHeight = (int)(height * _previewScale);

                    if (_previewImage != null)
                    {
                        _previewImage.Dispose();
                    }

                    _previewImage = new Bitmap(windowBitmap, previewWidth, previewHeight);

                    // Set the form size to match the preview
                    this.Size = new Size(previewWidth, previewHeight);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating preview: {ex.Message}", "Preview Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdatePreviewBasedOnCorner(string corner)
        {
            if (_windowManager != null)
            {
                _windowManager.MoveWindowToCorner(corner);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (_previewImage != null)
            {
                e.Graphics.DrawImage(_previewImage, 0, 0);

                // Draw a border
                using (Pen pen = new Pen(Color.DodgerBlue, 2))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
                }

                // Draw a crosshair in the center to indicate cursor position
                int centerX = this.Width / 2;
                int centerY = this.Height / 2;

                using (Pen pen = new Pen(Color.Red, 1))
                {
                    // Horizontal line
                    e.Graphics.DrawLine(pen, centerX - 5, centerY, centerX + 5, centerY);
                    // Vertical line
                    e.Graphics.DrawLine(pen, centerX, centerY - 5, centerX, centerY + 5);
                }
            }
        }

        public new void Activate()
        {
            this.Show();
            this.BringToFront();
        }

        public new void Deactivate()
        {
            this.Hide();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _previewImage?.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    // Additional User32 methods for window capture
    internal static class User32
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hWnd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
    }
}