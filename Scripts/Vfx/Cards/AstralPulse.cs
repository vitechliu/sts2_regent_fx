using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.TestSupport;
using RegentFx.Core.Audio;

#pragma warning disable CS4014

namespace RegentFX.Scripts.Vfx.Cards;

[CardFx(typeof(MegaCrit.Sts2.Core.Models.Cards.AstralPulse))]
public class AstralPulse : CardFX {
    public override int StarCount => 3;

    // 更靠左上的位置
    public override Vector2 TargetOffset => new(0, -180f);

    public override float VfxClearDelay => 3f;
    public override bool HasExposureEffect => true;
    public override float ExposureInDuration => 0.2f;
    public override float ExposureOutDuration => 0.3f;

    private readonly List<Vector2> starPos = [
        new Vector2(0f, 0f),
        new Vector2(3f, -5f),
        new Vector2(-4f, -2f)
    ];

    public override Vector2 CalculateTargetPosition(Vector2 basePosition, int index, int totalCount) {
        return basePosition + TargetOffset + (starPos[index] * 2.5f);
    }

    public override void OnStartHolding(Star star, int index) {
        star.PulseMinScale *= 2f;
        star.PulseMaxScale *= 1.8f;
        star.PulseSpeed *= 3f;
        star.ChangeColorTo(new Color(14.551f, 0.683f, 9.982f)); //pink
    }
}

[HarmonyPatch]
public static class AstralPulsePatch {

    [HarmonyPrefix]
    [HarmonyPatch(typeof(MegaCrit.Sts2.Core.Models.Cards.AstralPulse), "OnPlay")]
    public static bool OnPlay(
        MegaCrit.Sts2.Core.Models.Cards.AstralPulse __instance,
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ref Task __result) {
        __result = MyOnPlay(__instance, choiceContext, cardPlay);
        return false;
    }

    private static async Task MyOnPlay(
        MegaCrit.Sts2.Core.Models.Cards.AstralPulse card,
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay) {
        await CreatureCmd.TriggerAnim(card.Owner.Creature, "Cast", card.Owner.Character.CastAnimDelay);
        
        Creature owner = card.Owner.Creature;
        NCreature? ownerNode = NCombatRoom.Instance?.GetCreatureNode(owner);
        if (ownerNode == null) {
            Entry.Logger.Info("Could not get creature nodes for VFX");
        }
        else {
            SimpleSfxUtil.Play("res://RegentFX/sfx/common_magic_1.mp3");
            VFXUtil.ShakeAfter(0.03f, ShakeStrength.Strong, ShakeDuration.Normal);
            Node2D? node = VFXUtil.PlaySimple("res://RegentFX/scenes/astral_pulse.tscn", ownerNode.VfxSpawnPosition);
            if (node != null) {
                node.Scale *= 1.3f;
            }
            WorldEnvironmentUtil.TweenExposure(2.8f, 0.05f);
            await Cmd.Wait(0.15f);
            Node2D ntest = VFXUtil.PlaySimple("res://RegentFX/scenes/vfx/distortions/vfx_outward_screen_distortion_ellipse.tscn", ownerNode.VfxSpawnPosition);
            VFXUtil.ReplayAllParticles(ntest);
            WorldEnvironmentUtil.TweenExposure(1f, 0.44f);
        }
        Entry.StarEffectController?.OnPlayCard();
        await DamageCmd.Attack(card.DynamicVars.Damage.BaseValue)
            .FromCard((CardModel) card)
            .TargetingAllOpponents(card.CombatState)
            .WithHitFx("vfx/vfx_starry_impact")
            .WithNoAttackerAnim()
            .SpawningHitVfxOnEachCreature()
            .Execute(choiceContext);
    }
}
