using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace RegentFX.Scripts.Patches;

/// <summary>
/// 战斗开始时固定手牌Patch
/// 无视默认抽牌流程，将初始手牌替换为特定生成的几张牌
/// </summary>
[HarmonyPatch]
public static class FixedOpeningHandPatch {

    /// <summary>
    /// 拦截 CardPileCmd.Draw，在第一轮抽初始手牌时替换为生成特定牌
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.Draw), new[] {
        typeof(PlayerChoiceContext), typeof(decimal), typeof(Player), typeof(bool)
    })]
    public static bool DrawPrefix(PlayerChoiceContext choiceContext, decimal count, Player player, bool fromHandDraw, ref Task<IEnumerable<CardModel>> __result) {
        // 只拦截战斗开始时的初始手牌抽取（第一轮 + fromHandDraw）
        if (!fromHandDraw || player.Creature.CombatState?.RoundNumber != 1) {
            return true; // 执行默认逻辑
        }

        Entry.Logger.Info($"[FixedOpeningHand] 拦截初始抽牌，为玩家 {player.NetId} 生成固定手牌");
        __result = GenerateFixedHand(choiceContext, player);
        return false; // 跳过默认抽牌逻辑
    }

    /// <summary>
    /// 生成固定手牌并添加到手牌区
    /// </summary>
    private static async Task<IEnumerable<CardModel>> GenerateFixedHand(PlayerChoiceContext choiceContext, Player player) {
        var combatState = player.Creature.CombatState;
        if (combatState == null) {
            Entry.Logger.Warn("[FixedOpeningHand] CombatState 为空，无法生成手牌");
            return Enumerable.Empty<CardModel>();
        }

        var result = new List<CardModel>();

        // 在这里定义你想要的固定手牌
        // 示例：生成 3张打击 + 2张防御 + 1张铁斩波
        // 你可以根据需求修改为任意牌

        for (int i = 0; i < 3; i++) {
            var strike = combatState.CreateCard<StrikeIronclad>(player);
            await CardPileCmd.AddGeneratedCardToCombat(strike, PileType.Hand, addedByPlayer: true);
            result.Add(strike);
        }

        for (int i = 0; i < 2; i++) {
            var defend = combatState.CreateCard<DefendIronclad>(player);
            await CardPileCmd.AddGeneratedCardToCombat(defend, PileType.Hand, addedByPlayer: true);
            result.Add(defend);
        }

        var ironWave = combatState.CreateCard<IronWave>(player);
        await CardPileCmd.AddGeneratedCardToCombat(ironWave, PileType.Hand, addedByPlayer: true);
        result.Add(ironWave);

        Entry.Logger.Info($"[FixedOpeningHand] 已为玩家 {player.NetId} 生成 {result.Count} 张固定手牌");
        return result;
    }
}
