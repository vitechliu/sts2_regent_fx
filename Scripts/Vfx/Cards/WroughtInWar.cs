using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.TestSupport;
using RegentFx.Core.Audio;

#pragma warning disable CS4014

namespace RegentFX.Scripts.Vfx.Cards;

public class WroughtInWar : CardFX {
    public override bool BorrowStar => false;
    
    public override bool UseV2Patch => true;
    public override bool HasOnBeforeDamage => true;
    public override async Task OnBeforeDamage(AttackCommand command) {
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
        SimpleSfxUtil.Play("res://RegentFX/sfx/wiw1.mp3");
        VFXUtil.PlaySimple("res://RegentFX/scenes/wrought_in_war.tscn", targetNode.VfxSpawnPosition);
        await Cmd.Wait(0.1f);
        NGame.Instance?.ScreenShake(ShakeStrength.Strong, ShakeDuration.Normal);
        await Cmd.Wait(0.15f);
    }
}