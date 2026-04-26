using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
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

/// <summary>
/// FallingStar 卡牌特效
/// </summary>
[CardFx(typeof(MegaCrit.Sts2.Core.Models.Cards.FallingStar))]
public class FallingStar : CardFX {
    public override int StarCount => 2;

    // 更靠左上的位置
    public override Vector2 TargetOffset => new(-200f, -450f);

    public override string VfxScenePath => "res://RegentFX/scenes/falling_star.tscn";
    public override string HitSfxPath => "res://RegentFX/sfx/falling_star.mp3";
    public override bool HasExposureEffect => true;
    public override float ExposureInDuration => 0.1f;
    public override float ExposureOutDuration => 0.5f;

    private List<Vector2> starPos = new() {
        new Vector2(0f, 0f),
        new Vector2(30f, -50f),
    };

    public override Vector2 CalculateTargetPosition(Vector2 basePosition, int index, int totalCount) {
        return basePosition + TargetOffset + (starPos[index] * 1.2f);
    }

    public override void OnStartHolding(Star star, int index) {
        if (index == 0) {
            star.ChangeColorTo(new Color(14.551f, 14.551f, 0.0f)); //yellow
        }
        if (index == 1) {
            star.ChangeColorTo(new Color(14.551f, 0.683f, 9.982f)); //pink
        }
    }
}

[HarmonyPatch]
public static class FallingStarPatch {

    [HarmonyPrefix]
    [HarmonyPatch(typeof(MegaCrit.Sts2.Core.Models.Cards.FallingStar), "OnPlay")]
    public static bool OnPlay(
        MegaCrit.Sts2.Core.Models.Cards.FallingStar __instance,
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ref Task __result) {
        if (!LocalContext.IsMe(__instance.Owner)) return true;
        __result = MyOnPlay(__instance, choiceContext, cardPlay);
        return false;
    }

    private static async Task MyOnPlay(
        MegaCrit.Sts2.Core.Models.Cards.FallingStar card,
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay) {

        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await CreatureCmd.TriggerAnim(card.Owner.Creature, "Cast", card.Owner.Character.CastAnimDelay);
        var config = CardFX.FromCard(card)!;
        var cmd = DamageCmd.Attack(card.DynamicVars.Damage.BaseValue)
            .FromCard(card)
            .Targeting(cardPlay.Target)
            .BeforeDamage(async delegate {
                await CardVfxUtil.PlayTargetedVfx(config, card.Owner.Creature, cardPlay.Target, nameof(FallingStar));
            });
        cmd._attackerAnimName = null;
        await cmd.Execute(choiceContext);
        WeakPower weakPower = await PowerCmd.Apply<WeakPower>(cardPlay.Target, card.DynamicVars.Weak.BaseValue, card.Owner.Creature, card);
        VulnerablePower vulnerablePower = await PowerCmd.Apply<VulnerablePower>(cardPlay.Target, card.DynamicVars.Vulnerable.BaseValue, card.Owner.Creature, card);
    }
}
