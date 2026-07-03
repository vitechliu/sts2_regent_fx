using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using RegentFX.Scripts.Vfx;
using RegentFX.Scripts.Vfx.Cards;

namespace RegentFX.Scripts.Patches;

[HarmonyPatch]
public static class DamageTargetPatch {
    private static int _depth;
    private static readonly MethodInfo? Damage108 = AccessTools.Method(typeof(CreatureCmd), nameof(CreatureCmd.Damage), new[] {
        typeof(PlayerChoiceContext), typeof(IEnumerable<Creature>), typeof(decimal),
        typeof(ValueProp), typeof(Creature), typeof(CardModel), typeof(CardPlay)
    });
    private static readonly MethodInfo? Damage107 = AccessTools.Method(typeof(CreatureCmd), nameof(CreatureCmd.Damage), new[] {
        typeof(PlayerChoiceContext), typeof(IEnumerable<Creature>), typeof(decimal),
        typeof(ValueProp), typeof(Creature), typeof(CardModel)
    });

    public static MethodBase TargetMethod() {
        return Damage108 ?? Damage107 ?? throw new MissingMethodException(
            typeof(CreatureCmd).FullName,
            nameof(CreatureCmd.Damage));
    }

    [HarmonyPrefix]
    public static bool DamagePrefix(
        PlayerChoiceContext choiceContext,
        IEnumerable<Creature> targets,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        object?[] __args,
        ref Task<IEnumerable<DamageResult>> __result) {

        if (cardSource == null) return true;

        var cardFX = CardFX.FromCard(cardSource);
        if (cardFX == null) return true;
        if (!LocalContext.IsMe(cardSource.Owner)) return true;
        if (!cardFX.HasOnBeforeDamage && !cardFX.HasOnBeforeDamageTargeted) return true;

        if (Interlocked.Increment(ref _depth) > 1) {
            Interlocked.Decrement(ref _depth);
            return true;
        }

        var command = AttackVfxContext.CurrentAttackCommand.Value;
        CardPlay? cardPlay = __args.Length > 6 ? __args[6] as CardPlay : null;

        __result = RunBeforeAndDamage(cardFX, command, targets, choiceContext, amount, props, dealer, cardSource, cardPlay);
        return false;
    }

    private static async Task<IEnumerable<DamageResult>> RunBeforeAndDamage(
        CardFX cardFX,
        AttackCommand? command,
        IEnumerable<Creature> targets,
        PlayerChoiceContext choiceContext,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel cardSource,
        CardPlay? cardPlay) {

        try {
            List<Creature> targetList = targets.ToList();

            if (cardFX.HasOnBeforeDamageTargeted && command != null) {
                await cardFX.OnBeforeDamage(command, targetList);
            }
            else if (cardFX.HasOnBeforeDamage && command != null) {
                await cardFX.OnBeforeDamage(command);
            }

            return await InvokeDamage(choiceContext, targetList, amount, props, dealer, cardSource, cardPlay);
        }
        finally {
            Interlocked.Decrement(ref _depth);
        }
    }

    private static Task<IEnumerable<DamageResult>> InvokeDamage(
        PlayerChoiceContext choiceContext,
        IEnumerable<Creature> targets,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel cardSource,
        CardPlay? cardPlay) {

        MethodInfo method = Damage108 ?? Damage107 ?? throw new MissingMethodException(
            typeof(CreatureCmd).FullName,
            nameof(CreatureCmd.Damage));

        object?[] args = method.GetParameters().Length == 7
            ? new object?[] { choiceContext, targets, amount, props, dealer, cardSource, cardPlay }
            : new object?[] { choiceContext, targets, amount, props, dealer, cardSource };

        object? result = method.Invoke(null, args);
        return result as Task<IEnumerable<DamageResult>>
               ?? throw new InvalidOperationException("CreatureCmd.Damage returned an unexpected result type.");
    }
}
