using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using RitsuFmodLite;
using Godot;

#pragma warning disable CS4014

namespace RegentFX.Scripts.Vfx.Cards;

[CardFx(typeof(MegaCrit.Sts2.Core.Models.Cards.Resonance))]
public class Resonance : CardFX {
    public override int StarCount => 3;

    private List<Vector2> starPos = new() {
        new Vector2(0, -5f),
        new Vector2(6f, 5f),
        new Vector2(-6f, 5f),
    };

    public override Vector2 CalculateTargetPosition(Vector2 basePosition, int index, int totalCount) {
        Vector2 target = starPos[index];
        return basePosition + new Vector2(0f, -100f) + target;
    }
    public override void OnStartHolding(Star star, int index) {
        star.ChangeColorTo(new Color(14.551f, 14.551f, 0.0f));
    }
    public override string? VfxScenePath => "res://RegentFX/scenes/vfx/resonance.tscn";
}

[HarmonyPatch]
public static class ResonancePatch {
    [HarmonyPrefix]
    [HarmonyPatch(typeof(MegaCrit.Sts2.Core.Models.Cards.Resonance), "OnPlay")]
    public static void OnPlay(MegaCrit.Sts2.Core.Models.Cards.Resonance __instance) {
        if (!CardFX.IsTypeEnabled<Resonance>()) return;
        if (!LocalContext.IsMe(__instance.Owner)) return;
        MyOnPlay(__instance);
    }
    
    private static async Task MyOnPlay(
        MegaCrit.Sts2.Core.Models.Cards.Resonance card) {
        NCreature? ownerNode = NCombatRoom.Instance?.GetCreatureNode(card.Owner.Creature);
        if (ownerNode != null) {
            //todo 音效
            // FmodLite.Play("event:/RegentFx/sfx/glow");
            await VFXUtil.Wait( .1f);
            VFXUtil.PlaySimpleBack(CardFX.FromCard(card).VfxScenePath, ownerNode.VfxSpawnPosition, 2f);
            await VFXUtil.Wait( .1f);
            WorldEnvironmentUtil.TweenExposure(1.5f, .1f);
            await VFXUtil.Wait( .1f);
            WorldEnvironmentUtil.TweenExposure(1f, .3f);
        }
        Entry.StarEffectController?.OnPlayCard();
    }
}
