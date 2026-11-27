using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using RegentFX.Scripts.Vfx;

namespace RegentFX.Scripts.Patches;

[HarmonyPatch]
public static class RegentStarRingPatch {
    [HarmonyPostfix]
    [HarmonyPatch(typeof(NCombatRoom), nameof(NCombatRoom._Ready))]
    public static void RoomReady(NCombatRoom __instance) {
        var player = CombatManager.Instance.DebugOnlyGetState()?.Players.FirstOrDefault(LocalContext.IsMe);
        EnsureRing(__instance, player);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerCombatState), nameof(PlayerCombatState.Stars), MethodType.Setter)]
    public static void StarsChanged(PlayerCombatState __instance, int value) {
        var player = CombatManager.Instance.DebugOnlyGetState()?.Players.FirstOrDefault(p => p.PlayerCombatState == __instance);
        if (NCombatRoom.Instance is { } room) EnsureRing(room, player)?.SetStarCount(value);
    }

    private static StarRingController? EnsureRing(NCombatRoom room, Player? player) {
        if (player?.Character is not Regent || !LocalContext.IsMe(player) || player.Creature.IsDead) return null;
        var parent = room.BackCombatVfxContainer;
        if (parent == null || !parent.IsNodeReady() || room.IsQueuedForDeletion()) return null;
        var current = Entry.StarRingController;
        if (GodotObject.IsInstanceValid(current) && !current.IsQueuedForDeletion() && current.GetParent() == parent)
            return current;

        var playerNode = room.GetCreatureNode(player.Creature);
        if (playerNode == null || !playerNode.IsNodeReady()) return null;
        var ring = new StarRingController();
        parent.AddChildSafely(ring);
        ring.Initialize(playerNode, player);
        Entry.StarRingController = ring;
        Entry.StarEffectController = ring.Preview;
        ring.SetStarCount(player.PlayerCombatState?.Stars ?? 0);
        return ring;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(NCombatRoom), nameof(NCombatRoom.OnProceedButtonPressed))]
    public static void CombatEnd(NCombatRoom __instance) {
        var ring = Entry.StarRingController;
        if (GodotObject.IsInstanceValid(ring) && __instance.IsAncestorOf(ring)) ring.QueueFreeSafely();
    }
}
