using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using RegentFX.ThirdParty.Audio;

namespace RegentFX.Scripts.Vfx.Cards;

//前置动画，无需迁移
[CardFx(typeof(MegaCrit.Sts2.Core.Models.Cards.Alignment))]
public class Alignment : CardFX {
    // -1 表示使用所有星星
    public override int StarCount => 3;
    
    public override string? VfxScenePath => "res://RegentFX/scenes/vfx/alignment.tscn";

    private List<Vector2> starPos = new() {
        new Vector2(-90f, -320f), 
        new Vector2(0f, -320f), 
        new Vector2(80f, -320f),
    };

    public override void OnStartHolding(Star star, int index) {
        if (Entry.StarEffectController == null) return;
        Star? lastStar = Entry.StarEffectController.Stars.FindLast(star1 => star != star1);
        if (lastStar != null) {
            star.ConnectTo(lastStar);
        }
        if (index == 0) {
            star.PulseMaxScale = 2.3f;
            star.PulseMinScale = 2.1f;
        } else if (index == 1) {
            star.PulseMaxScale = 1.6f;
            star.PulseMinScale = 1.4f;
        } else if (index == 2) {
            star.PulseMaxScale = 0.8f;
            star.PulseMinScale = 0.6f;
        }
    }

    public override Vector2 CalculateTargetPosition(Vector2 basePosition, int index, int totalCount) {
        return basePosition + (starPos[index] * 1.2f);
    }
}


[HarmonyPatch]
public static class AlignmentPatch {
    [HarmonyPrefix]
    [HarmonyPatch(typeof(MegaCrit.Sts2.Core.Models.Cards.Alignment), "OnPlay")]
    public static void OnPlay(MegaCrit.Sts2.Core.Models.Cards.Alignment __instance) {
        if (!CardFX.IsTypeEnabled<Alignment>()) return;
        if (!LocalContext.IsMe(__instance.Owner)) return;
        Creature owner = __instance.Owner.Creature;
        NCreature? ownerNode = NCombatRoom.Instance?.GetCreatureNode(owner);
        if (ownerNode == null) {
            Entry.Logger.Info("Could not get creature nodes for VFX");
        }
        else {
            FmodLite.Play("event:/RegentFx/sfx/alignment");
            VFXUtil.PlaySimple(CardFX.FromCard(__instance).VfxScenePath, ownerNode.VfxSpawnPosition, 2f);
        }
        Entry.StarEffectController?.OnPlayCard();
    }
}