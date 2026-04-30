using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using RegentFX.Scripts.Vfx.Cards;

namespace RegentFX.Scripts.Patches;

[HarmonyPatch]
public static class CardAnimPatch {
    private static bool _isProcessing = false;
    
    
    [HarmonyPatch(typeof(AttackCommand), nameof(AttackCommand.Execute))]
    [HarmonyPrefix]
    public static bool ExecutePatch(AttackCommand __instance, PlayerChoiceContext? choiceContext, ref Task<AttackCommand> __result) {
        if (_isProcessing) return true; 
        if (__instance.ModelSource == null) return true;
        try {
            var card = __instance.ModelSource as CardModel;
            var cardFX = CardFX.FromCard(card);
            if (cardFX == null) return true;
            if (!cardFX.UseV2Patch) return true;
            if (!LocalContext.IsMe(card.Owner)) return true;


            if (cardFX.DisableAttackAnim) {
                __instance.WithNoAttackerAnim();
            }
            if (cardFX.HasOnBeforeDamage) {
                __instance.BeforeDamage(async delegate {
                    await cardFX.OnBeforeDamage(__instance);
                });
            }

            if (cardFX.ChangeHitFx != null) {
                __instance.WithHitFx(cardFX.ChangeHitFx);
            }
            if (cardFX.HasOnBeforeExecute) {
                __result = RunCustomFlow(cardFX, card, __instance, choiceContext);
                return false;
            }
        }
        catch (InvalidCastException) {
        }
        return true;
    }

    static async Task<AttackCommand> RunCustomFlow(CardFX cardFX, CardModel card, AttackCommand instance, PlayerChoiceContext? choiceContext) {
        _isProcessing = true;
        try {
            await BeforeExecute(cardFX, card, instance);
            return await instance.Execute(choiceContext);
        } finally{
            _isProcessing = false;
        }
    }
    
    
    static async Task BeforeExecute(CardFX cardFX, CardModel card, AttackCommand command) {
        if (cardFX.PlayCastAnim) {
            await CreatureCmd.TriggerAnim(card.Owner.Creature, "Cast", card.Owner.Character.CastAnimDelay);
        }
        await cardFX.OnBeforeExecute();
    }


    [HarmonyPatch(typeof(CardModel), "OnPlay")]
    [HarmonyPostfix]
    public static void PostOnPlayPatch(CardModel __instance) {
        var cardFX = CardFX.FromCard(__instance);
        if (cardFX == null) return;
        if (!cardFX.UseV2Patch) return;
    }
    
    
}
