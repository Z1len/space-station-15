using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Audio;

namespace Content.Server.RoM.Printer;

[RegisterComponent]
public sealed partial class PrinterComponent : Component
{
    [DataField("paperSlot", required: true)]
    public ItemSlot PaperSlot = new();

    [DataField("paperInsertionTimeRemaining")]
    public float InsertionTimeRemaining;

    [DataField("printingTimeRemaining")]
    public float PrintingTimeRemaining;

    [DataField("printSound")]
    public SoundSpecifier PrintSound = new SoundPathSpecifier("/Audio/Machines/printer.ogg");

    [ViewVariables]
    [DataField("DocumentText")]
    public string? DocumentText;

    [ViewVariables]
    public float InsertionTime = 1.88f;

    [ViewVariables]
    public float PrintingTime = 2.88f;
}
