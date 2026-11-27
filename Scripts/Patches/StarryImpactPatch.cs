using Godot;
using HarmonyLib;

using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace RegentFX.Scripts.Patches;

[HarmonyPatch]
public static class StarryImpactPatch {
    [HarmonyPatch(typeof(NStarryImpactVfx), nameof(NStarryImpactVfx.PlaySequence))]
    [HarmonyPrefix]
    public static bool p(NStarryImpactVfx __instance, ref Task __result) {
        if (!VFXUtil.StarryImpactNodes.Remove(__instance.GetInstanceId())) return true;
        __result = MyTask(__instance);
        return false;
    }


    private static HashSet<string> exceptNodes = new() {
        "vfx_starry_impact_smoke_flipbook",
        "vfx_outward_screen_distortion",
    };
    static async Task MyTask(NStarryImpactVfx node) {
        node._cts = new CancellationTokenSource();
        foreach (GpuParticles2D p in node._particles) {
            // Entry.Logger.Info("Name:" + p.Name);
            if (exceptNodes.Contains(p.Name)) {
                // Entry.Logger.Info("Dispose:" + p.Name);
                p.QueueFreeSafely();
            }
            else {
                // Entry.Logger.Info("Restart:" + p.Name);
                ParticleProcessMaterial pm = (ParticleProcessMaterial)p.ProcessMaterial.Duplicate();
                pm.Scale *= 0.2f;
                p.ProcessMaterial = pm;
                p.Restart();
            }
        }
        await VFXUtil.Wait(2f, node._cts.Token);
        if (node != null && GodotObject.IsInstanceValid(node)) node.QueueFreeSafely();
    }
}
