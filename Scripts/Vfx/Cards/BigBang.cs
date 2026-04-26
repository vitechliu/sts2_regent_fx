using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using RegentFx.Core.Audio;

#pragma warning disable CS4014

namespace RegentFX.Scripts.Vfx.Cards;

public class BigBang : CardFX {
}

[HarmonyPatch]
public static class BigBangPatch {
    [HarmonyPrefix]
    [HarmonyPatch(typeof(MegaCrit.Sts2.Core.Models.Cards.BigBang), "OnPlay")]
    public static bool OnPlay(
        MegaCrit.Sts2.Core.Models.Cards.BigBang __instance,
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ref Task __result) {
        if (!LocalContext.IsMe(__instance.Owner)) return true;
        __result = MyOnPlay(__instance, choiceContext, cardPlay);
        return false;
    }
    
    private static async Task MyOnPlay(
        MegaCrit.Sts2.Core.Models.Cards.BigBang card,
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay) {
        await CreatureCmd.TriggerAnim(card.Owner.Creature, "Cast", card.Owner.Character.CastAnimDelay);
        NCreature? ownerNode = NCombatRoom.Instance?.GetCreatureNode(card.Owner.Creature);
        if (ownerNode != null) {
            VFXUtil.PlaySimple("res://RegentFX/scenes/big_bang.tscn", ownerNode.VfxSpawnPosition, 2f);
            VFXUtil.ShakeAfter(0.3f, ShakeStrength.Strong, ShakeDuration.Normal);
            await Cmd.Wait( .25f);
            SimpleSfxUtil.Play("res://RegentFX/sfx/big_bang_1.mp3");
            WorldEnvironmentUtil.TweenExposure(3.5f, .15f);
            await Cmd.Wait( .15f);
            WorldEnvironmentUtil.TweenExposure(1f, .4f);
        }
        await CardPileCmd.Draw(choiceContext, card.DynamicVars.Cards.BaseValue, card.Owner);
        await PlayerCmd.GainStars(card.DynamicVars.Stars.BaseValue, card.Owner);
        await PlayerCmd.GainEnergy((Decimal) card.DynamicVars.Energy.IntValue, card.Owner);
        await ForgeCmd.Forge((Decimal) card.DynamicVars.Forge.IntValue, card.Owner, card);
        
    }
}
