using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.TestSupport;
using RitsuFmodLite;

#pragma warning disable CS4014

namespace RegentFX.Scripts.Vfx.Cards;

[CardFx(typeof(MegaCrit.Sts2.Core.Models.Cards.WroughtInWar))]
public class WroughtInWar : CardFX {
    public override HoldingModes HoldingMode => HoldingModes.None;

    public override string? VfxScenePath => "res://RegentFX/scenes/vfx/wrought_in_war.tscn";

    public override bool UseV2Patch => true;
    public override bool HasOnBeforeDamage => true;
    public override async Task OnBeforeDamage(AttackCommand command) {
        Creature? target = command._singleTarget;
        if (target == null) return;
        await PlayVfx(target);
    }
    
    private async Task PlayVfx(Creature target) {
        if (TestMode.IsOn) return;
        NCreature? targetNode = NCombatRoom.Instance?.GetCreatureNode(target);
        if (targetNode == null) {
            return;
        }
        FmodLite.Play("event:/RegentFx/sfx/wiw1");
        VFXUtil.PlaySimple(VfxScenePath, targetNode.VfxSpawnPosition);
        await Cmd.Wait(0.1f);
        NGame.Instance?.ScreenShake(ShakeStrength.Strong, ShakeDuration.Normal);
        await Cmd.Wait(0.15f);
    }
}