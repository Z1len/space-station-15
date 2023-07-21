using Content.Shared.Inventory.Events;
using Content.Shared.RoM.MedicalHud;
using JetBrains.Annotations;

namespace Content.Server.RoM.MedicalHud;

[UsedImplicitly]
public sealed class MedicalHudSystem : SharedMedicalHudSystem
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MedicalHudComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<MedicalHudComponent, GotUnequippedEvent>(OnUnequipped);
    }

    private void OnUnequipped(EntityUid uid, MedicalHudComponent component, GotUnequippedEvent args)
    {
        if (args.Slot == "eyes" && HasComp<MedicalHudComponent>(args.Equipee))
            _entityManager.RemoveComponent<MedicalHudComponent>(args.Equipee);
    }

    private void OnEquipped(EntityUid uid, MedicalHudComponent comp, GotEquippedEvent args)
    {
        if(args.Slot == "eyes" && !HasComp<MedicalHudComponent>(args.Equipee))
            _entityManager.AddComponent<MedicalHudComponent>(args.Equipee);
    }
}

