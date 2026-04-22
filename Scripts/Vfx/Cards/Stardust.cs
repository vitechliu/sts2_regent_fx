using Godot;

namespace RegentFX.Scripts.Vfx.Cards;

/// <summary>
/// Stardust 卡牌特效 todo
/// </summary>
[CardFx(typeof(MegaCrit.Sts2.Core.Models.Cards.Stardust))]
public class Stardust : CardFX {
    // -1 表示使用所有星星
    public override int StarCount => -1;
}
