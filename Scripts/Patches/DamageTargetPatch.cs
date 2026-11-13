using System.Collections;
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

    // v0.113 的攻击直接使用 DamageTargetData，旧版只有 Creature 目标。
    private static readonly Type? DamageTargetDataType = typeof(Creature).Assembly.GetType(
        "MegaCrit.Sts2.Core.Entities.Creatures.DamageTargetData");
    private static readonly FieldInfo? DamageTargetField = DamageTargetDataType == null
        ? null
        : AccessTools.Field(DamageTargetDataType, "target");

    public static MethodBase TargetMethod() {
        Type? hookType = typeof(CreatureCmd).Assembly.GetType("MegaCrit.Sts2.Core.Hooks.ModifyDamageHookType");
        MethodInfo? damageWithTargetData = DamageTargetField != null && hookType != null
            ? FindDamageMethod(DamageTargetField.DeclaringType!, typeof(CardPlay), hookType)
            : null;
        return damageWithTargetData
            ?? FindDamageMethod(typeof(Creature), typeof(CardPlay))
            ?? FindDamageMethod(typeof(Creature))
            ?? throw new MissingMethodException(
                typeof(CreatureCmd).FullName,
                nameof(CreatureCmd.Damage));
    }

    private static MethodInfo? FindDamageMethod(Type targetType, params Type[] extraParameters) {
        return AccessTools.Method(typeof(CreatureCmd), nameof(CreatureCmd.Damage), [
            typeof(PlayerChoiceContext), typeof(IEnumerable<>).MakeGenericType(targetType), typeof(decimal),
            typeof(ValueProp), typeof(Creature), typeof(CardModel),
            .. extraParameters
        ]);
    }

    [HarmonyPrefix]
    public static bool DamagePrefix(
        CardModel? __5,
        object?[] __args,
        MethodBase __originalMethod,
        ref Task<IEnumerable<DamageResult>> __result) {

        CardModel? cardSource = __5;
        if (cardSource == null) return true;

        var cardFX = CardFX.FromCard(cardSource);
        if (cardFX == null) return true;
        if (!LocalContext.IsMe(cardSource.Owner)) return true;
        if (!cardFX.HasOnBeforeDamage && !cardFX.HasOnBeforeDamageTargeted) return true;

        var command = AttackVfxContext.CurrentAttackCommand.Value;
        if (command == null) return true;

        if (Interlocked.Increment(ref _depth) > 1) {
            Interlocked.Decrement(ref _depth);
            return true;
        }

        __result = RunBeforeAndDamage(cardFX, command, __originalMethod, (object?[])__args.Clone());
        return false;
    }

    private static async Task<IEnumerable<DamageResult>> RunBeforeAndDamage(
        CardFX cardFX,
        AttackCommand command,
        MethodBase damageMethod,
        object?[] args) {

        try {
            List<Creature> targets = SnapshotTargets(args);

            if (cardFX.HasOnBeforeDamageTargeted) {
                await cardFX.OnBeforeDamage(command, targets);
            } else {
                await cardFX.OnBeforeDamage(command);
            }

            return await (Task<IEnumerable<DamageResult>>)damageMethod.Invoke(null, args)!;
        }
        finally {
            Interlocked.Decrement(ref _depth);
        }
    }

    private static List<Creature> SnapshotTargets(object?[] args) {
        const int targetIndex = 1;
        if (args[targetIndex] is IEnumerable<Creature> targets) {
            var snapshot = targets.ToList();
            args[targetIndex] = snapshot;
            return snapshot;
        }

        // 将目标提取给特效，同时保留传给原方法的每个目标的命中特效数据。
        FieldInfo targetField = DamageTargetField!;
        var targetData = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(targetField.DeclaringType!))!;
        var creatures = new List<Creature>();
        foreach (object entry in (IEnumerable)args[targetIndex]!) {
            targetData.Add(entry);
            creatures.Add((Creature)targetField.GetValue(entry)!);
        }
        args[targetIndex] = targetData;
        return creatures;
    }
}
