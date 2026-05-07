using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using RegentFx.Core.Audio;

#pragma warning disable CS4014

namespace RegentFX.Scripts.Vfx.Cards;

[CardFx(typeof(MegaCrit.Sts2.Core.Models.Cards.ParticleWall))]
public class ParticleWall: CardFX {
    public override string? VfxScenePath => "res://RegentFX/scenes/vfx/particle_wall.tscn";
}

[HarmonyPatch]
public static class ParticleWallPatch {
    [HarmonyPrefix]
    [HarmonyPatch(typeof(MegaCrit.Sts2.Core.Models.Cards.ParticleWall), "OnPlay")]
    public static void OnPlay(MegaCrit.Sts2.Core.Models.Cards.ParticleWall __instance) {
        if (!CardFX.IsTypeEnabled<ParticleWall>()) return;
        if (!LocalContext.IsMe(__instance.Owner)) return;
        MyOnPlay(__instance);
    }
    
    private static async Task MyOnPlay(
        MegaCrit.Sts2.Core.Models.Cards.ParticleWall card) {
        NCreature? ownerNode = NCombatRoom.Instance?.GetCreatureNode(card.Owner.Creature);
        if (ownerNode != null) {
            SimpleSfxUtil.Play("res://RegentFX/sfx/particle_wall.mp3");
            Creature owner = card.Owner.Creature;
            int xFac = VFXUtil.IsCharacterFacingRight(owner) ? 1 : -1;
            Vector2 pos = ownerNode.VfxSpawnPosition +
                          new Vector2(180f * xFac, 110f);
            Node2D? d = VFXUtil.PlaySimple(CardFX.FromCard(card).VfxScenePath, pos, 3f);
            if (d != null) {
                d.Scale *= new Vector2(xFac, 1);
                d.Scale *= 0.7f;
            }
        }
    }
}
