using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace RegentFX.Scripts.Vfx;

/// <summary>
/// 星星攻击特效控制器（单例）
/// 仅本地玩家拥有，用于处理星星的攻击特效
/// </summary>
public partial class StarEffectController : Node2D {
    private StarRingController? _starRingController;
    private NCreature? _playerNode;

    StarRingController? StarRingController => Entry.StarRingController;

    /// <summary>
    /// 初始化控制器
    /// </summary>
    public void Initialize(NCreature playerNode, StarRingController? starRingController = null)
    {
        _playerNode = playerNode;
        _starRingController = starRingController;
        GlobalPosition = playerNode.GlobalPosition;
    }

    /// <summary>
    /// 清除所有特效
    /// </summary>
    public void ClearAllEffects()
    {
        // TODO: 实现特效清理逻辑
    }

    public override void _ExitTree()
    {
        ClearAllEffects();
        base._ExitTree();
    }
}
