using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using RegentFX.Scripts;
using RegentFX.Vfx;

namespace RegentFX.Scripts.Patches;

/// <summary>
/// 储君环绕星星系统补丁
/// 在战斗中为储君角色添加环绕星星特效
/// </summary>
public static class RegentStarRingPatch
{
    // 存储每个玩家的星星控制器
    private static System.Collections.Generic.Dictionary<Player, StarRingController> _playerStarRings = new();

    /// <summary>
    /// 监听玩家进入战斗，为储君创建星星环绕系统
    /// </summary>
    [HarmonyPatch(typeof(NCombatRoom), nameof(NCombatRoom.CreateAllyNodes))]
    public static class CreateAllyNodesPatch
    {
        public static void Postfix()
        {
            // Entry.Logger.Info("[RegentStarRing] CreateAllyNodes called, setting up star rings for Regent players");

            if (NCombatRoom.Instance == null) return;

            // 获取所有玩家
            var players = CombatManager.Instance.DebugOnlyGetState()?.Players;
            if (players == null) return;

            // 遍历所有玩家
            foreach (var player in players)
            {
                // 检查是否是储君角色
                if (player.Character is Regent)
                {
                    SetupStarRingForPlayer(player);
                }
            }
        }
    }

    /// <summary>
    /// 监听Star数量变化，更新环绕星星
    /// </summary>
    [HarmonyPatch(typeof(PlayerCombatState), nameof(PlayerCombatState.Stars), MethodType.Setter)]
    public static class StarsChangedPatch
    {
        public static void Postfix(PlayerCombatState __instance, int value)
        {
            // 通过CombatManager获取玩家
            var player = GetPlayerFromCombatState(__instance);
            if (player?.Character is not Regent) return;

            // 直接调用更新（不使用CallDeferred避免类型转换问题）
            UpdateStarRingForPlayer(player, value);
        }

        private static Player? GetPlayerFromCombatState(PlayerCombatState state)
        {
            // 通过遍历所有玩家找到对应的Player
            var players = CombatManager.Instance.DebugOnlyGetState()?.Players;
            if (players == null) return null;

            foreach (var p in players)
            {
                if (p.PlayerCombatState == state)
                {
                    return p;
                }
            }
            return null;
        }
    }

    /// <summary>
    /// 监听战斗结束，清理星星
    /// </summary>
    [HarmonyPatch(typeof(NCombatRoom), nameof(NCombatRoom.OnProceedButtonPressed))]
    public static class CombatEndPatch
    {
        public static void Prefix()
        {
            // Entry.Logger.Info("[RegentStarRing] Combat ending, clearing star rings");
            ClearAllStarRings();
        }
    }

    /// <summary>
    /// 为储君玩家设置星星环绕
    /// </summary>
    private static void SetupStarRingForPlayer(Player player)
    {
        if (NCombatRoom.Instance == null) return;

        var playerNode = NCombatRoom.Instance.GetCreatureNode(player.Creature);
        if (playerNode == null)
        {
            Entry.Logger.Warn("[RegentStarRing] Could not find player node");
            return;
        }

        // 如果已存在，先移除旧的
        if (_playerStarRings.TryGetValue(player, out var existingController))
        {
            existingController?.QueueFree();
            _playerStarRings.Remove(player);
        }

        // 创建新的星星控制器
        var controller = new StarRingController();

        // 将控制器添加到CombatVfxContainer（Z=-9），这样ZIndex范围更大
        // 既能显示在角色前面，也不会被背景挡住
        NCombatRoom.Instance?.CombatVfxContainer.AddChild(controller);
        controller.Initialize(playerNode);

        // 设置初始星星数量
        int starCount = player.PlayerCombatState?.Stars ?? 0;
        controller.SetStarCount(starCount);

        _playerStarRings[player] = controller;

        // Entry.Logger.Info($"[RegentStarRing] Star ring created for Regent player with {starCount} stars");
    }

    /// <summary>
    /// 更新玩家的星星环绕数量
    /// </summary>
    private static void UpdateStarRingForPlayer(Player player, int starCount)
    {
        if (_playerStarRings.TryGetValue(player, out var controller) && controller != null)
        {
            controller.SetStarCount(starCount);
            // Entry.Logger.Info($"[RegentStarRing] Star count updated to {starCount}");
        }
        else if (player.Character is Regent)
        {
            // 如果控制器不存在但玩家是储君，重新创建
            SetupStarRingForPlayer(player);
        }
    }

    /// <summary>
    /// 清除所有星星环绕
    /// </summary>
    private static void ClearAllStarRings()
    {
        foreach (var controller in _playerStarRings.Values)
        {
            controller?.ClearAllStars(animate: true);
            controller?.QueueFree();
        }

        _playerStarRings.Clear();
    }
}
