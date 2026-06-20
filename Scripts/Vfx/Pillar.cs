using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.TestSupport;
using RegentFX.ThirdParty.Audio;

namespace RegentFX.Scripts.Vfx;

/// <summary>
/// 柱子特效节点
/// 从高空砸落并震动，激活时Active节点同步闪烁并伴随白色外发光
/// </summary>
[GlobalClass]
public partial class Pillar : Node2D {

    #region 可配置参数

    [Export] public float FallHeight { get; set; } = 800f;
    [Export] public float FallDuration { get; set; } = 0.3f;
    [Export] public float ShakeDuration { get; set; } = 0.3f;
    [Export] public float ShakeIntensity { get; set; } = 6f;
    [Export] public float ShakeSpeed { get; set; } = 40f;
    [Export] public float ActivateFlashDuration { get; set; } = 0.8f;
    [Export] public float ActivatePeakTime { get; set; } = 0.12f;
    [Export] public float GlowMaxIntensity { get; set; } = 1.5f;
    [Export] public float StartGlow { get; set; } = 0.1f;

    #endregion

    #region 节点引用

    private Sprite2D? _topSprite;
    private AnimatedSprite2D? _spinBase;
    private AnimatedSprite2D? _spinActive;
    private Sprite2D? _mainBase;
    private Sprite2D? _mainActive;
    private Sprite2D? _lightSprite;
    private readonly List<ShaderMaterial> _glowMaterials = new();

    #endregion

    #region 状态

    private bool _isShaking = false;
    private float _shakeTimer = 0f;
    private float _shakePhase = 0f;
    private Vector2 _shakeBasePosition;
    private Vector2 _targetPosition;
    private Tween? _fallTween;
    private Tween? _activateTween;
    private Tween? _scaleTween;
    private Tween? _glowTween;

    #endregion

    #region 生命周期

    public override void _Ready() {
        _topSprite = GetNodeOrNull<Sprite2D>("Top");
        _spinBase = GetNodeOrNull<AnimatedSprite2D>("Spin/Base");
        _spinActive = GetNodeOrNull<AnimatedSprite2D>("Spin/Active");
        _mainBase = GetNodeOrNull<Sprite2D>("Main/Base");
        _mainActive = GetNodeOrNull<Sprite2D>("Main/Active");
        _lightSprite = GetNodeOrNull<Sprite2D>("Light");

        // 同步启动两个旋转动画
        if (_spinBase != null) {
            _spinBase.Play("default");
        }
        if (_spinActive != null) {
            _spinActive.Play("default");
            _spinActive.Modulate = new Color(1, 1, 1, 0);
        }

        // 初始隐藏 Active
        if (_mainActive != null) {
            _mainActive.Modulate = new Color(1, 1, 1, 0);
        }

        // 初始隐藏光晕
        if (_lightSprite != null) {
            _lightSprite.Modulate = new Color(1, 1, 1, 0);
        }

        // 为各可见节点设置白色外发光材质
        SetupGlowMaterial(_topSprite);
        SetupGlowMaterial(_mainBase);
        // SetupGlowMaterial(_mainActive);
        SetupGlowMaterial(_spinBase);
        // SetupGlowMaterial(_spinActive);
    }

    /// <summary>
    /// 为 Sprite 设置 pillar 外发光材质
    /// </summary>
    private void SetupGlowMaterial(CanvasItem? sprite) {
        if (sprite == null) return;

        ShaderMaterial? material = null;

        // 尝试复用已有的 ShaderMaterial
        if (sprite.Material is ShaderMaterial existingMat) {
            material = existingMat.Duplicate() as ShaderMaterial;
        }

        // 如果没有，则新建
        if (material == null) {
            var shader = GD.Load<Shader>("res://RegentFX/shaders/vfx/pillar/pillar_glow.gdshader");
            if (shader != null) {
                material = new ShaderMaterial { Shader = shader };
            }
        }

        if (material != null) {
            material.SetShaderParameter("glow_color", Colors.White);
            material.SetShaderParameter("glow_intensity", StartGlow);
            material.SetShaderParameter("glow_radius", 34.0f);
            material.SetShaderParameter("inner_glow", 0.2f);
            sprite.Material = material;
            _glowMaterials.Add(material);
        }
    }

    public override void _Process(double delta) {
        if (!_isShaking) return;

        float dt = (float)delta;
        _shakeTimer += dt;
        _shakePhase += ShakeSpeed * dt;

        if (_shakeTimer >= ShakeDuration) {
            _isShaking = false;
            GlobalPosition = _shakeBasePosition;
            return;
        }

        float offsetX = Mathf.Sin(_shakePhase) * ShakeIntensity;
        float offsetY = Mathf.Cos(_shakePhase * 1.3f) * ShakeIntensity;
        GlobalPosition = _shakeBasePosition + new Vector2(offsetX, offsetY);
    }

    #endregion

    #region 激活功能

    /// <summary>
    /// 激活柱子：所有 Active 节点同步闪烁（透明度 0→1→0），伴随白色外发光
    /// Spin 动画保持与 Base 同步
    /// </summary>
    public void Activate() {
        // 确保 Spin/Active 与 Spin/Base 帧同步
        if (_spinBase != null && _spinActive != null) {
            _spinActive.Frame = _spinBase.Frame;
        }

        FmodLite.Play("event:/RegentFx/sfx/seven_stars_hold");

        // 透明度闪烁
        _activateTween = CreateTween();
        _activateTween.SetTrans(Tween.TransitionType.Quad);
        _activateTween.SetEase(Tween.EaseType.Out);
        _activateTween.TweenMethod(Callable.From<float>(SetActiveAlpha), 0f, 0.5f, ActivatePeakTime);
        _activateTween.Chain();
        _activateTween.SetTrans(Tween.TransitionType.Quad);
        _activateTween.SetEase(Tween.EaseType.In);
        _activateTween.TweenMethod(Callable.From<float>(SetActiveAlpha), 0.5f, 0f, ActivateFlashDuration - ActivatePeakTime);

        // 白色外发光闪烁（与透明度同步）
        _glowTween = CreateTween();
        _glowTween.SetTrans(Tween.TransitionType.Quad);
        _glowTween.SetEase(Tween.EaseType.Out);
        _glowTween.TweenMethod(Callable.From<float>(SetGlowIntensity), StartGlow, GlowMaxIntensity, ActivatePeakTime);
        _glowTween.Chain();
        _glowTween.SetTrans(Tween.TransitionType.Quad);
        _glowTween.SetEase(Tween.EaseType.In);
        _glowTween.TweenMethod(Callable.From<float>(SetGlowIntensity), GlowMaxIntensity, StartGlow, ActivateFlashDuration - ActivatePeakTime);
    }

    /// <summary>
    /// 统一设置所有 Active 节点的透明度
    /// </summary>
    private void SetActiveAlpha(float alpha) {
        if (_spinActive != null) {
            _spinActive.Modulate = new Color(1, 1, 1, alpha);
        }
        if (_mainActive != null) {
            _mainActive.Modulate = new Color(1, 1, 1, alpha);
        }
    }

    /// <summary>
    /// 统一设置所有节点的发光强度
    /// </summary>
    private void SetGlowIntensity(float intensity) {
        foreach (var material in _glowMaterials) {
            material.SetShaderParameter("glow_intensity", intensity);
        }
    }

    #endregion

    #region 创建/下落功能

    /// <summary>
    /// 从高空砸落到指定位置
    /// </summary>
    public void Create(Vector2 position) {
        _targetPosition = position;
        _shakeBasePosition = position;

        // 起始位置：屏幕上方
        GlobalPosition = position + new Vector2(0, -FallHeight);

        // 初始拉伸模拟下落速度感
        Scale = new Vector2(1f, 1.3f);

        // 下落动画
        _fallTween = CreateTween();
        _fallTween.SetTrans(Tween.TransitionType.Expo);
        _fallTween.SetEase(Tween.EaseType.In);
        _fallTween.TweenProperty(this, "global_position", position, FallDuration);

        // 下落过程中恢复 scale
        _scaleTween = CreateTween();
        _scaleTween.SetTrans(Tween.TransitionType.Quad);
        _scaleTween.SetEase(Tween.EaseType.Out);
        _scaleTween.TweenProperty(this, "scale", Vector2.One, FallDuration);

        _fallTween.Finished += OnLand;
    }

    /// <summary>
    /// 落地回调：震动 + 弹性效果
    /// </summary>
    private void OnLand() {
        _isShaking = true;
        _shakeTimer = 0f;
        _shakePhase = 0f;

        Node2D? n = VFXUtil.PlaySimple(BurstPath, _targetPosition);
        if (n != null) n.Scale *= 3f;
        FmodLite.Play("event:/RegentFx/sfx/pillar_burst");

        NGame.Instance?.ScreenShake(ShakeStrength.Medium, MegaCrit.Sts2.Core.Nodes.Vfx.Utilities.ShakeDuration.Normal, 90f);
        // 落地弹性：压扁后弹回
        var bounceTween = CreateTween();
        bounceTween.SetTrans(Tween.TransitionType.Quad);
        bounceTween.SetEase(Tween.EaseType.Out);
        bounceTween.TweenProperty(this, "scale:y", 0.85f, 0.08f);

        bounceTween.Chain();
        bounceTween.SetTrans(Tween.TransitionType.Elastic);
        bounceTween.SetEase(Tween.EaseType.Out);
        bounceTween.TweenProperty(this, "scale:y", 1f, 0.25f);

        // Light 光晕快速显示
        if (_lightSprite != null) {
            var lightTween = CreateTween();
            lightTween.SetTrans(Tween.TransitionType.Quad);
            lightTween.SetEase(Tween.EaseType.Out);
            lightTween.TweenProperty(_lightSprite, "modulate:a", 1f, 0.15f);
        }
    }

    #endregion

    #region 静态工厂方法

    public const string VfxScenePath = "res://RegentFX/scenes/vfx/pillar.tscn";
    public const string BurstPath = "res://RegentFX/scenes/vfx/p_burst.tscn";

    /// <summary>
    /// 在指定位置创建柱子特效
    /// </summary>
    public static Pillar? Spawn(Creature creature, Vector2 position) {
        if (TestMode.IsOn) return null;

        try {
            var pillar = VFXUtil.GenVFXNode<Pillar>(VfxScenePath);
            Node? parent = NCombatRoom.Instance?.BackCombatVfxContainer;
            if (parent == null) {
                Entry.Logger.Warn("[Pillar] No BackCombatVfxContainer available");
                pillar.QueueFree();
                return null;
            }
            FmodLite.Play("event:/RegentFx/sfx/pillar_create");
            
            parent.AddChildSafely(pillar);
            pillar.Create(position);
            Pillars[creature] = pillar;
            return pillar;
        }
        catch (Exception ex) {
            Entry.Logger.Warn($"[Pillar] Failed to create pillar: {ex.Message}");
            return null;
        }
    }

    #endregion

    public static Dictionary<Creature, Pillar> Pillars = new();
    
    #region 清理

    public override void _ExitTree() {
        _fallTween?.Kill();
        _activateTween?.Kill();
        _scaleTween?.Kill();
        _glowTween?.Kill();
        base._ExitTree();
    }

    #endregion
}
