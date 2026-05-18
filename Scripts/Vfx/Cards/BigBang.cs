using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using RitsuFmodLite;

#pragma warning disable CS4014

namespace RegentFX.Scripts.Vfx.Cards;

[CardFx(typeof(MegaCrit.Sts2.Core.Models.Cards.BigBang))]
public class BigBang : CardFX {
    public override HoldingModes HoldingMode => HoldingModes.None;
    public override string? VfxScenePath => "res://RegentFX/scenes/vfx/big_bang.tscn";
}

[HarmonyPatch]
public static class BigBangPatch {
    [HarmonyPrefix]
    [HarmonyPatch(typeof(MegaCrit.Sts2.Core.Models.Cards.BigBang), "OnPlay")]
    public static void OnPlay(MegaCrit.Sts2.Core.Models.Cards.BigBang __instance) {
        if (!CardFX.IsTypeEnabled<BigBang>()) return;
        if (!LocalContext.IsMe(__instance.Owner)) return;
        MyOnPlay(__instance);
    }
    
    private static async Task MyOnPlay(MegaCrit.Sts2.Core.Models.Cards.BigBang card) {
        NCreature? ownerNode = NCombatRoom.Instance?.GetCreatureNode(card.Owner.Creature);
        if (ownerNode != null) {
            VFXUtil.PlaySimple(CardFX.FromCard(card).VfxScenePath, ownerNode.VfxSpawnPosition, 2f);
            VFXUtil.ShakeAfter(0.3f, ShakeStrength.Strong, ShakeDuration.Normal);
            await Cmd.Wait( .25f);
            FmodLite.Play("event:/RegentFx/sfx/big_bang_1");
            WorldEnvironmentUtil.TweenExposure(3f, .15f);
            await Cmd.Wait( .15f);
            WorldEnvironmentUtil.TweenExposure(1f, .4f);
        }
    }
}
