using Robust.Shared.Prototypes;

namespace Content.Shared.RoM.Clothing.Glasses;

[RegisterComponent]
public sealed partial class GlassesHudComponent : Component
{
    [DataField("component", required: true)]
    public ComponentRegistry Components = new();
}
