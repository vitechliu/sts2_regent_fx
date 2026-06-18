using Godot;

namespace RegentFX.Scripts.Vfx;

/// <summary>
/// 星星特效节点
/// 支持自转、律动、随机变化效果
/// </summary>
public partial class Star : Node2D {

	public static string VfxScenePath = "res://RegentFX/scenes/Star.tscn";

	public static Star Create() {
		return VFXUtil.GenVFXNode<Star>(VfxScenePath);
	}
	
	public enum TintMode {
		Multiply = 0,
		Screen = 1,
		Overlay = 2,
		HueShift = 3,
	}

	#region 可配置参数 (可在Godot编辑器中调整)

	[Export] public float RotationSpeed { get; set; } = 90f;
	[Export] public bool RandomRotationDirection { get; set; } = true;
	[Export] public float RotationSpeedVariance { get; set; } = 30f;

	[Export] public bool EnablePulse { get; set; } = true;
	[Export] public float PulseSpeed { get; set; } = 2f;
	[Export] public float PulseMinScale { get; set; } = 0.7f;
	[Export] public float PulseMaxScale { get; set; } = 1.1f;
	[Export] public float PulseSpeedVariance { get; set; } = 0.5f;

	[Export] public bool EnableRandomOffset { get; set; } = true;
	[Export] public float RandomRotationOffset { get; set; } = 360f;
	[Export] public float RandomPulsePhaseOffset { get; set; } = 6.28f;

	[Export] public float BaseScale { get; set; } = 1f;
	[Export] public bool EnableTrail { get; set; } = false;

	[ExportGroup("Connection")] [Export] public float ConnectionLineWidth { get; set; } = 2f;
	[Export] public Color ConnectionLineColor { get; set; } = Colors.White;
	[Export] public float ConnectionLineAlpha { get; set; } = 1f;
	[Export] public bool EnableConnectionLinePulse { get; set; } = true;
	[Export] public float ConnectionLinePulseSpeed { get; set; } = 1.5f;
	[Export] public float ConnectionLinePulseMinAlpha { get; set; } = 0.3f;
	[Export] public float ConnectionLinePulseMaxAlpha { get; set; } = 1f;

	#endregion

	private Sprite2D? _sprite;
	private GpuParticles2D? _trailParticles;
	private ShaderMaterial? _tintMaterial;
	private float _actualRotationSpeed;
	private float _actualPulseSpeed;
	private float _pulsePhase;
	private float _rotationPhase;
	private int _rotationDirection;
	private float _connectionLinePulsePhase;
	private readonly Dictionary<Star, float> _connections = new();

	public override void _Ready() {
		_sprite = GetNode<Sprite2D>("Sprite");
		_trailParticles = GetNodeOrNull<GpuParticles2D>("TrailParticles");
		_tintMaterial = (_sprite?.Material as ShaderMaterial)?.Duplicate() as ShaderMaterial;
		if (_sprite != null && _tintMaterial != null)
			_sprite.Material = _tintMaterial;

		InitializeRandomValues();
		ApplyInitialTransform();
		UpdateTrailState();
	}

	/// <summary>
	/// 更新尾迹状态
	/// </summary>
	private void UpdateTrailState() {
		if (_trailParticles == null) return;

		_trailParticles.Visible = EnableTrail;
		_trailParticles.Emitting = EnableTrail;
	}

	public void ToggleTrail(bool trail) {
		EnableTrail = trail;
		UpdateTrailState();
	}

	/// <summary>
	/// 初始化随机值，让每个星星都有独特的行为
	/// </summary>
	private void InitializeRandomValues() {
		var rng = new RandomNumberGenerator();
		rng.Randomize();

		// 旋转方向和速度随机
		_rotationDirection = RandomRotationDirection ? (rng.RandiRange(0, 1) * 2 - 1) : 1;
		_actualRotationSpeed = RotationSpeed + rng.RandfRange(-RotationSpeedVariance, RotationSpeedVariance);
		_actualRotationSpeed *= _rotationDirection;

		// 旋转初始偏移
		_rotationPhase = EnableRandomOffset ? rng.RandfRange(0, RandomRotationOffset) : 0f;

		// 律动参数随机
		_actualPulseSpeed = PulseSpeed + rng.RandfRange(-PulseSpeedVariance, PulseSpeedVariance);
		_pulsePhase = EnableRandomOffset ? rng.RandfRange(0, RandomPulsePhaseOffset) : 0f;
	}

	/// <summary>
	/// 应用初始变换
	/// </summary>
	private void ApplyInitialTransform() {
		RotationDegrees = _rotationPhase;
		UpdateScale(0f);
	}

	public override void _Process(double delta) {
		float dt = (float)delta;

		// 自转
		if (Mathf.Abs(_actualRotationSpeed) > 0.001f) {
			RotationDegrees += _actualRotationSpeed * dt;
		}

		// 律动
		if (EnablePulse) {
			UpdateScale(dt);
		}

		// 连线脉冲
		if (EnableConnectionLinePulse && _connections.Count > 0) {
			_connectionLinePulsePhase += ConnectionLinePulseSpeed * dt;
			float pulseFactor = (Mathf.Sin(_connectionLinePulsePhase) + 1f) / 2f;
			ConnectionLineAlpha = Mathf.Lerp(ConnectionLinePulseMinAlpha, ConnectionLinePulseMaxAlpha, pulseFactor);
		}

		// 需要重绘连线
		if (_connections.Count > 0) {
			QueueRedraw();
		}
	}

	public override void _Draw() {
		foreach (var (target, baseAlpha) in _connections) {
			if (target == null || !IsInstanceValid(target)) continue;

			Vector2 targetPos = target.GlobalPosition;
			Vector2 from = ToLocal(GlobalPosition);
			Vector2 to = ToLocal(targetPos);

			Color lineColor = ConnectionLineColor;
			lineColor.A = baseAlpha * ConnectionLineAlpha;

			DrawLine(from, to, lineColor, ConnectionLineWidth);
		}
	}

	/// <summary>
	/// 更新缩放实现律动效果
	/// </summary>
	private void UpdateScale(float dt) {
		if (_sprite == null) return;

		_pulsePhase += _actualPulseSpeed * dt;

		// 使用正弦波创建平滑的律动效果
		float pulseFactor = (Mathf.Sin(_pulsePhase) + 1f) / 2f;
		float currentScale = Mathf.Lerp(PulseMinScale, PulseMaxScale, pulseFactor) * BaseScale;

		_sprite.Scale = new Vector2(currentScale, currentScale);
	}

	/// <summary>
	/// 重新随机化当前星星的参数（用于动态变化）
	/// </summary>
	public void ReRandomize() {
		InitializeRandomValues();
	}

	/// <summary>
	/// 设置基础缩放
	/// </summary>
	public void SetBaseScale(float scale) {
		BaseScale = scale;
	}

	/// <summary>
	/// 设置旋转速度
	/// </summary>
	public void SetRotationSpeed(float speed) {
		RotationSpeed = speed;
		_actualRotationSpeed = speed * _rotationDirection;
	}

	/// <summary>
	/// 设置律动速度
	/// </summary>
	public void SetPulseSpeed(float speed) {
		PulseSpeed = speed;
		_actualPulseSpeed = speed;
	}


	public void ResetColor() {
		ChangeColorImmediate(new Color(0f, 2.855f, 17.829f));
	}
	/// <summary>
	/// 渐变色调到目标颜色
	/// </summary>
	/// <param name="color">目标颜色</param>
	/// <param name="duration">过渡时长(秒)，0为瞬间切换</param>
	/// <param name="mode">混合模式，默认不变</param>
	public void ChangeColorTo(Color color, float duration = 0.3f, TintMode? mode = null) {
		if (_tintMaterial == null) return;

		if (mode.HasValue)
			SetTintMode(mode.Value);

		if (duration <= 0f) {
			_tintMaterial.SetShaderParameter("tint_color", color);
			UpdateTrailColor(color);
			return;
		}

		Color from = (Color)_tintMaterial.GetShaderParameter("tint_color");
		var tween = CreateTween();
		tween.SetTrans(Tween.TransitionType.Quad);
		tween.SetEase(Tween.EaseType.Out);
		tween.TweenMethod(Callable.From<float>((t) => {
			if (_tintMaterial == null) return;
			Color c = from.Lerp(color, t);
			_tintMaterial.SetShaderParameter("tint_color", c);
			UpdateTrailColor(c);
		}), 0f, 1f, duration);
	}
	public void ChangeColorImmediate(Color color) {
		ChangeColorTo(color, -1f);
	}

	/// <summary>
	/// 设置色调混合模式
	/// </summary>
	public void SetTintMode(TintMode mode) {
		if (_tintMaterial == null) return;
		_tintMaterial.SetShaderParameter("tint_mode", (int)mode);
	}

	private void UpdateTrailColor(Color color) {
		if (_trailParticles?.ProcessMaterial is not ParticleProcessMaterial mat) return;
		color.A = 0.17254902f;
		mat.Color = color;
	}

	/// <summary>
	/// 渐隐销毁
	/// </summary>
	public async void FadeOutAndDestroy(float duration = 0.5f) {
		if (_sprite == null) return;

		var tween = CreateTween();
		tween.SetTrans(Tween.TransitionType.Quad);
		tween.SetEase(Tween.EaseType.Out);

		tween.TweenProperty(_sprite, "modulate:a", 0f, duration);
		tween.TweenProperty(_sprite, "scale", Vector2.Zero, duration);

		await ToSignal(tween, Tween.SignalName.Finished);
		QueueFree();
	}

	/// <summary>
	/// 与目标星星建立连线
	/// </summary>
	public void ConnectTo(Star target, float? alpha = null) {
		if (target == null || target == this) return;
		if (!_connections.ContainsKey(target)) {
			_connections.Add(target, alpha ?? ConnectionLineAlpha);
		}
	}

	/// <summary>
	/// 断开与目标星星的连线
	/// </summary>
	public void DisconnectFrom(Star target) {
		if (target != null) {
			_connections.Remove(target);
			QueueRedraw();
		}
	}

	/// <summary>
	/// 断开所有连线
	/// </summary>
	public void DisconnectAll() {
		_connections.Clear();
		QueueRedraw();
	}

	/// <summary>
	/// 检查是否与目标星星有连线
	/// </summary>
	public bool IsConnectedTo(Star target) {
		return target != null && _connections.ContainsKey(target);
	}

	/// <summary>
	/// 设置指定连线的透明度
	/// </summary>
	public void SetConnectionAlpha(Star target, float alpha) {
		if (_connections.ContainsKey(target)) {
			_connections[target] = alpha;
		}
	}

	/// <summary>
	/// 获取所有已连接的星星
	/// </summary>
	public IReadOnlyDictionary<Star, float> GetConnections() {
		return _connections;
	}

	public override void _ExitTree() {
		DisconnectAll();
	}
}
