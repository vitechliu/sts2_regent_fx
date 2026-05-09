using System.Reflection;
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
        if (!Setting.DevTestStartMode) return true;
        // 只拦截战斗开始时的初始手牌抽取（第一轮 + fromHandDraw）
        var combatState = Traverse.Create(player.Creature).Property("CombatState").GetValue();
        if (combatState == null) return true;
        int roundNumber = Traverse.Create(combatState).Property("RoundNumber").GetValue<int>();
        if (!fromHandDraw || roundNumber != 1) {
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

        var result = new List<CardModel>() {
            combatState.CreateCard<StrikeRegent>(player),
            combatState.CreateCard<MakeItSo>(player),
            // combatState.CreateCard<SevenStars>(player),
            combatState.CreateCard<FallingStar>(player),
            combatState.CreateCard<ParticleWall>(player),
            combatState.CreateCard<PillarOfCreation>(player),
            combatState.CreateCard<Glow>(player),
            combatState.CreateCard<Genesis>(player),
        };

        // 在这里定义你想要的固定手牌
        // 示例：生成 3张打击 + 2张防御 + 1张铁斩波
        // 你可以根据需求修改为任意牌

        
        foreach (var t in result) {
            await AddGeneratedCardToCombatDynamic(t, PileType.Hand, player);
        }

        await PlayerCmd.GainEnergy(100, player);
        await PlayerCmd.GainStars(20, player);

        Entry.Logger.Info($"[FixedOpeningHand] 已为玩家 {player.NetId} 生成 {result.Count} 张固定手牌");
        return result;
    }
    
    
  
    /// <summary>
    /// 动态调用 AddGeneratedCardToCombat，自动适配两个版本
    /// </summary>
    static async Task<CardPileAddResult> AddGeneratedCardToCombatDynamic(
        CardModel card,               // CardModel
        PileType pileType,           // PileType（可能是 enum / int）
        Player player)
    {
        Type targetType = typeof(CardPileCmd); // 替换成实际的第三方类

        // 查找名称匹配的方法
        MethodInfo[] methods = targetType.GetMethods(BindingFlags.Public | BindingFlags.Static);

        foreach (var method in methods) {
            if (method.Name != "AddGeneratedCardToCombat")
                continue;

            ParameterInfo[] parameters = method.GetParameters();

            // 版本 A: (CardModel, PileType, Player?, CardPilePosition)
            if (parameters.Length == 4 &&
                parameters[0].ParameterType == typeof(CardModel) &&
                parameters[2].ParameterType == typeof(Player))
            {
                return await (Task<CardPileAddResult>)method.Invoke(null, [
                    card,
                    pileType,
                    player, // 这里传入 Player? 或 null
                    CardPilePosition.Bottom
                ]);
            }

            // 版本 B: (CardModel, PileType, bool, CardPilePosition)
            if (parameters.Length == 4 &&
                parameters[0].ParameterType == typeof(CardModel) &&
                parameters[2].ParameterType == typeof(bool))
            {
                return await (Task<CardPileAddResult>)method.Invoke(null, [
                    card,
                    pileType,
                    true,
                    CardPilePosition.Bottom
                ]);
            }
        }

        throw new MissingMethodException("未找到匹配的 AddGeneratedCardToCombat 方法");
    }
}
