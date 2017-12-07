using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Windows.Forms;

namespace EyeOnYou
{
    public class EyeForm : OverlayForm
    {
        readonly Timer _recordingTimer = new Timer { Enabled = true, Interval = 35 };
        readonly bool SitTopMost;

        public EyeForm(bool topMost)
        {
            SitTopMost = topMost;
            _recordingTimer.Tick += _recordingTimer_Tick;
            FormBorderStyle = FormBorderStyle.None;
            MinimumSize = new Size(20,20);
        }

        private void _recordingTimer_Tick(object sender, EventArgs e)
        {
            UpdateOverlay();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            UpdateOverlay();
        }
        protected override void OnResize(EventArgs e)
        {
            UpdateOverlay();
        }

        protected override void OnActivated(EventArgs e)
        {
            UpdateOverlay();
            base.OnActivated(e);
        }

        void UpdateOverlay()
        {
            if (SitTopMost){
                TopMost = true;
                TopLevel = true;
                BringToFront();
            }

            using (var bitmap = DrawInner())
            {
                SetBitmap(bitmap);
            }
        }

        Bitmap DrawInner()
        {
            var b = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(b))
            {
                g.Clear(Color.FromArgb(0, 0, 0, 0));
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAlias;

                g.FillEllipse(Brushes.White,  4, 4, Width - 10, Height - 10);
                using(var p = new Pen(Color.Black, 4)){
                    g.DrawEllipse(p, 4, 4, Width - 10, Height - 10);
                }

                var c = Cursor.Current;
                if (c != null) {
                    var cx = Width / 2.0;
                    var cy = Height / 2.0;

                    var min = Math.Min(cx,cy) / 2;

                    var dx = Cursor.Position.X - (Left + cx);
                    var dy = Cursor.Position.Y - (Top + cy);
                    var scale = Math.Sqrt((dx*dx)+(dy*dy));
                    if (Math.Abs(scale) > min) {
                        dx /= scale;
                        dy /= scale;
                        dx *= cx / 2; dy *= cy / 2;
                    }
                    var pupSz = (Width + Height) / 12;
                    var dp = pupSz / 2;
                    g.FillEllipse(Brushes.Black, (int)(dx+cx-dp), (int)(dy+cy-dp), pupSz, pupSz);
                }
            }
            return b;
        }

        
        static Point GetPoint(IntPtr packed)
        {
            int x = (short)(packed.ToInt32() & 0x0000FFFF);
            int y = (short)((packed.ToInt32() & 0xFFFF0000) >> 16);
            return new Point(x, y);
        }

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case Win32.WM_NCHITTEST: // screen-space coords
                {
                    var c = PointToClient(GetPoint(m.LParam));

                    if (c.Y > Height / 2) m.Result = Win32.HTBOTTOMRIGHT; // scaling
                    else m.Result = Win32.HTCAPTION;
                    return;
                }
                default:
                    base.WndProc(ref m);
                    return;
            }
        }
    }
}
