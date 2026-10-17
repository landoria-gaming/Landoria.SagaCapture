using System;
using System.Runtime.InteropServices;

namespace Landoria.SagaCapture
{
    // Reads the actual pixel dimensions of the cursor displayed by Windows.
    internal static class SagaCaptureNativeCursorSize
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct CursorInfo
        {
            internal int Size;
            internal int Flags;
            internal IntPtr Cursor;
            internal int ScreenX;
            internal int ScreenY;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IconInfo
        {
            [MarshalAs(UnmanagedType.Bool)] internal bool IsIcon;
            internal uint HotspotX;
            internal uint HotspotY;
            internal IntPtr Mask;
            internal IntPtr Color;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BitmapInfo
        {
            internal int Type;
            internal int Width;
            internal int Height;
            internal int WidthBytes;
            internal short Planes;
            internal short BitsPerPixel;
            internal IntPtr Bits;
        }

        [DllImport("user32.dll")]
        private static extern bool GetCursorInfo(ref CursorInfo info);

        [DllImport("user32.dll")]
        private static extern bool GetIconInfo(IntPtr cursor, out IconInfo info);

        [DllImport("gdi32.dll", EntryPoint = "GetObjectW")]
        private static extern int GetBitmapObject(
            IntPtr bitmap, int size, out BitmapInfo info);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr handle);

        private static IntPtr _lastCursor;
        private static int _width;
        private static int _height;

        // Caches size until the game or Windows switches to another cursor.
        internal static bool TryGet(out int width, out int height)
        {
            width = 0;
            height = 0;
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                return false;
            }
            var cursor = new CursorInfo { Size = Marshal.SizeOf<CursorInfo>() };
            if (!GetCursorInfo(ref cursor) || cursor.Cursor == IntPtr.Zero)
            {
                return false;
            }
            if (cursor.Cursor != _lastCursor)
            {
                ReadSize(cursor.Cursor);
            }
            width = _width;
            height = _height;
            return width > 0 && height > 0;
        }

        // Gets the icon bitmap size and frees the copies returned by Win32.
        private static void ReadSize(IntPtr cursor)
        {
            _lastCursor = cursor;
            _width = 0;
            _height = 0;
            if (!GetIconInfo(cursor, out IconInfo icon))
            {
                return;
            }
            try
            {
                IntPtr bitmap = icon.Color != IntPtr.Zero ? icon.Color : icon.Mask;
                if (GetBitmapObject(bitmap, Marshal.SizeOf<BitmapInfo>(),
                    out BitmapInfo info) == 0)
                {
                    return;
                }
                _width = info.Width;
                _height = icon.Color != IntPtr.Zero ? info.Height : info.Height / 2;
            }
            finally
            {
                if (icon.Color != IntPtr.Zero) DeleteObject(icon.Color);
                if (icon.Mask != IntPtr.Zero) DeleteObject(icon.Mask);
            }
        }
    }
}
