using Content.Shared.GameTicking;
using Content.Shared.RoM.MedicalHud;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.RoM.MedicalHud;


public sealed class MedicalHudSystem : SharedMedicalHudSystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MedicalHudComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<MedicalHudComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<MedicalHudComponent, PlayerAttachedEvent>(OnAttached);
        SubscribeLocalEvent<MedicalHudComponent, PlayerDetachedEvent>(OnDetached);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _overlayManager.RemoveOverlay<MedicalHudOverlay>();
    }

    private void OnAttached(EntityUid uid, MedicalHudComponent component, PlayerAttachedEvent args)
    {
        _overlayManager.AddOverlay(new MedicalHudOverlay(EntityManager,_prototypeManager));
    }

    private void OnDetached(EntityUid uid, MedicalHudComponent component, PlayerDetachedEvent args)
    {
        _overlayManager.RemoveOverlay<MedicalHudOverlay>();
    }

    private void OnInit(EntityUid uid, MedicalHudComponent component, ComponentInit args)
    {
        if(_playerManager.LocalPlayer?.ControlledEntity == uid)
            _overlayManager.AddOverlay(new MedicalHudOverlay(EntityManager,_prototypeManager));
    }

    private void OnRemove(EntityUid uid, MedicalHudComponent component, ComponentRemove args)
    {
        if(_playerManager.LocalPlayer?.ControlledEntity == uid)
            _overlayManager.RemoveOverlay<MedicalHudOverlay>();
    }
}
