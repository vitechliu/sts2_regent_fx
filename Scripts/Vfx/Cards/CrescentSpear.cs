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

namespace RegentFX.Scripts.Vfx.Cards;

/// <summary>
/// CrescentSpear 卡牌特效
/// </summary>
public class CrescentSpear : CardFX {
    public override int StarCount => 1;

    // 更靠左上的位置
    public override Vector2 TargetOffset => new(-200f, -350f);

    public override bool DisableWeaponAnim => true;
}

[HarmonyPatch]
public static class CrescentSpearPatch {
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
        var cmd = DamageCmd.Attack(card.DynamicVars.CalculatedDamage)
            .FromCard(card)
            .Targeting(cardPlay.Target)
            .BeforeDamage(async delegate {
                await PlayCrescentSpearVfx(card.Owner.Creature, cardPlay.Target);
            });
        cmd._attackerAnimName = null;
        await cmd.Execute(choiceContext);
    }

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
            // 加载并实例化场景
            PackedScene scene = GD.Load<PackedScene>(ScenePath);
            if (scene == null) {
                Entry.Logger.Info($"[CrescentSpear] Failed to load scene: {ScenePath}");
                return;
            }

            Node2D vfxNode = scene.Instantiate<Node2D>();
            if (vfxNode == null) {
                Entry.Logger.Info("[CrescentSpear] Failed to instantiate VFX node");
                return;
            }

            // 计算起始位置（玩家）和目标位置（敌人）
            Vector2 startPos = ownerNode.VfxSpawnPosition;
            Vector2 targetPos = targetNode.VfxSpawnPosition;

            // 设置特效位置为玩家位置
            vfxNode.GlobalPosition = startPos;

            // 计算旋转角度，使特效朝向目标
            Vector2 direction = targetPos - startPos;
            float rotationDegrees = Mathf.RadToDeg(Mathf.Atan2(direction.Y, direction.X));
            vfxNode.RotationDegrees = rotationDegrees;

            // 添加到战斗特效容器
            NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(vfxNode);

            // 获取 AnimatedSprite2D 并等待动画播放完成
            AnimatedSprite2D? animSprite = vfxNode.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
            if (animSprite != null) {
                // 计算动画总时长：帧数 / 速度 = 45 / 60 = 0.75秒
                float animDuration = (float)animSprite.SpriteFrames.GetFrameCount("default") / (float)animSprite.SpriteFrames.GetAnimationSpeed("default");
                await Cmd.Wait(animDuration);
            } else {
                // 如果没有找到动画，等待固定时间（根据场景文件，动画速度为60fps，45帧约0.75秒）
                await Cmd.Wait(0.75f);
            }

            // 清理
            vfxNode.QueueFreeSafely();
        } catch (Exception ex) {
            Entry.Logger.Info($"[CrescentSpear] Error playing VFX: {ex.Message}");
        }
    }
}