
using MegaCrit.Sts2.Core.Models;

namespace RegentFX.Scripts.Vfx.Powers;

/// <summary>
/// 卡牌特效基类
/// 每张卡牌继承此类，实现各自的星星借用逻辑
/// </summary>
public abstract class PowerFX {
    private static readonly Dictionary<Type, Type> PowerFxRegistry = new();
    private static bool _registryInitialized;

    private static void EnsureRegistry() {
        if (_registryInitialized) return;
        _registryInitialized = true;

        var fxTypes = typeof(PowerFX).Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(PowerFX)) && !t.IsAbstract);

        foreach (var fxType in fxTypes) {
            var attr = fxType.GetCustomAttributes(typeof(PowerFxAttribute), false)
                .Cast<PowerFxAttribute>()
                .FirstOrDefault();
            if (attr != null) {
                PowerFxRegistry[attr.PowerType] = fxType;
            }
        }
    }

    public static PowerFX? FromPower(PowerModel power) {
        EnsureRegistry();
        if (power == null) return null;
        var powerType = power.GetType();
        if (PowerFxRegistry.TryGetValue(powerType, out var fxType)) {
            PowerFX? fx = (PowerFX?)Activator.CreateInstance(fxType);
            if (fx != null) fx.power = power;
            return fx;
        }
        return null;
    }

    public PowerModel? power;

}
