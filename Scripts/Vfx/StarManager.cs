using Godot;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Helpers;

namespace RegentFX.Vfx;

/// <summary>
/// 星星管理器
/// 用于批量生成和管理多个星星
/// </summary>
public static class StarManager
{
    private const string StarScenePath = "res://RegentFX/scenes/Star.tscn";
    private static PackedScene? _cachedScene;

    /// <summary>
    /// 获取星星场景（带缓存）
    /// </summary>
    private static PackedScene GetStarScene()
    {
        if (_cachedScene == null)
        {
            _cachedScene = GD.Load<PackedScene>(StarScenePath);
            if (_cachedScene == null)
            {
                GD.PushError($"[StarManager] Failed to load star scene: {StarScenePath}");
            }
        }
        return _cachedScene!;
    }

    /// <summary>
    /// 在指定位置生成单个星星
    /// </summary>
    public static Star? SpawnStar(Vector2 position, Node? parent = null)
    {
        var scene = GetStarScene();
        if (scene == null) return null;

        var star = scene.Instantiate<Star>();
        if (star == null)
        {
            GD.PushError("[StarManager] Failed to instantiate star");
            return null;
        }

        // 如果没有指定父节点，尝试使用战斗场景的VFX容器
        if (parent == null)
        {
            parent = NCombatRoom.Instance?.CombatVfxContainer;
        }

        if (parent == null)
        {
            GD.PushWarning("[StarManager] No parent available for star");
            star.QueueFree();
            return null;
        }

        parent.AddChildSafely(star);
        star.GlobalPosition = position;

        return star;
    }

    /// <summary>
    /// 在区域内随机生成多个星星
    /// </summary>
    public static List<Star> SpawnStarsInArea(Vector2 center, Vector2 size, int count, Node? parent = null)
    {
        var stars = new List<Star>();
        var rng = new RandomNumberGenerator();
        rng.Randomize();

        for (int i = 0; i < count; i++)
        {
            float x = rng.RandfRange(center.X - size.X / 2, center.X + size.X / 2);
            float y = rng.RandfRange(center.Y - size.Y / 2, center.Y + size.Y / 2);

            var star = SpawnStar(new Vector2(x, y), parent);
            if (star != null)
            {
                stars.Add(star);
            }
        }

        return stars;
    }

    /// <summary>
    /// 在圆形区域内随机生成多个星星
    /// </summary>
    public static List<Star> SpawnStarsInCircle(Vector2 center, float radius, int count, Node? parent = null)
    {
        var stars = new List<Star>();
        var rng = new RandomNumberGenerator();
        rng.Randomize();

        for (int i = 0; i < count; i++)
        {
            float angle = rng.RandfRange(0, Mathf.Tau);
            float distance = rng.RandfRange(0, radius);

            float x = center.X + Mathf.Cos(angle) * distance;
            float y = center.Y + Mathf.Sin(angle) * distance;

            var star = SpawnStar(new Vector2(x, y), parent);
            if (star != null)
            {
                stars.Add(star);
            }
        }

        return stars;
    }

    /// <summary>
    /// 生成围绕中心的星星环
    /// </summary>
    public static List<Star> SpawnStarRing(Vector2 center, float radius, int count, float startAngle = 0f, Node? parent = null)
    {
        var stars = new List<Star>();
        float angleStep = Mathf.Tau / count;

        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + angleStep * i;
            float x = center.X + Mathf.Cos(angle) * radius;
            float y = center.Y + Mathf.Sin(angle) * radius;

            var star = SpawnStar(new Vector2(x, y), parent);
            if (star != null)
            {
                // 让星星朝向中心
                star.Rotation = angle + Mathf.Pi / 2;
                stars.Add(star);
            }
        }

        return stars;
    }

    /// <summary>
    /// 生成带有延迟的星星序列（用于动画效果）
    /// </summary>
    public static async void SpawnStarSequence(Vector2[] positions, float delayBetweenStars, Node? parent = null)
    {
        foreach (var pos in positions)
        {
            SpawnStar(pos, parent);
            await Task.Delay((int)(delayBetweenStars * 1000));
        }
    }

    /// <summary>
    /// 清除所有星星（渐隐销毁）
    /// </summary>
    public static void ClearAllStars(float fadeDuration = 0.5f)
    {
        var parent = NCombatRoom.Instance?.CombatVfxContainer;
        if (parent == null) return;

        foreach (var child in parent.GetChildren())
        {
            if (child is Star star)
            {
                star.FadeOutAndDestroy(fadeDuration);
            }
        }
    }

    /// <summary>
    /// 获取当前所有星星
    /// </summary>
    public static List<Star> GetAllStars()
    {
        var stars = new List<Star>();
        var parent = NCombatRoom.Instance?.CombatVfxContainer;
        if (parent == null) return stars;

        foreach (var child in parent.GetChildren())
        {
            if (child is Star star)
            {
                stars.Add(star);
            }
        }

        return stars;
    }
}
