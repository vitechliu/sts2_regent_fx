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
        return basePosition + TargetOffset + VFXUtil.RandVec2(40f);
    }
    public override bool ShouldDisableRegentWeaponAttack => false;
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