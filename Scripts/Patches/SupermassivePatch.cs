using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using RegentFX.Scripts.Vfx;
using RegentFX.Scripts.Vfx.Cards;

namespace RegentFX.Scripts.Patches;

/// <summary>
/// 超质量体黑洞的战斗事件桥接。
/// </summary>
[HarmonyPatch]
public static class SupermassivePatch {
    [HarmonyPatch(typeof(NCombatRoom), nameof(NCombatRoom.CreateAllyNodes))]
    [HarmonyPostfix]
    public static void CreateAllyNodesPostfix() {
        EnsureController();
    }

    [HarmonyPatch(typeof(Hook), nameof(Hook.AfterCardGeneratedForCombat))]
    [HarmonyPostfix]
    public static void CardGeneratedPostfix(
        ICombatState combatState,
        CardModel card,
        Player? creator,
        ref Task __result) {
        if (creator == null || !LocalContext.IsMe(creator)) return;
        __result = NotifyAfterGenerated(__result, combatState, creator);
    }

    private static async Task NotifyAfterGenerated(Task original, ICombatState combatState, Player creator) {
        await original;
        if (creator.Creature.CombatState != combatState || creator.Creature.IsDead) return;
        EnsureController(creator)?.OnCardGenerated(creator);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(NCombatRoom), nameof(NCombatRoom.OnProceedButtonPressed))]
    public static void CombatEndPrefix() {
        ClearController();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(NCombatRoom), "_ExitTree")]
    public static void CombatRoomExitPrefix() {
        ClearController();
    }

    internal static SupermassiveController? EnsureController(Player? preferredPlayer = null) {
        if (!CardFX.IsTypeEnabled<Supermassive>()) return null;

        if (Entry.SupermassiveController != null) {
            if (GodotObject.IsInstanceValid(Entry.SupermassiveController)) {
                return Entry.SupermassiveController;
            }
            Entry.SupermassiveController = null;
        }

        NCombatRoom? room = NCombatRoom.Instance;
        Player? player = preferredPlayer ?? GetLocalPlayer();
        if (room == null || player?.PlayerCombatState == null || player.Creature.IsDead) return null;
        if (!LocalContext.IsMe(player)) return null;

        var playerNode = room.GetCreatureNode(player.Creature);
        if (playerNode == null || room.CombatVfxContainer == null) return null;

        var controller = new SupermassiveController();
        room.CombatVfxContainer.AddChildSafely(controller);
        controller.Initialize(playerNode, player);
        Entry.SupermassiveController = controller;
        return controller;
    }

    private static Player? GetLocalPlayer() {
        var players = CombatManager.Instance.DebugOnlyGetState()?.Players;
        return players?.FirstOrDefault(LocalContext.IsMe);
    }

    private static void ClearController() {
        SupermassiveController? controller = Entry.SupermassiveController;
        Entry.SupermassiveController = null;
        if (controller == null || !GodotObject.IsInstanceValid(controller)) return;
        controller.ClearImmediate();
        controller.QueueFreeSafely();
    }
}
