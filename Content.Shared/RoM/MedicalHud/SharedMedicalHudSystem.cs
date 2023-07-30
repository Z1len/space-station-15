using Content.Shared.Inventory.Events;
namespace Content.Shared.RoM.MedicalHud;

public abstract class SharedMedicalHudSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MedicalHudComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<MedicalHudComponent, GotUnequippedEvent>(OnUnequipped);
    }

    private void OnUnequipped(EntityUid uid, MedicalHudComponent component, GotUnequippedEvent args)
    {
        if (args.Slot == "eyes" && HasComp<MedicalHudComponent>(args.Equipee))
            EntityManager.RemoveComponent<MedicalHudComponent>(args.Equipee);
    }

    private void OnEquipped(EntityUid uid, MedicalHudComponent component, GotEquippedEvent args)
    {
        if (args.Slot == "eyes" && !HasComp<MedicalHudComponent>(args.Equipee))
            EntityManager.AddComponent<MedicalHudComponent>(args.Equipee);
    }
}
