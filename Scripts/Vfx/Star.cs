using Godot;

namespace RegentFX.Scripts.Vfx;

/// <summary>
/// 星星特效节点
/// 支持自转、律动、随机变化效果
/// </summary>
public partial class Star : Node2D
{
	#region 可配置参数 (可在Godot编辑器中调整)

	[Export] public float RotationSpeed { get; set; } = 90f;
	[Export] public bool RandomRotationDirection { get; set; } = true;
	[Export] public float RotationSpeedVariance { get; set; } = 30f;

	[Export] public bool EnablePulse { get; set; } = true;
	[Export] public float PulseSpeed { get; set; } = 2f;
	[Export] public float PulseMinScale { get; set; } = 0.7f;
	[Export] public float PulseMaxScale { get; set; } = 1.3f;
	[Export] public float PulseSpeedVariance { get; set; } = 0.5f;

	[Export] public bool EnableRandomOffset { get; set; } = true;
	[Export] public float RandomRotationOffset { get; set; } = 360f;
	[Export] public float RandomPulsePhaseOffset { get; set; } = 6.28f;

	[Export] public float BaseScale { get; set; } = 1f;
	[Export] public bool EnableTrail { get; set; } = false;

	#endregion

	private Sprite2D? _sprite;
	private GpuParticles2D? _trailParticles;
	private float _actualRotationSpeed;
	private float _actualPulseSpeed;
	private float _pulsePhase;
	private float _rotationPhase;
	private int _rotationDirection;

	public override void _Ready()
	{
		_sprite = GetNode<Sprite2D>("Sprite");
		_trailParticles = GetNodeOrNull<GpuParticles2D>("TrailParticles");

		InitializeRandomValues();
		ApplyInitialTransform();
		UpdateTrailState();
	}

	/// <summary>
	/// 更新尾迹状态
	/// </summary>
	private void UpdateTrailState()
	{
		if (_trailParticles == null) return;

		_trailParticles.Visible = EnableTrail;
		_trailParticles.Emitting = EnableTrail;
	}

	/// <summary>
	/// 初始化随机值，让每个星星都有独特的行为
	/// </summary>
	private void InitializeRandomValues()
	{
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
	private void ApplyInitialTransform()
	{
		RotationDegrees = _rotationPhase;
		UpdateScale(0f);
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;

		// 自转
		if (Mathf.Abs(_actualRotationSpeed) > 0.001f)
		{
			RotationDegrees += _actualRotationSpeed * dt;
		}

		// 律动
		if (EnablePulse)
		{
			UpdateScale(dt);
		}
	}

	/// <summary>
	/// 更新缩放实现律动效果
	/// </summary>
	private void UpdateScale(float dt)
	{
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
	public void ReRandomize()
	{
		InitializeRandomValues();
	}

	/// <summary>
	/// 设置基础缩放
	/// </summary>
	public void SetBaseScale(float scale)
	{
		BaseScale = scale;
	}

	/// <summary>
	/// 设置旋转速度
	/// </summary>
	public void SetRotationSpeed(float speed)
	{
		RotationSpeed = speed;
		_actualRotationSpeed = speed * _rotationDirection;
	}

	/// <summary>
	/// 设置律动速度
	/// </summary>
	public void SetPulseSpeed(float speed)
	{
		PulseSpeed = speed;
		_actualPulseSpeed = speed;
	}

	/// <summary>
	/// 渐隐销毁
	/// </summary>
	public async void FadeOutAndDestroy(float duration = 0.5f)
	{
		if (_sprite == null) return;

		var tween = CreateTween();
		tween.SetTrans(Tween.TransitionType.Quad);
		tween.SetEase(Tween.EaseType.Out);

		tween.TweenProperty(_sprite, "modulate:a", 0f, duration);
		tween.TweenProperty(_sprite, "scale", Vector2.Zero, duration);

		await ToSignal(tween, Tween.SignalName.Finished);
		QueueFree();
	}
}
