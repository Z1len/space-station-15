using Content.Shared.RoM.MedicalHud;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.RoM.MedicalHud;

[UsedImplicitly]
public sealed class MedicalHudSystem : SharedMedicalHudSystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlayManager.AddOverlay(new MedicalHudOverlay(_entityManager, _prototypeManager, _player));
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlayManager.RemoveOverlay<MedicalHudOverlay>();
    }
}
