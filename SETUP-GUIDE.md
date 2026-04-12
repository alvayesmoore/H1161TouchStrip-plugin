# H1161 Touch Strip Setup Guide

## Current Status ✅

Your plugin is working! The logs confirm:
- Touch strip is detected correctly
- Direction (UP/DOWN) is calculated properly
- Positions 1-7 are tracked
- Stylus is not blocked

## Problem: No Aux 12/13 Buttons Visible

This is **normal** - aux buttons 12/13 are **virtual buttons** created by the plugin.
They don't appear in the tablet debugger or physical button list.

## Solution: Use Scroll Bindings Plugin

### Step 1: Install Scroll Bindings

1. Open OpenTabletDriver GUI
2. Go to **Tools** → **Plugin Manager**
3. Find **"Scroll Bindings"** by Mrcubix
4. Click **Install**
5. Restart OTD

### Step 2: Configure Bindings

In OTD 0.6.x, there are TWO ways to configure bindings:

#### Method A: Through Settings File (Recommended)

1. Close OpenTabletDriver GUI completely
2. Edit `~/.config/OpenTabletDriver/settings.json`
3. Add binding configuration (see below)
4. Save and restart OTD

#### Method B: Through GUI (If Available)

1. Open OTD GUI
2. Look for **"Bindings"** tab or section
3. Click **"+"** to add binding
4. Select binding type: **"Scroll Bindings"**
5. Configure:
   - **Aux Button**: 12 → **Action**: Vertical Scroll Up
   - **Aux Button**: 13 → **Action**: Vertical Scroll Down
6. Save

### Step 3: Test

1. Open any application (browser, text editor)
2. Slide your touch strip
3. You should see scrolling!

## Alternative: Direct Mouse Wheel Binding

If Scroll Bindings doesn't work, you can create a simpler plugin that directly
emits mouse wheel events instead of aux button events. Let me know if needed.

## Settings File Configuration

Add this to your `settings.json` under the tablet configuration:

```json
{
  "Tablet": {
    "Identifier": "your-tablet-id",
    "Bindings": {
      "Aux12": {
        "Plugin": "ScrollBinding",
        "Action": "VerticalScrollUp",
        "Speed": 5
      },
      "Aux13": {
        "Plugin": "ScrollBinding",
        "Action": "VerticalScrollDown",
        "Speed": 5
      }
    }
  }
}
```

## Still Not Working?

1. Check if Scroll Bindings plugin is installed:
   - `ls ~/.config/OpenTabletDriver/Plugins/Scroll\ Bindings/`

2. Enable debug logging and check for aux events:
   - You should see logs when sliding the strip

3. Try using a different output method:
   - Instead of aux buttons, we can make the plugin emit mouse wheel events directly

## Quick Test Command

Run this while sliding your touch strip to see the events:

```fish
journalctl --user -u opentabletdriver -f | grep -i "touch\|aux\|scroll"
```

You should see:
- `[H1161TouchStrip] Touch strip δ=+1 → DOWN pos=5`
- (Eventually) Scroll events from Scroll Bindings plugin
