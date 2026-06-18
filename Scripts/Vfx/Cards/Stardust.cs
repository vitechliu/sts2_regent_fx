using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

namespace RegentFX.Scripts.Vfx.Cards;

/// <summary>
/// Stardust 卡牌特效 todo
/// </summary>
[CardFx(typeof(MegaCrit.Sts2.Core.Models.Cards.Stardust))]
public class Stardust : CardFX {
    public override HoldingModes HoldingMode => HoldingModes.BorrowAll;

    public override string? VfxScenePath => "res://RegentFX/scenes/vfx/star_strike.tscn";

    public override Vector2 CalculateTargetPosition(Vector2 basePosition, int index, int totalCount) {
        return basePosition + TargetOffset + VFXUtil.RandVec2(40f);
    }
    public override bool ShouldDisableRegentWeaponSFX => false;
    public override bool RemoveHitFx => true;

    public override bool UseV2Patch => true;
    public override bool HasOnBeforeDamageTargeted => true;
    
    public override void OnStartHolding(Star star, int index) {
        star.ChangeColorTo(new Color(14.551f, 14.551f, 0.0f)); //yellow
    }
    
    public override async Task OnBeforeDamage(AttackCommand command, IReadOnlyList<Creature> targets) {
        // Entry.Logger.Info("StarDustDamage");
        Creature? owner = card?.Owner.Creature;
        if (targets.Count != 1) return;
        Creature target = targets.First();
        if (owner == null || target == null) return;
        Vector2? starPosD = Entry.StarEffectController?.PopStar(this);
        Vector2 starPos;
        if (!starPosD.HasValue) {
            NCreature? ownerNode = NCombatRoom.Instance?.GetCreatureNode(owner);
            Vector2 ownerPos = ownerNode?.VfxSpawnPosition ?? Vector2.Zero;
            starPos = ownerPos + new Vector2(0f, -400f);
        }
        else {
            starPos = starPosD.Value + new Vector2(0f, -200f);
        }
        starPos += VFXUtil.RandVec2(200f);
        if (TestMode.IsOn) return;
        NCreature? targetNode = NCombatRoom.Instance?.GetCreatureNode(target);
        if (targetNode == null) return;
        Vector2 targetPos = targetNode.VfxSpawnPosition;
        StardustVfx.PlayStardust(starPos, targetPos);
        _ = TaskHelper.RunSafely(OnHit(targetPos, target));
        await VFXUtil.Wait(0.1f);
    }

    async Task OnHit(Vector2 targetPos, Creature target) {
        await VFXUtil.Wait(.35f);
        var node = VFXUtil.PlaySimple(VfxScenePath, targetPos);
        VFXUtil.ReplayAllParticles(node);
        VfxCmd.PlayOnCreatureCenter(target, "vfx/vfx_starry_impact");
    }
    
}
//
//
// [HarmonyPatch]
// public static class StardustPatch {
//     [HarmonyPrefix]
//     [HarmonyPatch(typeof(MegaCrit.Sts2.Core.Models.Cards.Stardust), "OnPlay")]
//     public static void OnPlay(MegaCrit.Sts2.Core.Models.Cards.Stardust __instance) {
//         if (!CardFX.IsTypeEnabled<Stardust>()) return;
//         if (!LocalContext.IsMe(__instance.Owner)) return;
//         Entry.StarEffectController?.OnPlayCard();
//     }
// }