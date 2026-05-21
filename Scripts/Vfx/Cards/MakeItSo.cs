using Godot;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.TestSupport;
using RitsuFmodLite;

#pragma warning disable CS4014

namespace RegentFX.Scripts.Vfx.Cards;

[CardFx(typeof(MegaCrit.Sts2.Core.Models.Cards.MakeItSo))]
public class MakeItSo : CardFX {
    public override HoldingModes HoldingMode => HoldingModes.None;
    
    public override bool UseV2Patch => true;
    public override bool HasOnBeforeDamage => true;

    public override string? VfxScenePath => "res://RegentFX/scenes/vfx/make_it_so.tscn";
    public override string? HitSfxPath => "event:/RegentFx/sfx/make_it_so_2";
    public override bool HasExposureEffect => false;

    public override bool RemoveHitFx => true;

    public override async Task OnBeforeDamage(AttackCommand command) {
        Creature? target = command._singleTarget;
        if (target == null || command._singleTarget == null || card == null) return;
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
            vfxNode.Scale *= 1.3f;
            // 计算长矛射出方向（从玩家指向目标）
            Vector2 startPos = ownerNode.GlobalPosition + new Vector2(0f, -460f) + VFXUtil.RandVec2(80f);
            Vector2 targetPos = targetNode.VfxSpawnPosition;

            // 计算旋转角度，使特效朝向目标
            Vector2 direction = targetPos - startPos;
            float rotationDegrees = Mathf.RadToDeg(Mathf.Atan2(direction.Y, direction.X));
            vfxNode.RotationDegrees = rotationDegrees;

            // 特效位置: 目标位置减去长矛长度（沿旋转方向）
            Vector2 rotationDirection = direction.Normalized();
            vfxNode.GlobalPosition = targetPos;
            // 添加到战斗特效容器
            NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(vfxNode);

            Entry.StarEffectController?.OnPlayCard();
            FmodLite.Play(HitSfxPath);
            FmodLite.Play("event:/RegentFx/sfx/post_magic");
            WorldEnvironmentUtil.FullExposure(1.2f, 0.1f, 0.1f, 0.1f);
            TaskHelper.RunSafely(CardVfxUtil.ClearAfter(vfxNode, 3f));
            await VFXUtil.Wait(0.15f);
            NGame.Instance?.ScreenShake(ShakeStrength.Weak, ShakeDuration.Short);

        } catch (Exception ex) {
            Entry.Logger.Warn($"[MakeItSo] Error playing VFX: {ex.Message}");
        }
    }
}