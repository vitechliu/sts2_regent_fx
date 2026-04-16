using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace RegentFX.Scripts;

public static class VFXUtil {
    public static void FitVFX(
        this Node2D node, 
        Vector2 nodeStartPos, 
        Vector2 nodeEndPos,
        Vector2 sceneStartPos, 
        Vector2 sceneEndPos
    ) {
        Vector2 originalVec = nodeStartPos - nodeEndPos;
        Vector2 targetVec = sceneStartPos - sceneEndPos;
            
        // 计算旋转角度（弧度）
        float angle = targetVec.Angle() - originalVec.Angle();
        // 计算均匀缩放因子
        float scale = targetVec.Length() / originalVec.Length();
        
        node.Rotation = angle;
        node.Scale = Vector2.One * scale;
    }
    
    public static Node2D? GenVFXNode(string scenePath) {
        return PreloadManager.Cache.GetScene(scenePath).Instantiate<Node2D>();
    }

    public static Vector2 GetEnemiesCenter(CombatState state) {
        Vector2 posFin = Vector2.Zero;
        IReadOnlyList<Creature> enemies = state.HittableEnemies;
        if (enemies.Count <= 0) return posFin;
        foreach (var creature in enemies) {
            NCreature? targetNode = NCombatRoom.Instance?.GetCreatureNode(creature);
            if (targetNode == null) continue;
            posFin += targetNode.VfxSpawnPosition;
        }
        posFin /= enemies.Count;
        return posFin;
    }
}
