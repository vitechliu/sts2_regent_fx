namespace RegentFX.Scripts.Vfx.Powers;

/// <summary>
/// 标记一个 PowerFx 子类对应的游戏卡牌类型。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class PowerFxAttribute(Type powerType) : Attribute {
    /// <summary>
    /// 游戏内能力类型（如 typeof(MegaCrit.Sts2.Core.Models.Powers.BlackHolePower)）
    /// </summary>
    public Type PowerType { get; } = powerType;
}
