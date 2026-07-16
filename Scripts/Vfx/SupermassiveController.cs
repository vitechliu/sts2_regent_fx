using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using RegentFX.Scripts.Vfx.Cards;

namespace RegentFX.Scripts.Vfx;

/// <summary>
/// 管理本地玩家在当前战斗中的超质量体黑洞。
/// 所有动画均由 Tween/动画结束事件驱动，不阻塞战斗命令。
/// </summary>
public partial class SupermassiveController : Node2D {
    public const string ScenePath = "res://RegentFX/scenes/vfx/super_massive.tscn";

    private const float BaseScale = 0.2f;
    private const float MaxScaleMultiplier = 4f;
    private const float SpawnDuration = 0.25f;
    private const float GrowScaleDuration = 0.2f;
    private const float DismissDuration = 0.2f;
    private const float FlightDuration = 0.45f;
    private const float WanderRadius = 55f;
    private const float WanderLerpSpeed = 0.85f;
    private const float WanderRetargetMin = 2f;
    private const float WanderRetargetMax = 3.5f;

    private static readonly Vector2 BaseAnchorOffset = new(120f, -250f);

    private readonly List<CardPile> _subscribedPiles = new();
    private readonly RandomNumberGenerator _rng = new();

    private NCreature? _playerNode;
    private Player? _player;
    private Node2D? _orb;
    private Node2D? _attackOrb;
    private Tween? _scaleTween;
    private Tween? _flightTween;
    private Vector2 _wanderOffset;
    private Vector2 _wanderTarget;
    private float _wanderRetargetTimer;
    private float _latestScale = BaseScale;
    private ulong _lifecycleVersion;
    private bool _isAttacking;
    private bool _possessionSyncQueued;
    private bool _isClearing;

    public bool HasReadyOrb => !_isClearing && !_isAttacking && IsValid(_orb);

    public void Initialize(NCreature playerNode, Player player) {
        _playerNode = playerNode;
        _player = player;
        _rng.Randomize();
        GlobalPosition = playerNode.GlobalPosition;
        ZAsRelative = true;
        SubscribeToPiles();
        PickNewWanderTarget();
    }

    public override void _Process(double delta) {
        if (_isClearing || _playerNode == null || _player == null) return;
        if (!GodotObject.IsInstanceValid(_playerNode) || NCombatRoom.Instance == null) return;

        if (_player.Creature.IsDead) {
            ClearImmediate();
            return;
        }

        GlobalPosition = _playerNode.GlobalPosition;
        UpdateWander((float)delta);
    }

    /// <summary>
    /// 响应一张牌生成完成。creator 口径与原卡的伤害统计保持一致。
    /// </summary>
    public void OnCardGenerated(Player? creator) {
        if (_isClearing || _player == null || creator != _player) return;
        if (!CardFX.IsTypeEnabled<Cards.Supermassive>()) return;
        if (NCombatRoom.Instance == null || _player.Creature.IsDead) return;

        _latestScale = CalculateScale(GetGeneratedCardCount());
        if (!HasActiveSupermassive()) return;

        // 飞行和爆炸期间仅同步最新尺寸，不补播成长动画。
        if (_isAttacking) return;

        if (!IsValid(_orb)) {
            CreateOrb(playGrow: true);
            return;
        }

        TweenOrbToLatestScale(GrowScaleDuration, ensureVisible: true);
        PlayOneShotAnimation(_orb!, "grow", 1);
    }

    /// <summary>
    /// 将当前黑洞发射到目标。返回 false 表示当前没有可用黑洞。
    /// </summary>
    public bool TryLaunch(Creature target) {
        if (_isClearing || _isAttacking || !IsValid(_orb)) return false;
        if (_player == null || _player.Creature.IsDead || target.IsDead) return false;
        if (!CardFX.IsTypeEnabled<Cards.Supermassive>() || !HasActiveSupermassive()) return false;

        NCombatRoom? room = NCombatRoom.Instance;
        NCreature? targetNode = room?.GetCreatureNode(target);
        Node? container = room?.CombatVfxContainer;
        if (room == null || targetNode == null || container == null) return false;

        Vector2 targetPosition = targetNode.VfxSpawnPosition;
        Node2D attackOrb = _orb!;
        _orb = null;
        _attackOrb = attackOrb;
        _isAttacking = true;
        _lifecycleVersion++;
        ulong version = _lifecycleVersion;

        _scaleTween?.Kill();
        _scaleTween = null;

        Transform2D globalTransform = attackOrb.GlobalTransform;
        attackOrb.GetParent()?.RemoveChildSafely(attackOrb);
        container.AddChildSafely(attackOrb);
        attackOrb.GlobalTransform = globalTransform;

        //todo 超质量体攻击音效
        
        _flightTween?.Kill();
        _flightTween = attackOrb.CreateTween();
        _flightTween.SetTrans(Tween.TransitionType.Quad);
        _flightTween.SetEase(Tween.EaseType.In);
        _flightTween.TweenProperty(attackOrb, "global_position", targetPosition, FlightDuration);
        _flightTween.Finished += () => OnFlightFinished(attackOrb, version);
        return true;
    }

    /// <summary>
    /// 立即停止控制器并释放所有节点，用于玩家死亡和战斗退出。
    /// </summary>
    public void ClearImmediate() {
        if (_isClearing) return;
        _isClearing = true;
        _lifecycleVersion++;
        _possessionSyncQueued = false;
        UnsubscribeFromPiles();

        _scaleTween?.Kill();
        _flightTween?.Kill();
        _scaleTween = null;
        _flightTween = null;

        QueueFreeIfValid(_orb);
        if (_attackOrb != _orb) QueueFreeIfValid(_attackOrb);
        _orb = null;
        _attackOrb = null;
        _isAttacking = false;
    }

    internal static float CalculateScale(int generatedCardCount) {
        float multiplier = Mathf.Min(
            MaxScaleMultiplier,
            1f + Mathf.Sqrt(Mathf.Max(0, generatedCardCount) / 3f));
        return BaseScale * multiplier;
    }

    private void SubscribeToPiles() {
        PlayerCombatState? state = _player?.PlayerCombatState;
        if (state == null) {
            Entry.Logger.Warn("[Supermassive] 玩家战斗牌堆尚未初始化");
            return;
        }

        foreach (CardPile pile in state.AllPiles) {
            pile.ContentsChanged += OnPileContentsChanged;
            _subscribedPiles.Add(pile);
        }
    }

    private void UnsubscribeFromPiles() {
        foreach (CardPile pile in _subscribedPiles) {
            pile.ContentsChanged -= OnPileContentsChanged;
        }
        _subscribedPiles.Clear();
    }

    private void OnPileContentsChanged() {
        if (_isClearing || _possessionSyncQueued) return;
        _possessionSyncQueued = true;
        Callable.From(SyncPossessionDeferred).CallDeferred();
    }

    private void SyncPossessionDeferred() {
        _possessionSyncQueued = false;
        if (_isClearing) return;

        if (!CardFX.IsTypeEnabled<Cards.Supermassive>() || !HasActiveSupermassive()) {
            DismissAllVisuals();
        }
    }

    private bool HasActiveSupermassive() {
        PlayerCombatState? state = _player?.PlayerCombatState;
        if (state == null) return false;

        return state.Hand.Cards.Any(IsSupermassive)
               || state.DrawPile.Cards.Any(IsSupermassive)
               || state.DiscardPile.Cards.Any(IsSupermassive)
               || state.PlayPile.Cards.Any(IsSupermassive);
    }

    private static bool IsSupermassive(MegaCrit.Sts2.Core.Models.CardModel card) {
        return card is MegaCrit.Sts2.Core.Models.Cards.Supermassive;
    }

    private int GetGeneratedCardCount() {
        if (_player == null) return 0;
        return CombatManager.Instance.History.Entries
            .OfType<CardGeneratedEntry>()
            .Count(entry => entry.Creator == _player);
    }

    private void CreateOrb(bool playGrow) {
        if (_isClearing || _isAttacking || IsValid(_orb) || NCombatRoom.Instance == null) return;

        Node2D? createdOrb = null;
        try {
            Node2D orb = VFXUtil.GenVFXNode(ScenePath);
            createdOrb = orb;
            AnimatedSprite2D? ball = orb.GetNodeOrNull<AnimatedSprite2D>("Ball");
            if (ball == null || ball.SpriteFrames == null) {
                Entry.Logger.Error("[Supermassive] super_massive.tscn 缺少 Ball/SpriteFrames");
                orb.QueueFreeSafely();
                return;
            }

            //todo 超质量体产生音效
            _lifecycleVersion++;
            _orb = orb;
            orb.Position = GetAnchorPosition();
            orb.Scale = Vector2.Zero;
            orb.Modulate = new Color(1f, 1f, 1f, 0f);
            this.AddChildSafely(orb);
            ball.Play("ball");
            TweenOrbToLatestScale(SpawnDuration, ensureVisible: true);

            if (playGrow) PlayOneShotAnimation(orb, "grow", 1);
        }
        catch (Exception ex) {
            Entry.Logger.Warn($"[Supermassive] 创建黑洞失败: {ex.Message}");
            QueueFreeIfValid(createdOrb);
            _orb = null;
        }
    }

    private void TweenOrbToLatestScale(float duration, bool ensureVisible) {
        if (!IsValid(_orb)) return;

        _scaleTween?.Kill();
        _scaleTween = _orb!.CreateTween().SetParallel();
        _scaleTween.SetTrans(Tween.TransitionType.Quad);
        _scaleTween.SetEase(Tween.EaseType.Out);
        _scaleTween.TweenProperty(_orb, "scale", Vector2.One * _latestScale, duration);
        if (ensureVisible) {
            _scaleTween.TweenProperty(_orb, "modulate:a", 1f, duration);
        }
    }

    private void PlayOneShotAnimation(Node2D parent, StringName animation, int zIndex) {
        if (!IsValid(parent)) return;
        AnimatedSprite2D? ball = parent.GetNodeOrNull<AnimatedSprite2D>("Ball");
        SpriteFrames? frames = ball?.SpriteFrames;
        if (frames == null || !frames.HasAnimation(animation)) return;

        var sprite = new AnimatedSprite2D {
            SpriteFrames = frames,
            Animation = animation,
            ZIndex = zIndex,
            ZAsRelative = true,
        };
        parent.AddChildSafely(sprite);
        sprite.AnimationFinished += () => QueueFreeIfValid(sprite);
        //todo 超质量体触发音效
        sprite.Play(animation);
    }

    private void OnFlightFinished(Node2D attackOrb, ulong version) {
        _flightTween = null;
        if (!IsCurrentAttack(attackOrb, version)) return;

        AnimatedSprite2D? ball = attackOrb.GetNodeOrNull<AnimatedSprite2D>("Ball");
        SpriteFrames? frames = ball?.SpriteFrames;
        if (ball != null) {
            ball.Stop();
            ball.Visible = false;
        }

        if (frames == null || !frames.HasAnimation("explode")) {
            FinishAttack(attackOrb, version);
            return;
        }

        var explosion = new AnimatedSprite2D {
            SpriteFrames = frames,
            Animation = "explode",
            ZIndex = 2,
            ZAsRelative = true,
        };
        attackOrb.AddChildSafely(explosion);
        explosion.AnimationFinished += () => FinishAttack(attackOrb, version);
        explosion.Play("explode");
    }

    private void FinishAttack(Node2D attackOrb, ulong version) {
        if (!IsCurrentAttack(attackOrb, version)) return;

        QueueFreeIfValid(attackOrb);
        _attackOrb = null;
        _isAttacking = false;

        if (_isClearing || _player == null || _player.Creature.IsDead) return;
        if (NCombatRoom.Instance == null || !CardFX.IsTypeEnabled<Cards.Supermassive>()) return;
        if (!HasActiveSupermassive()) return;

        _latestScale = CalculateScale(GetGeneratedCardCount());
        CreateOrb(playGrow: false);
    }

    private bool IsCurrentAttack(Node2D attackOrb, ulong version) {
        return !_isClearing
               && _isAttacking
               && version == _lifecycleVersion
               && ReferenceEquals(_attackOrb, attackOrb)
               && IsValid(attackOrb);
    }

    private void DismissAllVisuals() {
        if (!IsValid(_orb) && !IsValid(_attackOrb)) {
            _orb = null;
            _attackOrb = null;
            _isAttacking = false;
            return;
        }

        _lifecycleVersion++;
        _isAttacking = false;
        _scaleTween?.Kill();
        _flightTween?.Kill();
        _scaleTween = null;
        _flightTween = null;

        Node2D? orb = _orb;
        Node2D? attackOrb = _attackOrb;
        _orb = null;
        _attackOrb = null;
        FadeAndFree(orb);
        if (attackOrb != orb) FadeAndFree(attackOrb);
    }

    private static void FadeAndFree(Node2D? node) {
        if (!IsValid(node)) return;
        Tween tween = node!.CreateTween().SetParallel();
        tween.SetTrans(Tween.TransitionType.Quad);
        tween.SetEase(Tween.EaseType.In);
        tween.TweenProperty(node, "modulate:a", 0f, DismissDuration);
        tween.TweenProperty(node, "scale", Vector2.Zero, DismissDuration);
        tween.Finished += () => QueueFreeIfValid(node);
    }

    private void UpdateWander(float delta) {
        if (!IsValid(_orb) || _isAttacking) return;

        _wanderRetargetTimer -= delta;
        if (_wanderRetargetTimer <= 0f) PickNewWanderTarget();

        float lerpWeight = 1f - Mathf.Exp(-WanderLerpSpeed * delta);
        _wanderOffset = _wanderOffset.Lerp(_wanderTarget, lerpWeight);
        if (_wanderOffset.Length() > WanderRadius) {
            _wanderOffset = _wanderOffset.Normalized() * WanderRadius;
        }

        _orb!.Position = GetAnchorPosition() + _wanderOffset * GetPlayerVisualScale();
    }

    private void PickNewWanderTarget() {
        float angle = _rng.RandfRange(0f, Mathf.Tau);
        float radius = Mathf.Sqrt(_rng.Randf()) * WanderRadius;
        _wanderTarget = Vector2.FromAngle(angle) * radius;
        _wanderRetargetTimer = _rng.RandfRange(WanderRetargetMin, WanderRetargetMax);
    }

    private Vector2 GetAnchorPosition() {
        if (_playerNode == null) return Vector2.Zero;
        float x = VFXUtil.IsCharacterFacingRight(_playerNode.Entity) ? -BaseAnchorOffset.X : BaseAnchorOffset.X;
        return new Vector2(x, BaseAnchorOffset.Y) * GetPlayerVisualScale();
    }

    private float GetPlayerVisualScale() {
        if (_playerNode?.Visuals == null) return 1f;
        return Mathf.Max(0.01f, Mathf.Abs(_playerNode.Visuals.Scale.X));
    }

    private static bool IsValid(GodotObject? value) {
        return value != null && GodotObject.IsInstanceValid(value);
    }

    private static void QueueFreeIfValid(Node? node) {
        if (node != null && GodotObject.IsInstanceValid(node)) node.QueueFreeSafely();
    }

    public override void _ExitTree() {
        ClearImmediate();
        base._ExitTree();
    }
}
