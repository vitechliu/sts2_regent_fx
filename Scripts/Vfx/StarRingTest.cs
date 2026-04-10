using Godot;

namespace RegentFX.Scripts.Vfx;

/// <summary>
/// 星星环形旋转测试场景
/// 4个星星围绕中心点匀速圆周旋转
/// </summary>
public partial class StarRingTest : Node2D
{
	[Export] public float Radius { get; set; } = 100f;
	[Export] public float RotationSpeed { get; set; } = 90f;
	[Export] public bool Clockwise { get; set; } = true;

	private Node2D[]? _stars;
	private float _currentAngle = 0f;

	public override void _Ready()
	{
		// 获取4个子节点星星
		_stars = new Node2D[4];
		for (int i = 0; i < 4; i++)
		{
			_stars[i] = GetNode<Node2D>($"Star{i + 1}");
		}

		// 初始位置：均匀分布在圆周上（0°, 90°, 180°, 270°）
		UpdateStarPositions();
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;

		// 更新旋转角度
		float direction = Clockwise ? 1f : -1f;
		_currentAngle += RotationSpeed * direction * dt * Mathf.Pi / 180f;

		// 更新所有星星位置
		UpdateStarPositions();
	}

	/// <summary>
	/// 根据当前角度更新4个星星的位置
	/// </summary>
	private void UpdateStarPositions()
	{
		if (_stars == null) return;

		for (int i = 0; i < 4; i++)
		{
			// 每个星星间隔90度（PI/2）
			float angle = _currentAngle + i * Mathf.Pi / 2f;

			float x = Mathf.Cos(angle) * Radius;
			float y = Mathf.Sin(angle) * Radius;

			_stars[i].Position = new Vector2(x, y);
		}
	}

	/// <summary>
	/// 设置旋转半径
	/// </summary>
	public void SetRadius(float radius)
	{
		Radius = radius;
	}

	/// <summary>
	/// 设置旋转速度（度/秒）
	/// </summary>
	public void SetRotationSpeed(float speed)
	{
		RotationSpeed = speed;
	}

	/// <summary>
	/// 切换旋转方向
	/// </summary>
	public void ToggleDirection()
	{
		Clockwise = !Clockwise;
	}
}
