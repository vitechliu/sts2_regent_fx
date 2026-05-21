using Godot;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;
using RitsuFmodLite;

namespace RegentFX.Scripts.Vfx.Cards;

#pragma warning disable CS4014

/// <summary>
/// CrescentSpear 卡牌特效
/// </summary>
[CardFx(typeof(MegaCrit.Sts2.Core.Models.Cards.CrescentSpear))]
public class CrescentSpear: CardFX {
    public override int StarCount => 1;
    
    public override string? VfxScenePath => "res://RegentFX/scenes/vfx/crescent_spear.tscn";
    
    // 更靠左上的位置
    public override Vector2 TargetOffset => new(-200f, -350f);
    
    public override void OnStartHolding(Star star, int index) {
        star.ChangeColorTo(new Color(14.551f, 0.683f, 9.982f)); //pink
    }

    public override bool UseV2Patch => true;
    public override bool HasOnBeforeDamage => true;
    public override async Task OnBeforeDamage(AttackCommand command) {
        Creature? owner = card?.Owner.Creature;
        Creature? target = command._singleTarget;
        if (owner == null || target == null) return;
        await PlayCrescentSpearVfx(owner, target);
    }
    
    private const float SpearLength = 900f;
    private const float ScaleFactor = 1.2f;

    private const string HitSFX = "event:/RegentFx/sfx/crescent_spear";
    
    private async Task PlayCrescentSpearVfx(Creature owner, Creature target) {
        if (TestMode.IsOn) {
            return;
        }
        NCreature? ownerNode = NCombatRoom.Instance?.GetCreatureNode(owner);
        NCreature? targetNode = NCombatRoom.Instance?.GetCreatureNode(target);

        if (ownerNode == null || targetNode == null) {
            Entry.Logger.Warn("[CrescentSpear] Could not get creature nodes for VFX");
            return;
        }
        try {
            Node2D vfxNode = VFXUtil.GenVFXNode(VfxScenePath);

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
            

            Entry.StarEffectController?.OnPlayCard();
            FmodLite.Play(HitSFX);
            TaskHelper.RunSafely(ClearAfter(vfxNode));
            await VFXUtil.Wait(0.15f);

        } catch (Exception ex) {
            Entry.Logger.Warn($"[CrescentSpear] Error playing VFX: {ex.Message}");
        }
    }

    public static async Task ClearAfter(Node2D? node) {
        await VFXUtil.Wait(1f);
        if (node != null && GodotObject.IsInstanceValid(node)) node.QueueFreeSafely();
    }
}