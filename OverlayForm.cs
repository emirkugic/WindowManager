using System;
using System.Drawing;
using System.Windows.Forms;

namespace WindowManager
{
    internal class OverlayForm : Form
    {
        private WindowManager _windowManager;
        private bool _isActive = false;

        public OverlayForm(WindowManager windowManager)
        {
            _windowManager = windowManager;

            // Set up form properties
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.Opacity = 0.5;
            this.BackColor = Color.Black;

            // Set form to cover the entire screen
            this.Bounds = Screen.PrimaryScreen.Bounds;

            // Set up mouse move handler
            this.MouseMove += OverlayForm_MouseMove;

            // Manually set the form to click-through after it's shown
            this.Shown += (s, e) =>
            {
                int exStyle = WindowsAPI.GetWindowLong(this.Handle, WindowsAPI.GWL_EXSTYLE);
                exStyle |= WindowsAPI.WS_EX_TRANSPARENT | WindowsAPI.WS_EX_LAYERED;
                WindowsAPI.SetWindowLong(this.Handle, WindowsAPI.GWL_EXSTYLE, exStyle);

                // Set the form opacity using the Windows API
                WindowsAPI.SetLayeredWindowAttributes(this.Handle, 0, 128, WindowsAPI.LWA_ALPHA);
            };
        }

        public new void Activate()
        {
            // Show and make active
            _isActive = true;
            this.Show();
            this.BringToFront();
            this.Update();

            // Force redraw
            this.Invalidate();
        }

        public new void Deactivate()
        {
            _isActive = false;
            this.Hide();
        }

        private void OverlayForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isActive)
            {
                Point cursorPos = new Point(e.X, e.Y);
                string corner = _windowManager.GetCornerFromPosition(cursorPos);
                _windowManager.MoveWindowToCorner(corner);

                this.Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (_isActive)
            {
                using (Graphics g = e.Graphics)
                {
                    // Draw screen divisions
                    int centerX = this.Width / 2;
                    int centerY = this.Height / 2;

                    using (Pen pen = new Pen(Color.White, 2))
                    {
                        // Draw vertical divider
                        g.DrawLine(pen, centerX, 0, centerX, this.Height);

                        // Draw horizontal divider
                        g.DrawLine(pen, 0, centerY, this.Width, centerY);
                    }

                    // Draw corner labels
                    using (Font font = new Font("Arial", 16))
                    using (SolidBrush brush = new SolidBrush(Color.White))
                    {
                        g.DrawString("Top Left", font, brush, 20, 20);
                        g.DrawString("Top Right", font, brush, this.Width - 120, 20);
                        g.DrawString("Bottom Left", font, brush, 20, this.Height - 50);
                        g.DrawString("Bottom Right", font, brush, this.Width - 150, this.Height - 50);
                    }
                }
            }
        }
    }
}