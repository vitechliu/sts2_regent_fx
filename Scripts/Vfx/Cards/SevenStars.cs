using Godot;

namespace RegentFX.Scripts.Vfx.Cards;

public class SevenStars : CardFX {
    // -1 表示使用所有星星
    public override int StarCount => 7;

    // 更高的位置
    public override Vector2 TargetOffset => new(-100f, -450f);
}
