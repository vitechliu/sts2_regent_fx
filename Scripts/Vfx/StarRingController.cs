using Godot;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using RegentFX.Scripts;

namespace RegentFX.Vfx;

/// <summary>
/// 储君周围环绕星星控制器
/// 管理星星的生成、销毁、旋转动画
/// </summary>
public partial class StarRingController : Node2D
{
    [Export] public float OrbitRadius { get; set; } = 104f;  // 80f * 1.3 = 104f，水平半径提升30%
    [Export] public float OrbitSpeed { get; set; } = 30f;
    [Export] public float StarScaleMin { get; set; } = 0.6f;
    [Export] public float StarScaleMax { get; set; } = 1.0f;
    [Export] public int MaxStarCount { get; set; } = 20;
    [Export] public float SpawnAnimationDuration { get; set; } = 0.3f;
    [Export] public float VerticalOffset { get; set; } = -80f;  // 从-30提升到-80，让星星在角色胸部/头部高度

    private readonly List<Star> _orbitStars = new();
    private NCreature? _playerNode;
    private float _orbitAngle;
    private bool _isActive;

    public override void _Process(double delta)
    {
        if (!_isActive || _playerNode == null) return;

        // 更新控制器位置到玩家位置（因为控制器现在在VFX容器中）
        GlobalPosition = _playerNode.GlobalPosition;

        // 更新轨道角度
        _orbitAngle += OrbitSpeed * (float)delta;

        // 更新所有星星位置
        UpdateStarPositions();
    }

    /// <summary>
    /// 输出节点层级和ZIndex信息
    /// </summary>
    private void LogNodeHierarchy(Node node)
    {
        Entry.Logger.Info("[StarRing] ========== Node Hierarchy Debug ==========");

        // 输出角色信息
        Entry.Logger.Info($"[StarRing] PlayerNode: {node.Name}, Type: {node.GetType().Name}");
        if (node is CanvasItem nodeCanvas)
        {
            Entry.Logger.Info($"[StarRing] PlayerNode ZIndex: {nodeCanvas.ZIndex}, ZAsRelative: {nodeCanvas.ZAsRelative}");
        }

        // 输出角色父节点信息
        var playerParent = node.GetParent();
        Entry.Logger.Info($"[StarRing] PlayerNode Parent: {playerParent?.Name}, Type: {playerParent?.GetType().Name}");
        if (playerParent is CanvasItem playerParentCanvas)
        {
            Entry.Logger.Info($"[StarRing] PlayerNode Parent ZIndex: {playerParentCanvas.ZIndex}, ZAsRelative: {playerParentCanvas.ZAsRelative}");
        }

        // 输出当前控制器信息
        Entry.Logger.Info($"[StarRing] StarRingController Parent: {GetParent()?.Name}, Type: {GetParent()?.GetType().Name}");
        if (GetParent() is CanvasItem controllerParentCanvas)
        {
            Entry.Logger.Info($"[StarRing] StarRingController Parent ZIndex: {controllerParentCanvas.ZIndex}, ZAsRelative: {controllerParentCanvas.ZAsRelative}");
        }

        // 向上遍历层级
        Node? current = playerParent;
        int level = 0;
        while (current != null && level < 5)
        {
            if (current is CanvasItem canvasItem)
            {
                Entry.Logger.Info($"[StarRing] Level {level}: {current.Name} (ZIndex={canvasItem.ZIndex}, ZAsRelative={canvasItem.ZAsRelative})");
            }
            else
            {
                Entry.Logger.Info($"[StarRing] Level {level}: {current.Name} (not CanvasItem)");
            }
            current = current.GetParent();
            level++;
        }

        Entry.Logger.Info("[StarRing] ========== End Debug ==========");
    }

    /// <summary>
    /// 初始化控制器，绑定到玩家节点
    /// </summary>
    public void Initialize(NCreature playerNode)
    {
        _playerNode = playerNode;
        _isActive = true;

        // 初始位置设置为玩家位置
        GlobalPosition = playerNode.GlobalPosition;

        // 调试：输出详细的节点层级和ZIndex信息
        LogNodeHierarchy(playerNode);

        // 设置ZAsRelative为true，使ZIndex相对于AllyContainer（与角色一致）
        ZAsRelative = true;
    }

    /// <summary>
    /// 设置星星数量
    /// </summary>
    public void SetStarCount(int count)
    {
        int targetCount = Mathf.Min(count, MaxStarCount);
        int currentCount = _orbitStars.Count;

        if (targetCount > currentCount)
        {
            // 需要增加星星
            for (int i = 0; i < targetCount - currentCount; i++)
            {
                SpawnStarFromBehind();
            }
        }
        else if (targetCount < currentCount)
        {
            // 需要减少星星（直接消失，后续会改为攻击动画）
            for (int i = 0; i < currentCount - targetCount; i++)
            {
                if (_orbitStars.Count > 0)
                {
                    RemoveLastStar();
                }
            }
        }
    }

    /// <summary>
    /// 从背后生成星星（避免突兀）
    /// </summary>
    private void SpawnStarFromBehind()
    {
        if (_playerNode == null) return;

        var starScene = GD.Load<PackedScene>("res://RegentFX/scenes/Star.tscn");
        if (starScene == null)
        {
            GD.PushError("[StarRingController] Failed to load Star.tscn");
            return;
        }

        var star = starScene.Instantiate<Star>();
        if (star == null) return;

        AddChild(star);

        // 计算新星星的角度位置（从背后开始，即角度 PI）
        float angleOffset = _orbitStars.Count > 0 ? Mathf.Tau / (_orbitStars.Count + 1) : 0f;
        float spawnAngle = Mathf.Pi + angleOffset * _orbitStars.Count;

        // 初始位置在玩家背后（缩小状态）
        Vector2 spawnPos = CalculateOrbitPosition(spawnAngle, 0.3f);
        star.Position = spawnPos;
        star.Scale = Vector2.Zero;

        // 配置星星参数
        star.EnablePulse = true;
        star.PulseSpeed = 2f + GD.Randf() * 1f;
        star.RotationSpeed = 45f + GD.Randf() * 45f;
        star.ZAsRelative = true;  // 使ZIndex为绝对值

        _orbitStars.Add(star);

        // 播放出现动画
        AnimateStarSpawn(star, spawnAngle);
    }

    /// <summary>
    /// 播放星星出现动画
    /// </summary>
    private void AnimateStarSpawn(Star star, float targetAngle)
    {
        var tween = CreateTween();
        tween.SetTrans(Tween.TransitionType.Back);
        tween.SetEase(Tween.EaseType.Out);

        // 计算目标位置
        Vector2 targetPos = CalculateOrbitPosition(targetAngle, 1.0f);

        tween.TweenProperty(star, "position", targetPos, SpawnAnimationDuration);
        tween.Parallel().TweenProperty(star, "scale", Vector2.One, SpawnAnimationDuration);
    }

    /// <summary>
    /// 移除最后一个星星
    /// </summary>
    private void RemoveLastStar()
    {
        if (_orbitStars.Count == 0) return;

        var star = _orbitStars[^1];
        _orbitStars.RemoveAt(_orbitStars.Count - 1);

        // 直接销毁（后续改为攻击动画）
        star.QueueFree();
    }

    /// <summary>
    /// 更新所有星星的位置
    /// </summary>
    private void UpdateStarPositions()
    {
        if (_orbitStars.Count == 0) return;

        float angleStep = Mathf.Tau / _orbitStars.Count;

        for (int i = 0; i < _orbitStars.Count; i++)
        {
            var star = _orbitStars[i];
            if (star == null) continue;

            // 计算当前角度（基础角度 + 轨道旋转）
            float angle = angleStep * i + Mathf.DegToRad(_orbitAngle);

            // 计算深度因子（0 = 最前面，1 = 最后面）
            float depthFactor = (Mathf.Sin(angle) + 1f) / 2f;

            // 根据深度调整缩放（伪3D效果）
            float scale = Mathf.Lerp(StarScaleMax, StarScaleMin, depthFactor);
            star.Scale = new Vector2(scale, scale);

            // 计算位置
            star.Position = CalculateOrbitPosition(angle, 1.0f);

            // 根据深度调整ZIndex
            // CombatVfxContainer ZIndex = -9
            // 角色在AllyContainer下，实际ZIndex ≈ -10
            // 星星在前面时：ZIndex = 1（实际-8），显示在角色前面
            // 星星在后面时：ZIndex = -2（实际-11），显示在角色后面
            float sinAngle = Mathf.Sin(angle);
            int newZIndex = sinAngle > 0 ? 1 : -2;
            if (star.ZIndex != newZIndex)
            {
                star.ZIndex = newZIndex;
                // 调试输出
                if (i == 0)  // 只输出第一颗星星避免日志过多
                {
                    Entry.Logger.Info($"[StarRing] Star {i} ZIndex changed to {newZIndex} (sin={sinAngle:F2})");
                }
            }
        }
    }

    /// <summary>
    /// 计算轨道位置
    /// </summary>
    private Vector2 CalculateOrbitPosition(float angle, float radiusMultiplier)
    {
        // 椭圆轨道（水平长轴，垂直短轴，产生伪3D效果）
        float x = Mathf.Cos(angle) * OrbitRadius * radiusMultiplier;
        float y = Mathf.Sin(angle) * OrbitRadius * 0.4f * radiusMultiplier + VerticalOffset;

        return new Vector2(x, y);
    }

    /// <summary>
    /// 清除所有星星
    /// </summary>
    public void ClearAllStars(bool animate = false)
    {
        if (animate)
        {
            foreach (var star in _orbitStars)
            {
                star?.FadeOutAndDestroy(0.3f);
            }
        }
        else
        {
            foreach (var star in _orbitStars)
            {
                star?.QueueFree();
            }
        }

        _orbitStars.Clear();
    }

    /// <summary>
    /// 激活/停用控制器
    /// </summary>
    public void SetActive(bool active)
    {
        _isActive = active;
        Visible = active;
    }

    public override void _ExitTree()
    {
        ClearAllStars();
        base._ExitTree();
    }
}
