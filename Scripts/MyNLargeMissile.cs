
using Godot;
using Godot.Collections;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Nodes;

#nullable enable
namespace RegentFX.Scripts;

[ScriptPath("res://Scripts/MyNLargeMissile.cs")]
public partial class MyNLargeMissile : Node2D
{
  public static readonly string scenePath = SceneHelper.GetScenePath("vfx/vfx_large_magic_missile");
  [Export(PropertyHint.None, "")]
  public Array<GpuParticles2D> _anticipationParticles = new Array<GpuParticles2D>();
  [Export(PropertyHint.None, "")]
  public Array<GpuParticles2D> _projectileStartParticles = new Array<GpuParticles2D>();
  [Export(PropertyHint.None, "")]
  public Array<GpuParticles2D> _projectileParticles = new Array<GpuParticles2D>();
  [Export(PropertyHint.None, "")]
  public Array<GpuParticles2D> _impactParticles = new Array<GpuParticles2D>();
  [Export(PropertyHint.None, "")]
  public Array<GpuParticles2D> _modulateParticles = new Array<GpuParticles2D>();
  [Export(PropertyHint.None, "")]
  public Node2D? _anticipationContainer;
  [Export(PropertyHint.None, "")]
  public float _anticipationDuration = 0.2f;
  [Export(PropertyHint.None, "")]
  public Node2D? _projectileContainer;
  [Export(PropertyHint.None, "")]
  public Node2D? _projectileStartPoint;
  [Export(PropertyHint.None, "")]
  public Node2D? _projectileEndPoint;
  [Export(PropertyHint.None, "")]
  public float _projectileOffset = 100f;
  public CancellationTokenSource? _cts;

  [field: Export(PropertyHint.None, "")]
  public float WaitTime { get; set; } = 0.2f;

  public static MyNLargeMissile? Create(Vector2 targetFloorPosition, Color tint)
  {
	if (TestMode.IsOn)
	  return (MyNLargeMissile) null;
	MyNLargeMissile MyNLargeMissile = PreloadManager.Cache.GetScene(MyNLargeMissile.scenePath).Instantiate<MyNLargeMissile>();
	MyNLargeMissile.GlobalPosition = targetFloorPosition;
	MyNLargeMissile.Initialize();
	MyNLargeMissile.ModulateParticles(tint);
	return MyNLargeMissile;
  }

  public Vector2 GetProjectileDirection()
  {
	Vector3 vector3 = Quaternion.FromEuler(new Vector3(0.0f, 0.0f, Mathf.DegToRad(-30f))) * Vector3.Up;
	return new Vector2(vector3.X, vector3.Y).Normalized();
  }

  public Vector2 GetTopPosition(Vector2 projectileDirection)
  {
	return (Vector2) Geometry2D.LineIntersectsLine(this.GlobalPosition, projectileDirection, new Vector2(0.0f, 80f), Vector2.Right);
  }

  public void Initialize()
  {
	Vector2 projectileDirection = this.GetProjectileDirection();
	Vector2 topPosition = this.GetTopPosition(projectileDirection);
	this._anticipationContainer.GlobalPosition = topPosition;
	this._projectileStartPoint.GlobalPosition = topPosition + projectileDirection * this._projectileOffset;
	this._projectileEndPoint.GlobalPosition = this.GlobalPosition + projectileDirection * this._projectileOffset;
	this._projectileContainer.Visible = false;
  }

  public void ModulateParticles(Color tint)
  {
	for (int index = 0; index < this._modulateParticles.Count; ++index)
	  this._modulateParticles[index].SelfModulate = tint;
  }

  public override void _Ready() => TaskHelper.RunSafely(this.PlaySequence());

  public override void _ExitTree()
  {
	this._cts?.Cancel();
	this._cts?.Dispose();
  }

  public async Task PlaySequence()
  {
	MyNLargeMissile node = this;
	node._cts = new CancellationTokenSource();
	for (int index = 0; index < node._anticipationParticles.Count; ++index)
	  node._anticipationParticles[index].Restart();
	await Cmd.Wait(node._anticipationDuration, node._cts.Token);
	for (int index = 0; index < node._projectileStartParticles.Count; ++index)
	  node._projectileStartParticles[index].Restart();
	node._projectileContainer.GlobalPosition = node._projectileStartPoint.GlobalPosition;
	node._projectileContainer.Visible = true;
	for (int index = 0; index < node._projectileParticles.Count; ++index)
	  node._projectileParticles[index].Restart();
	double timer = 0.0;
	while (timer < (double) node.WaitTime && !node._cts.IsCancellationRequested)
	{
	  float weight = (float) timer / node.WaitTime;
	  node._projectileContainer.GlobalPosition = node._projectileStartPoint.GlobalPosition.Lerp(node._projectileEndPoint.GlobalPosition, weight);
	  timer += node.GetProcessDeltaTime();
	  Variant[] signal = await node.ToSignal((GodotObject) node.GetTree(), SceneTree.SignalName.ProcessFrame);
	}
	if (node._cts.IsCancellationRequested)
	  return;
	node._projectileContainer.Visible = false;
	for (int index = 0; index < node._impactParticles.Count; ++index)
	  node._impactParticles[index].Restart();
	NGame.Instance?.ScreenShake(ShakeStrength.Strong, ShakeDuration.Normal);
	await Cmd.Wait(2f, node._cts.Token);
	node.QueueFreeSafely();
  }
}
