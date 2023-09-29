using Content.Shared.Inventory.Events;

namespace Content.Shared.RoM.Clothing.Glasses;

public sealed class GlassesHudSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _componentFactory = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GlassesHudComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<GlassesHudComponent, GotUnequippedEvent>(OnUnequipped);
    }

    private void OnUnequipped(EntityUid uid, GlassesHudComponent component, GotUnequippedEvent args)
    {
        foreach (var (name, data) in component.Components)
        {
            var comp = (Component) _componentFactory.GetComponent(name);
            if (args.Slot == "eyes" && HasComp(args.Equipee, comp.GetType()))
                EntityManager.RemoveComponent(args.Equipee, comp.GetType());
        }
    }

    private void OnEquipped(EntityUid uid, GlassesHudComponent component, GotEquippedEvent args)
    {
        foreach (var (name, data) in component.Components)
        {
            var comp = (Component) _componentFactory.GetComponent(name);
            comp.Owner = args.Equipee;
            if (args.Slot == "eyes" && !HasComp(args.Equipee, comp.GetType()))
                EntityManager.AddComponent(args.Equipee, comp);
        }
    }
}
