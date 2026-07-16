using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.TestSupport;
using RegentFX.ThirdParty.Audio;

#pragma warning disable CS4014

namespace RegentFX.Scripts.Vfx.Cards;

/// <summary>
/// Comet 卡牌特效
/// </summary>
[CardFx(typeof(MegaCrit.Sts2.Core.Models.Cards.Comet))]
public class Comet : CardFX {
    public override int StarCount => 5;

    // 更靠左上的位置
    public override Vector2 TargetOffset => new(-250f, -520f);

    public override string VfxScenePath => "res://RegentFX/scenes/vfx/comet.tscn";
    public override string HitSfxPath => "event:/RegentFx/sfx/Comet";
    public override bool HasExposureEffect => false;
    public override bool RemoveHitFx => true;

    public override Vector2 CalculateTargetPosition(Vector2 basePosition, int index, int totalCount) {
        return basePosition + TargetOffset + VFXUtil.RandVec2(14f);
    }
    
    public override bool UseV2Patch => true;
    public override bool HasOnBeforeDamage => true;
    
    public override async Task OnBeforeDamage(AttackCommand command) {
        Creature? owner = card?.Owner.Creature;
        Creature? target = command._singleTarget;
        if (owner == null || target == null || command._singleTarget == null) return;
        await PlayVfx(card.Owner.Creature, target);
    }

    private async Task PlayVfx(Creature owner, Creature target) {
        if (TestMode.IsOn) return;
        NCreature? ownerNode = NCombatRoom.Instance?.GetCreatureNode(owner);
        NCreature? targetNode = NCombatRoom.Instance?.GetCreatureNode(target);

        if (ownerNode == null || targetNode == null) {
            return;
        }

        try {
            Node2D vfxNode = VFXUtil.GenVFXNode(VfxScenePath);
            vfxNode.Scale *= 1.26f;
            if (!VFXUtil.IsCharacterFacingRight(owner))
                vfxNode.Scale *= new Vector2(-1f, 1f);
            // 计算长矛射出方向（从玩家指向目标）
            Vector2 targetPos = (targetNode.VfxSpawnPosition + targetNode.GlobalPosition * 2) / 3;
            vfxNode.GlobalPosition = targetPos;
            // 添加到战斗特效容器
            NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(vfxNode);

            Entry.StarEffectController?.OnPlayCard();
            FmodLite.Play(HitSfxPath);
            WorldEnvironmentUtil.FullExposure(1.3f, 0.3f, 0.3f, 0.1f);
            TaskHelper.RunSafely(CardVfxUtil.ClearAfter(vfxNode, 3f));
            await VFXUtil.Wait(0.6f);
            NGame.Instance?.ScreenShake(ShakeStrength.Medium, ShakeDuration.Normal);

        }
        catch (Exception ex) {
            Entry.Logger.Warn($"[Comet] Error playing VFX: {ex.Message}");
        }
    }
}



[HarmonyPatch]
public static class CometPatch {
    private static readonly MethodInfo? FromCard108 = AccessTools.Method(
        typeof(AttackCommand),
        nameof(AttackCommand.FromCard),
        [typeof(CardModel), typeof(CardPlay)]);
    private static readonly MethodInfo? FromCard107 = AccessTools.Method(
        typeof(AttackCommand),
        nameof(AttackCommand.FromCard),
        [typeof(CardModel)]);

    [HarmonyPrefix]
    [HarmonyPatch(typeof(MegaCrit.Sts2.Core.Models.Cards.Comet), "OnPlay")]
    public static bool OnPlay(
        MegaCrit.Sts2.Core.Models.Cards.Comet __instance,
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ref Task __result) {
        if (!CardFX.IsTypeEnabled<Comet>()) return true;
        if (!LocalContext.IsMe(__instance.Owner)) return true;
        __result = MyOnPlay(__instance, choiceContext, cardPlay);
        return false;
    }

    private static async Task MyOnPlay(
        MegaCrit.Sts2.Core.Models.Cards.Comet card,
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay) {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await CreatureCmd.TriggerAnim(card.Owner.Creature, "Cast", card.Owner.Character.CastAnimDelay);
        await FromCardCompat(DamageCmd.Attack(card.DynamicVars.Damage.BaseValue), card, cardPlay)
            .Targeting(cardPlay.Target)
            .WithNoAttackerAnim()
            .Execute(choiceContext);
        await PowerCmd.Apply<WeakPower>(choiceContext, cardPlay.Target, card.DynamicVars.Weak.BaseValue, card.Owner.Creature, card);
        await PowerCmd.Apply<VulnerablePower>(choiceContext, cardPlay.Target, card.DynamicVars.Vulnerable.BaseValue, card.Owner.Creature, card);
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
