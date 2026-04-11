using Godot;

namespace RegentFX.Scripts.Vfx.Cards;

/// <summary>
/// FallingStar 卡牌特效
/// </summary>
public class FallingStar : CardFX {
    public override int StarCount => 2;

    // 更靠左上的位置
    public override Vector2 TargetOffset => new(-200f, -350f);

    // 更快的移动
    public override float MoveDuration => 0.2f;

    // 更剧烈的震颤
    public override float ShakeIntensity => 6f;
    public override float ShakeSpeed => 25f;

    // 更大的间距
    public override float StarSpacing => 60f;
}
