using Godot;
using MegaCrit.Sts2.Core.Models;

namespace RegentFX.Scripts.Vfx.Cards;

/// <summary>
/// 卡牌特效基类
/// 每张卡牌继承此类，实现各自的星星借用逻辑
/// </summary>
public abstract class CardFX {
    private static readonly Dictionary<Type, Type> CardFxRegistry = new();
    private static bool _registryInitialized;

    private static void EnsureRegistry() {
        if (_registryInitialized) return;
        _registryInitialized = true;

        var fxTypes = typeof(CardFX).Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(CardFX)) && !t.IsAbstract);

        foreach (var fxType in fxTypes) {
            var attr = fxType.GetCustomAttributes(typeof(CardFxAttribute), false)
                .Cast<CardFxAttribute>()
                .FirstOrDefault();
            if (attr != null) {
                CardFxRegistry[attr.CardType] = fxType;
            }
        }
    }

    public static CardFX? FromCard(CardModel card) {
        EnsureRegistry();
        if (card == null) return null;
        var cardType = card.GetType();
        if (CardFxRegistry.TryGetValue(cardType, out var fxType)) {
            return (CardFX?)Activator.CreateInstance(fxType);
        }
        return null;
    }

    public virtual bool BorrowStar => true;
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

    /// <summary>
    /// VFX 场景路径
    /// </summary>
    public virtual string? VfxScenePath => null;

    /// <summary>
    /// 命中音效路径
    /// </summary>
    public virtual string? HitSfxPath => null;

    /// <summary>
    /// 第二段音效路径（如需要）
    /// </summary>
    public virtual string? SecondarySfxPath => null;

    /// <summary>
    /// VFX 清理延迟（秒）
    /// </summary>
    public virtual float VfxClearDelay => 2f;

    /// <summary>
    /// 是否触发曝光效果
    /// </summary>
    public virtual bool HasExposureEffect => false;

    /// <summary>
    /// 曝光峰值
    /// </summary>
    public virtual float ExposurePeak => 3f;

    /// <summary>
    /// 曝光进入时长
    /// </summary>
    public virtual float ExposureInDuration => 0.1f;

    /// <summary>
    /// 曝光恢复时长
    /// </summary>
    public virtual float ExposureOutDuration => 0.5f;

    /// <summary>
    /// 播放 VFX 时是否通知 StarEffectController 取消卡牌效果
    /// </summary>
    public virtual bool CancelsStarEffect => true;

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
