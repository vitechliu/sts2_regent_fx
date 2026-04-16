using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;
using RegentFx.Core.Audio;

#pragma warning disable CS4014

namespace RegentFX.Scripts.Vfx.Cards;

public class DyingStar : CardFX {
    public override int StarCount => 3;

    // 更靠左上的位置
    public override Vector2 TargetOffset => new(-200f, -450f);

    public override string VfxScenePath => "res://RegentFX/scenes/dying_star.tscn";
    public override string HitSfxPath => "res://RegentFX/sfx/common_hold_4.mp3";
    public override string SecondarySfxPath => "res://RegentFX/sfx/dying_star.mp3";
    public override float VfxClearDelay => 3f;
    public override bool HasExposureEffect => true;
    public override float ExposureInDuration => 0.2f;
    public override float ExposureOutDuration => 0.3f;

    private List<Vector2> starPos = new() {
        new Vector2(0f, 0f),
        new Vector2(13f, -24f),
        new Vector2(30f, -5f),
    };

    public override Vector2 CalculateTargetPosition(Vector2 basePosition, int index, int totalCount) {
        return basePosition + TargetOffset + (starPos[index] * 2.5f);
    }

    public override void OnStartHolding(Star star, int index) {
        star.PulseMinScale *= 2f;
        star.PulseMaxScale *= 1.8f;
    }
}

[HarmonyPatch]
public static class DyingStarPatch {

    [HarmonyPrefix]
    [HarmonyPatch(typeof(MegaCrit.Sts2.Core.Models.Cards.DyingStar), "OnPlay")]
    public static bool OnPlay(
        MegaCrit.Sts2.Core.Models.Cards.DyingStar __instance,
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ref Task __result) {
        __result = MyOnPlay(__instance, choiceContext, cardPlay);
        return false;
    }

    private static async Task MyOnPlay(
        MegaCrit.Sts2.Core.Models.Cards.DyingStar card,
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay) {
        await CreatureCmd.TriggerAnim(card.Owner.Creature, "Cast", card.Owner.Character.CastAnimDelay);
        IReadOnlyList<Creature> enemies = card.CombatState.HittableEnemies;
        var config = CardFX.FromCard(card)!;
        var cmd = DamageCmd.Attack(card.DynamicVars.Damage.BaseValue)
            .FromCard(card)
            .TargetingAllOpponents(card.CombatState)
            .WithHitFx("vfx/vfx_starry_impact")
            .SpawningHitVfxOnEachCreature()
            .BeforeDamage(async delegate {
                await CardVfxUtil.PlayAoeVfx(config, card.Owner.Creature, card.CombatState, nameof(DyingStar));
            });
        cmd._attackerAnimName = null;
        await cmd.Execute(choiceContext);
        foreach (Creature enemy in enemies) {
            await PowerCmd.Apply<DyingStarPower>(enemy,
                card.DynamicVars["StrengthLoss"].BaseValue, card.Owner.Creature, card);
        }
    }
}
