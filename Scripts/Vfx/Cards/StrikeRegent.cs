using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

#pragma warning disable CS4014

namespace RegentFX.Scripts.Vfx.Cards;

[CardFx(typeof(MegaCrit.Sts2.Core.Models.Cards.StrikeRegent))]
public class StrikeRegent : CardFX {
    public override HoldingModes HoldingMode => HoldingModes.None;
    
    public override bool UseV2Patch => true;
    public override bool HasOnBeforeDamage => true;
    public override string? ChangeHitFx => "vfx/vfx_starry_impact";


    public override async Task OnBeforeDamage(AttackCommand command) {
        SfxCmd.Play("event:/sfx/characters/regent/regent_attack");
        Creature? target = command._singleTarget;
        if (target == null || command._singleTarget == null) return;
        await PlayVfx(target);
    }
    private static async Task PlayVfx(Creature target) {
        if (TestMode.IsOn) return;
        NCreature? targetNode = NCombatRoom.Instance?.GetCreatureNode(target);
        if (targetNode == null) {
            Entry.Logger.Info("Could not get creature nodes for VFX");
            return;
        }
        Blade.PlayBlade(targetNode.VfxSpawnPosition);
        await Cmd.Wait(0.05f);
    }
}