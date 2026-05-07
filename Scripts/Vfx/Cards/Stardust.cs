using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;

namespace RegentFX.Scripts.Vfx.Cards;

/// <summary>
/// Stardust 卡牌特效 todo
/// </summary>
[CardFx(typeof(MegaCrit.Sts2.Core.Models.Cards.Stardust))]
public class Stardust : CardFX {
    // -1 表示使用所有星星
    public override int StarCount => -1;
    
    public override Vector2 CalculateTargetPosition(Vector2 basePosition, int index, int totalCount) {
        Vector2 randVec = new Vector2((float)GD.RandRange(-40f, 40f), (float)GD.RandRange(-40f, 40f));
        return basePosition + TargetOffset + randVec;
    }
}


[HarmonyPatch]
public static class StardustPatch {
    [HarmonyPrefix]
    [HarmonyPatch(typeof(MegaCrit.Sts2.Core.Models.Cards.Stardust), "OnPlay")]
    public static void OnPlay(MegaCrit.Sts2.Core.Models.Cards.Stardust __instance) {
        if (!CardFX.IsTypeEnabled<Stardust>()) return;
        if (!LocalContext.IsMe(__instance.Owner)) return;
        Entry.StarEffectController?.OnPlayCard();
    }
}