using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;
using RegentFX.ThirdParty.Audio;

namespace RegentFX.Scripts.Vfx;

/// <summary>
/// 黑洞特效节点
/// 具有律动、边缘虚化、漩涡扭曲、边缘辉光等效果
/// 支持爆发功能：剧烈律动 + 大范围屏幕扭曲
/// </summary>
[GlobalClass]
public partial class Blackhole : Node2D {

    #region 可配置参数

    [Export] public float BlackholeSize { get; set; } = 100f;
    [Export] public float PulseFrequency { get; set; } = 1.5f;
    [Export] public float PulseIntensity { get; set; } = 0.15f;
    [Export] public float SwirlStrength { get; set; } = 0.8f;

    #endregion

    #region 常量

    private const float BURST_DURATION = 0.4f;
    private const float BURST_PEAK_TIME = 0.15f;
    private const float BURST_MAX_SCALE = 1.5f;
    private const float BURST_MAX_DISTORTION = 0.15f;
    private const float BURST_RADIUS = 300f;
    private const float BURST_PULSE_MULTIPLIER = 3f;

    #endregion

    #region 节点引用

    private Sprite2D? _coreSprite;
    private Sprite2D? _burstSprite;
    private ShaderMaterial? _coreMaterial;
    private ShaderMaterial? _burstMaterial;

    #endregion

    #region 状态

    private bool _isBursting = false;
    private float _burstTimer = 0f;
    private float _baseScale = 1f;
    private float _basePulseFrequency = 1.5f;
    private float _basePulseIntensity = 0.15f;

    #endregion

    #region 生命周期

    public override void _Ready() {
        _coreSprite = GetNodeOrNull<Sprite2D>("Core");
        _burstSprite = GetNodeOrNull<Sprite2D>("BurstDistortion");

        if (_coreSprite != null) {
            _coreMaterial = (_coreSprite.Material as ShaderMaterial)?.Duplicate() as ShaderMaterial;
            if (_coreMaterial != null) {
                _coreSprite.Material = _coreMaterial;
                UpdateCoreShaderParams();
            }
        }

        if (_burstSprite != null) {
            _burstMaterial = (_burstSprite.Material as ShaderMaterial)?.Duplicate() as ShaderMaterial;
            if (_burstMaterial != null) {
                _burstSprite.Material = _burstMaterial;
                _burstSprite.Visible = false;
            }
        }

        _baseScale = Scale.X;
        _basePulseFrequency = PulseFrequency;
        _basePulseIntensity = PulseIntensity;
    }

    public override void _Process(double delta) {
        float dt = (float)delta;

        if (_isBursting) {
            _burstTimer += dt;
            UpdateBurst(dt);
        }
        else {
            // 正常律动更新
            UpdateCoreShaderParams();
        }
    }

    #endregion

    #region 爆发功能

    /// <summary>
    /// 触发黑洞爆发
    /// 剧烈律动 + 大范围屏幕扭曲
    /// 外部调用者可自行添加 ScreenShake 等额外效果
    /// </summary>
    public void Burst() {
        if (_isBursting) return;

        FmodLite.Play("event:/RegentFx/sfx/black_hole_1");

        _isBursting = true;
        _burstTimer = 0f;
        _baseScale = Scale.X;

        // 显示扭曲层
        if (_burstSprite != null) {
            _burstSprite.Visible = true;
            _burstSprite.GlobalPosition = GlobalPosition;
        }

        // Entry.Logger.Info($"[Blackhole] Burst triggered at position {GlobalPosition}");
    }

    /// <summary>
    /// 更新爆发状态
    /// </summary>
    private void UpdateBurst(float dt) {
        float t = _burstTimer / BURST_DURATION;

        if (t >= 1.0f) {
            EndBurst();
            return;
        }

        // === 阶段1: 快速 buildup ===
        float buildupProgress = Mathf.Min(_burstTimer / BURST_PEAK_TIME, 1.0f);
        float buildupEase = Mathf.Ease(buildupProgress, 0.0f); // Out

        // === 阶段2: 缓慢 decay ===
        float decayProgress = Mathf.Max(0f, (_burstTimer - BURST_PEAK_TIME) / (BURST_DURATION - BURST_PEAK_TIME));
        float decayEase = 1.0f - Mathf.Ease(decayProgress, 1.0f); // In

        // 综合强度（buildup → peak → decay）
        float intensity = buildupProgress < 1.0f ? buildupEase : decayEase;

        // 1. 本体剧烈缩放
        float currentScale = Mathf.Lerp(_baseScale, _baseScale * BURST_MAX_SCALE, intensity);
        Scale = new Vector2(currentScale, currentScale);

        // 2. 剧烈律动
        float burstPulseFreq = _basePulseFrequency * BURST_PULSE_MULTIPLIER;
        float burstPulseIntensity = _basePulseIntensity * (1.0f + intensity * 2.0f);

        if (_coreMaterial != null) {
            _coreMaterial.SetShaderParameter("pulse_frequency", burstPulseFreq);
            _coreMaterial.SetShaderParameter("pulse_intensity", burstPulseIntensity);
            _coreMaterial.SetShaderParameter("swirl_strength", SwirlStrength * (1.0f + intensity));
        }

        // 3. 屏幕扭曲
        if (_burstMaterial != null) {
            float distortionIntensity = BURST_MAX_DISTORTION * intensity;
            _burstMaterial.SetShaderParameter("distortion_intensity", distortionIntensity);
            _burstMaterial.SetShaderParameter("distortion_radius", BURST_RADIUS);
            _burstMaterial.SetShaderParameter("center_uv", new Vector2(0.5f, 0.5f));
        }

        // 4. 更新扭曲层位置跟随黑洞
        if (_burstSprite != null) {
            _burstSprite.GlobalPosition = GlobalPosition;
        }
    }

    /// <summary>
    /// 结束爆发，恢复正常状态
    /// </summary>
    private void EndBurst() {
        _isBursting = false;
        _burstTimer = 0f;

        // 恢复本体缩放
        Scale = new Vector2(_baseScale, _baseScale);

        // 隐藏扭曲层
        if (_burstSprite != null) {
            _burstSprite.Visible = false;
        }

        // 恢复 Shader 参数
        UpdateCoreShaderParams();

        Entry.Logger.Debug("[Blackhole] Burst ended, returning to normal state");
    }

    #endregion

    #region 参数控制

    /// <summary>
    /// 设置黑洞大小
    /// </summary>
    public void SetSize(float size) {
        BlackholeSize = size;
        UpdateCoreShaderParams();
    }

    /// <summary>
    /// 设置律动频率
    /// </summary>
    public void SetPulseFrequency(float freq) {
        PulseFrequency = freq;
        _basePulseFrequency = freq;
        if (!_isBursting) {
            UpdateCoreShaderParams();
        }
    }

    /// <summary>
    /// 设置律动强度
    /// </summary>
    public void SetPulseIntensity(float intensity) {
        PulseIntensity = intensity;
        _basePulseIntensity = intensity;
        if (!_isBursting) {
            UpdateCoreShaderParams();
        }
    }

    /// <summary>
    /// 设置漩涡强度
    /// </summary>
    public void SetSwirlStrength(float strength) {
        SwirlStrength = strength;
        if (!_isBursting) {
            UpdateCoreShaderParams();
        }
    }

    /// <summary>
    /// 更新核心 Shader 参数
    /// </summary>
    private void UpdateCoreShaderParams() {
        if (_coreMaterial == null) return;

        _coreMaterial.SetShaderParameter("blackhole_size", BlackholeSize);
        _coreMaterial.SetShaderParameter("pulse_frequency", PulseFrequency);
        _coreMaterial.SetShaderParameter("pulse_intensity", PulseIntensity);
        _coreMaterial.SetShaderParameter("swirl_strength", SwirlStrength);
    }

    #endregion

    #region 静态工厂方法

    public const float DEFAULT_SIZE = 100f;
    /// <summary>
    /// 在指定位置创建黑洞
    /// </summary>
    public static Blackhole? Create(Creature creature, float size = DEFAULT_SIZE) {
        if (TestMode.IsOn) return null;
        NCreature nCreature = NCombatRoom.Instance?.GetCreatureNode(creature);
        if (nCreature == null) return null;
        try {
            var blackhole = VFXUtil.GenVFXNode<Blackhole>(VfxScenePath);

            Node? backVfx = NCombatRoom.Instance?.BackCombatVfxContainer;
            if (backVfx == null) {
                Entry.Logger.Warn("[Blackhole] No BackCombatVfxContainer available for blackhole");
                blackhole.QueueFree();
                return null;
            }
            backVfx.AddChildSafely(blackhole);
            blackhole.GlobalPosition = nCreature.VfxSpawnPosition;
            blackhole.SetSize(size);
            Blackholes[creature] = blackhole;
            return blackhole;
        }
        catch (Exception ex) {
            Entry.Logger.Warn($"[Blackhole] Failed to create blackhole: {ex.Message}");
            return null;
        }
    }

    #endregion

	public static string VfxScenePath = "res://RegentFX/scenes/vfx/Blackhole.tscn";
    
    public static Dictionary<Creature, Blackhole> Blackholes = new();
    
    
    #region 清理

    public override void _ExitTree() {
        base._ExitTree();
    }

    #endregion
}
