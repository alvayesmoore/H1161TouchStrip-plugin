using OpenTabletDriver.Plugin.Tablet;

namespace H1161TouchStrip;

/// <summary>
/// A synthetic IAuxReport that the OTD pipeline — and the Scroll Bindings
/// plugin — treat exactly like a real hardware aux-button event.
/// </summary>
public class SyntheticAuxReport : IAuxReport
{
    public SyntheticAuxReport(bool[] buttons, byte[] raw)
    {
        AuxButtons = buttons;
        Raw        = raw;
    }

    public bool[] AuxButtons { get; set; }
    public byte[] Raw        { get; set; }
}
