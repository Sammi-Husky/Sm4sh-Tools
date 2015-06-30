using System;
using System.Runtime.InteropServices;

namespace Be.Windows.Forms {
    internal sealed class NativeMethods {
        // Caret definitions
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool CreateCaret(IntPtr hWnd, IntPtr hBitmap, int nWidth, int nHeight);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool ShowCaret(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool DestroyCaret();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetCaretPos(int x, int y);

        // Key definitions
        public const int WMKeydown = 0x100;
        public const int WMKeyup = 0x101;
        public const int WMChar = 0x102;
    }
}