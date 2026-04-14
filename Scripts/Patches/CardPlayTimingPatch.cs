using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using RegentFX.Scripts;
using RegentFX.Scripts.Vfx.Cards;

namespace RegentFX.Scripts.Patches;

/// <summary>
/// 卡牌打出时点监听补丁
/// </summary>
[HarmonyPatch]
public static class CardPlayTimingPatch {
    /// <summary>
    /// 监听卡牌拖拽开始时点
    /// 当玩家点击卡牌开始拖拽时触发
    /// </summary>
    [HarmonyPatch(typeof(NPlayerHand), nameof(NPlayerHand.StartCardPlay))]
    [HarmonyPostfix]
    public static void CardDragStartPatch(NHandCardHolder holder, bool startedViaShortcut) {
        var cardModel = holder?.CardModel;
        if (cardModel != null) {
            var cardFX = CardFX.FromCard(cardModel);
            if (cardFX != null) {
                Entry.StarEffectController?.OnCardHolding(cardModel, cardFX);
            }
        }
        else {
            Entry.Logger.Warn("[CardPlayTiming]缺少CardModel");
        }
    }


    /// <summary>
    /// 监听卡牌放弃打出时点
    /// 当卡牌通过验证并准备加入行动队列时触发
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(NCardPlay), nameof(NCardPlay.CancelPlayCard))]
    public static void CardCancel(NCardPlay __instance) {
        string isTrying = __instance._isTryingToPlayCard.ToString();
        
        Entry.Logger.Info($"[CardPlayTiming] 卡牌放弃打出 isTryingToPlayCard:" + isTrying);
        if (!__instance._isTryingToPlayCard) {
            Entry.StarEffectController?.OnCancelCard();
        }
    }
}
