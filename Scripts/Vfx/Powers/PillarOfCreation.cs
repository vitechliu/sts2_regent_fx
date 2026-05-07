using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

#pragma warning disable CS4014

namespace RegentFX.Scripts.Vfx.Powers;

[PowerFx(typeof(PillarOfCreationPower))]
[HarmonyPatch]
public class PillarOfCreation : PowerFX {
    public override string? VfxScenePath => Pillar.VfxScenePath;

    public override void BeforeBeforeApplied(Creature target, Decimal amount) {
        NCreature ownerNode = NCombatRoom.Instance?.GetCreatureNode(target);
        if (ownerNode == null) return;
        int xFac = VFXUtil.IsCharacterFacingRight(target) ? 1 : -1;
        Vector2 pos = ownerNode.GlobalPosition + new Vector2(-130f * xFac, -50f);
        Pillar? b = Pillar.Spawn(target, pos);
        if (b != null) SetSize(b, amount);
    }

    private static void SetSize(Pillar PillarOfCreationNode, Decimal amount) {
        var size = Mathf.Min(1.3f, ((float)amount - 3) * 0.04f + 1);
        // Entry.Logger.Info("[PillarOfCreationSetSize]" + size + " amount:" + amount);
        PillarOfCreationNode.Scale = Vector2.One * size;
    }
    
    public override void AfterAfterRemoved(Creature target) {
        if (Pillar.Pillars.TryGetValue(target, out var PillarOfCreation)) {
            PillarOfCreation.QueueFreeSafely();
            Pillar.Pillars.Remove(target);
        }
    }

    public override void AfterSetAmount(Decimal amount) {
        var c = power?.Owner;
        if (c != null && Pillar.Pillars.TryGetValue(c, out var PillarOfCreation)) {
            SetSize(PillarOfCreation, amount);
        }
    }
}

[HarmonyPatch]
public static class PillarOfCreationPatch {
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PillarOfCreationPower), nameof(PillarOfCreationPower.AfterCardGeneratedForCombat))]
    static void PillarOfCreationActivate(PillarOfCreationPower __instance) {
        if (!PowerFX.IsTypeEnabled<PillarOfCreation>()) return;
        if (!LocalContext.IsMe(__instance.Owner)) return;
        if (Pillar.Pillars.TryGetValue(__instance.Owner, out var PillarOfCreation)) {
            Entry.Logger.Info("PillarOfCreation Activate");
            PillarOfCreation.Activate();
            // NGame.Instance?.ScreenShake(ShakeStrength.Weak, ShakeDuration.Short);
        }
    }
}
