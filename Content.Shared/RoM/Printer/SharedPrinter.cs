using Robust.Shared.Serialization;

namespace Content.Shared.RoM.Printer;

[Serializable, NetSerializable]
public enum PrinterUI : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class PrintUiState : BoundUserInterfaceState
{
    public bool IsPaperInserted { get; set; }

    public PrintUiState(bool isInserted)
    {
        IsPaperInserted = isInserted;
    }
}

[Serializable, NetSerializable]
public sealed class PrintingMessage : BoundUserInterfaceMessage
{
    public PrintingMessage()
    {
    }
}

[Serializable, NetSerializable]
public sealed class GetDataMessage : BoundUserInterfaceMessage
{
    public string Id;

    public GetDataMessage(string id)
    {
        Id = id;
    }
}
