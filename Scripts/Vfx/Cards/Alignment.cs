using Godot;
using HarmonyLib;

namespace RegentFX.Scripts.Vfx.Cards;

[CardFx(typeof(MegaCrit.Sts2.Core.Models.Cards.Alignment))]
public class Alignment : CardFX {
    // -1 表示使用所有星星
    public override int StarCount => 3;
    // 更高的位置

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
    public static void OnPlay() {
        Entry.StarEffectController?.OnPlayCard();
    }
}