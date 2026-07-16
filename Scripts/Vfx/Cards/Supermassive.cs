using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace RegentFX.Scripts.Vfx.Cards;

/// <summary>
/// 超质量体：使用战斗中积累的黑洞替换原版武器/斩击表现。
/// </summary>
[CardFx(typeof(MegaCrit.Sts2.Core.Models.Cards.Supermassive))]
public sealed class Supermassive : CardFX {
    public override HoldingModes HoldingMode => HoldingModes.None;
    public override string VfxScenePath => SupermassiveController.ScenePath;
    public override bool UseV2Patch => true;
    public override bool HasOnBeforeDamageTargeted => true;
    public override bool RemoveHitFx => Entry.SupermassiveController?.HasReadyOrb == true;
    public override bool ShouldDisableRegentWeaponAttack => Entry.SupermassiveController?.HasReadyOrb == true;
    public override bool ShouldDisableRegentWeaponSFX => false;

    public override Task OnBeforeDamage(AttackCommand command, IReadOnlyList<Creature> targets) {
        Creature? target = targets.FirstOrDefault();
        if (target == null || target.IsDead || card?.Owner?.Creature.IsDead != false) {
            return Task.CompletedTask;
        }

        Entry.SupermassiveController?.TryLaunch(target);
        return Task.CompletedTask;
    }
}
