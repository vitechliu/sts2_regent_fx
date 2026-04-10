using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using RegentFX.Scripts;

namespace RegentFX.Scripts.Patches;

/// <summary>
/// ZIndex调整补丁
/// 调整角色和背景的ZIndex，为星星环绕效果创造空间
/// </summary>
public static class ZIndexPatch
{
    /// <summary>
    /// 监听角色节点创建，调整ZIndex
    /// </summary>
    [HarmonyPatch(typeof(NCreature), "_Ready")]
    public static class NCreatureReadyPatch
    {
        public static void Postfix(NCreature __instance)
        {
            // 将角色的ZIndex设置为10（相对于父节点）
            __instance.ZIndex = 10;
            __instance.ZAsRelative = true;
            Entry.Logger.Info($"[ZIndexPatch] Set NCreature ZIndex to 10");
        }
    }

    /// <summary>
    /// 监听战斗场景创建，调整背景ZIndex
    /// </summary>
    [HarmonyPatch(typeof(NCombatRoom), "_Ready")]
    public static class NCombatRoomReadyPatch
    {
        public static void Postfix(NCombatRoom __instance)
        {
            // 将背景的ZIndex设置为-20（确保比角色低很多）
            if (__instance.BgContainer != null)
            {
                __instance.BgContainer.ZIndex = -20;
                Entry.Logger.Info($"[ZIndexPatch] Set BgContainer ZIndex to -20");
            }
            
            // 将CombatVfxContainer的ZIndex设置为0（在中间）
            if (__instance.CombatVfxContainer != null) {
                __instance.CombatVfxContainer.ZIndex = 0;
                Entry.Logger.Info($"[ZIndexPatch] Set CombatVfxContainer ZIndex to 0");
            }
        }
    }
}
