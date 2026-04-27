using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

namespace RegentFX.Scripts.Vfx.Cards;

[CardFx(typeof(MegaCrit.Sts2.Core.Models.Cards.SevenStars))]
public class SevenStars : CardFX {
    // -1 表示使用所有星星
    public override int StarCount => 7;

    public override string HoldSfxPath => "res://RegentFX/sfx/seven_stars_hold.mp3";

    // 更高的位置
    public override Vector2 TargetOffset => new(-100f, -450f);

    private readonly List<Vector2> starPos = new() {
        new Vector2(5f, 0f),      // 天枢
        new Vector2(-47f, -5f),    // 天璇
        new Vector2(-55f, 54f),   // 天玑
        new Vector2(0f, 50f),     // 天权
        new Vector2(50f, 60f),    // 玉衡
        new Vector2(100f, 70f),   // 开阳
        new Vector2(150f, 85f)    // 摇光
    };

    public override void OnStartHolding(Star star, int index) {
        if (Entry.StarEffectController == null) return;
        Star? lastStar = Entry.StarEffectController.Stars.FindLast(star1 => star != star1);
        if (lastStar != null) {
            // Entry.Logger.Info("ConnectToStar");
            star.ConnectTo(lastStar);
        }
        else {
            // Entry.Logger.Info("MainStar");
            //天枢
            star.ChangeColorTo(new Color(14.551f, 14.551f, 0.0f)); //yellow
            star.PulseSpeed = 2.4f;
            star.PulseMaxScale *= 1.3f;
            star.PulseMinScale *= 1.4f;
        }
    }

    public override Vector2 CalculateTargetPosition(Vector2 basePosition, int index, int totalCount) {
        return basePosition + TargetOffset + (starPos[index] * 1.2f);
    }
    
    private async Task PlayVfx(IReadOnlyList<Creature> enemies) {
        if (TestMode.IsOn) return;
        Entry.StarEffectController?.PopStar(this);
        SfxCmd.Play("event:/sfx/characters/regent/regent_attack");
        foreach (Creature enemy in enemies) {
            NCreature? targetNode = NCombatRoom.Instance?.GetCreatureNode(enemy);
            if (targetNode == null) {
                Entry.Logger.Info("Could not get creature nodes for VFX");
                continue;
            }
            Blade.PlayBlade(targetNode.VfxSpawnPosition);
        }
        await Cmd.Wait(0.05f);
    }
    
    public override bool UseV2Patch => true;
    public override bool HasOnBeforeDamage => true;
    public override async Task OnBeforeDamage(AttackCommand command) {
        var enemies = VFXUtil.GetHittableEnemiesFromCard(card);
        if (enemies != null)
            await PlayVfx(enemies);
    }
}

