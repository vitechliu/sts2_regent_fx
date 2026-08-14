using Godot;
using HarmonyLib;
using System.Reflection;
using System.Reflection.Emit;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;

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
    private static MethodBase TargetMethod() {
        MethodInfo? onPlay = AccessTools.DeclaredMethod(
            typeof(MegaCrit.Sts2.Core.Models.Cards.GuidingStar),
            "OnPlay",
            [typeof(PlayerChoiceContext), typeof(CardPlay)]);

        return onPlay == null
            ? throw new MissingMethodException(typeof(MegaCrit.Sts2.Core.Models.Cards.GuidingStar).FullName, "OnPlay")
            : AccessTools.AsyncMoveNext(onPlay);
    }

    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> RemoveOriginalVfx(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator,
        MethodBase __originalMethod) {
        List<CodeInstruction> codes = instructions.ToList();

        MethodInfo? getCombatRoom = AccessTools.PropertyGetter(typeof(NCombatRoom), nameof(NCombatRoom.Instance));
        MethodInfo? getCreatureNode = AccessTools.Method(typeof(NCombatRoom), nameof(NCombatRoom.GetCreatureNode));
        MethodInfo? createMissile = AccessTools.Method(typeof(NSmallMagicMissileVfx), nameof(NSmallMagicMissileVfx.Create));
        MethodInfo? wait = AccessTools.Method(typeof(Cmd), nameof(Cmd.Wait), [typeof(float), typeof(bool)]);
        MethodInfo? getDynamicVars = AccessTools.PropertyGetter(typeof(CardModel), nameof(CardModel.DynamicVars));
        MethodInfo? attack = AccessTools.Method(typeof(DamageCmd), nameof(DamageCmd.Attack), [typeof(decimal)]);

        int getCreatureIndex = FindCall(codes, getCreatureNode);
        int vfxStartIndex = FindLastCall(codes, getCombatRoom, getCreatureIndex);
        int createMissileIndex = FindCall(codes, createMissile, getCreatureIndex + 1);
        int waitIndex = FindCall(codes, wait, createMissileIndex + 1);
        int getDynamicVarsIndex = FindCall(codes, getDynamicVars, waitIndex + 1);
        int attackIndex = FindCall(codes, attack, waitIndex + 1);
        int damageStartIndex = getDynamicVarsIndex - 1;

        FieldInfo? cardField = __originalMethod.DeclaringType?
            .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .SingleOrDefault(field => field.FieldType == typeof(MegaCrit.Sts2.Core.Models.Cards.GuidingStar));
        MethodInfo? shouldSkipVfx = AccessTools.Method(typeof(GuidingStarPatch), nameof(ShouldSkipOriginalVfx));

        if (vfxStartIndex < 0 || getCreatureIndex < vfxStartIndex || createMissileIndex < getCreatureIndex ||
            waitIndex < createMissileIndex || getDynamicVarsIndex < waitIndex || attackIndex < getDynamicVarsIndex ||
            damageStartIndex < 0 || !LoadsLocal(codes[damageStartIndex]) || cardField == null || shouldSkipVfx == null) {
            Entry.Logger.Warn("GuidingStar Transpiler未找到预期的原版特效IL，保留原逻辑");
            return codes;
        }

        var skipVfx = generator.DefineLabel();
        codes[damageStartIndex].labels.Add(skipVfx);

        var skipInstructions = new List<CodeInstruction> {
            new(OpCodes.Ldarg_0),
            new(OpCodes.Ldfld, cardField),
            new(OpCodes.Call, shouldSkipVfx),
            new(OpCodes.Brtrue, skipVfx),
        };
        codes[vfxStartIndex].MoveLabelsTo(skipInstructions[0]);
        codes.InsertRange(vfxStartIndex, skipInstructions);

        return codes;
    }

    private static bool ShouldSkipOriginalVfx(MegaCrit.Sts2.Core.Models.Cards.GuidingStar card) {
        return CardFX.IsTypeEnabled<GuidingStar>() && LocalContext.IsMe(card.Owner);
    }

    private static int FindCall(IReadOnlyList<CodeInstruction> codes, MethodInfo? method, int startIndex = 0) {
        if (method == null || startIndex < 0) return -1;
        for (int i = startIndex; i < codes.Count; i++) {
            if (codes[i].Calls(method)) return i;
        }
        return -1;
    }

    private static int FindLastCall(IReadOnlyList<CodeInstruction> codes, MethodInfo? method, int beforeIndex) {
        if (method == null || beforeIndex < 0) return -1;
        for (int i = beforeIndex - 1; i >= 0; i--) {
            if (codes[i].Calls(method)) return i;
        }
        return -1;
    }

    private static bool LoadsLocal(CodeInstruction instruction) {
        return instruction.opcode == OpCodes.Ldloc || instruction.opcode == OpCodes.Ldloc_S ||
               instruction.opcode == OpCodes.Ldloc_0 || instruction.opcode == OpCodes.Ldloc_1 ||
               instruction.opcode == OpCodes.Ldloc_2 || instruction.opcode == OpCodes.Ldloc_3;
    }
}
