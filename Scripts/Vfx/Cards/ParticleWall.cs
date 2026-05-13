using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using STS2RitsuLib.Audio;

#pragma warning disable CS4014

namespace RegentFX.Scripts.Vfx.Cards;

[CardFx(typeof(MegaCrit.Sts2.Core.Models.Cards.ParticleWall))]
public class ParticleWall: CardFX {
    public override string? VfxScenePath => "res://RegentFX/scenes/vfx/particle_wall.tscn";
    public override int StarCount => 2;

    private List<Vector2> starPos = new() {
        new Vector2(240f, -170f),
        new Vector2(140f, -60f),
    };

    public override Vector2 CalculateTargetPosition(Vector2 basePosition, int index, int totalCount) {
        Vector2 target = starPos[index];
        if (!IsCharacterFacingRight) target.X *= -1;
        return basePosition + target;
    }
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
            Sts2SfxAlignedFmod.PlayOneShot("event:/RegentFx/sfx/particle_wall");
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
        Entry.StarEffectController?.OnPlayCard();
    }
}
