using Godot;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;

#pragma warning disable CS4014

namespace RegentFX.Scripts.Vfx.Cards;

/// <summary>
/// FallingStar 卡牌特效
/// </summary>
[CardFx(typeof(MegaCrit.Sts2.Core.Models.Cards.FallingStar))]
public class FallingStar : CardFX {
    public override int StarCount => 2;

    // 更靠左上的位置
    public override Vector2 TargetOffset => new(-200f, -450f);

    public override string VfxScenePath => "res://RegentFX/scenes/vfx/falling_star.tscn";
    public override string HitSfxPath => "event:/RegentFx/sfx/falling_star";
    public override bool HasExposureEffect => true;
    public override float ExposureInDuration => 0.1f;
    public override float ExposurePeak => 1.3f;
    public override float ExposureOutDuration => 0.5f;

    private List<Vector2> starPos = new() {
        new Vector2(0f, 0f),
        new Vector2(30f, -50f),
    };

    public override Vector2 CalculateTargetPosition(Vector2 basePosition, int index, int totalCount) {
        return basePosition + TargetOffset + (starPos[index] * 1.2f);
    }

    public override void OnStartHolding(Star star, int index) {
        if (index == 0) {
            star.ChangeColorTo(new Color(14.551f, 14.551f, 0.0f)); //yellow
        }
        if (index == 1) {
            star.ChangeColorTo(new Color(14.551f, 0.683f, 9.982f)); //pink
        }
    }
    
    public override bool UseV2Patch => true;
    public override bool HasOnBeforeDamage => true;
    
    public override async Task OnBeforeDamage(AttackCommand command) {
        Creature? owner = card?.Owner.Creature;
        Creature? target = command._singleTarget;
        if (owner == null || target == null || command._singleTarget == null) return;
        Entry.StarEffectController?.OnPlayCard();
        await CardVfxUtil.PlayTargetedVfx(this, card.Owner.Creature, target, nameof(FallingStar));
    }
}