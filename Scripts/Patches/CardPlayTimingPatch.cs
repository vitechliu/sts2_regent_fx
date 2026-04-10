using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using RegentFX.Scripts;

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
            Entry.Logger.Info(
                $"[CardPlayTiming] 卡牌拖拽开始 | 卡牌: {cardModel.Title} | ID: {cardModel.Id.Entry} | 目标类型: {cardModel.TargetType} | 通过快捷键: {startedViaShortcut}");
        }
        else {
            Entry.Logger.Warn("[CardPlayTiming]缺少CardModel");
        }
    }

    /// <summary>
    /// 监听卡牌真正加入打出队列的时点
    /// 当卡牌通过验证并准备加入行动队列时触发
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CardModel), nameof(CardModel.EnqueueManualPlay))]
    public static void CardStartEnqueue(CardModel __instance, Creature? target) {
        string targetInfo = target != null ? $"目标: {target.Name} (CombatID: {target.CombatId})" : "无目标";
        Entry.Logger.Info(
            $"[CardPlayTiming] 卡牌加入打出队列 | 卡牌: {__instance.Title} | ID: {__instance.Id.Entry} | 费用: {__instance.EnergyCost} | {targetInfo} | 类型: {__instance.Type}");
    }
    
    /// <summary>
    /// 监听卡牌放弃打出时点
    /// 当卡牌通过验证并准备加入行动队列时触发
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(NCardPlay), nameof(NCardPlay.CancelPlayCard))]
    public static void CardCancel(NCardPlay __instance) {
        Entry.Logger.Info($"[CardPlayTiming] 卡牌放弃打出");
    }
}