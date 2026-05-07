using Godot;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Models;
using RegentFx.Core.Audio;

namespace RegentFX.Scripts.Vfx.Cards;

/// <summary>
/// 卡牌特效基类
/// 每张卡牌继承此类，实现各自的星星借用逻辑
/// </summary>
public abstract class CardFX: FX {
    protected static bool _registryInitialized;
    public static readonly Dictionary<Type, Type> Registry = new();
    public static string GetToggleKey(Type type) => $"card_{type.Name}";
    public static bool IsTypeEnabled<T>() where T : CardFX => Setting.ToggleEnabled(GetToggleKey(typeof(T)));
    public bool Enabled => Setting.ToggleEnabled(GetToggleKey(GetType()));

    public static IEnumerable<Type> GetTypes => typeof(CardFX).Assembly.GetTypes()
        .Where(t => t.IsSubclassOf(typeof(CardFX)) && !t.IsAbstract);
    
    public static void EnsureRegistry() {
        if (_registryInitialized) return;
        _registryInitialized = true;

        var fxTypes = GetTypes;

        foreach (var fxType in fxTypes) {
            var attr = fxType.GetCustomAttributes(typeof(CardFxAttribute), false)
                .Cast<CardFxAttribute>()
                .FirstOrDefault();
            if (attr != null) {
                Registry[attr.CardType] = fxType;
            }
        }
    }

    public static CardFX? FromCard(CardModel card) {
        EnsureRegistry();
        if (card == null) return null;
        var cardType = card.GetType();
        if (Registry.TryGetValue(cardType, out var fxType)) {
            CardFX? fx = (CardFX?)Activator.CreateInstance(fxType);
            if (fx != null && fx.Enabled) {
                fx.card = card;
                return fx;
            }
        }
        return null;
    }

    public CardModel? card;

    
    public enum HoldingModes {
        BorrowDefault,
        BorrowAll,
        Custom,
        None, //不借取
    }

    public virtual HoldingModes HoldingMode => HoldingModes.BorrowDefault;
    public virtual void HoldingCustom() {}
    
    //星星数量
    public virtual int StarCount => 0;
    //目标位置偏移（相对于角色位置）
    public virtual Vector2 TargetOffset => new(-260f, -400f);
    //星星间距
    public virtual float StarSpacing => 40f;
    // 基础移动时长
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
    public virtual string HoldSfxPath => "res://RegentFX/sfx/common_hold_1.mp3";

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



    public virtual bool UseV2Patch => false;

    // public virtual string? AttackerAnimNameChange => null;
    
    //v2Patch
    // public virtual bool DisableAttackAnim => true;
    // public virtual bool PlayCastAnim => true;

    //是否要阻止默认的储君武器动画
    public virtual bool ShouldDisableRegentWeaponAttack => true;
    
    //AttackExecute执行前触发
    public virtual bool HasOnBeforeExecute => false;
    public virtual async Task OnBeforeExecute() {}
    
    //BeforeDamage植入
    public virtual bool HasOnBeforeDamage => false;
    public virtual async Task OnBeforeDamage(AttackCommand command) {}

    public virtual bool HasAfterPlay => false;
    public virtual void AfterPlay() {}

    public virtual string? ChangeHitFx => null;


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


    public void TryPlayHoldingSfx() {
        if (!string.IsNullOrEmpty(HoldSfxPath)) {
            SimpleSfxUtil.Play(HoldSfxPath);
        }
    }
}
