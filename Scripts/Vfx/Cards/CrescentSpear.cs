using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;
using RegentFx.Core.Audio;

namespace RegentFX.Scripts.Vfx.Cards;

#pragma warning disable CS4014

/// <summary>
/// CrescentSpear 卡牌特效
/// </summary>
public class CrescentSpear : CardFX {
    public override int StarCount => 1;
    // 更靠左上的位置
    public override Vector2 TargetOffset => new(-200f, -350f);
}

[HarmonyPatch]
public static class CrescentSpearPatch {
    private const string HitSFX = "res://RegentFX/sfx/crescent_spear.mp3";
    private const string ScenePath = "res://RegentFX/scenes/crescent_spear.tscn";

    [HarmonyPrefix]
    [HarmonyPatch(typeof(MegaCrit.Sts2.Core.Models.Cards.CrescentSpear), "OnPlay")]
    public static bool OnPlay(
        MegaCrit.Sts2.Core.Models.Cards.CrescentSpear __instance,
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ref Task __result) {
        __result = MyOnPlay(__instance, choiceContext, cardPlay);
        return false;
    }

    private static async Task MyOnPlay(
        MegaCrit.Sts2.Core.Models.Cards.CrescentSpear card,
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay) {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await CreatureCmd.TriggerAnim(card.Owner.Creature, "Cast", card.Owner.Character.CastAnimDelay);
        var cmd = DamageCmd.Attack(card.DynamicVars.CalculatedDamage)
            .FromCard(card)
            .Targeting(cardPlay.Target)
            .BeforeDamage(async delegate {
                await PlayCrescentSpearVfx(card.Owner.Creature, cardPlay.Target);
            });
        cmd._attackerAnimName = null;
        // Entry.Logger.Info("HasHitVFX?:" + cmd.HitVfx);
        await cmd.Execute(choiceContext);
    }

    private const float SpearLength = 900f;
    private const float ScaleFactor = 1.2f;

    private static async Task PlayCrescentSpearVfx(Creature owner, Creature target) {
        if (TestMode.IsOn) {
            return;
        }
        NCreature? ownerNode = NCombatRoom.Instance?.GetCreatureNode(owner);
        NCreature? targetNode = NCombatRoom.Instance?.GetCreatureNode(target);

        if (ownerNode == null || targetNode == null) {
            Entry.Logger.Info("[CrescentSpear] Could not get creature nodes for VFX");
            return;
        }
        try {
            Node2D vfxNode = CardFX.GenVFXNode(ScenePath);
            if (vfxNode == null) return;

            // 等比放大1.5倍
            vfxNode.Scale = Vector2.One * ScaleFactor;

            // 计算长矛射出方向（从玩家指向目标）
            Vector2 ownerPos = ownerNode.GlobalPosition;
            Vector2 targetPos = targetNode.VfxSpawnPosition;

            // startPos: 玩家位置 + TargetOffset
            Vector2 startPos = ownerPos + new CrescentSpear().TargetOffset;

            // 计算旋转角度，使特效朝向目标
            Vector2 direction = targetPos - startPos;
            float rotationDegrees = Mathf.RadToDeg(Mathf.Atan2(direction.Y, direction.X));
            vfxNode.RotationDegrees = rotationDegrees;

            // 特效位置: 目标位置减去长矛长度（沿旋转方向）
            Vector2 rotationDirection = direction.Normalized();
            vfxNode.GlobalPosition = targetPos - rotationDirection * SpearLength;

            // 添加到战斗特效容器
            NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(vfxNode);
            

            SimpleSfxUtil.Play(HitSFX);
            TaskHelper.RunSafely(ClearAfter(vfxNode));
            await Cmd.Wait(0.15f);

        } catch (Exception ex) {
            Entry.Logger.Info($"[CrescentSpear] Error playing VFX: {ex.Message}");
        }
    }

    public static async Task ClearAfter(Node2D? node) {
        await Cmd.Wait(1f);
        if (node != null && GodotObject.IsInstanceValid(node)) node.QueueFreeSafely();
    }
}