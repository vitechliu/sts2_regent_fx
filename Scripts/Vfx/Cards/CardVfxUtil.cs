#pragma warning disable CS4014

using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.TestSupport;
using RegentFx.Core.Audio;

namespace RegentFX.Scripts.Vfx.Cards;

/// <summary>
/// 卡牌 VFX 通用工具类
/// </summary>
public static class CardVfxUtil {

    /// <summary>
    /// 播放从 owner 飞向 target 的单体目标 VFX
    /// </summary>
    public static async Task PlayTargetedVfx(CardFX config, Creature owner, Creature target, string logTag) {
        NCreature? ownerNode = NCombatRoom.Instance?.GetCreatureNode(owner);
        NCreature? targetNode = NCombatRoom.Instance?.GetCreatureNode(target);

        if (ownerNode == null || targetNode == null) {
            Entry.Logger.Info($"[{logTag}] Could not get creature nodes for VFX");
            return;
        }

        Vector2 startPos = ownerNode.GlobalPosition + config.TargetOffset;
        Vector2 targetPos = targetNode.VfxSpawnPosition;
        await PlayVfxInternal(config, startPos, targetPos, logTag);
    }

    /// <summary>
    /// 播放从 owner 飞向敌人中心点的 AOE VFX
    /// </summary>
    public static async Task PlayAoeVfx(CardFX config, Creature owner, CombatState combatState, string logTag) {
        NCreature? ownerNode = NCombatRoom.Instance?.GetCreatureNode(owner);
        if (ownerNode == null) {
            Entry.Logger.Info($"[{logTag}] Could not get owner creature node for VFX");
            return;
        }

        Vector2 startPos = ownerNode.GlobalPosition + config.TargetOffset;
        Vector2 targetPos = VFXUtil.GetEnemiesCenter(combatState);
        await PlayVfxInternal(config, startPos, targetPos, logTag);
    }

    /// <summary>
    /// 通用的 VFX 节点设置和播放
    /// </summary>
    private static async Task PlayVfxInternal(CardFX config, Vector2 startPos, Vector2 targetPos, string logTag) {
        if (TestMode.IsOn) {
            return;
        }

        string? scenePath = config.VfxScenePath;
        if (string.IsNullOrEmpty(scenePath)) {
            Entry.Logger.Warn($"[{logTag}] No VfxScenePath configured");
            return;
        }

        try {
            Node2D vfxNode = VFXUtil.GenVFXNode(scenePath);
            Node2D? startNode = vfxNode.FindChild("StartPos") as Node2D;
            if (startNode == null) {
                Entry.Logger.Error($"[{logTag}] No StartPos found in VFX scene");
                return;
            }

            vfxNode.FitVFX(startNode.GlobalPosition, Vector2.Zero, startPos, targetPos);
            vfxNode.GlobalPosition = targetPos;

            if (config.CancelsStarEffect) {
                Entry.StarEffectController?.OnCancelCard();
            }

            NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(vfxNode);

            if (!string.IsNullOrEmpty(config.HitSfxPath)) {
                _ = SimpleSfxUtil.Play(config.HitSfxPath);
            }

            TaskHelper.RunSafely(ClearAfter(vfxNode, config.VfxClearDelay));

            if (config.HasExposureEffect) {
                WorldEnvironmentUtil.TweenExposure(config.ExposurePeak, config.ExposureInDuration);
                await Cmd.Wait(config.ExposureInDuration + 0.05f);
                WorldEnvironmentUtil.TweenExposure(1f, config.ExposureOutDuration);
            } else {
                await Cmd.Wait(0.15f);
            }

            if (!string.IsNullOrEmpty(config.SecondarySfxPath)) {
                _ = SimpleSfxUtil.Play(config.SecondarySfxPath);
            }

        } catch (Exception ex) {
            Entry.Logger.Info($"[{logTag}] Error playing VFX: {ex.Message}");
        }
    }

    /// <summary>
    /// 延迟清理 VFX 节点
    /// </summary>
    public static async Task ClearAfter(Node2D? node, float delay) {
        await Cmd.Wait(delay);
        if (node != null && GodotObject.IsInstanceValid(node)) {
            node.QueueFreeSafely();
        }
    }
}
