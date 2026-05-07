using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;
using RegentFx.Core.Audio;

namespace RegentFX.Scripts.Vfx.Cards;

[CardFx(typeof(MegaCrit.Sts2.Core.Models.Cards.LunarBlast))]
public class LunarBlast : CardFX {
    // -1 表示使用所有星星
    public override HoldingModes HoldingMode => HoldingModes.Custom;

    // 更高的位置
    public override Vector2 TargetOffset => new(100f, -450f);
    Vector2 GeneratePosAt() {
        return TargetOffset + VFXUtil.RandVec2(100f);
    }

    private static List<string> LunarScenePaths = new() {
        "res://RegentFX/scenes/vfx/lunar_blast_1.tscn",
        "res://RegentFX/scenes/vfx/lunar_blast_2.tscn",
    };

    public override List<string> AssetPaths => (new List<string> {VfxScenePath}).Concat(LunarScenePaths).ToList();

    public override string VfxScenePath => "res://RegentFX/scenes/vfx/laser_1.tscn";
    public static string LunarScenePath => LunarScenePaths[GD.RandRange(0,  LunarScenePaths.Count - 1)];

    public override bool HasExposureEffect => false;
    public override string HitSfxPath => "res://RegentFX/sfx/lunarTest1.mp3";
    public string HitSfxPath2 => "res://RegentFX/sfx/lunarTest1.mp3";
    
    public override void HoldingCustom() {
        if (card is MegaCrit.Sts2.Core.Models.Cards.LunarBlast lb) {
            int starCount = (int)((CalculatedVar)lb.DynamicVars["CalculatedHits"]).Calculate(null);
            Entry.Logger.Info("Lunar Blast Hits: " + starCount);
            Creature owner = card.Owner.Creature;
            NCreature? ownerNode = NCombatRoom.Instance?.GetCreatureNode(owner);
            if (Entry.StarEffectController == null) return;
            if (ownerNode == null) return;
            if (starCount > 0) {
                for (var i = 0; i < starCount; i++) {
                    var starPos = GeneratePosAt();
                    Entry.StarEffectController.GenerateStarAt(starPos, new Color(8.4f, 8.9f, 9f));
                }
                Entry.StarEffectController.StartShaking();
                TryPlayHoldingSfx();
            }
        }
    }
    
    public override bool UseV2Patch => true;
    public override bool HasOnBeforeDamage => true;
    public override async Task OnBeforeDamage(AttackCommand command) {
        Creature? owner = card?.Owner.Creature;
        Creature? target = command._singleTarget;
        if (owner == null || target == null || command._singleTarget == null) return;
        Vector2? starPos = Entry.StarEffectController?.PopStar(this);
        if (!starPos.HasValue) {
            NCreature? ownerNode = NCombatRoom.Instance?.GetCreatureNode(owner);
            Vector2 ownerPos = ownerNode?.VfxSpawnPosition ?? Vector2.Zero;
            starPos = ownerPos + GeneratePosAt();
        }
        if (TestMode.IsOn) return;
        NCreature? targetNode = NCombatRoom.Instance?.GetCreatureNode(target);
        if (targetNode == null) return;
        Vector2 targetPos = targetNode.VfxSpawnPosition;
        _ = WorldEnvironmentUtil.FullExposure(1.5f, 0f, 0.05f, 0.05f);
        await Task.WhenAll(
            PlayLunarVfx(targetPos, starPos.Value),
            PlayLaserVfx(targetPos, starPos.Value)
        );
    }

    private async Task PlayLaserVfx(Vector2 targetPos, Vector2 starPos) {
        string scenePath = VfxScenePath;
        try {
            Node2D vfxNode = VFXUtil.GenVFXNode(scenePath);
            Node2D? startNode = vfxNode.FindChild("StartPos") as Node2D;
            if (startNode == null) {
                Entry.Logger.Error($"[Laser] No StartPos found in VFX scene");
                return;
            }

            vfxNode.FitVFX(startNode.GlobalPosition, Vector2.Zero, starPos, targetPos);
            vfxNode.GlobalPosition = targetPos;

            NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(vfxNode);

            SimpleSfxUtil.Play(HitSfxPath);
            SimpleSfxUtil.Play(HitSfxPath2);

            _ = TaskHelper.RunSafely(CardVfxUtil.ClearAfter(vfxNode, VfxClearDelay));
            await Cmd.Wait(0.15f);

        } catch (Exception ex) {
            Entry.Logger.Warn($"[Laser] Error playing VFX: {ex.Message}");
        }
    }

    private async Task PlayLunarVfx(Vector2 targetPos, Vector2 starPos) {
        try {
            Node2D vfxNode = VFXUtil.GenVFXNode(LunarScenePath);
            if (vfxNode == null) return;
            // 计算旋转角度，使特效朝向目标
            Vector2 direction = targetPos - starPos;
            float rotationDegrees = Mathf.RadToDeg(Mathf.Atan2(direction.Y, direction.X));
            vfxNode.RotationDegrees = rotationDegrees;

            // 特效位置: 目标位置减去长矛长度（沿旋转方向）
            Vector2 rotationDirection = direction.Normalized();
            vfxNode.GlobalPosition = starPos;

            // 添加到战斗特效容器
            NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(vfxNode);

            _ = TaskHelper.RunSafely(CardVfxUtil.ClearAfter(vfxNode, 2f));
            await Cmd.Wait(0.15f);

        } catch (Exception ex) {
            Entry.Logger.Warn($"[CrescentSpear] Error playing VFX: {ex.Message}");
        }
    }
}

