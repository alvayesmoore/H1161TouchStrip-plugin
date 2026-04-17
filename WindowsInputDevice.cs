/*
 * WindowsInputDevice.cs - Windows implementation using SendInput for scroll injection
 */
using System;
using System.Runtime.InteropServices;

namespace H1161TouchStrip
{
    public class WindowsInputDevice : VirtualMouseDevice
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, ref INPUT pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern IntPtr GetMessageExtraInfo();

        private const uint INPUT_MOUSE = 0;
        private const uint MOUSEEVENTF_WHEEL = 0x0800;
        private const uint MOUSEEVENTF_HWHEEL = 0x1000;
        private const uint MOUSEEVENTF_MOVE = 0x0001;

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public int mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public MOUSEINPUT mi;
        }

        public WindowsInputDevice(string deviceName)
        {
            // Windows doesn't require device setup like evdev
            // The device name is stored for potential debugging purposes
        }

        public override int Initialize()
        {
            CanWrite = true;
            return (int)ERRNO.NONE;
        }

        protected override int SendScrollImpl(int value, bool horizontal)
        {
            if (!CanWrite) return int.MinValue;

            uint flags = horizontal ? MOUSEEVENTF_HWHEEL : MOUSEEVENTF_WHEEL;

            INPUT input = new INPUT();
            input.type = INPUT_MOUSE;
            input.mi.dx = 0;
            input.mi.dy = 0;
            input.mi.mouseData = value;
            input.mi.dwFlags = flags;
            input.mi.time = 0;
            input.mi.dwExtraInfo = GetMessageExtraInfo();

            uint result = SendInput(1, ref input, Marshal.SizeOf(typeof(INPUT)));
            return result == 1 ? 0 : -1;
        }

        public override void Dispose()
        {
            if (!_disposed)
            {
                CanWrite = false;
                _disposed = true;
            }
        }

        private bool _disposed = false;
    }
}