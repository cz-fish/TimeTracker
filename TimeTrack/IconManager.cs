using System;
using System.Runtime.InteropServices;
using System.Drawing;

namespace TimeTrack
{
    class IconManager: IDisposable
    {
        [DllImport("user32.dll", EntryPoint = "DestroyIcon")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        private IntPtr hIcon = default(IntPtr);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (hIcon != default(IntPtr))
            {
                if (disposing)
                {
                    DestroyIcon(hIcon);
                }
                hIcon = default(IntPtr);
            }
        }

        ~IconManager()
        {
            Dispose(false);
        }

        public Icon GetRecordingIcon(double workedHours)
        {
            return UpdateIcon(IconPainter.GetRecordingIcon(workedHours));
        }

        public Icon GetStoppedIcon(double workedHours)
        {
            return UpdateIcon(IconPainter.GetStoppedIcon(workedHours));
        }

        private Icon UpdateIcon(IntPtr newIcon)
        {
            if (hIcon != default(IntPtr))
            {
                DestroyIcon(hIcon);
            }
            hIcon = newIcon;
            return System.Drawing.Icon.FromHandle(hIcon);
        }
    }
}
