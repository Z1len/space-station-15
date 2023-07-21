using Content.Shared.Inventory.Events;
using JetBrains.Annotations;

namespace Content.Shared.RoM.MedicalHud;

[UsedImplicitly]
public abstract class SharedMedicalHudSystem : EntitySystem
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
        if (HasComp<MedicalHudComponent>(args.Equipee))
            _entityManager.RemoveComponent<MedicalHudComponent>(args.Equipee);
    }

    private void OnEquipped(EntityUid uid, MedicalHudComponent comp, GotEquippedEvent args)
    {
        _entityManager.AddComponent<MedicalHudComponent>(args.Equipee);
    }
}

