using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using RegentFx.Core.Audio;
using RegentFX.Scripts.Vfx.Cards;

namespace RegentFX.Scripts.Vfx;

/// <summary>
/// 星星攻击特效控制器（单例）
/// 仅本地玩家拥有，用于处理星星的攻击特效
/// </summary>
public partial class StarEffectController : Node2D {
    private StarRingController? _starRingController;
    private NCreature? _playerNode;

    StarRingController? StarRingController => Entry.StarRingController;

    // 借取的星星列表
    private readonly List<Star> _borrowedStars = new();

    // 内部状态
    private bool _isShaking = false;
    // private readonly Dictionary<Star, Vector2> _starOriginalPositions = new();
    private readonly Dictionary<Star, float> _starShakePhases = new();
    private readonly Dictionary<Star, Vector2> _starTargetPositions = new();

    // 当前卡牌特效配置
    private CardFX? _currentCardFX;

    private Vector2 PlayerCenterPos => _playerNode.VfxSpawnPosition;

    public List<Star> Stars => _borrowedStars;

    /// <summary>
    /// 初始化控制器
    /// </summary>
    public void Initialize(NCreature playerNode, StarRingController? starRingController = null) {
        _playerNode = playerNode;
        _starRingController = starRingController;
        GlobalPosition = playerNode.GlobalPosition;
    }

    /// <summary>
    /// 卡牌持有时的回调
    /// </summary>
    public void OnCardHolding(CardModel card, CardFX cardFX) {
        // 如果已经有借取的星星，先清理
        if (_borrowedStars.Count > 0) {
            ReturnAllStars();
        }

        _currentCardFX = cardFX;

        // 从 StarRingController 借取星星
        BorrowStars();

        // 播放音效
        if (!string.IsNullOrEmpty(cardFX.HoldSfxPath)) {
            SimpleSfxUtil.Play(cardFX.HoldSfxPath);
        }
    }

    private void BorrowStars() {
        if (StarRingController == null) {
            Entry.Logger.Warn("[StarEffectController] StarRingController is null, cannot borrow stars");
            return;
        }
        if (_currentCardFX == null) {
            Entry.Logger.Warn("[StarEffectController] CardFX is null, cannot borrow stars");
            return;
        }

        // -1 表示借用所有星星
        int starCount = _currentCardFX.StarCount;
        int targetCount = starCount == -1 ? StarRingController.GetCurrentStarCount() : starCount;

        for (int i = 0; i < targetCount; i++) {
            var star = StarRingController.TakeStarForProjectile();
            if (star == null) {
                Entry.Logger.Warn($"[StarEffectController] Failed to borrow star {i + 1}/{targetCount}");
                break;
            }

            star.ToggleTrail(true);

            // _starOriginalPositions[star] = star.GlobalPosition;

            // 先从原父节点移除，再添加到当前控制器
            star.GetParent()?.RemoveChild(star);
            AddChild(star);
            _borrowedStars.Add(star);

            // 计算目标位置（使用 CardFX 的配置）
            Vector2 targetPosition = _currentCardFX.CalculateTargetPosition(GlobalPosition, i, targetCount);
            _currentCardFX.OnStartHolding(star);
            _starTargetPositions[star] = targetPosition;

            // 启动移动动画，最后一个星星到达后启动震颤
            bool isLastStar = (i == targetCount - 1);
            AnimateStarMove(star, targetPosition, isLastStar ? StartShaking : null);
        }
    }

    private void AnimateStarMove(Star star, Vector2 targetPosition, Action? onComplete = null) {
        if (_currentCardFX == null) return;

        var tween = CreateTween();

        // 使用 Expo 缓动：前期极快，后期极慢
        tween.SetTrans(Tween.TransitionType.Expo);
        tween.SetEase(Tween.EaseType.Out);

        // 随机波动：基础时长的 min ~ max 范围
        var rng = new RandomNumberGenerator();
        var (min, max) = _currentCardFX.DurationRange;
        float randomDuration = _currentCardFX.MoveDuration * (min + rng.Randf() * (max - min));

        // 快速移动到目标位置
        tween.TweenProperty(star, "global_position", targetPosition, randomDuration);

        if (onComplete != null) {
            tween.Finished += onComplete;
        }
        // Entry.Logger.Debug($"[StarEffectController] Animating star move to {targetPosition}, duration: {randomDuration:F3}s");
    }

    private void InitializeShakePhases() {
        _starShakePhases.Clear();
        var rng = new RandomNumberGenerator();
        rng.Randomize();

        foreach (var star in _borrowedStars) {
            _starShakePhases[star] = rng.RandfRange(0f, Mathf.Tau);
        }
    }

    private void StartShaking() {
        _isShaking = true;
        InitializeShakePhases();
    }

    public override void _Process(double delta) {
        if (!_isShaking || _borrowedStars.Count == 0 || _currentCardFX == null) return;

        float dt = (float)delta;
        float shakeIntensity = _currentCardFX.ShakeIntensity;
        float shakeSpeed = _currentCardFX.ShakeSpeed;

        foreach (var star in _borrowedStars) {
            if (!_starShakePhases.TryGetValue(star, out float phase)) continue;
            if (!_starTargetPositions.TryGetValue(star, out Vector2 targetPos)) continue;

            // 更新相位
            phase += shakeSpeed * dt;
            _starShakePhases[star] = phase;

            // 计算震颤偏移（使用正弦波）
            float offsetX = Mathf.Sin(phase) * shakeIntensity;
            float offsetY = Mathf.Cos(phase * 1.3f) * shakeIntensity;

            // 直接设置 GlobalPosition = 目标位置 + 震颤偏移
            star.GlobalPosition = targetPos + new Vector2(offsetX, offsetY);
        }
    }

    public void OnCancelCard() {
        Entry.Logger.Info($"[StarEffectController] OnCancelCard called, returning {_borrowedStars.Count} stars");

        ReturnAllStars();

        // 通知 StarRingController 重置星星数量
        StarRingController?.ResetStarCount();
    }

    private void ReturnAllStars() {
        _isShaking = false;

        foreach (var star in _borrowedStars) {
            // 闪烁效果
            var tween = CreateTween();
            tween.SetTrans(Tween.TransitionType.Quad);
            tween.SetEase(Tween.EaseType.Out);

            // 闪烁：快速缩放后消失
            tween.TweenProperty(star, "scale", star.Scale * 1.3f, 0.05f);
            tween.TweenProperty(star, "modulate:a", 0f, 0.1f);

            // 动画完成后销毁
            tween.Finished += () => {
                star.QueueFree();
            };
        }

        // 清空列表
        _borrowedStars.Clear();
        // _starOriginalPositions.Clear();
        _starShakePhases.Clear();
        _starTargetPositions.Clear();
        _currentCardFX = null;
    }

    /// <summary>
    /// 清除所有特效
    /// </summary>
    public void ClearAllEffects() {
        _isShaking = false;

        foreach (var star in _borrowedStars) {
            star?.QueueFree();
        }

        _borrowedStars.Clear();
        // _starOriginalPositions.Clear();
        _starShakePhases.Clear();
        _starTargetPositions.Clear();
        _currentCardFX = null;
    }

    public override void _ExitTree() {
        ClearAllEffects();
        base._ExitTree();
    }
}
