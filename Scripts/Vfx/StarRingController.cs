using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace RegentFX.Scripts.Vfx;

/// <summary>
/// 储君周围环绕星星控制器
/// 管理星星的生成、销毁、旋转动画
/// 支持星星数量变化时的平滑过渡
/// </summary>
public partial class StarRingController : Node2D {
    //Default: 104f, Mesugaki Regent: 120f
    [Export] public float OrbitRadius { get; set; } = 120f;
    [Export] public float OrbitSpeed { get; set; } = 30f;
    [Export] public float StarScaleMin { get; set; } = 0.6f;
    [Export] public float StarScaleMax { get; set; } = 1.0f;
    [Export] public int MaxStarCount { get; set; } = 20;
    [Export] public float SpawnAnimationDuration { get; set; } = 0.3f;
    [Export] public float VerticalOffset { get; set; } = -180f;
    [Export] public float AngleLerpSpeed { get; set; } = 8f; // 角度插值速度，越大过渡越快

    // ====================== 功能1：缩放适配 新增字段 ======================
    // 星环基础参数（初始固定值，动态计算的基准）
    private float _baseOrbitRadius;
    private float _baseVerticalOffset;
    // ====================================================================

    public const int STAR_FRONT_ZINDEX = 0;
    public const int STAR_BACK_ZINDEX = -5;

    private readonly List<StarData> _orbitStars = new();
    private NCreature? _playerNode;
    private float _orbitAngle;
    private bool _isActive;
    private Player _player;

    StarEffectController? StarEffectController => Entry.StarEffectController;

    private bool childOfTheStarsMode = false;

    /// <summary>
    /// 星星数据类，存储每个星星的状态
    /// </summary>
    private class StarData {
        public Star Star { get; set; } = null!;
        public float CurrentAngle { get; set; } // 当前实际角度
        public float TargetAngle { get; set; } // 目标角度（均匀分布）
        public bool IsSpawning { get; set; } // 是否正在生成动画中
        public float SpawnProgress { get; set; } // 生成动画进度
        public bool IsRemoving { get; set; } // 是否正在移除
        public float RemoveProgress { get; set; } // 移除动画进度
    }

    public override void _Process(double delta) {
        if (!_isActive || _playerNode == null) return;

        // 读取玩家当前缩放值（X/Y轴一致，取X即可）
        float currentScale = _playerNode.Visuals.Scale.X;
        // 基于基础值 × 缩放比例，动态更新星环参数
        OrbitRadius = _baseOrbitRadius * currentScale;
        VerticalOffset = _baseVerticalOffset * currentScale;

        // 更新控制器位置到玩家位置
        GlobalPosition = _playerNode.GlobalPosition;

        // 更新轨道角度
        _orbitAngle += OrbitSpeed * (float)delta;

        // 更新所有星星
        UpdateStars((float)delta);
    }

    /// <summary>
    /// 初始化控制器，绑定到玩家节点
    /// </summary>
    public void Initialize(NCreature playerNode, Player player) {
        _playerNode = playerNode;
        _isActive = true;
        GlobalPosition = playerNode.GlobalPosition;
        ZAsRelative = true;
        _player = player;

        _baseOrbitRadius = OrbitRadius;
        _baseVerticalOffset = VerticalOffset;
    }

    public void ResetStarCount() {
        if (_player.PlayerCombatState != null) {
            SetStarCount(_player.PlayerCombatState.Stars);
        }
        else {
            Entry.Logger.Warn("ResetStarCount: 无法获取playerCombatState");
        }
    }

    /// <summary>
    /// 设置星星数量
    /// </summary>
    public void SetStarCount(int count) {
        int targetCount = Mathf.Min(count, MaxStarCount);
        // 只计算活跃的星星（不包括正在移除的）
        int activeCount = _orbitStars.Count(s => !s.IsRemoving);

        if (targetCount > activeCount) {
            // 需要增加星星
            SpawnStars(targetCount - activeCount);
        }
        else if (targetCount < activeCount) {
            // 需要减少星星
            RemoveStars(activeCount - targetCount);
        }

        // 重新计算所有星星的目标角度
        RecalculateTargetAngles();
    }

    /// <summary>
    /// 从背后生成多个星星
    /// </summary>
    private void SpawnStars(int count) {
        if (_playerNode == null || count <= 0) return;

        // 所有新星星的生成角度都在背后（PI）
        float baseSpawnAngle = Mathf.Pi;

        for (int i = 0; i < count; i++) {
            var star = Star.Create();

            AddChild(star);

            // 配置星星参数
            star.EnablePulse = true;
            star.PulseSpeed = 2f + GD.Randf() * 1f;
            star.RotationSpeed = 45f + GD.Randf() * 45f;
            star.ZAsRelative = true;
            star.Scale = Vector2.Zero;
            star.ZIndex = STAR_BACK_ZINDEX;
            star.EnableTrail = false;

            // 初始位置
            star.Position = CalculateOrbitPosition(baseSpawnAngle, 1.0f);

            // 创建星星数据
            var starData = new StarData {
                Star = star,
                CurrentAngle = baseSpawnAngle,
                TargetAngle = baseSpawnAngle, // 临时设置，稍后会被重新计算
                IsSpawning = true,
                SpawnProgress = 0f
            };

            _orbitStars.Add(starData);
        }
    }

    /// <summary>
    /// 标记要移除的星星
    /// </summary>
    private void RemoveStars(int count) {
        // 优先移除正在生成中的星星（避免浪费）
        // 然后移除角度最接近的星星（从背后开始）

        var starsToRemove = new List<StarData>();

        // 首先找正在生成中的星星
        foreach (var starData in _orbitStars) {
            if (starsToRemove.Count >= count) break;
            if (starData.IsSpawning && !starData.IsRemoving) {
                starsToRemove.Add(starData);
            }
        }

        // 如果还不够，找背后的星星（角度接近PI的）
        if (starsToRemove.Count < count) {
            var remainingStars = _orbitStars
                .Where(s => !s.IsRemoving && !starsToRemove.Contains(s))
                .OrderBy(s => Mathf.Abs(NormalizeAngle(s.CurrentAngle - Mathf.Pi)))
                .Take(count - starsToRemove.Count)
                .ToList();

            starsToRemove.AddRange(remainingStars);
        }

        // 标记为移除状态
        foreach (var starData in starsToRemove) {
            starData.IsRemoving = true;
            starData.RemoveProgress = 0f;
        }
    }

    /// <summary>
    /// 重新计算所有星星的目标角度（均匀分布）
    /// 策略：基于实际渲染位置（包含轨道旋转）计算，确保平滑过渡
    /// </summary>
    private void RecalculateTargetAngles() {
        // 只考虑未被标记为移除的星星
        var activeStars = _orbitStars.Where(s => !s.IsRemoving).ToList();
        int count = activeStars.Count;

        if (count == 0) return;

        // 如果只有1颗星星，不需要均匀分布
        if (count == 1) {
            activeStars[0].TargetAngle = activeStars[0].CurrentAngle;
            return;
        }

        // 计算均匀分布的角度步长
        float angleStep = Mathf.Tau / count;

        // 获取所有星星的实际渲染角度（CurrentAngle + _orbitAngle）
        float orbitAngleRad = Mathf.DegToRad(_orbitAngle);
        var renderAngles = activeStars.Select(s => NormalizeAngle(s.CurrentAngle + orbitAngleRad)).ToList();

        // 尝试不同的基准角度，找到总移动距离最小的方案
        float bestBaseAngle = 0f;
        float minTotalDistance = float.MaxValue;

        // 以每个星星的当前渲染角度作为候选基准角度
        for (int i = 0; i < count; i++) {
            float candidateBase = renderAngles[i];
            float totalDistance = CalculateTotalDistance(renderAngles, candidateBase, angleStep);

            if (totalDistance < minTotalDistance) {
                minTotalDistance = totalDistance;
                bestBaseAngle = candidateBase;
            }
        }

        // 使用最佳基准角度为每个星星分配目标角度
        AssignTargetAngles(activeStars, bestBaseAngle, angleStep, orbitAngleRad);
    }

    /// <summary>
    /// 计算给定基准角度下，所有星星移动到均匀分布位置的总距离
    /// </summary>
    private float CalculateTotalDistance(List<float> currentAngles, float baseAngle, float angleStep) {
        float totalDistance = 0f;
        int count = currentAngles.Count;

        // 生成均匀分布的目标位置
        var targetPositions = new List<float>();
        for (int i = 0; i < count; i++) {
            targetPositions.Add(NormalizeAngle(baseAngle + angleStep * i));
        }

        // 为每个当前角度找到最近的目标位置（贪心算法）
        var usedTargets = new bool[count];
        foreach (var currentAngle in currentAngles) {
            float minDist = float.MaxValue;
            int bestTarget = -1;

            for (int i = 0; i < count; i++) {
                if (usedTargets[i]) continue;

                float dist = AngleDistance(currentAngle, targetPositions[i]);
                if (dist < minDist) {
                    minDist = dist;
                    bestTarget = i;
                }
            }

            if (bestTarget >= 0) {
                usedTargets[bestTarget] = true;
                totalDistance += minDist;
            }
        }

        return totalDistance;
    }

    /// <summary>
    /// 为每个星星分配最佳目标角度
    /// </summary>
    private void AssignTargetAngles(List<StarData> activeStars, float baseAngle, float angleStep, float orbitAngleRad) {
        int count = activeStars.Count;

        // 生成均匀分布的目标位置（渲染坐标系）
        var targetPositions = new List<(float angle, bool assigned)>();
        for (int i = 0; i < count; i++) {
            targetPositions.Add((NormalizeAngle(baseAngle + angleStep * i), false));
        }

        // 按与目标位置的距离排序星星，优先分配距离近的
        var assignmentQueue = new List<(StarData star, int targetIndex, float distance)>();

        for (int starIdx = 0; starIdx < count; starIdx++) {
            var star = activeStars[starIdx];
            float renderAngle = NormalizeAngle(star.CurrentAngle + orbitAngleRad);

            for (int targetIdx = 0; targetIdx < count; targetIdx++) {
                float dist = AngleDistance(renderAngle, targetPositions[targetIdx].angle);
                assignmentQueue.Add((star, targetIdx, dist));
            }
        }

        // 按距离排序
        assignmentQueue = assignmentQueue.OrderBy(x => x.distance).ToList();

        // 贪心分配
        var assignedStars = new HashSet<StarData>();
        var assignedTargets = new HashSet<int>();

        foreach (var (star, targetIndex, _) in assignmentQueue) {
            if (assignedStars.Contains(star)) continue;
            if (assignedTargets.Contains(targetIndex)) continue;

            // 将目标渲染角度转换回相对角度存储
            float targetRenderAngle = targetPositions[targetIndex].angle;
            float targetRelativeAngle = targetRenderAngle - orbitAngleRad;
            star.TargetAngle = FindNearestAngle(star.CurrentAngle, targetRelativeAngle);

            assignedStars.Add(star);
            assignedTargets.Add(targetIndex);
        }
    }

    /// <summary>
    /// 计算两个角度之间的最短距离（绝对值）
    /// </summary>
    private float AngleDistance(float angle1, float angle2) {
        float diff = Mathf.Abs(angle2 - angle1);
        while (diff > Mathf.Pi) diff -= Mathf.Tau;
        return Mathf.Abs(diff);
    }

    /// <summary>
    /// 更新所有星星的状态和位置
    /// </summary>
    private void UpdateStars(float delta) {
        // 先清理已完成的移除动画
        for (int i = _orbitStars.Count - 1; i >= 0; i--) {
            var starData = _orbitStars[i];
            if (starData.IsRemoving && starData.RemoveProgress >= 1f) {
                starData.Star.QueueFree();
                _orbitStars.RemoveAt(i);
            }
        }

        if (_orbitStars.Count == 0) return;

        // 更新每个星星
        foreach (var starData in _orbitStars) {
            UpdateStar(starData, delta);
        }
    }

    /// <summary>
    /// 更新单个星星
    /// </summary>
    private void UpdateStar(StarData starData, float delta) {
        var star = starData.Star;
        if (star == null) return;

        // 处理生成动画
        if (starData.IsSpawning) {
            starData.SpawnProgress += delta / SpawnAnimationDuration;
            if (starData.SpawnProgress >= 1f) {
                starData.SpawnProgress = 1f;
                starData.IsSpawning = false;
            }

            // 生成时的缩放动画（Back缓出效果）
            float t = starData.SpawnProgress;
            float scale = BackEaseOut(t);
            star.Scale = new Vector2(scale, scale);
        }

        // 处理移除动画
        if (starData.IsRemoving) {
            starData.RemoveProgress += delta / SpawnAnimationDuration;
            if (starData.RemoveProgress > 1f) starData.RemoveProgress = 1f;

            // 移除时的缩放动画（缩放到0）
            float t = 1f - starData.RemoveProgress;
            star.Scale = new Vector2(t, t);

            // 移除时不需要更新位置，直接返回
            return;
        }

        // 平滑插值当前角度到目标角度
        float angleDiff = starData.TargetAngle - starData.CurrentAngle;

        // 处理角度环绕（选择最短路径）
        if (angleDiff > Mathf.Pi) angleDiff -= Mathf.Tau;
        if (angleDiff < -Mathf.Pi) angleDiff += Mathf.Tau;

        // 使用平滑插值
        float lerpFactor = Mathf.Min(AngleLerpSpeed * delta, 1f);
        starData.CurrentAngle += angleDiff * lerpFactor;

        // 计算实际渲染角度（当前角度 + 轨道旋转）
        float renderAngle = starData.CurrentAngle + Mathf.DegToRad(_orbitAngle);

        // 计算深度因子（0 = 最前面，1 = 最后面）
        float sinAngle = Mathf.Sin(renderAngle);
        float depthFactor = (sinAngle + 1f) / 2f;

        // 根据深度调整缩放（伪3D效果）
        float depthScale = Mathf.Lerp(StarScaleMin, StarScaleMax, depthFactor);
        Vector2 currentScale = star.Scale;
        // 保持生成/移除动画的缩放，同时应用深度缩放
        if (!starData.IsSpawning && !starData.IsRemoving) {
            star.Scale = new Vector2(depthScale, depthScale);
        }
        else {
            // 生成/移除动画期间，深度缩放作为乘数
            star.Scale = new Vector2(
                currentScale.X * depthScale,
                currentScale.Y * depthScale
            );
        }

        // 计算位置
        star.Position = CalculateOrbitPosition(renderAngle, 1.0f);

        // 更新ZIndex
        star.ZIndex = sinAngle > 0 ? STAR_FRONT_ZINDEX : STAR_BACK_ZINDEX;
        star.ZAsRelative = true;
    }

    /// <summary>
    /// Back缓出函数
    /// </summary>
    private float BackEaseOut(float t) {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1;
        return 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2);
    }

    /// <summary>
    /// 将角度归一化到 [0, 2π) 范围
    /// </summary>
    private float NormalizeAngle(float angle) {
        while (angle < 0) angle += Mathf.Tau;
        while (angle >= Mathf.Tau) angle -= Mathf.Tau;
        return angle;
    }

    /// <summary>
    /// 找到与当前角度最接近的等效角度
    /// </summary>
    private float FindNearestAngle(float currentAngle, float targetAngle) {
        // 计算差值
        float diff = targetAngle - currentAngle;

        // 调整到 [-π, π] 范围
        while (diff > Mathf.Pi) diff -= Mathf.Tau;
        while (diff < -Mathf.Pi) diff += Mathf.Tau;

        return currentAngle + diff;
    }

    /// <summary>
    /// 计算轨道位置
    /// </summary>
    private Vector2 CalculateOrbitPosition(float angle, float radiusMultiplier) {
        float x = Mathf.Cos(angle) * OrbitRadius * radiusMultiplier;
        float y = Mathf.Sin(angle) * OrbitRadius * 0.4f * radiusMultiplier + VerticalOffset;
        return new Vector2(x, y);
    }

    /// <summary>
    /// 清除所有星星
    /// </summary>
    public void ClearAllStars(bool animate = false) {
        if (animate) {
            foreach (var starData in _orbitStars) {
                starData.Star?.FadeOutAndDestroy(0.3f);
            }
        }
        else {
            foreach (var starData in _orbitStars) {
                starData.Star?.QueueFree();
            }
        }

        _orbitStars.Clear();
    }

    /// <summary>
    /// 激活/停用控制器
    /// </summary>
    public void SetActive(bool active) {
        _isActive = active;
        Visible = active;
    }

    /// <summary>
    /// 获取一颗可用的星星用于攻击发射
    /// 返回星星并将其从环绕列表中移除
    /// </summary>
    public Star? TakeStarForProjectile() {
        var starData = _orbitStars
            .FirstOrDefault(s => !s.IsRemoving);

        if (starData == null) return null;

        var star = starData.Star;
        _orbitStars.Remove(starData);

        // 重新计算剩余星星的角度分布
        RecalculateTargetAngles();

        return star;
    }

    /// <summary>
    /// 获取当前环绕的星星数量
    /// </summary>
    public int GetCurrentStarCount() {
        return _orbitStars.Count(s => !s.IsRemoving);
    }

    public override void _ExitTree() {
        ClearAllStars();
        base._ExitTree();
    }
}