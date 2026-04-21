using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

#pragma warning disable CS4014

namespace RegentFX.Scripts.Vfx.Cards;

public class Glow : CardFX {
}

[HarmonyPatch]
public static class GlowPatch {
    [HarmonyPrefix]
    [HarmonyPatch(typeof(MegaCrit.Sts2.Core.Models.Cards.Glow), "OnPlay")]
    public static bool OnPlay(
        MegaCrit.Sts2.Core.Models.Cards.Glow __instance,
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ref Task __result) {
        __result = MyOnPlay(__instance, choiceContext, cardPlay);
        return false;
    }
    
    private static async Task MyOnPlay(
        MegaCrit.Sts2.Core.Models.Cards.Glow card,
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay) {
        await CreatureCmd.TriggerAnim(card.Owner.Creature, "Cast", card.Owner.Character.CastAnimDelay);
        NCreature? ownerNode = NCombatRoom.Instance?.GetCreatureNode(card.Owner.Creature);
        if (ownerNode != null) {
            VFXUtil.PlaySimple("res://RegentFX/scenes/glow.tscn", ownerNode.VfxSpawnPosition, 2f);
            await Cmd.Wait( .1f);
            WorldEnvironmentUtil.TweenExposure(2.2f, .1f);
            await Cmd.Wait( .1f);
            WorldEnvironmentUtil.TweenExposure(1f, .3f);
        }
        await PlayerCmd.GainStars(card.DynamicVars.Stars.BaseValue, card.Owner);
        IEnumerable<CardModel> cardModels = await CardPileCmd.Draw(choiceContext, card.DynamicVars.Cards.BaseValue, card.Owner);
        DrawCardsNextTurnPower cardsNextTurnPower = await PowerCmd.Apply<DrawCardsNextTurnPower>(card.Owner.Creature, card.DynamicVars.Cards.BaseValue, card.Owner.Creature, card);
        // SfxCmd.Play("event:/sfx/characters/regent/regent_attack");
    }
}
