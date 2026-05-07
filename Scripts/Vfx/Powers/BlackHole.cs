using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

#pragma warning disable CS4014

namespace RegentFX.Scripts.Vfx.Powers;

[PowerFx(typeof(BlackHolePower))]
[HarmonyPatch]
public class BlackHole : PowerFX {
    public override string? VfxScenePath => Blackhole.VfxScenePath;

    public override void BeforeBeforeApplied(Creature target, Decimal amount) {
        Blackhole? b = Blackhole.Create(target);
        if (b != null) SetSize(b, amount);
    }

    private static void SetSize(Blackhole blackHoleNode, Decimal amount) {
        var size = Mathf.Min(1.6f, ((float)amount - 3) * 0.05f + 1);
        // Entry.Logger.Info("[BlackHoleSetSize]" + size + " amount:" + amount);
        blackHoleNode.Scale = Vector2.One * size;
    }
    
    public override void AfterAfterRemoved(Creature target) {
        if (Blackhole.Blackholes.TryGetValue(target, out var blackhole)) {
            blackhole.QueueFreeSafely();
            Blackhole.Blackholes.Remove(target);
        }
    }

    public override void AfterSetAmount(Decimal amount) {
        var c = power?.Owner;
        if (c != null && Blackhole.Blackholes.TryGetValue(c, out var blackhole)) {
            SetSize(blackhole, amount);
        }
    }
}

[HarmonyPatch]
public static class BlackHolePatch {
    [HarmonyPostfix]
    [HarmonyPatch(typeof(BlackHolePower), nameof(BlackHolePower.DealDamageToAllEnemies))]
    static void BlackholeBurst(BlackHolePower __instance) {
        if (!PowerFX.IsTypeEnabled<BlackHole>()) return;
        if (!LocalContext.IsMe(__instance.Owner)) return;
        if (Blackhole.Blackholes.TryGetValue(__instance.Owner, out var blackhole)) {
            // Entry.Logger.Info("Blackhole Burst");
            blackhole.Burst();
            NGame.Instance?.ScreenShake(ShakeStrength.Weak, ShakeDuration.Short);
        }
    }
}
