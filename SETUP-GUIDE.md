# H1161 Touch Strip Setup Guide

## Quick Start

Follow these steps to get the touch strip working as a scroll wheel.

### 1. Install system dependencies

```bash
# Arch / CachyOS
sudo pacman -S libevdev dotnet-sdk-8.0

# Ubuntu / Debian
sudo apt install libevdev-dev dotnet-sdk-8.0

# Fedora
sudo dnf install libevdev-devel dotnet-sdk-8.0
```

### 2. Ensure uinput kernel module is loaded

```bash
sudo modprobe uinput
```

This is usually loaded automatically. If you get "device or resource busy" it's already active.

### 3. Build and install the plugin

```bash
git clone https://github.com/alvayesmoore/H1161TouchStrip-plugin.git
cd H1161TouchStrip-plugin
chmod +x build-and-install.fish
./build-and-install.fish
```

Or manually:

```bash
dotnet build -c Release
mkdir -p ~/.config/OpenTabletDriver/Plugins/H1161TouchStrip
cp bin/Release/net8.0/H1161TouchStrip.dll ~/.config/OpenTabletDriver/Plugins/H1161TouchStrip/
```

### 4. Restart OpenTabletDriver

```bash
systemctl --user restart opentabletdriver.service
```

Or restart the GUI manually if you don't use the systemd service.

### 5. Add the filter in OpenTabletDriver GUI

1. Open OpenTabletDriver GUI
2. Go to **Tablets** → select your H1161 → **Configuration**
3. In the **Filters** section, click **Add Filter**
4. Select **H1161 Touch Strip → Scroll**
5. Configure settings (defaults should work for H1161)
6. Click **Save & Apply**

### 6. Test

Open any scrollable window (browser, text editor, file manager) and slide the touch strip. You should see content scrolling.

## uinput Permissions

The plugin creates a virtual evdev device via `/dev/uinput`. If OpenTabletDriver runs as your user and you see "Failed to initialize evdev" in the logs, you likely need uinput permissions.

### Check current permissions

```bash
ls -la /dev/uinput
```

### Option A: Add yourself to the input group

```bash
sudo usermod -aG input $USER
```

Log out and back in for this to take effect.

### Option B: Udev rule (recommended)

Create `/etc/udev/rules.d/80-uinput.rules`:

```
KERNEL=="uinput", MODE="0660", GROUP="input"
```

Then reload rules:

```bash
sudo udevadm control --reload-rules
sudo udevadm trigger
```

### Option C: If OTD runs as root (systemd service)

If OpenTabletDriver's systemd service runs as root, uinput access should work automatically. Check with:

```bash
systemctl --user cat opentabletdriver.service
```

## Configuration Reference

### Scroll Direction

- **Vertical Scroll** (default) — standard mouse wheel scrolling
- **Horizontal Scroll** — side-scrolling for trackpads / wide documents

### Reverse Scroll Direction

Toggle this if scrolling up moves content down (or vice versa). Equivalent to "natural scrolling."

### Scroll Delay

Time in milliseconds between scroll events. Lower values = faster response. Default: **15 ms**. Range: 1–1000.

### Scroll Amount

How much each scroll tick moves. Default: **120** (one standard mouse wheel notch). Range: 0–2400.

### Enable Inertia

When enabled, scrolling continues briefly after you lift your finger, with gradually decreasing speed. Similar to trackpad momentum scrolling.

### Inertia Friction

How quickly inertia decelerates. Range: **0.50** (stops fast) to **0.95** (coasts longer). Default: **0.85**.

### Advanced Settings

These default values are correct for the H1161. Only change them if you're adapting the plugin for a different tablet:

| Setting | Default | Description |
|---|---|---|
| Touch Strip Report ID (hex) | 0x08 | First byte of touch strip HID reports |
| Touch Strip Identifier Byte | 0xF0 | Second byte identifier |
| Position Byte Index | 5 | Byte index containing position value |
| Min Movement Threshold | 1 | Minimum position delta to trigger scroll |

### Debug Logging

Enable this to see detailed output in OTD logs. Useful for troubleshooting but will spam the log during normal use.

## Verifying It Works

```bash
# Watch OTD logs for plugin messages
journalctl --user -u opentabletdriver -f | grep H1161

# Check that the virtual scroll device exists
cat /proc/bus/input/devices | grep -A5 "H1161 Touch Strip"

# Monitor scroll events directly
sudo evmon /dev/input/eventXX
# OR
sudo cat /dev/input/eventXX | od -A x -t x1z
```

With debug logging enabled, sliding the touch strip should produce:

```
[H1161TouchStrip] δ=+1 → scroll 120
[H1161TouchStrip] δ=-1 → scroll -120
```

## Adapting for Other Tablets

This plugin can work with other Huion tablets that have touch strips or wheels if they use a similar HID report format. You would need to:

1. Capture raw HID reports from your tablet (enable debug logging, or use `evtest` / `usbhid-dump`)
2. Identify which bytes identify the touch strip report (update **Touch Strip Report ID** and **Identifier Byte**)
3. Find which byte contains the position data (update **Position Byte Index**)
4. Set **Min Movement Threshold** as needed

The plugin does not hardcode any tablet-specific logic beyond the configurable byte offsets.

## Troubleshooting

### Plugin doesn't appear in filter list

- Ensure the DLL is in `~/.config/OpenTabletDriver/Plugins/H1161TouchStrip/`
- Restart OpenTabletDriver after installing
- Check that you're running OTD 0.6.x with .NET 8.0 support

### "Failed to initialize evdev" error

- Verify `libevdev` is installed: `ldconfig -p | grep libevdev`
- Check `/dev/uinput` exists: `ls -la /dev/uinput`
- Check permissions (see uinput section above)
- Try loading the kernel module: `sudo modprobe uinput`

### Touch strip is detected but nothing scrolls

- Enable **Debug Logging** and check that position values appear (0x01–0x07)
- If you see "δ=+1 → scroll 120" but no scrolling, verify the evdev device is registered: `cat /proc/bus/input/devices`
- Try adjusting **Scroll Delay** — very low values may overwhelm some applications

### Scrolling works but direction is inverted

Enable **Reverse Scroll Direction** in the filter settings.

### Scrolling is too fast or too slow

- **Too fast**: Increase **Scroll Delay** (e.g., 30–50 ms) or decrease **Scroll Amount** (e.g., 60)
- **Too slow**: Decrease **Scroll Delay** (e.g., 5–10 ms) or increase **Scroll Amount** (e.g., 240)