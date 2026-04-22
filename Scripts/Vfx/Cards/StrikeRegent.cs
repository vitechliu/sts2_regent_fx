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
        NCreature? ownerNode = NCombatRoom.Instance?.GetCreatureNode(owner);
        NCreature? targetNode = NCombatRoom.Instance?.GetCreatureNode(target);

        if (ownerNode == null || targetNode == null) {
            Entry.Logger.Info("Could not get creature nodes for VFX");
            return;
        }

        Vector2 startPos = GD.Randf() < 0.7 ? new Vector2(GD.Randi() % 600, 0f) : new Vector2(0f, GD.Randi() % 500);
        Vector2 targetPos = targetNode.VfxSpawnPosition + new Vector2((float)GD.RandRange(-30f, 30f), (float)GD.RandRange(-30f, 30f));
        Blade.SpawnAndLaunch(Blade.Blade1Path, startPos, targetPos);
        if (GD.Randf() < 0.5f) {
            Blade.SpawnAndLaunch(Blade.Blade2Path, startPos - new Vector2(300f, 300f), targetPos);
        }
        // Entry.StarEffectController?.OnPlayCard();
        await Cmd.Wait(0.05f);
    }
}
