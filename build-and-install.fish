#!/usr/bin/env fish
# Build and install the H1161 Touch Strip plugin for OpenTabletDriver

set PLUGIN_DIR ~/.config/OpenTabletDriver/Plugins/H1161TouchStrip
set DLL_PATH bin/Release/net8.0/H1161TouchStrip.dll

function print_step
    echo (set_color cyan)"→ $argv[1]"(set_color normal)
end

function print_success
    echo (set_color green)"✓ $argv[1]"(set_color normal)
end

function print_error
    echo (set_color red)"✗ $argv[1]"(set_color normal)
end

# ── 1. Check for .NET SDK ────────────────────────────────────────────────────
print_step "Checking for .NET SDK..."

if not command -q dotnet
    print_error "dotnet not found"
    echo ""
    echo "Install .NET SDK 8.0:"
    echo "  Arch/CachyOS:  sudo pacman -S dotnet-sdk-8.0"
    echo "  Ubuntu/Debian: sudo apt install dotnet-sdk-8.0"
    echo "  Fedora:        sudo dnf install dotnet-sdk-8.0"
    exit 1
end

set dotnet_version (dotnet --version 2>/dev/null)
print_success "Found .NET SDK $dotnet_version"

# ── 2. Check for libevdev ─────────────────────────────────────────────────────
print_step "Checking for libevdev..."

if test -f /usr/lib/libevdev.so.2; or test -f /usr/lib/x86_64-linux-gnu/libevdev.so.2
    print_success "libevdev found"
else if test (ldconfig -p 2>/dev/null | grep -c libevdev) -gt 0
    print_success "libevdev found"
else
    print_error "libevdev not found"
    echo ""
    echo "Install libevdev:"
    echo "  Arch/CachyOS:  sudo pacman -S libevdev"
    echo "  Ubuntu/Debian: sudo apt install libevdev-dev"
    echo "  Fedora:        sudo dnf install libevdev-devel"
    exit 1
end

# ── 3. Check uinput ──────────────────────────────────────────────────────────
print_step "Checking uinput kernel module..."

if test -e /dev/uinput
    print_success "/dev/uinput available"
else
    print_error "/dev/uinput not found"
    echo "  Try: sudo modprobe uinput"
    exit 1
end

# ── 4. Clean previous builds ─────────────────────────────────────────────────
if test -d bin
    print_step "Cleaning previous build..."
    rm -rf bin obj
    print_success "Clean complete"
end

# ── 5. Build ──────────────────────────────────────────────────────────────────
print_step "Building plugin..."
dotnet build -c Release

if test $status -ne 0
    print_error "Build failed"
    exit 1
end

print_success "Build successful"

# ── 6. Install ────────────────────────────────────────────────────────────────
print_step "Installing plugin to $PLUGIN_DIR ..."

mkdir -p $PLUGIN_DIR
cp $DLL_PATH $PLUGIN_DIR/H1161TouchStrip.dll

if test $status -eq 0
    print_success "Plugin installed"
else
    print_error "Failed to copy plugin"
    exit 1
end

# ── 7. Restart OTD ───────────────────────────────────────────────────────────
print_step "Restarting OpenTabletDriver..."

if systemctl --user is-active opentabletdriver.service >/dev/null 2>&1
    systemctl --user restart opentabletdriver.service
    print_success "OpenTabletDriver service restarted"
else
    pkill -f OpenTabletDriver.UX.Gtk 2>/dev/null
    pkill -f OpenTabletDriver 2>/dev/null
    print_success "OpenTabletDriver processes stopped"
    echo "  Please start OpenTabletDriver GUI manually"
end

# ── 8. Instructions ──────────────────────────────────────────────────────────
echo ""
echo (set_color yellow)"═══════════════════════════════════════════════════════════════"(set_color normal)
echo (set_color yellow)"  Next Steps"(set_color normal)
echo (set_color yellow)"═══════════════════════════════════════════════════════════════"(set_color normal)
echo ""
echo "  1. Open OpenTabletDriver GUI"
echo ""
echo "  2. Go to: Tablets → [Your H1161] → Configuration"
echo ""
echo "  3. In the Filters section, add:"
echo "     'H1161 Touch Strip → Scroll'"
echo ""
echo "  4. Click 'Save & Apply'"
echo ""
echo "  The plugin creates a virtual scroll device — no other"
echo "  plugins or binding configuration needed."
echo ""
echo (set_color yellow)"═══════════════════════════════════════════════════════════════"(set_color normal)
echo ""
echo (set_color cyan)"Troubleshooting:"(set_color normal)
echo "  • Enable 'Debug Logging' in filter settings"
echo "  • Check logs: journalctl --user -u opentabletdriver -f | grep H1161"
echo "  • Verify device: cat /proc/bus/input/devices | grep -A5 'H1161 Touch Strip'"
echo ""