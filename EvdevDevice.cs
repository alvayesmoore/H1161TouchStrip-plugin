/*
 * EvdevDevice.cs - Linux evdev implementation using libevdev
 * Based on Scroll Bindings plugin by Mrcubix
 */
using System;
using System.Runtime.InteropServices;
namespace H1161TouchStrip
{
    public class EvdevDevice : VirtualMouseDevice
    {
        private const string libevdev = "libevdev.so.2";
        private const int LIBEVDEV_UINPUT_OPEN_MANAGED = -2;
        [DllImport(libevdev)]
        private static extern IntPtr libevdev_new();
        [DllImport(libevdev)]
        private static extern void libevdev_set_name(IntPtr dev, string name);
        [DllImport(libevdev)]
        private static extern int libevdev_enable_event_type(IntPtr dev, uint type);
        [DllImport(libevdev)]
        private static extern int libevdev_enable_event_code(IntPtr dev, uint type, uint code, IntPtr data);
        [DllImport(libevdev)]
        private static extern int libevdev_uinput_create_from_device(IntPtr dev, int uinput_fd, out IntPtr uinput_dev);
        [DllImport(libevdev)]
        private static extern void libevdev_uinput_destroy(IntPtr uinput_dev);
        [DllImport(libevdev)]
        private static extern int libevdev_uinput_write_event(IntPtr uinput_dev, uint type, uint code, int value);
        private IntPtr _device = IntPtr.Zero;
        private IntPtr _uinput = IntPtr.Zero;
        public EvdevDevice(string deviceName)
        {
            _device = libevdev_new();
            if (_device == IntPtr.Zero) throw new Exception("Failed to create libevdev device");
            libevdev_set_name(_device, deviceName);
        }
        public override int Initialize()
        {
            var err = libevdev_uinput_create_from_device(_device, LIBEVDEV_UINPUT_OPEN_MANAGED, out _uinput);
            CanWrite = err == 0;
            return (ERRNO)(-err) == ERRNO.NONE ? 0 : (int)(ERRNO)(-err);
        }
        public override void EnableType(EventType type)
        {
            libevdev_enable_event_type(_device, (uint)type);
        }
        public override void EnableCode(EventType type, EventCode code)
        {
            libevdev_enable_event_code(_device, (uint)type, (uint)code, IntPtr.Zero);
        }
        protected override int SendScrollImpl(int value, bool horizontal)
        {
            if (!CanWrite || _uinput == IntPtr.Zero) return int.MinValue;
            // This method should not be called directly on Linux
            // The base class Write() routes to this, but we override Write on Linux
            return 0;
        }
        public override int Write(EventType type, EventCode code, int value)
        {
            if (!CanWrite || _uinput == IntPtr.Zero) return int.MinValue;
            return libevdev_uinput_write_event(_uinput, (uint)type, (uint)code, value);
        }
        public override bool Sync()
        {
            return Write(EventType.EV_SYN, EventCode.SYN_REPORT, 0) == 0;
        }
        public override void Dispose()
        {
            if (!_disposed)
            {
                CanWrite = false;
                if (_uinput != IntPtr.Zero)
                {
                    libevdev_uinput_destroy(_uinput);
                    _uinput = IntPtr.Zero;
                    _device = IntPtr.Zero;
                }
                _disposed = true;
            }
        }
        private bool _disposed = false;
    }
}