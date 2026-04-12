/*
 * EvdevDevice.cs - Linux evdev implementation using libevdev
 * Based on Scroll Bindings plugin by Mrcubix
 */

using System;
using System.Runtime.InteropServices;

namespace H1161TouchStrip
{
public enum EventType : uint
{
EV_SYN = 0x00,
EV_REL = 0x02,
}

public enum EventCode : uint
{
SYN_REPORT = 0,
REL_WHEEL = 0x08,
REL_WHEEL_HI_RES = 0x0B,
REL_HWHEEL = 0x06,
REL_HWHEEL_HI_RES = 0x0C,
}

public enum ERRNO : int
{
NONE = 0,
}

public class EvdevDevice : IDisposable
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
private bool _disposed = false;

public bool CanWrite { get; private set; } = false;

public EvdevDevice(string deviceName)
{
_device = libevdev_new();
if (_device == IntPtr.Zero)
throw new Exception("Failed to create libevdev device");

libevdev_set_name(_device, deviceName);
}

public ERRNO Initialize()
{
var err = libevdev_uinput_create_from_device(_device, LIBEVDEV_UINPUT_OPEN_MANAGED, out _uinput);
CanWrite = err == 0;
return (ERRNO)(-err);
}

public void EnableType(EventType type)
{
libevdev_enable_event_type(_device, (uint)type);
}

public void EnableCode(EventType type, EventCode code)
{
libevdev_enable_event_code(_device, (uint)type, (uint)code, IntPtr.Zero);
}

public void EnableTypeCodes(EventType type, params EventCode[] codes)
{
EnableType(type);
foreach (var code in codes)
EnableCode(type, code);
}

public int Write(EventType type, EventCode code, int value)
{
if (!CanWrite || _uinput == IntPtr.Zero)
return int.MinValue;

return libevdev_uinput_write_event(_uinput, (uint)type, (uint)code, value);
}

public bool Sync()
{
return Write(EventType.EV_SYN, EventCode.SYN_REPORT, 0) == 0;
}

public void Dispose()
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
}
}
