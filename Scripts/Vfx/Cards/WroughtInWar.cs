using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

#pragma warning disable CS4014

namespace RegentFX.Scripts.Vfx.Cards;

public class WroughtInWar : CardFX {
}

[HarmonyPatch]
public static class WroughtInWarPatch {
    [HarmonyPrefix]
    [HarmonyPatch(typeof(MegaCrit.Sts2.Core.Models.Cards.WroughtInWar), "OnPlay")]
    public static bool OnPlay(
        MegaCrit.Sts2.Core.Models.Cards.WroughtInWar __instance,
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ref Task __result) {
        __result = MyOnPlay(__instance, choiceContext, cardPlay);
        return false;
    }
    
    private static async Task MyOnPlay(
        MegaCrit.Sts2.Core.Models.Cards.WroughtInWar card,
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay) {
        ArgumentNullException.ThrowIfNull((object) cardPlay.Target, "cardPlay.Target");
        await CreatureCmd.TriggerAnim(card.Owner.Creature, "Cast", card.Owner.Character.CastAnimDelay);
        AttackCommand attackCommand = await DamageCmd.Attack(card.DynamicVars.Damage.BaseValue)
            .FromCard(card)
            .Targeting(cardPlay.Target)
            .BeforeDamage(async delegate {
                await PlayVfx(card.Owner.Creature, cardPlay.Target);
            })
            .WithNoAttackerAnim()
            .Execute(choiceContext);
        await ForgeCmd.Forge((Decimal) card.DynamicVars.Forge.IntValue, card.Owner, card);
    }
    
    private static async Task PlayVfx(Creature owner, Creature target) {
        if (TestMode.IsOn) return;
        NCreature? targetNode = NCombatRoom.Instance?.GetCreatureNode(target);
        if (targetNode == null) {
            Entry.Logger.Info("Could not get creature nodes for VFX");
            return;
        }
        // SfxCmd.Play("event:/sfx/characters/regent/regent_attack");
        VFXUtil.PlaySimple("res://RegentFX/scenes/wrought_in_war.tscn", targetNode.VfxSpawnPosition);
        await Cmd.Wait(0.25f);
    }
}
