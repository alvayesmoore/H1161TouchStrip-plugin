#!/usr/bin/env fish
# build-and-install.fish for Cachy OS
# Builds and installs the H1161 Touch Strip plugin for OpenTabletDriver

set PLUGIN_DIR ~/.config/OpenTabletDriver/Plugins/H1161TouchStrip
set DLL_PATH bin/Release/net8.0/H1161TouchStrip.dll

# ── Color output helpers ────────────────────────────────────────────────────
function print_step -a msg
    echo (set_color cyan)"→ $msg"(set_color normal)
end

function print_success -a msg
    echo (set_color green)"✓ $msg"(set_color normal)
end

function print_error -a msg
    echo (set_color red)"✗ $msg"(set_color normal)
end

# ── 1. Check for .NET SDK ────────────────────────────────────────────────────
print_step "Checking for .NET SDK..."

if not command -q dotnet
    print_error "dotnet not found"
    echo ""
    echo "Please install .NET SDK 8.0 first:"
    echo "  yay -S dotnet-sdk-8.0"
    echo ""
    echo "Or using pacman:"
    echo "  sudo pacman -S dotnet-sdk-8.0"
    exit 1
end

set dotnet_version (dotnet --version 2>/dev/null)
if test $status -ne 0
    print_error "No .NET SDK found"
    echo "Install with: yay -S dotnet-sdk-8.0"
    exit 1
end

print_success "Found .NET SDK version $dotnet_version"

# ── 2. Clean previous builds ────────────────────────────────────────────────
if test -d bin
    print_step "Cleaning previous build..."
    rm -rf bin obj
    print_success "Clean complete"
end

# ── 3. Build ─────────────────────────────────────────────────────────────────
print_step "Building plugin..."
dotnet build -c Release

if test $status -ne 0
    print_error "Build failed"
    exit 1
end

print_success "Build successful"

# ── 4. Install ───────────────────────────────────────────────────────────────
print_step "Installing plugin to $PLUGIN_DIR ..."

mkdir -p $PLUGIN_DIR
cp $DLL_PATH $PLUGIN_DIR/H1161TouchStrip.dll

if test $status -eq 0
    print_success "Plugin installed"
else
    print_error "Failed to copy plugin"
    exit 1
end

# ── 5. Restart OTD ───────────────────────────────────────────────────────────
print_step "Restarting OpenTabletDriver..."

# Try systemd service first
if systemctl --user is-active opentabletdriver.service >/dev/null 2>&1
    systemctl --user restart opentabletdriver.service
    print_success "OpenTabletDriver service restarted"
else
    # Kill any running OTD GUI
    pkill -f OpenTabletDriver.UX.Gtk 2>/dev/null
    pkill -f OpenTabletDriver 2>/dev/null
    print_success "OpenTabletDriver processes stopped"
    echo "  Please start OpenTabletDriver GUI manually"
end

# ── 6. Instructions ──────────────────────────────────────────────────────────
echo ""
echo (set_color yellow)"═══════════════════════════════════════════════════════════════"(set_color normal)
echo (set_color yellow)"  Configuration Required in OpenTabletDriver GUI"(set_color normal)
echo (set_color yellow)"═══════════════════════════════════════════════════════════════"(set_color normal)
echo ""
echo "  The plugin is installed but needs to be added to the filter pipeline:"
echo ""
echo "  1. Open OpenTabletDriver GUI"
echo ""
echo "  2. Go to: Tablets → [Your H1161] → Configuration"
echo ""
echo "  3. In the Filters section, add:"
echo "     'H1161 Touch Strip → Aux Buttons'"
echo ""
echo "  4. Configure the filter settings:"
echo "     • Wheel Report ID: 08 (hex)"
echo "     • Wheel Data Byte Index: 5"
echo "     • Scroll Up Aux Button Index: 12"
echo "     • Scroll Down Aux Button Index: 13"
echo ""
echo "  5. Go to Plugin Manager"
echo "     → Install 'Scroll Bindings' by Mrcubix"
echo ""
echo "  6. Go to Bindings tab"
echo "     → Bind Aux 12 to: Scroll Up"
echo "     → Bind Aux 13 to: Scroll Down"
echo ""
echo "  7. Click 'Save & Apply'"
echo ""
echo (set_color yellow)"═══════════════════════════════════════════════════════════════"(set_color normal)
echo ""
echo "Touch strip raw data (for reference):"
echo "  Position 0 (no touch):  08 F0 01 01 00 00 00 00 00 00 13 F5"
echo "  Position 1 (top):       08 F0 01 01 00 01 00 00 00 00 13 F5"
echo "  Position 7 (bottom):    08 F0 01 01 00 07 00 00 00 00 13 F5"
echo ""
echo (set_color cyan)"If the strip doesn't work after configuration:"(set_color normal)
echo "  • Enable 'Debug Logging' in the filter settings"
echo "  • Slide the strip and check OTD console: journalctl --user -u opentabletdriver -f"
echo "  • Verify Report ID (first byte) and position byte index"
echo ""
