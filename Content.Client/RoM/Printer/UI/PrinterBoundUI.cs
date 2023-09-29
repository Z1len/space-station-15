using Content.Shared.RoM.Printer;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.RoM.Printer.UI;

[UsedImplicitly]
public sealed class PrinterBoundUI : BoundUserInterface
{
    private PrinterWindow? _printerWindow;

    public PrinterBoundUI(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _printerWindow = new PrinterWindow();
        _printerWindow.OnPrintButtonPress += Print;
        _printerWindow.OnClose += Close;
        _printerWindow.OnDocumentItemPress += SubmitData;
        _printerWindow.OpenCentered();
    }

    public void SubmitData(string id)
    {
        SendMessage(
            new GetDataMessage(id));
    }

    private void Print()
    {
        SendMessage(new PrintingMessage());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _printerWindow?.Dispose();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (_printerWindow == null || state is not PrintUiState cast)
            return;
        _printerWindow.UpdateState(cast);
    }
}
