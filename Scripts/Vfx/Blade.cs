using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

namespace RegentFX.Scripts.Vfx;

/// <summary>
/// 飞刀特效节点
/// 从起点高速射向目标，伴随scale和发光变化，击中后震颤并淡出销毁
/// </summary>
public partial class Blade : Node2D {
	[Export] public float MoveDuration { get; set; } = 0.15f;
	[Export] public float ShakeDuration { get; set; } = 0.5f;
	[Export] public float ShakeIntensity { get; set; } = 4f;
	[Export] public float ShakeSpeed { get; set; } = 30f;
	[Export] public float FadeOutDuration { get; set; } = .3f;
	[Export] public float StartScaleX { get; set; } = 3f;
	[Export] public float EndScaleX { get; set; } = 1f;
	[Export] public float StartGlowIntensity { get; set; } = 2.0f;
	[Export] public float EndGlowIntensity { get; set; } = 0.2f;
	[Export] public Color StartGlowColor { get; set; } = new(1f, 1f, 1f, 1f);
	[Export] public Color EndGlowColor { get; set; } = new(1f, 1f, 0.8f, 1f);

	private Sprite2D? _sprite;
	private ShaderMaterial? _shaderMaterial;
	private Tween? _moveTween;
	private Tween? _shakeTween;
	private Tween? _fadeTween;
	private Vector2 _targetPosition;
	private bool _isShaking = false;
	private float _shakePhase = 0f;
	private Vector2 _shakeBasePosition;

	public override void _Ready() {
		_sprite = GetNodeOrNull<Sprite2D>("Sprite");
		if (_sprite != null) {
			_shaderMaterial = (_sprite.Material as ShaderMaterial)?.Duplicate() as ShaderMaterial;
			if (_shaderMaterial != null) {
				_sprite.Material = _shaderMaterial;
			}
		}
	}

	public override void _Process(double delta) {
		if (!_isShaking) return;

		float dt = (float)delta;
		_shakePhase += ShakeSpeed * dt;
		float offsetX = Mathf.Sin(_shakePhase) * ShakeIntensity;
		float offsetY = Mathf.Cos(_shakePhase * 1.3f) * ShakeIntensity;
		GlobalPosition = _shakeBasePosition + new Vector2(offsetX, offsetY);
	}

	/// <summary>
	/// 从起点射向目标位置，自动计算旋转角度朝向目标
	/// </summary>
	public void Launch(Vector2 from, Vector2 to) {
		GlobalPosition = from;
		_targetPosition = to;

		// 计算朝向目标的旋转角度
		Vector2 direction = to - from;
		Rotation = direction.Angle();

		// 初始状态：高拉伸 + 高发光
		Scale = new Vector2(StartScaleX, 1f);
		SetGlow(StartGlowIntensity, StartGlowColor);

		// 移动动画：EaseOut 高速平移
		_moveTween = CreateTween();
		_moveTween.SetTrans(Tween.TransitionType.Expo);
		_moveTween.SetEase(Tween.EaseType.Out);
		_moveTween.TweenProperty(this, "global_position", to, MoveDuration);

		// scale.x 从 StartScaleX -> EndScaleX
		var scaleTween = CreateTween();
		scaleTween.SetTrans(Tween.TransitionType.Quad);
		scaleTween.SetEase(Tween.EaseType.Out);
		scaleTween.TweenProperty(this, "scale:x", EndScaleX, MoveDuration);

		_moveTween.Finished += OnHitTarget;
	}

	private void OnHitTarget() {
		_isShaking = true;
		_shakeBasePosition = _targetPosition;
		_shakePhase = 0f;

		// 使用 Tween 的延迟来触发淡出
		var delayTween = CreateTween();
		delayTween.TweenInterval(ShakeDuration);
		delayTween.Finished += StartFadeOut;
		
		// 发光从强到弱
		var glowTween = CreateTween();
		glowTween.SetTrans(Tween.TransitionType.Quad);
		glowTween.SetEase(Tween.EaseType.Out);
		glowTween.TweenMethod(Callable.From<float>(t => {
			float intensity = Mathf.Lerp(StartGlowIntensity, EndGlowIntensity, t);
			Color color = StartGlowColor.Lerp(EndGlowColor, t);
			SetGlow(intensity, color);
		}), 0f, 1f, ShakeDuration + FadeOutDuration);
	}

	private void StartFadeOut() {
		_isShaking = false;

		_fadeTween = CreateTween();
		_fadeTween.SetTrans(Tween.TransitionType.Quad);
		_fadeTween.SetEase(Tween.EaseType.In);
		
		if (_sprite != null) {
			_fadeTween.TweenProperty(_sprite, "modulate:a", 0f, FadeOutDuration);
		}
		_fadeTween.TweenProperty(this, "scale:y", .2f, FadeOutDuration);
		_fadeTween.Finished += QueueFree;
	}

	private void SetGlow(float intensity, Color color) {
		if (_shaderMaterial == null) return;
		_shaderMaterial.SetShaderParameter("glow_intensity", intensity);
		_shaderMaterial.SetShaderParameter("glow_color", color);
	}

	public static string Blade1Path = "res://RegentFX/scenes/Blade1.tscn";
	public static string Blade2Path = "res://RegentFX/scenes/Blade2.tscn";

	/// <summary>
	/// 静态便捷方法：加载场景并发射飞刀
	/// </summary>
	public static Blade? SpawnAndLaunch(string scenePath, Vector2 from, Vector2 to, Node? parent = null) {
		var blade = VFXUtil.GenVFXNode<Blade>(scenePath);
		if (parent == null) {
			parent = NCombatRoom.Instance?.CombatVfxContainer;
		}

		if (parent == null) {
			Entry.Logger.Warn("[Blade] No parent available for blade");
			blade.QueueFree();
			return null;
		}

		parent.AddChildSafely(blade);
		blade.Launch(from, to);
		return blade;
	}

	public override void _ExitTree() {
		_moveTween?.Kill();
		_shakeTween?.Kill();
		_fadeTween?.Kill();
		base._ExitTree();
	}

	public static void PlayBlade(Vector2 position) {
		if (TestMode.IsOn) return;
		Vector2 startPos = GD.Randf() < 0.7 ? new Vector2(GD.Randi() % 600, 0f) : new Vector2(0f, GD.Randi() % 500);
		Vector2 targetPos = position + VFXUtil.RandVec2(30f);
		SpawnAndLaunch(Blade.Blade1Path, startPos, targetPos);
		if (GD.Randf() < 0.5f) {
			SpawnAndLaunch(Blade.Blade2Path, startPos - new Vector2(300f, 300f), targetPos);
		}
	}
}
