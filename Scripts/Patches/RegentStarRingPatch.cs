using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using RegentFX.Scripts;
using RegentFX.Scripts.Vfx;

namespace RegentFX.Scripts.Patches;

/// <summary>
/// 储君环绕星星系统补丁
/// 在战斗中为储君角色添加环绕星星特效（本地玩家单例）
/// </summary>
public static class RegentStarRingPatch {
    static void EnsureControllers(Player? player) {
        if (Entry.StarRingController != null) {
            if (GodotObject.IsInstanceValid(Entry.StarRingController)) {
                return;
            }
        }
        if (player != null && player.Character is Regent && LocalContext.IsMe(player)) {
            SetupStarRingForLocalPlayer(player);
        }
    }
    
    /// <summary>
    /// 监听玩家进入战斗，为储君创建星星环绕系统
    /// </summary>
    [HarmonyPatch(typeof(NCombatRoom), nameof(NCombatRoom.CreateAllyNodes))]
    public static class CreateAllyNodesPatch {
        public static void Postfix() {
            Entry.Logger.Info("CreateAllyNodesPatch_PostFix");
            if (NCombatRoom.Instance == null) return;
            // 获取所有玩家
            var players = CombatManager.Instance.DebugOnlyGetState()?.Players;
            if (players == null) return;
            Entry.Logger.Info("CreateAllyNodesPatch_PostFix2");

            // 找到本地储君玩家
            foreach (var player in players) {
                EnsureControllers(player);
            }
        }
    }

    /// <summary>
    /// 监听Star数量变化，更新环绕星星
    /// </summary>
    [HarmonyPatch(typeof(PlayerCombatState), nameof(PlayerCombatState.Stars), MethodType.Setter)]
    public static class StarsChangedPatch {
        public static void Postfix(PlayerCombatState __instance, int value) {
            // 只处理本地储君玩家
            var player = GetLocalRegentPlayer();
            if (player?.PlayerCombatState != __instance) return;
            EnsureControllers(player);
            Entry.StarRingController?.SetStarCount(value);
        }

        private static Player? GetLocalRegentPlayer() {
            var players = CombatManager.Instance.DebugOnlyGetState()?.Players;
            if (players == null) return null;

            foreach (var p in players) {
                if (p.Character is Regent && LocalContext.IsMe(p)) {
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
    public static class CombatEndPatch {
        public static void Prefix() {
            ClearStarRing();
        }
    }

    /// <summary>
    /// 为本地储君玩家设置星星环绕
    /// </summary>
    private static void SetupStarRingForLocalPlayer(Player player) {
        if (NCombatRoom.Instance == null) return;

        var playerNode = NCombatRoom.Instance.GetCreatureNode(player.Creature);
        if (playerNode == null) {
            Entry.Logger.Warn("[RegentStarRing] Could not find player node");
            return;
        }

        // 如果已存在，先移除旧的
        ClearStarRing();

        // 创建新的星星控制器
        var controller = new StarRingController();
        NCombatRoom.Instance.CombatVfxContainer.AddChild(controller);
        controller.Initialize(playerNode, player);

        // 设置初始星星数量
        int starCount = player.PlayerCombatState?.Stars ?? 0;
        controller.SetStarCount(starCount);

        // 设置单例引用
        Entry.StarRingController = controller;

        // 同时初始化特效控制器
        var effectController = new StarEffectController();
        NCombatRoom.Instance.CombatVfxContainer.AddChild(effectController);
        effectController.Initialize(playerNode, controller);
        Entry.StarEffectController = effectController;
    }

    /// <summary>
    /// 清除星星环绕
    /// </summary>
    private static void ClearStarRing() {
        if (Entry.StarRingController != null) {
            Entry.StarRingController.ClearAllStars(animate: true);
            Entry.StarRingController.QueueFree();
            Entry.StarRingController = null;
        }

        if (Entry.StarEffectController != null) {
            Entry.StarEffectController.ClearAllEffects();
            Entry.StarEffectController.QueueFree();
            Entry.StarEffectController = null;
        }
    }
}