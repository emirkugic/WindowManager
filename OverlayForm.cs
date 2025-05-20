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
            this.Opacity = 0.3; // Lower opacity for better visibility
            this.BackColor = Color.Black;

            // Set form to cover the entire screen
            this.Bounds = Screen.PrimaryScreen.Bounds;

            // Set up mouse move handler
            this.MouseMove += OverlayForm_MouseMove;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            // Make the form click-through after it's shown
            int exStyle = WindowsAPI.GetWindowLong(this.Handle, WindowsAPI.GWL_EXSTYLE);
            exStyle |= WindowsAPI.WS_EX_TRANSPARENT | WindowsAPI.WS_EX_LAYERED;
            WindowsAPI.SetWindowLong(this.Handle, WindowsAPI.GWL_EXSTYLE, exStyle);
        }

        public new void Activate()
        {
            // Show and make active
            _isActive = true;
            this.Show();
            this.BringToFront();
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