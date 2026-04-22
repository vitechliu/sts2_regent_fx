using System;

namespace RegentFX.Scripts.Vfx.Cards;

/// <summary>
/// 标记一个 CardFX 子类对应的游戏卡牌类型。
/// 用于自动注册，避免在 FromCard 中手动维护 switch 语句。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class CardFxAttribute(Type cardType) : Attribute {
    /// <summary>
    /// 游戏内卡牌类型（如 typeof(MegaCrit.Sts2.Core.Models.Cards.FallingStar)）
    /// </summary>
    public Type CardType { get; } = cardType;
}
