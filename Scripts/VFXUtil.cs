using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.TestSupport;

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

    public static Node2D? PlaySimple(string scenePath, Vector2 position, float lifetime = 2f) {
        if (!TestMode.IsOn && NCombatRoom.Instance != null) {
            Node2D node2D = GenVFXNode(scenePath);
            NCombatRoom.Instance.CombatVfxContainer.AddChildSafely(node2D);
            node2D.GlobalPosition = position;
            
            SceneTreeTimer timer = node2D.GetTree().CreateTimer(lifetime);
            timer.Timeout += () => {
                if (GodotObject.IsInstanceValid(node2D)) {
                    node2D.QueueFreeSafely();
                }
            };
            return node2D;
        }
        return null;
    }

    public static async void ShakeAfter(float time, ShakeStrength strength, ShakeDuration duration, float degAngle = -1f) {
        await Cmd.Wait(time);
        NGame.Instance?.ScreenShake(strength, duration, degAngle);
    }
    public static HashSet<ulong> StarryImpactNodes = new();

    public static void PlaySpecialStarAt(Vector2 position) {
        if (!TestMode.IsOn && NCombatRoom.Instance != null) {
            NStarryImpactVfx node2D = GenVFXNode<NStarryImpactVfx>("res://scenes/vfx/vfx_starry_impact.tscn");
            StarryImpactNodes.Add(node2D.GetInstanceId());
            NCombatRoom.Instance.CombatVfxContainer.AddChildSafely(node2D);
            node2D.GlobalPosition = position;
        }
    }
    public static T? PlaySimple<T>(string scenePath, Vector2 position) where T : Node2D {
        if (!TestMode.IsOn && NCombatRoom.Instance != null) {
            T node2D = GenVFXNode<T>(scenePath);
            NCombatRoom.Instance.CombatVfxContainer.AddChildSafely(node2D);
            node2D.GlobalPosition = position;
            return node2D;
        }
        return null;
    }
    
    public static Node2D GenVFXNode(string scenePath) {
        return PreloadManager.Cache.GetScene(scenePath).Instantiate<Node2D>();
    }
    public static T GenVFXNode<T>(string scenePath) where T : Node2D {
        return PreloadManager.Cache.GetScene(scenePath).Instantiate<T>();
    }

    public static void ReplayAllParticles(Node2D node) {
        if (node is GpuParticles2D particles) {
            particles.Restart();
        }
        foreach (Node child in node.GetChildren()) {
            if (child is Node2D childNode) {
                ReplayAllParticles(childNode);
            }
        }
    }

    public static Vector2 GetEnemiesCenter(CardModel card) {
        Vector2 posFin = Vector2.Zero;
        IReadOnlyList<Creature> enemies = card.CombatState.HittableEnemies;
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
