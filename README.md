# H1161 Touch Strip Plugin for OpenTabletDriver

Converts Huion Inspiroy H1161 touch strip input into synthetic aux button events that can be bound via the Scroll Bindings plugin.

## Raw Data Format

The H1161 touch strip sends 12-byte HID reports:

```
Position 0 (no touch):  08 F0 01 01 00 00 00 00 00 00 13 F5
Position 1 (top):       08 F0 01 01 00 01 00 00 00 00 13 F5
Position 2:             08 F0 01 01 00 02 00 00 00 00 13 F5
Position 3:             08 F0 01 01 00 03 00 00 00 00 13 F5
Position 4:             08 F0 01 01 00 04 00 00 00 00 13 F5
Position 5:             08 F0 01 01 00 05 00 00 00 00 13 F5
Position 6:             08 F0 01 01 00 06 00 00 00 00 13 F5
Position 7 (bottom):    08 F0 01 01 00 07 00 00 00 00 13 F5
```

Key bytes:
- **Byte 0**: Report ID = `08` (identifies touch strip reports)
- **Byte 5**: Position = `00` (no touch) or `01-07` (touch position, top to bottom)

## Installation

### Prerequisites

1. Install .NET SDK 8.0:
   ```fish
   yay -S dotnet-sdk-8.0
   ```

2. Verify installation:
   ```fish
   dotnet --version
   ```

### Build and Install

```fish
cd /home/chtrey/projects/H1161TouchStrip-plugin
chmod +x build-and-install.fish
./build-and-install.fish
```

The script will:
1. Check for .NET SDK
2. Build the plugin
3. Install to `~/.config/OpenTabletDriver/Plugins/H1161TouchStrip/`
4. Restart OpenTabletDriver

## Configuration in OpenTabletDriver

### Step 1: Restart OpenTabletDriver

After installing, restart OTD:
```fish
systemctl --user restart opentabletdriver.service
# OR kill and restart the GUI
pkill -f OpenTabletDriver.UX.Gtk
```

### Step 2: Add Plugin to Filter Pipeline

**Important**: This plugin is a **pipeline element** that needs to be added to the tablet's filter pipeline manually.

1. Open OpenTabletDriver GUI
2. Go to **Tablets** → Select your H1161
3. Click **Configuration** or **Edit Configuration**
4. In the **Filters** section, add:
   - `H1161 Touch Strip → Aux Buttons`
5. Save the configuration

### Step 3: Configure Filter Settings

In the filter settings:
- **Wheel Report ID**: `08` (hex) - default, correct for H1161
- **Wheel Data Byte Index**: `5` - default, correct for H1161
- **Scroll Up Aux Button Index**: `12` - use 12+ to avoid conflicts
- **Scroll Down Aux Button Index**: `13` - use 12+ to avoid conflicts
- **Min Movement Threshold**: `1` - increase if too sensitive
- **Debug Logging**: `false` - enable for troubleshooting

### Step 4: Install Scroll Bindings Plugin

1. Go to **Plugin Manager**
2. Find and install **"Scroll Bindings"** by Mrcubix
3. Restart OTD

### Step 5: Configure Scroll Bindings

1. Go to **Bindings** tab
2. Find **Aux Button Bindings** or **Scroll Bindings** section
3. Bind:
   - **Aux 12** → Scroll Up (or Scroll Bindings → Vertical Scroll Up)
   - **Aux 13** → Scroll Down (or Scroll Bindings → Vertical Scroll Down)
4. Adjust scroll speed if needed

### Step 6: Save and Apply

Click **Save & Apply** in the main window.

## Troubleshooting

### Touch strip not working?

#### 1. Verify plugin is loaded

Check OTD logs:
```fish
journalctl --user -u opentabletdriver -f
```

Look for plugin loading messages or errors.

#### 2. Enable debug logging

1. In filter settings, enable **"Debug Logging"**
2. Slide the touch strip
3. Check console output for:
   ```
   [H1161TouchStrip] raw[12]: 08 F0 01 01 00 03 00 00 00 00 13 F5
   [H1161TouchStrip] δ=+1 → DOWN pos=3
   ```

#### 3. Verify raw data

If you see different raw data:
- Update **Wheel Report ID** to match the first byte
- Update **Wheel Data Byte Index** to the position of the changing byte

#### 4. Check filter pipeline

Make sure the filter is actually in the tablet's filter pipeline:
- Tablets → Configuration → Filters
- The plugin should be listed there

#### 5. Check aux button bindings

In Bindings tab, verify:
- Aux 12 and Aux 13 are bound to scroll actions
- The bindings are enabled

### Direction reversed?

If up/down are swapped:
1. Swap the Scroll Up/Down indices in filter settings, OR
2. Swap the bindings in Scroll Bindings settings

### Too sensitive or not sensitive enough?

- **Too many scroll events**: Increase **Min Movement Threshold** to 2
- **Not enough scroll events**: Keep threshold at 1

## How It Works

1. The plugin sits in the tablet report pipeline
2. It inspects every raw HID report from the tablet
3. When it sees report ID `08`, it extracts the touch position from byte 5
4. It tracks position changes between reports:
   - Increasing position (1→2→3...) = moving DOWN = Scroll Down
   - Decreasing position (7→6→5...) = moving UP = Scroll Up
5. On movement, it emits synthetic `IAuxReport` events:
   - One with the target aux button pressed
   - One with all buttons released
6. Scroll Bindings plugin catches these aux events and converts to scroll actions

## Technical Details

### Touch Strip Positions

The H1161 touch strip has 7 discrete positions (1-7):
```
Position 1: Top of strip
Position 2-6: Middle positions
Position 7: Bottom of strip
```

### Button Index Mappings

- Physical buttons: 0-11 (12 buttons on H1161)
- Touch strip scroll up: 12 (configurable)
- Touch strip scroll down: 13 (configurable)

### Report Format

```
Byte 0:    08 (Report ID)
Byte 1-4:  F0 01 01 00 (fixed)
Byte 5:    00-07 (touch position)
Byte 6-11: 00 00 00 00 13 F5 (fixed/footer)
```

## Files

- `H1161TouchStripFilter.cs` - Main filter implementation
- `SyntheticAuxReport.cs` - Synthetic aux button report class
- `H1161TouchStrip.csproj` - Build configuration
- `build-and-install.fish` - Build and install script
- `README.md` - This file

## License

Feel free to modify and redistribute.

## Credits

Based on OpenTabletDriver plugin API. Compatible with Scroll Bindings plugin by Mrcubix.
