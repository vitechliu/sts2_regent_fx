using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

namespace RegentFX.Scripts.Vfx;

/// <summary>
/// 星尘特效节点
/// 从指定起点射向目标，伴随拖尾、发光与缩放变化，命中后淡出销毁
/// </summary>
public partial class StardustVfx : Node2D {
	public const string ScenePath = "res://RegentFX/scenes/Stardust.tscn";

	public float MoveDuration { get; set; } = 0.35f;
	public float FadeOutDuration { get; set; } = 0.45f;
	public float StartScale { get; set; } = 0.6f;
	public float EndScale { get; set; } = 1.2f;
	public float RotationAmount { get; set; } = Mathf.Pi * 2f;

	private GpuParticles2D? _trailParticles;
	private Sprite2D? _glow;
	private Sprite2D? _sprite;
	private ShaderMaterial? _tintMaterial;
	private Tween? _moveTween;
	private Tween? _fadeTween;
	private Tween? _rotationTween;

	public override void _Ready() {
		_trailParticles = GetNodeOrNull<GpuParticles2D>("TrailParticles");
		_glow = GetNodeOrNull<Sprite2D>("Glow");
		_sprite = GetNodeOrNull<Sprite2D>("Sprite");

		if (_sprite != null) {
			_tintMaterial = (_sprite.Material as ShaderMaterial)?.Duplicate() as ShaderMaterial;
			if (_tintMaterial != null) {
				_sprite.Material = _tintMaterial;
			}
		}

		SetVisualActive(false);
	}

	/// <summary>
	/// 从指定起点射向目标位置，自动计算旋转角度朝向目标
	/// </summary>
	public void Launch(Vector2 from, Vector2 to) {
		GlobalPosition = from;
		Rotation = (to - from).Angle();

		SetVisualActive(true);
		Scale = Vector2.One * StartScale;

		_trailParticles?.Restart();

		_moveTween = CreateTween();
		_moveTween.SetTrans(Tween.TransitionType.Quad);
		_moveTween.SetEase(Tween.EaseType.Out);
		_moveTween.TweenProperty(this, "global_position", to, MoveDuration);
		
		_rotationTween = CreateTween();
		_rotationTween.SetTrans(Tween.TransitionType.Linear);
		_rotationTween.SetEase(Tween.EaseType.InOut);
		float startRotation = Rotation;
		float endRotation = startRotation + RotationAmount;
		_rotationTween.TweenMethod(Callable.From<float>(r => Rotation = r), startRotation, endRotation, MoveDuration);
		

		var scaleTween = CreateTween();
		scaleTween.SetTrans(Tween.TransitionType.Quad);
		scaleTween.SetEase(Tween.EaseType.Out);
		scaleTween.TweenProperty(this, "scale", Vector2.One * EndScale, MoveDuration);

		_moveTween.Finished += OnHitTarget;
	}

	private void OnHitTarget() {
		if (_trailParticles != null) {
			_trailParticles.Emitting = false;
		}

		_fadeTween = CreateTween();
		_fadeTween.SetTrans(Tween.TransitionType.Quad);
		_fadeTween.SetEase(Tween.EaseType.In);

		if (_sprite != null) {
			_fadeTween.TweenProperty(_sprite, "modulate:a", 0f, FadeOutDuration);
		}
		if (_glow != null) {
			_fadeTween.Parallel().TweenProperty(_glow, "modulate:a", 0f, FadeOutDuration);
		}
		_fadeTween.Parallel().TweenProperty(this, "scale", Vector2.Zero, FadeOutDuration);
		_fadeTween.Finished += Finished;
	}

	async void Finished() {
		await VFXUtil.Wait(1f);
		QueueFree();
	}

	private void SetVisualActive(bool active) {
		if (_sprite != null) {
			_sprite.Visible = active;
			_sprite.Modulate = new Color(_sprite.Modulate.R, _sprite.Modulate.G, _sprite.Modulate.B, 1f);
		}
		if (_glow != null) {
			_glow.Visible = active;
			_glow.Modulate = new Color(_glow.Modulate.R, _glow.Modulate.G, _glow.Modulate.B, 1f);
		}
		if (_trailParticles != null) {
			_trailParticles.Emitting = active;
		}
	}

	/// <summary>
	/// 静态便捷方法：加载场景并发射星尘
	/// </summary>
	public static StardustVfx? SpawnAndLaunch(Vector2 from, Vector2 to, Node? parent = null) {
		var stardust = VFXUtil.GenVFXNode<StardustVfx>(ScenePath);
		if (parent == null) {
			parent = NCombatRoom.Instance?.CombatVfxContainer;
		}

		if (parent == null) {
			Entry.Logger.Warn("[StardustVfx] No parent available for stardust");
			stardust.QueueFree();
			return null;
		}

		parent.AddChildSafely(stardust);
		stardust.Launch(from, to);
		return stardust;
	}

	/// <summary>
	/// 从指定起始位置发射星尘到目标位置
	/// </summary>
	public static void PlayStardust(Vector2 startPos, Vector2 targetPos) {
		if (TestMode.IsOn) return;
		SpawnAndLaunch(startPos, targetPos + VFXUtil.RandVec2(20f));
	}

	public override void _ExitTree() {
		_moveTween?.Kill();
		_rotationTween?.Kill();
		_fadeTween?.Kill();
		base._ExitTree();
	}
}
