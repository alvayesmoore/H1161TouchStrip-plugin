/*
 * H1161TouchStripFilter.cs
 */

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Timers;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;

namespace H1161TouchStrip;

[PluginName("H1161 Touch Strip → Scroll")]
public class H1161TouchStripFilter : IPositionedPipelineElement<IDeviceReport>, IDisposable
{
[Property("Scroll Direction"),
PropertyValidated(nameof(ValidDirections)),
ToolTip("Vertical Scroll = up/down, Horizontal Scroll = left/right")]
public string Direction { get; set; } = "Vertical Scroll";

public static IEnumerable<string> ValidDirections => new[] { "Vertical Scroll", "Horizontal Scroll" };

[BooleanProperty("Reverse Scroll Direction", ""),
DefaultPropertyValue(false)]
public bool ReverseDirection { get; set; } = false;

[Property("Scroll Delay"),
DefaultPropertyValue(15),
Unit("ms"),
ToolTip("Delay between scroll events. Lower = faster. Range: 1-1000")]
public int ScrollDelay
{
get => _scrollDelay;
set
{
_scrollDelay = Math.Clamp(value, 1, 1000);
if (_scrollTimer != null)
_scrollTimer.Interval = _scrollDelay;
}
}

[Property("Scroll Amount"),
DefaultPropertyValue(120),
ToolTip("Scroll amount per tick. Range: 0-2400")]
public int ScrollAmount
{
get => _scrollAmount;
set => _scrollAmount = Math.Clamp(value, 0, 2400);
}

[BooleanProperty("Enable Inertia", ""),
DefaultPropertyValue(false),
ToolTip("Continue scrolling after finger lift.")]
public bool EnableInertia { get; set; } = false;

[Property("Inertia Friction"),
DefaultPropertyValue(0.85),
ToolTip("Deceleration factor. Lower = stops faster. Range: 0.50-0.95")]
public double InertiaFriction
{
get => _inertiaFriction;
set => _inertiaFriction = Math.Clamp(value, 0.50, 0.95);
}

[Property("Touch Strip Report ID (hex)"),
DefaultPropertyValue(0x08)]
public int TouchStripReportId { get; set; } = 0x08;

[Property("Touch Strip Identifier Byte"),
DefaultPropertyValue(0xF0)]
public int TouchStripIdentifier { get; set; } = 0xF0;

[Property("Position Byte Index"),
DefaultPropertyValue(5)]
public int PositionByteIndex { get; set; } = 5;

[Property("Min Movement Threshold"),
DefaultPropertyValue(1)]
public int Threshold { get; set; } = 1;

[BooleanProperty("Debug Logging", ""),
DefaultPropertyValue(false)]
public bool DebugLogging { get; set; } = false;

public PipelinePosition Position => PipelinePosition.PreTransform;

private int _lastPosition = 0;
private bool _fingerDown = false;
private int _scrollDelay = 15;
private int _scrollAmount = 120;
private double _inertiaFriction = 0.85;
private bool _disposed = false;
private VirtualMouseDevice? _wheelDevice;
private System.Timers.Timer? _scrollTimer;
private int _pendingScrollAmount = 0;
private bool _isScrolling = false;
private int _lastScrollDirection = 0;
private const int MAX_INERTIA_TICKS = 20;

public H1161TouchStripFilter()
{
try
{
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
_wheelDevice = new WindowsInputDevice("H1161 Touch Strip");
else
_wheelDevice = new EvdevDevice("H1161 Touch Strip");
_wheelDevice.EnableTypeCodes(EventType.EV_REL);
_wheelDevice.EnableTypeCodes(EventType.EV_REL,
EventCode.REL_WHEEL,
EventCode.REL_WHEEL_HI_RES,
EventCode.REL_HWHEEL,
EventCode.REL_HWHEEL_HI_RES
);

var result = _wheelDevice.Initialize();
if (result != (int)ERRNO.NONE)
{
Console.WriteLine($"[H1161TouchStrip] Failed to initialize virtual mouse device: {result}");
_wheelDevice?.Dispose();
_wheelDevice = null;
}

_scrollTimer = new System.Timers.Timer(_scrollDelay);
_scrollTimer.Elapsed += OnScrollTimer;
_scrollTimer.AutoReset = true;
}
catch (Exception ex)
{
Console.WriteLine($"[H1161TouchStrip] Init error: {ex.Message}");
_wheelDevice?.Dispose();
_wheelDevice = null;
}
}

public event Action<IDeviceReport>? Emit;

public void Consume(IDeviceReport report)
{
var raw = report.Raw;

if (raw == null || raw.Length <= PositionByteIndex)
{
Emit?.Invoke(report);
return;
}

if (raw[0] != (byte)TouchStripReportId || raw[1] != (byte)TouchStripIdentifier)
{
Emit?.Invoke(report);
return;
}

int position = raw[PositionByteIndex];

if (position == 0)
{
if (_fingerDown && EnableInertia && _lastScrollDirection != 0)
{
StopScrolling();
SendInertiaScroll();
}
else
{
StopScrolling();
}
_fingerDown = false;
_lastPosition = 0;
_lastScrollDirection = 0;
return;
}

if (!_fingerDown)
{
_fingerDown = true;
_lastPosition = position;
return;
}

int delta = position - _lastPosition;
if (Math.Abs(delta) < Threshold)
return;

_lastPosition = position;

int scrollDelta = delta > 0 ? _scrollAmount : -_scrollAmount;
if (ReverseDirection)
scrollDelta = -scrollDelta;

_lastScrollDirection = scrollDelta > 0 ? 1 : -1;
_pendingScrollAmount = scrollDelta;

if (!_isScrolling)
{
StartScrolling();
}

if (DebugLogging)
Console.WriteLine($"[H1161TouchStrip] δ={delta:+0;-0} → scroll {scrollDelta}");
}

private void StartScrolling()
{
_isScrolling = true;
_scrollTimer?.Start();
}

private void StopScrolling()
{
_isScrolling = false;
_scrollTimer?.Stop();
_pendingScrollAmount = 0;
}

private void OnScrollTimer(object? sender, ElapsedEventArgs e)
{
if (_wheelDevice == null || !_isScrolling || _pendingScrollAmount == 0)
return;

SendScroll(_pendingScrollAmount);
}

private void SendInertiaScroll()
{
double velocity = _lastScrollDirection * _scrollAmount;
int ticks = 0;

while (Math.Abs(velocity) > 5 && ticks < MAX_INERTIA_TICKS)
{
SendScroll((int)Math.Round(velocity));
velocity *= _inertiaFriction;
ticks++;
System.Threading.Thread.Sleep(16);
}

if (DebugLogging)
Console.WriteLine($"[H1161TouchStrip] Inertia: {ticks} ticks");
}

private void SendScroll(int amount)
{
if (_wheelDevice == null)
return;

try
{
if (Direction == "Vertical Scroll")
_wheelDevice.Write(EventType.EV_REL, EventCode.REL_WHEEL_HI_RES, amount);
else
_wheelDevice.Write(EventType.EV_REL, EventCode.REL_HWHEEL_HI_RES, amount);

_wheelDevice.Sync();
}
catch (Exception ex)
{
if (DebugLogging)
Console.WriteLine($"[H1161TouchStrip] Error: {ex.Message}");
}
}

public void Dispose()
{
if (!_disposed)
{
_scrollTimer?.Stop();
_scrollTimer?.Dispose();
_wheelDevice?.Dispose();
_wheelDevice = null;
_disposed = true;
}
}
}
