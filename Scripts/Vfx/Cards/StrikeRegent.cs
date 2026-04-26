using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;
using RegentFx.Core.Audio;

#pragma warning disable CS4014

namespace RegentFX.Scripts.Vfx.Cards;

[CardFx(typeof(MegaCrit.Sts2.Core.Models.Cards.StrikeRegent))]
public class StrikeRegent : CardFX {
}

[HarmonyPatch]
public static class StrikeRegentPatch {
    [HarmonyPrefix]
    [HarmonyPatch(typeof(MegaCrit.Sts2.Core.Models.Cards.StrikeRegent), "OnPlay")]
    public static bool OnPlay(
        MegaCrit.Sts2.Core.Models.Cards.StrikeRegent __instance,
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ref Task __result) {
        if (!LocalContext.IsMe(__instance.Owner)) return true;
        __result = MyOnPlay(__instance, choiceContext, cardPlay);
        return false;
    }
    
    private static async Task MyOnPlay(
        MegaCrit.Sts2.Core.Models.Cards.StrikeRegent card,
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay) {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await CreatureCmd.TriggerAnim(card.Owner.Creature, "Cast", card.Owner.Character.CastAnimDelay);
        SfxCmd.Play("event:/sfx/characters/regent/regent_attack");
        await DamageCmd.Attack(card.DynamicVars.Damage.BaseValue)
            .FromCard(card)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_starry_impact")
            .BeforeDamage(async delegate {
                await PlayVfx(card.Owner.Creature, cardPlay.Target);
            })
            .WithNoAttackerAnim()
            .Execute(choiceContext);
    }
    
    private static async Task PlayVfx(Creature owner, Creature target) {
        if (TestMode.IsOn) return;
        
        // //test
        // NCreature ownerNode = NCombatRoom.Instance?.GetCreatureNode(owner);
        // Node2D ntest = VFXUtil.PlaySimple("res://RegentFX/scenes/vfx/distortions/vfx_outward_screen_distortion_ellipse.tscn", ownerNode.VfxSpawnPosition);
        // ntest.Scale = new Vector2(4f, 0.5f);
        // VFXUtil.ReplayAllParticles(ntest);
        
        NCreature? targetNode = NCombatRoom.Instance?.GetCreatureNode(target);
        if (targetNode == null) {
            Entry.Logger.Info("Could not get creature nodes for VFX");
            return;
        }
        Blade.PlayBlade(targetNode.VfxSpawnPosition);
        await Cmd.Wait(0.05f);
    }
}
