using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Models;

namespace RegentFX.Scripts.Vfx;

/// <summary>
/// 攻击特效上下文：在AttackCommand执行时记录ModelSource，供动画链路末端读取
/// </summary>
public static class AttackVfxContext {
    private static readonly AsyncLocal<CardModel?> _currentModelSource = new();
    private static readonly AsyncLocal<bool> _shouldDisableRegentWeaponAttack = new();
    private static readonly AsyncLocal<bool> _shouldDisableRegentWeaponSfx = new();

    public static CardModel? CurrentModelSource {
        get => _currentModelSource.Value;
        set => _currentModelSource.Value = value;
    }

    public static bool ShouldDisableRegentWeaponAttack {
        get => _shouldDisableRegentWeaponAttack.Value;
        set => _shouldDisableRegentWeaponAttack.Value = value;
    }

    public static bool ShouldDisableRegentWeaponSFX {
        get => _shouldDisableRegentWeaponSfx.Value;
        set => _shouldDisableRegentWeaponSfx.Value = value;
    }

    public static AsyncLocal<AttackCommand?> CurrentAttackCommand { get; } = new();
}
