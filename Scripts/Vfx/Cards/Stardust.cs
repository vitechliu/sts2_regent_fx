using Godot;

namespace RegentFX.Scripts.Vfx.Cards;

/// <summary>
/// Stardust 卡牌特效
/// </summary>
public class Stardust : CardFX {
    // -1 表示使用所有星星
    public override int StarCount => -1;

    // 更高的位置
    public override Vector2 TargetOffset => new(-100f, -450f);

    // 较慢的移动
    public override float MoveDuration => 0.4f;

    // 轻微的震颤
    public override float ShakeIntensity => 3f;
    public override float ShakeSpeed => 15f;

    // 较小的间距
    public override float StarSpacing => 30f;
}
