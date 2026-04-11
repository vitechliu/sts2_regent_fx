using HarmonyLib;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Models;
using RegentFX.Scripts.Vfx.Cards;

namespace RegentFX.Scripts.Patches;

[HarmonyPatch]
public static class CardAnimPatch {
    // [HarmonyPatch(typeof(AttackCommand), nameof(AttackCommand.FromCard))]
    // [HarmonyPostfix]
    // public static void AttackCommandFromCard(AttackCommand __instance, CardModel card) {
    //     var cardFX = CardFX.FromCard(card);
    //     if (cardFX == null) return;
    //     if (!cardFX.DisableWeaponAnim) return;
    //     Entry.Logger.Info(
    //         $"[CardPlayTiming] 移除卡牌攻击动画 | 卡牌: {card.Title}");
    //     __instance._attackerAnimName = "Cast";
    // }
}
