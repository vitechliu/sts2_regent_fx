using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using RegentFX.Scripts;

namespace RegentFX.Scripts.Patches;

/// <summary>
/// 储君Star能量槽改变监听补丁
/// </summary>
public static class StarEnergyPatch
{
    /// <summary>
    /// 监听 Stars 属性的 setter 方法
    /// </summary>
    [HarmonyPatch(typeof(PlayerCombatState), nameof(PlayerCombatState.Stars), MethodType.Setter)]
    public static class StarsSetterPatch
    {
        public static void Prefix(PlayerCombatState __instance, int value)
        {
            int currentStars = __instance.Stars;
            if (currentStars != value)
            {
                Entry.Logger.Info($"[StarEnergy] Stars即将改变: {currentStars} -> {value} (变化: {value - currentStars:+#;-#;0})");
            }

            Player p = __instance._player;
        }

        public static void Postfix(PlayerCombatState __instance, int value)
        {
            Entry.Logger.Info($"[StarEnergy] Stars已改变，当前值: {value}");
        }
    }

    /// <summary>
    /// 监听 GainStars 方法
    /// </summary>
    [HarmonyPatch(typeof(PlayerCombatState), nameof(PlayerCombatState.GainStars))]
    public static class GainStarsPatch
    {
        public static void Prefix(decimal amount)
        {
            Entry.Logger.Info($"[StarEnergy] GainStars被调用，增加量: {amount}");
        }
    }

    /// <summary>
    /// 监听 LoseStars 方法
    /// </summary>
    [HarmonyPatch(typeof(PlayerCombatState), nameof(PlayerCombatState.LoseStars))]
    public static class LoseStarsPatch
    {
        public static void Prefix(decimal amount)
        {
            Entry.Logger.Info($"[StarEnergy] LoseStars被调用，减少量: {amount}");
        }
    }
}
