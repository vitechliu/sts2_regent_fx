using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;
using RegentFx.Core.Audio;

namespace RegentFX.Scripts.Vfx.Cards;

/// <summary>
/// 卡牌特效基类
/// 每张卡牌继承此类，实现各自的星星借用逻辑
/// </summary>
public abstract class CardFX {

    public CardFX(CardModel card) {
        
    }
    public static CardFX? FromCard(CardModel card) {
        return card switch {
            MegaCrit.Sts2.Core.Models.Cards.FallingStar => new FallingStar(card),
            MegaCrit.Sts2.Core.Models.Cards.CrescentSpear => new CrescentSpear(),
            MegaCrit.Sts2.Core.Models.Cards.Stardust => new Stardust(),
            MegaCrit.Sts2.Core.Models.Cards.SevenStars => new SevenStars(),
            _ => null
        };
    }
    
    /// <summary>
    /// 星星数量，-1 表示使用所有星星
    /// </summary>
    public virtual int StarCount => 0;

    /// <summary>
    /// 目标位置偏移（相对于角色位置）
    /// </summary>
    public virtual Vector2 TargetOffset => new(-260f, -400f);

    /// <summary>
    /// 星星间距
    /// </summary>
    public virtual float StarSpacing => 40f;

    /// <summary>
    /// 基础移动时长
    /// </summary>
    public virtual float MoveDuration => 0.3f;

    /// <summary>
    /// 时长随机波动范围 (0.8 ~ 1.2 表示 80% ~ 120%)
    /// </summary>
    public virtual (float min, float max) DurationRange => (0.8f, 1.2f);

    /// <summary>
    /// 震颤幅度
    /// </summary>
    public virtual float ShakeIntensity => 5f;

    /// <summary>
    /// 震颤速度
    /// </summary>
    public virtual float ShakeSpeed => 20f;

    /// <summary>
    /// 音效路径
    /// </summary>
    public virtual string? HoldSfxPath => "res://RegentFX/sfx/common_hold_1.mp3";


    // public virtual string? AttackerAnimNameChange => null;
    
    // public virtual bool DisableWeaponAnim => false;



    /// <summary>
    /// 计算第 index 颗星星的目标位置
    /// </summary>
    public virtual Vector2 CalculateTargetPosition(Vector2 basePosition, int index, int totalCount) {
        Vector2 position = basePosition + TargetOffset;

        if (totalCount > 1) {
            float totalWidth = (totalCount - 1) * StarSpacing;
            float startX = -totalWidth / 2f;
            position.X += startX + index * StarSpacing;
        }

        return position;
    }

    public virtual void OnStartHolding(Star star, int index) {}
}
