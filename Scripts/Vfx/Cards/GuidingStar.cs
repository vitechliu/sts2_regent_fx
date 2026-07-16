using Godot;
using HarmonyLib;
using System.Reflection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

#pragma warning disable CS4014

namespace RegentFX.Scripts.Vfx.Cards;

//引导之星仍然需要手动patch
[CardFx(typeof(MegaCrit.Sts2.Core.Models.Cards.GuidingStar))]
public class GuidingStar : CardFX {
    public override int StarCount => 3;

    // 更靠左上的位置
    public override Vector2 TargetOffset => new(-200f, -450f);

    public override string VfxScenePath => "res://RegentFX/scenes/vfx/guiding_star.tscn";

    private List<Vector2> starPos = new() {
        new Vector2(2f, 2f),
        new Vector2(0f, -5f),
        new Vector2(-5f, 0f),
    };

    public override Vector2 CalculateTargetPosition(Vector2 basePosition, int index, int totalCount) {
        return basePosition + TargetOffset + starPos[index];
    }
    
    public override bool UseV2Patch => true;
    public override bool HasOnBeforeDamage => true;
    // public override bool PlayCastAnim => false;

    public override async Task OnBeforeDamage(AttackCommand command) {
        Creature? owner = card?.Owner.Creature;
        Creature? target = command._singleTarget;
        if (owner == null || target == null || command._singleTarget == null) return;
        Entry.StarEffectController?.OnPlayCard();
        SfxCmd.Play("event:/sfx/characters/regent/regent_guiding_star");
        await CardVfxUtil.PlayTargetedVfx(this, card.Owner.Creature, target, nameof(GuidingStar));
    }
}

[HarmonyPatch]
public static class GuidingStarPatch {
    private static readonly MethodInfo? FromCard108 = AccessTools.Method(
        typeof(AttackCommand),
        nameof(AttackCommand.FromCard),
        new[] { typeof(CardModel), typeof(CardPlay) });
    private static readonly MethodInfo? FromCard107 = AccessTools.Method(
        typeof(AttackCommand),
        nameof(AttackCommand.FromCard),
        new[] { typeof(CardModel) });

    [HarmonyPrefix]
    [HarmonyPatch(typeof(MegaCrit.Sts2.Core.Models.Cards.GuidingStar), "OnPlay")]
    public static bool OnPlay(
        MegaCrit.Sts2.Core.Models.Cards.GuidingStar __instance,
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ref Task __result) {
        if (!CardFX.IsTypeEnabled<GuidingStar>()) return true;
        if (!LocalContext.IsMe(__instance.Owner)) return true;
        __result = MyOnPlay(__instance, choiceContext, cardPlay);
        return false;
    }

    private static async Task MyOnPlay(
        MegaCrit.Sts2.Core.Models.Cards.GuidingStar card,
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay) {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await CreatureCmd.TriggerAnim(card.Owner.Creature, "Cast", card.Owner.Character.CastAnimDelay);
        await FromCardCompat(DamageCmd.Attack(card.DynamicVars.Damage.BaseValue), card, cardPlay)
            .Targeting(cardPlay.Target)
            .WithNoAttackerAnim()
            .Execute(choiceContext);
        await CardPileCmd.Draw(choiceContext, card.DynamicVars.Cards.BaseValue, card.Owner);
    }

    private static AttackCommand FromCardCompat(AttackCommand command, CardModel card, CardPlay? cardPlay) {
        MethodInfo method = FromCard108 ?? FromCard107 ?? throw new MissingMethodException(
            typeof(AttackCommand).FullName,
            nameof(AttackCommand.FromCard));

        object?[] args = method.GetParameters().Length == 2
            ? [card, cardPlay]
            : [card];

        object? result = method.Invoke(command, args);
        return result as AttackCommand
               ?? throw new InvalidOperationException("AttackCommand.FromCard returned an unexpected result type.");
    }
}
