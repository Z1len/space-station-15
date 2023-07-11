using Content.Server.Paper;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.UserInterface;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.RoM.Printer;
using Content.Shared.RoM.Printer.Prototypes;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.RoM.Printer;

public sealed class PrinterSystem : EntitySystem
{
    [Dependency] private readonly PaperSystem _paperSystem = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly SharedAudioSystem _soundSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    private string _documentText = "";
    private const string PaperSlotId = "Paper";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PrinterComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<PrinterComponent, PrintingMessage>(OnPrintingRequest);
        SubscribeLocalEvent<PrinterComponent, AfterActivatableUIOpenEvent>(OnInteractUi);
        SubscribeLocalEvent<PrinterComponent, EntInsertedIntoContainerMessage>(OnInsertEnt);
        SubscribeLocalEvent<PrinterComponent, EntRemovedFromContainerMessage>(OnRemoveEnt);
        SubscribeLocalEvent<PrinterComponent, GetDataMessage>(OnGetData);
        SubscribeLocalEvent<PrinterComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<PrinterComponent, PowerChangedEvent>(OnPowerChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<PrinterComponent, ApcPowerReceiverComponent>();
        while (query.MoveNext(out var uid, out var printerComp, out var powerComp))
        {
            if (!powerComp.Powered)
                return;

            ProcessInsertingAnimation(uid, frameTime, printerComp);
            ProcessPrintingAnimation(uid, frameTime, printerComp);
        }
    }

    private void OnInteractUi(EntityUid uid, PrinterComponent component, AfterActivatableUIOpenEvent args)
    {
        UpdateUi(uid, component);
    }

    private void OnPowerChanged(EntityUid uid, PrinterComponent component, ref PowerChangedEvent args)
    {
        var isInsertInterrupted = !args.Powered && component.InsertionTimeRemaining > 0;
        if (isInsertInterrupted)
        {
            component.InsertionTimeRemaining = 0f;
            _itemSlotsSystem.SetLock(uid, component.PaperSlot, true);
            _itemSlotsSystem.TryEject(uid, component.PaperSlot, null, out _, true);
        }

        var isPrintInterrupted = !args.Powered && component.PrintingTimeRemaining > 0;
        if (isPrintInterrupted)
        {
            component.PrintingTimeRemaining = 0f;
        }

        if (isPrintInterrupted || isInsertInterrupted)
            UpdateAppearance(uid, component);

        _itemSlotsSystem.SetLock(uid, component.PaperSlot, !args.Powered);
    }

    private void OnInsertEnt(EntityUid uid, PrinterComponent component, EntInsertedIntoContainerMessage args)
    {
        if (!component.Initialized)
            return;

        if (args.Container.ID != component.PaperSlot.ID)

            return;

        component.InsertionTimeRemaining = component.InsertionTime;
        _itemSlotsSystem.SetLock(uid, component.PaperSlot, true);
    }

    private void OnRemoveEnt(EntityUid uid, PrinterComponent component, EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != component.PaperSlot.ID)
            return;

        UpdateUi(uid, component);
    }

    private void OnGetData(EntityUid uid, PrinterComponent component, GetDataMessage args)
    {
        _documentText = Loc.GetString(_prototype.Index<DocumentPrototype>(args.Id).Text);
    }

    private void OnComponentRemove(EntityUid uid, PrinterComponent component, ComponentRemove args)
    {
        _itemSlotsSystem.RemoveItemSlot(uid, component.PaperSlot);
    }

    private void OnComponentInit(EntityUid uid, PrinterComponent component, ComponentInit args)
    {
        _itemSlotsSystem.AddItemSlot(uid, PaperSlotId, component.PaperSlot);
    }

    private void OnPrintingRequest(EntityUid uid, PrinterComponent component, PrintingMessage args)
    {
        if (HasComp<PaperComponent>(component.PaperSlot.Item) || component.PaperSlot.HasItem)
	{
	    _soundSystem.PlayPvs(component.PrintSound, uid);
            _popupSystem.PopupEntity(Loc.GetString("printer-popup-printing"), uid);
            component.PrintingTimeRemaining = component.PrintingTime;
	}
    }

    private void ProcessInsertingAnimation(EntityUid uid, float frameTime, PrinterComponent component)
    {
        if (component.InsertionTimeRemaining <= 0)
            return;

        component.InsertionTimeRemaining -= frameTime;
        UpdateAppearance(uid, component);
        var isAnimationEnd = component.InsertionTimeRemaining <= 0;
        if (isAnimationEnd)
        {
            _itemSlotsSystem.SetLock(uid, component.PaperSlot, false);
            UpdateUi(uid, component);
        }
    }

    private void ProcessPrintingAnimation(EntityUid uid, float frameTime, PrinterComponent component)
    {
        if (component.PrintingTimeRemaining <= 0)
            return;

        component.PrintingTimeRemaining -= frameTime;
        UpdateAppearance(uid, component);
        var isAnimationEnd = component.PrintingTimeRemaining <= 0;
        if (isAnimationEnd)
        {
            PrintDocument(uid, component);
        }
    }

    private void UpdateAppearance(EntityUid uid, PrinterComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.InsertionTimeRemaining > 0)
            _appearance.SetData(uid, PrinterVisuals.VisualState, PrinterVisualState.Inserting);
        else if (component.PrintingTimeRemaining > 0)
            _appearance.SetData(uid, PrinterVisuals.VisualState, PrinterVisualState.Printing);
        else
            _appearance.SetData(uid, PrinterVisuals.VisualState, PrinterVisualState.Normal);
    }


    private void PrintDocument(EntityUid uid, PrinterComponent component)
    {
        var hasItem = component.PaperSlot.HasItem;
        var insertedPaper = component.PaperSlot.Item;

        if (!hasItem || !HasComp<PaperComponent>(insertedPaper))
            return;

        _entityManager.DeleteEntity(insertedPaper.Value);
        var paper = _entityManager.SpawnEntity("Paper", Transform(uid).Coordinates);
        if (HasComp<PaperComponent>(paper))
        {
            _paperSystem.SetContent(paper, _documentText);
        }

        UpdateUi(uid, component);
    }

    private void UpdateUi(EntityUid uid, PrinterComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;
        var state = new PrintUiState(component.PaperSlot.Item != null);
        _userInterface.TrySetUiState(uid, PrinterUI.Key, state);
    }
}
