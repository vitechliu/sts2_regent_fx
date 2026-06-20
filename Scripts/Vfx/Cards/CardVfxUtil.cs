#pragma warning disable CS4014

using Godot;

using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;
using RegentFX.ThirdParty.Audio;


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
            Entry.Logger.Warn($"[{logTag}] Could not get creature nodes for VFX");
            return;
        }

        Vector2 startPos = ownerNode.GlobalPosition + config.TargetOffset;
        Vector2 targetPos = targetNode.VfxSpawnPosition;
        await PlayVfxInternal(config, startPos, targetPos, logTag);
    }

    /// <summary>
    /// 播放从 owner 飞向敌人中心点的 AOE VFX
    /// </summary>
    public static async Task PlayAoeVfx(CardFX config, Creature owner, CardModel card, string logTag) {
        NCreature? ownerNode = NCombatRoom.Instance?.GetCreatureNode(owner);
        if (ownerNode == null) {
            Entry.Logger.Warn($"[{logTag}] Could not get owner creature node for VFX");
            return;
        }

        Vector2 startPos = ownerNode.GlobalPosition + config.TargetOffset;
        Vector2? targetPos = VFXUtil.GetCombatSidePos(card);
        // Vector2? targetPos = VFXUtil.GetEnemiesCenter(card);
        if (targetPos.HasValue) {
            await PlayVfxInternal(config, startPos, targetPos.Value, logTag);
        }
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

            NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(vfxNode);

            // 原音频播放模式代码：
            // if (!string.IsNullOrEmpty(config.HitSfxPath)) {
            //     FmodLite.Play(config.HitSfxPath);
            // }

            // 修改为：
            if (!string.IsNullOrEmpty(config.HitSfxPath)) {
                try {
                    FmodLite.Play(config.HitSfxPath);
                } catch (Exception ex) {
                    Entry.Logger.Warn($"[CardVfxUtil] FMOD 播放失败: {config.HitSfxPath}. 错误: {ex.Message}");
                }
            }

            TaskHelper.RunSafely(ClearAfter(vfxNode, config.VfxClearDelay));

            if (config.HasExposureEffect) {
                try {
                    WorldEnvironmentUtil.TweenExposure(config.ExposurePeak, config.ExposureInDuration);
                    await VFXUtil.Wait(config.ExposureInDuration + 0.05f);
                } finally {
                    WorldEnvironmentUtil.TweenExposure(1f, config.ExposureOutDuration);
                }
            } else {
                await VFXUtil.Wait(0.15f);
            }

            if (!string.IsNullOrEmpty(config.SecondarySfxPath)) {
                try {
                    FmodLite.Play(config.SecondarySfxPath);
                } catch (Exception ex) {
                    Entry.Logger.Warn($"[CardVfxUtil] FMOD 播放失败: {config.SecondarySfxPath}. 错误: {ex.Message}");
                }
            }

        } catch (Exception ex) {
            Entry.Logger.Warn($"[{logTag}] Error playing VFX: {ex.Message}");
        }
    }

    /// <summary>
    /// 延迟清理 VFX 节点
    /// </summary>
    public static async Task ClearAfter(Node2D? node, float delay) {
        await VFXUtil.Wait(delay);
        if (node != null && GodotObject.IsInstanceValid(node)) {
            node.QueueFreeSafely();
        }
    }
}
