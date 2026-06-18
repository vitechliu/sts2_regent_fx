using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Models;

namespace RegentFX.Scripts.Vfx;

/// <summary>
/// 攻击特效上下文：在AttackCommand执行时记录ModelSource，供动画链路末端读取
/// </summary>
public static class AttackVfxContext {
    public static CardModel? CurrentModelSource { get; set; }

    public static bool ShouldDisableRegentWeaponAttack = false;
    public static bool ShouldDisableRegentWeaponSFX = false;

    public static AsyncLocal<AttackCommand?> CurrentAttackCommand { get; } = new();
}
