using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.TestSupport;
using RegentFX.ThirdParty.Audio;

#pragma warning disable CS4014

namespace RegentFX.Scripts.Vfx.Cards;

[CardFx(typeof(MegaCrit.Sts2.Core.Models.Cards.SolarStrike))]
public class SolarStrike : CardFX {
    public override HoldingModes HoldingMode => HoldingModes.None;
    
    public override bool UseV2Patch => true;
    public override bool HasOnBeforeDamage => true;

    public override string? VfxScenePath => "res://RegentFX/scenes/vfx/solar_strike.tscn";
    public override string? HitSfxPath => "event:/RegentFx/sfx/make_it_so_2"; //todo
    public override bool HasExposureEffect => false;
    // public override bool RemoveHitFx => true;

    public override async Task OnBeforeDamage(AttackCommand command) {
        Creature? target = command._singleTarget;
        if (target == null || command._singleTarget == null || card == null) return;
        await PlayVfx(card.Owner.Creature, target);
    }
    private async Task PlayVfx(Creature owner, Creature target) {
        if (TestMode.IsOn) return;
        NCreature? ownerNode = NCombatRoom.Instance?.GetCreatureNode(owner);
        NCreature? targetNode = NCombatRoom.Instance?.GetCreatureNode(target);
        var container = NCombatRoom.Instance?.CombatVfxContainer;
        if (ownerNode == null || targetNode == null || container == null) {
            return;
        }
        try {
            Node2D vfxNode = VFXUtil.GenVFXNode(VfxScenePath);
            Node2D? startNode = vfxNode.FindChild("StartPos") as Node2D;
            if (startNode == null) {
                Entry.Logger.Error($"[SolarStrike] No StartPos found in VFX scene");
                return;
            }
            int xFac = VFXUtil.IsCharacterFacingRight(owner) ? 1 : -1;
            Vector2 startPos = ownerNode.GlobalPosition + new Vector2(-140f * xFac, -620f) + VFXUtil.RandVec2(70f);
            Vector2 targetPos = targetNode.VfxSpawnPosition;
            vfxNode.FitVFX(startNode.GlobalPosition, Vector2.Zero, startPos, targetPos);
            vfxNode.GlobalPosition = targetPos;
            var onode1Pos = startPos += new Vector2(0f, 20f);
            if (VFXUtil.RandRD(0.4f)) {
                vfxNode.Scale *= new Vector2(1f, -1f);
                onode1Pos += new Vector2(0f, 50f);
            }
            // 添加到战斗特效容器
            var onode1 = VFXUtil.PlaySimple("res://scenes/vfx/energy/regent/regent_energy_vfx_back.tscn", onode1Pos, 3f);
            VFXUtil.ReplayAllParticles(onode1);
            container.AddChildSafely(vfxNode);
            Entry.StarEffectController?.OnPlayCard();
            FmodLite.Play(HitSfxPath);
            WorldEnvironmentUtil.FullExposure(1.3f, 0.1f, 0.2f, 0.1f);
            TaskHelper.RunSafely(CardVfxUtil.ClearAfter(vfxNode, 3f));
            await VFXUtil.Wait(0.27f);
            FmodLite.Play("event:/RegentFx/sfx/post_magic"); //todo
            var onode2 = VFXUtil.PlaySimple("res://scenes/vfx/energy/regent/regent_energy_vfx_front.tscn", targetPos, 3f);
            VFXUtil.ReplayAllParticles(onode2);
            NGame.Instance?.ScreenShake(ShakeStrength.Weak, ShakeDuration.Short);

        } catch (Exception ex) {
            Entry.Logger.Warn($"[SolarStrike] Error playing VFX: {ex.Message}");
        }
    }
}