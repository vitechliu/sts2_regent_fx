using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using RegentFX.Scripts.Vfx;
using RegentFX.Scripts.Vfx.Powers;

namespace RegentFX.Scripts.Patches;

/// <summary>
/// 能力监听补丁
/// </summary>
[HarmonyPatch]
public static class PowerTimingPatch {
    [HarmonyPrefix]
    [HarmonyPatch(typeof(PowerModel), nameof(PowerModel.BeforeApplied))]
    public static void BeforeBeforeAppliedPatch(PowerModel __instance, Creature target, Decimal amount) {
        PowerFX? p = PowerFX.FromPower(__instance);
        if (p == null) return;
        p.BeforeBeforeApplied(target, amount);
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PowerModel), nameof(PowerModel.AfterRemoved))]
    public static void AfterAfterRemovedPatch(PowerModel __instance, Creature oldOwner) {
        PowerFX? p = PowerFX.FromPower(__instance);
        if (p == null) return;
        p.AfterAfterRemoved(oldOwner);
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PowerModel), nameof(PowerModel.SetAmount))]
    public static void AfterSetAmountPatch(PowerModel __instance, int amount) {
        PowerFX? p = PowerFX.FromPower(__instance);
        if (p == null) return;
        p.AfterSetAmount(amount);
    }
}
