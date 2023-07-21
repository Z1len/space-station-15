using Content.Server.Paper;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.UserInterface;
using Content.Shared.Administration.Logs;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
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
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;


    private const string PaperSlotId = "PrinterPaperSlot";

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

            InsertingProcess(uid, frameTime, printerComp);
            PrintingProcess(uid, frameTime, printerComp);
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
        component.DocumentText = Loc.GetString(_prototype.Index<DocumentPrototype>(args.Id).Text);
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
            var attachedEnt = args.Session.AttachedEntity;
            _soundSystem.PlayPvs(component.PrintSound, uid);
            _popupSystem.PopupEntity(Loc.GetString("printer-popup-printing"), uid);
            _adminLog.Add(LogType.Action,
                LogImpact.Low,
                $"{(attachedEnt != null ? ToPrettyString(attachedEnt.Value) : "Unknown"):user} has printed on {ToPrettyString(uid)}: {component.DocumentText ?? " "}");
            component.PrintingTimeRemaining = component.PrintingTime;
	    }
    }

    private void InsertingProcess(EntityUid uid, float frameTime, PrinterComponent component)
    {
        if (component.InsertionTimeRemaining <= 0)
            return;

        component.InsertionTimeRemaining -= frameTime;

        var isInsertingTimeEnded = component.InsertionTimeRemaining <= 0;
        if (isInsertingTimeEnded)
        {
            _itemSlotsSystem.SetLock(uid, component.PaperSlot, false);
            UpdateUi(uid, component);
        }
    }

    private void PrintingProcess(EntityUid uid, float frameTime, PrinterComponent component)
    {
        if (component.PrintingTimeRemaining <= 0)
            return;

        component.PrintingTimeRemaining -= frameTime;

        var isPrintingTimeEnded = component.PrintingTimeRemaining <= 0;
        if (isPrintingTimeEnded)
        {
            PrintDocument(uid, component);
        }
    }


    /// <summary>
    /// Checks if paper inserted in item slot and prints document text from component
    /// </summary>
    public void PrintDocument(EntityUid uid, PrinterComponent? component = null)
    {
        if(!Resolve(uid, ref component))
            return;

        var hasItem = component.PaperSlot.HasItem;
        var insertedPaper = component.PaperSlot.Item;

        if (!hasItem || !HasComp<PaperComponent>(insertedPaper))
            return;

        _entityManager.DeleteEntity(insertedPaper.Value);
        var paper = _entityManager.SpawnEntity("Paper", Transform(uid).Coordinates);
        if (HasComp<PaperComponent>(paper))
        {
            _paperSystem.SetContent(paper, component.DocumentText ?? " ");
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
