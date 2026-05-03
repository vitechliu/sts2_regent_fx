using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using RegentFx.Core.Audio;

#pragma warning disable CS4014

namespace RegentFX.Scripts.Vfx.Powers;

[PowerFx(typeof(MegaCrit.Sts2.Core.Models.Powers.BlackHolePower))]
[HarmonyPatch]
public class BlackHole : PowerFX {
    
    
}

[HarmonyPatch]
public static class BlackHolePatch {
    [HarmonyPostfix]
    [HarmonyPatch(typeof(BlackHolePower), nameof(BlackHolePower.DealDamageToAllEnemies))]
    static void BlackholeBurst(BlackHolePower __instance) {
        if (Blackhole.Blackholes.TryGetValue(__instance.Owner, out var blackhole)) {
            // Entry.Logger.Info("Blackhole Burst");
            blackhole.Burst();
            NGame.Instance?.ScreenShake(ShakeStrength.Weak, ShakeDuration.Short);
        }
    }

}
