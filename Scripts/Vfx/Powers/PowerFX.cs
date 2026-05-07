
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace RegentFX.Scripts.Vfx.Powers;

/// <summary>
/// 能力特效基类
/// </summary>
public abstract class PowerFX: FX {
    protected static bool _registryInitialized;
    public static readonly Dictionary<Type, Type> Registry = new();
    public static string GetToggleKey(Type type) => $"power_{type.Name}";
    public static bool IsTypeEnabled<T>() where T : PowerFX => Setting.ToggleEnabled(GetToggleKey(typeof(T)));
    public bool Enabled => Setting.ToggleEnabled(GetToggleKey(GetType()));
    
    public static IEnumerable<Type> GetTypes => typeof(PowerFX).Assembly.GetTypes()
        .Where(t => t.IsSubclassOf(typeof(PowerFX)) && !t.IsAbstract);
    
    public static void EnsureRegistry() {
        if (_registryInitialized) return;
        _registryInitialized = true;

        var fxTypes = GetTypes;

        foreach (var fxType in fxTypes) {
            var attr = fxType.GetCustomAttributes(typeof(PowerFxAttribute), false)
                .Cast<PowerFxAttribute>()
                .FirstOrDefault();
            if (attr != null) {
                Registry[attr.PowerType] = fxType;
            }
        }
    }

    public static PowerFX? FromPower(PowerModel power) {
        EnsureRegistry();
        if (power == null) return null;
        var powerType = power.GetType();
        if (Registry.TryGetValue(powerType, out var fxType)) {
            PowerFX? fx = (PowerFX?)Activator.CreateInstance(fxType);
            if (fx != null && fx.Enabled) {
                fx.power = power;
                return fx;
            }
        }
        return null;
    }

    public PowerModel? power;

    public virtual void BeforeBeforeApplied(Creature target, Decimal amount) {}
    public virtual void AfterAfterRemoved(Creature target) {}
    public virtual void AfterSetAmount(Decimal amount) {}
}
