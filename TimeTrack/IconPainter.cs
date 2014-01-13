using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace TimeTrack
{
    class IconPainter
    {
        private static Font m_Font = new Font("Arial", 8);
        private static RectangleF m_Rect = new RectangleF(0, 0, 15, 15);
        private static Pen m_Pen1 = new Pen(Brushes.White, 1);
        private static Pen m_Pen2 = new Pen(Brushes.White, 1.5f);
        private static Brush m_BlueBrush = new LinearGradientBrush(m_Rect, Color.Blue, Color.BlueViolet, 45);
        private static Brush m_RedBrush = new LinearGradientBrush(m_Rect, Color.Red, Color.OrangeRed, 45);

        public static IntPtr GetRecordingIcon(double workedHours)
        {
            return DrawIcon((g, part) =>
                {
                    g.FillRectangle(m_BlueBrush, new RectangleF(1,1,14,14));
                    // top - right part
                    g.DrawLine(m_Pen1, 8, 1, (int)(8 + 6 * 8 * Math.Min(part, 0.125)), 1);
                    // right
                    if (part > 0.125)
                        g.DrawLine(m_Pen1, 15, 1, 15, (int)(1 + 14 * 4 * Math.Min(part - 0.125, 0.25)));
                    // bottom
                    if (part > 0.375)
                        g.DrawLine(m_Pen1, 15, 15, (int)(15 - 14 * 4 * Math.Min(part - 0.375, 0.25)), 15);
                    // left
                    if (part > 0.625)
                        g.DrawLine(m_Pen1, 1, 15, 1, (int)(15 - 14 * 4 * Math.Min(part - 0.625, 0.25)));
                    // top - left part
                    if (part > 0.875)
                        g.DrawLine(m_Pen1, 1, 1, (int)(1 + 14 * 4 * Math.Min(part - 0.875, 0.125)), 1);
                }, workedHours);
        }

        public static IntPtr GetStoppedIcon(double workedHours)
        {
            return DrawIcon((g, part) =>
                {
                    var oldSmooth = g.SmoothingMode;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    g.FillEllipse(m_RedBrush, m_Rect);
                    g.DrawArc(m_Pen2, m_Rect, -90, (float)(360 * part));
                    g.SmoothingMode = oldSmooth;
                }, workedHours);
        }

        private static IntPtr DrawIcon(Action<Graphics, double> moreDrawing, double value)
        {
            int wholeHours = (int)Math.Floor(value);
            var bmp = new Bitmap(16, 16);
            var g = Graphics.FromImage(bmp);
            moreDrawing(g, value - wholeHours);

            g.DrawString(wholeHours.ToString(), m_Font, Brushes.White, new RectangleF(wholeHours < 10? 3: 0, 1, 16, 16));

            int endCoord = (int)(16 * (value - wholeHours));
            g.Flush();
            bmp.MakeTransparent();

            return bmp.GetHicon();
        }
    }
}
