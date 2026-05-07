using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using RegentFx.Core.Audio;

#pragma warning disable CS4014

namespace RegentFX.Scripts.Vfx.Cards;

[CardFx(typeof(MegaCrit.Sts2.Core.Models.Cards.Glow))]
public class Glow : CardFX {
    public override string? VfxScenePath => "res://RegentFX/scenes/vfx/glow.tscn";
}

[HarmonyPatch]
public static class GlowPatch {
    [HarmonyPrefix]
    [HarmonyPatch(typeof(MegaCrit.Sts2.Core.Models.Cards.Glow), "OnPlay")]
    public static void OnPlay(MegaCrit.Sts2.Core.Models.Cards.Glow __instance) {
        if (!CardFX.IsTypeEnabled<Glow>()) return;
        if (!LocalContext.IsMe(__instance.Owner)) return;
        MyOnPlay(__instance);
    }
    
    private static async Task MyOnPlay(
        MegaCrit.Sts2.Core.Models.Cards.Glow card) {
        NCreature? ownerNode = NCombatRoom.Instance?.GetCreatureNode(card.Owner.Creature);
        if (ownerNode != null) {
            SimpleSfxUtil.Play("res://RegentFX/sfx/glow.mp3");
            VFXUtil.PlaySimple(CardFX.FromCard(card).VfxScenePath, ownerNode.VfxSpawnPosition, 2f);
            await Cmd.Wait( .1f);
            WorldEnvironmentUtil.TweenExposure(2.2f, .1f);
            await Cmd.Wait( .1f);
            WorldEnvironmentUtil.TweenExposure(1f, .3f);
        }
    }
}
