using Robust.Shared.Prototypes;

namespace Content.Shared.RoM.Clothing.Glasses;

[RegisterComponent]
public sealed class GlassesHudComponent : Component
{
    [DataField("component", required: true)]
    public ComponentRegistry Components { get; } = new();
}
