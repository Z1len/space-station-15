using Robust.Shared.Serialization;

namespace Content.Shared.RoM.Printer;

[Serializable, NetSerializable]
public enum PrinterVisuals : byte
{
    VisualState,
}

[Serializable, NetSerializable]
public enum PrinterVisualState : byte
{
    Normal,
    Inserting,
    Printing
}
