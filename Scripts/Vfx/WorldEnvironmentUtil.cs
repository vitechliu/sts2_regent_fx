using Godot;
using MegaCrit.Sts2.Core.Nodes;

namespace RegentFX.Scripts.Vfx;

/// <summary>
/// 全屏特效工具类，基于 NGame 中的 WorldEnvironment 节点实现。
/// 支持发光（Glow/Bloom）、饱和度、亮度、对比度、曝光等后处理效果的动态调整。
/// </summary>
public static class WorldEnvironmentUtil
{
    private static WorldEnvironment? _cachedEnv;

    /// <summary>
    /// 获取当前激活的 WorldEnvironment 节点。
    /// 如果没有激活，会自动调用 NGame.Instance.ActivateWorldEnvironment()。
    /// </summary>
    public static WorldEnvironment? GetOrActivateEnvironment()
    {
        if (_cachedEnv != null && GodotObject.IsInstanceValid(_cachedEnv))
        {
            return _cachedEnv;
        }

        if (NGame.Instance == null)
        {
            Entry.Logger.Warn("NGame.Instance is null, cannot activate WorldEnvironment.");
            return null;
        }

        _cachedEnv = NGame.Instance.ActivateWorldEnvironment();
        return _cachedEnv;
    }

    /// <summary>
    /// 关闭 WorldEnvironment 特效节点。
    /// </summary>
    public static void DeactivateEnvironment()
    {
        if (NGame.Instance == null)
        {
            Entry.Logger.Warn("NGame.Instance is null, cannot deactivate WorldEnvironment.");
            return;
        }

        NGame.Instance.DeactivateWorldEnvironment();
        _cachedEnv = null;
    }

    /// <summary>
    /// 设置发光（Bloom）强度。需要 Environment 开启 Glow 才能看到效果。
    /// </summary>
    /// <param name="intensity">发光强度，默认 0.8，范围建议 0~3</param>
    public static void SetGlowIntensity(float intensity)
    {
        var env = GetOrActivateEnvironment();
        if (env == null) return;

        env.Environment.GlowIntensity = intensity;
    }

    /// <summary>
    /// 设置发光（Bloom）强度并支持 Tween 动画过渡。
    /// </summary>
    /// <param name="intensity">目标发光强度</param>
    /// <param name="duration">动画时长（秒）</param>
    /// <param name="ease">缓动类型</param>
    /// <param name="trans">过渡类型</param>
    /// <returns>创建的 Tween 实例，可用于链式调用或控制</returns>
    public static Tween? TweenGlowIntensity(float intensity, float duration, Tween.EaseType ease = Tween.EaseType.InOut, Tween.TransitionType trans = Tween.TransitionType.Cubic)
    {
        var env = GetOrActivateEnvironment();
        if (env == null) return null;

        var tween = env.CreateTween();
        tween?.TweenProperty(env, "environment:glow_intensity", intensity, duration)
            .SetEase(ease)
            .SetTrans(trans);
        return tween;
    }

    /// <summary>
    /// 设置曝光（Tonemap Exposure）。
    /// </summary>
    public static void SetExposure(float exposure)
    {
        var env = GetOrActivateEnvironment();
        if (env == null) return;

        env.Environment.TonemapExposure = exposure;
    }

    /// <summary>
    /// Tween 动画设置曝光。
    /// </summary>
    public static Tween? TweenExposure(float exposure, float duration, Tween.EaseType ease = Tween.EaseType.InOut, Tween.TransitionType trans = Tween.TransitionType.Cubic)
    {
        var env = GetOrActivateEnvironment();
        if (env == null) return null;

        var tween = env.CreateTween();
        tween?.TweenProperty(env, "environment:tonemap_exposure", exposure, duration)
            .SetEase(ease)
            .SetTrans(trans);
        return tween;
    }

    /// <summary>
    /// 设置亮度调整（Adjustment Brightness）。
    /// </summary>
    public static void SetBrightness(float brightness)
    {
        var env = GetOrActivateEnvironment();
        if (env == null) return;

        env.Environment.AdjustmentBrightness = brightness;
    }

    /// <summary>
    /// Tween 动画设置亮度。
    /// </summary>
    public static Tween? TweenBrightness(float brightness, float duration, Tween.EaseType ease = Tween.EaseType.InOut, Tween.TransitionType trans = Tween.TransitionType.Cubic)
    {
        var env = GetOrActivateEnvironment();
        if (env == null) return null;

        var tween = env.CreateTween();
        tween?.TweenProperty(env, "environment:adjustment_brightness", brightness, duration)
            .SetEase(ease)
            .SetTrans(trans);
        return tween;
    }

    /// <summary>
    /// 设置对比度调整（Adjustment Contrast）。
    /// </summary>
    public static void SetContrast(float contrast)
    {
        var env = GetOrActivateEnvironment();
        if (env == null) return;

        env.Environment.AdjustmentContrast = contrast;
    }

    /// <summary>
    /// Tween 动画设置对比度。
    /// </summary>
    public static Tween? TweenContrast(float contrast, float duration, Tween.EaseType ease = Tween.EaseType.InOut, Tween.TransitionType trans = Tween.TransitionType.Cubic)
    {
        var env = GetOrActivateEnvironment();
        if (env == null) return null;

        var tween = env.CreateTween();
        tween?.TweenProperty(env, "environment:adjustment_contrast", contrast, duration)
            .SetEase(ease)
            .SetTrans(trans);
        return tween;
    }

    /// <summary>
    /// 设置饱和度调整（Adjustment Saturation）。
    /// </summary>
    public static void SetSaturation(float saturation)
    {
        var env = GetOrActivateEnvironment();
        if (env == null) return;

        env.Environment.AdjustmentSaturation = saturation;
    }

    /// <summary>
    /// Tween 动画设置饱和度。
    /// </summary>
    public static Tween? TweenSaturation(float saturation, float duration, Tween.EaseType ease = Tween.EaseType.InOut, Tween.TransitionType trans = Tween.TransitionType.Cubic)
    {
        var env = GetOrActivateEnvironment();
        if (env == null) return null;

        var tween = env.CreateTween();
        tween?.TweenProperty(env, "environment:adjustment_saturation", saturation, duration)
            .SetEase(ease)
            .SetTrans(trans);
        return tween;
    }

    /// <summary>
    /// 重置所有调整参数为默认值（曝光1，亮度1，对比度1，饱和度1，发光强度0.8）。
    /// 不会自动 DeactivateEnvironment，如需关闭请手动调用。
    /// </summary>
    public static void ResetToDefaults()
    {
        var env = GetOrActivateEnvironment();
        if (env == null) return;

        env.Environment.TonemapExposure = 1f;
        env.Environment.AdjustmentBrightness = 1f;
        env.Environment.AdjustmentContrast = 1f;
        env.Environment.AdjustmentSaturation = 1f;
        env.Environment.GlowIntensity = 0.8f;
    }

    /// <summary>
    /// Tween 动画重置所有调整参数为默认值。
    /// </summary>
    /// <param name="duration">每个属性的动画时长（秒）</param>
    /// <returns>创建的 Tween 实例</returns>
    public static Tween? TweenResetToDefaults(float duration)
    {
        var env = GetOrActivateEnvironment();
        if (env == null) return null;

        var tween = env.CreateTween().SetParallel();
        tween.TweenProperty(env, "environment:tonemap_exposure", 1f, duration);
        tween.TweenProperty(env, "environment:adjustment_brightness", 1f, duration);
        tween.TweenProperty(env, "environment:adjustment_contrast", 1f, duration);
        tween.TweenProperty(env, "environment:adjustment_saturation", 1f, duration);
        tween.TweenProperty(env, "environment:glow_intensity", 0.8f, duration);
        return tween;
    }
}
