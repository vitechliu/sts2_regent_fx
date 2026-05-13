using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

#pragma warning disable CS4014

namespace RegentFX.Scripts.Vfx.Cards;

[CardFx(typeof(MegaCrit.Sts2.Core.Models.Cards.DyingStar))]
public class DyingStar : CardFX {
    public override int StarCount => 3;

    // 更靠左上的位置
    public override Vector2 TargetOffset => new(-200f, -450f);

    public override string VfxScenePath => "res://RegentFX/scenes/vfx/dying_star.tscn";
    public override string HitSfxPath => "event:/RegentFx/sfx/common_hold_4";
    public override string SecondarySfxPath => "event:/RegentFx/sfx/dying_star";
    public override float VfxClearDelay => 3f;
    public override bool HasExposureEffect => true;
    public override float ExposurePeak => 2.2f;
    public override float ExposureInDuration => 0.6f;
    public override float ExposureOutDuration => 0.2f;

    private List<Vector2> starPos = new() {
        new Vector2(0f, 0f),
        new Vector2(13f, -24f),
        new Vector2(30f, -5f),
    };

    public override Vector2 CalculateTargetPosition(Vector2 basePosition, int index, int totalCount) {
        return basePosition + TargetOffset + (starPos[index] * 2.5f);
    }

    public override void OnStartHolding(Star star, int index) {
        star.PulseMinScale *= 2f;
        star.PulseMaxScale *= 1.8f;
    }
    
    public override bool UseV2Patch => true;
    public override bool HasOnBeforeDamage => true;
    
    public override async Task OnBeforeDamage(AttackCommand command) {
        Creature? owner = card?.Owner.Creature;
        if (owner == null) return;
        VFXUtil.ShakeAfter(0.35f, ShakeStrength.Strong, ShakeDuration.Normal);
        Entry.StarEffectController?.OnPlayCard();
        await CardVfxUtil.PlayAoeVfx(this, owner, card, nameof(DyingStar));
        await Cmd.Wait(0.3f);
    }
}