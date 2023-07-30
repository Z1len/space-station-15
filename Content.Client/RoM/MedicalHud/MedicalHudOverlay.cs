using System.Numerics;
using Content.Shared.Damage;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.RoM.MedicalHud;

public sealed class MedicalHudOverlay : Overlay
{
    private const float StartX = 1;
    private const float EndX = 21f;
    private readonly ShaderInstance _shader;
    private readonly Texture _texture;
    private readonly SharedTransformSystem _transform;
    private readonly IEntityManager _entityManager;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    public MedicalHudOverlay(IEntityManager entManager,IPrototypeManager protoManager)
    {
        _entityManager = entManager;
        _transform = _entityManager.EntitySysManager.GetEntitySystem<SharedTransformSystem>();
        var sprite = new SpriteSpecifier.Rsi(new ("/Textures/RoM/Interface/Health/health_bar.rsi"), "icon");
        _texture = _entityManager.EntitySysManager.GetEntitySystem<SpriteSystem>().Frame0(sprite);
        _shader = protoManager.Index<ShaderPrototype>("unshaded").Instance();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;
        var rotation = args.Viewport.Eye?.Rotation ?? Angle.Zero;
        var xformQuery = _entityManager.GetEntityQuery<TransformComponent>();
        const float scale = 1f;
        var scaleMatrix = Matrix3.CreateScale(new Vector2(scale, scale));
        var rotationMatrix = Matrix3.CreateRotation(-rotation);

        var offset = 0f;

        handle.UseShader(_shader);

        var enumerator = _entityManager
            .AllEntityQueryEnumerator<TransformComponent, SpriteComponent, DamageableComponent, MobStateComponent>();
        while (enumerator.MoveNext(out var xform, out var sprite, out var damage, out var state))
        {
            if(xform.MapID != args.MapId)
                continue;

            var worldPos = _transform.GetWorldPosition(xform, xformQuery);

            var worldMatrix = Matrix3.CreateTranslation(worldPos);
            Matrix3.Multiply(scaleMatrix, worldMatrix, out var scaledWorld);
            Matrix3.Multiply(rotationMatrix, scaledWorld, out var matty);
            handle.SetTransform(matty);

            var yOffset = sprite.Bounds.Height / 2f + 0.05f;

            var yOffsetIcon = yOffset - 0.14f;

            var position = new Vector2(-_texture.Width / 2f / EyeManager.PixelsPerMeter,
                yOffset / scale + offset / EyeManager.PixelsPerMeter * scale);

            var iconPos = new Vector2(_texture.Width / 1.7f / EyeManager.PixelsPerMeter,
                yOffsetIcon / scale + offset / EyeManager.PixelsPerMeter * scale);

            var mobState = state.CurrentState;
            var totalDamage = damage.TotalDamage;
            var color = GetColor(mobState);

            var textureIcon = _entityManager.EntitySysManager.GetEntitySystem<SpriteSystem>().Frame0(GetHealthIconRsi(mobState));

            handle.DrawTexture(_texture, position);
            handle.DrawTexture(textureIcon, iconPos);

            var xProgress = (EndX - StartX) * (1f - totalDamage.Float()/100f) + StartX;

            if(mobState == MobState.Critical)
                xProgress = (EndX - StartX) * (2f - totalDamage.Float()/100f) + StartX;
            if (mobState == MobState.Dead)
                continue;

            var box = new Box2(new Vector2(StartX, 1f) / EyeManager.PixelsPerMeter,
                new Vector2(xProgress, 2f) / EyeManager.PixelsPerMeter);
            box = box.Translated(position);
            handle.DrawRect(box,color);

        }
        handle.UseShader(null);
        handle.SetTransform(Matrix3.Identity);
    }

    private static SpriteSpecifier.Rsi GetHealthIconRsi(MobState state)
    {
        switch (state)
        {
            case MobState.Alive:
                return new SpriteSpecifier.Rsi(new("/Textures/RoM/Interface/Health/health_icon.rsi"), "alive");
            case MobState.Critical:
                return new SpriteSpecifier.Rsi(new("/Textures/RoM/Interface/Health/health_icon.rsi"), "critical");
            default:
                return new SpriteSpecifier.Rsi(new("/Textures/RoM/Interface/Health/health_icon.rsi"), "dead");
        }
    }
    private static Color GetColor(MobState state)
    {
        if(state == MobState.Alive)
            return new Color(0f, 5f, 0f);
        return new Color(5f, 0f, 0f);
    }

}
