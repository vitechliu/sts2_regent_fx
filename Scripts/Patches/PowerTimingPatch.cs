using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using RegentFX.Scripts.Vfx;

namespace RegentFX.Scripts.Patches;

/// <summary>
/// 能力监听补丁
/// </summary>
[HarmonyPatch]
public static class PowerTimingPatch {
    [HarmonyPrefix]
    [HarmonyPatch(typeof(PowerModel), nameof(PowerModel.BeforeApplied))]
    public static void BeforeBeforeAppliedPatch(PowerModel __instance, Creature target) {
        // Entry.Logger.Info("BeforeBeforeApplied:" + __instance.GetType().Name);

        if (__instance is BlackHolePower bhp) {
            // Entry.Logger.Info("Blackhole");
            Blackhole.Create(target);
        }
    }
}
