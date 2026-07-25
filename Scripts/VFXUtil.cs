using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using MegaCrit.Sts2.Core.TestSupport;

namespace RegentFX.Scripts;

public static class VFXUtil {

    public static bool RandRD(float percentage) {
        return GD.Randf() < percentage;
    }
    public static Vector2 RandVec2(float beta) {
        return new Vector2((float)GD.RandRange(-1f, 1f) * beta,  (float)GD.RandRange(-1f, 1f) * beta);
    }
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
    
    public static Node2D? PlaySimpleBack(string scenePath, Vector2 position, float lifetime = 2f) {
        if (!TestMode.IsOn && NCombatRoom.Instance != null) {
            Node2D node2D = GenVFXNode(scenePath);
            NCombatRoom.Instance.BackCombatVfxContainer.AddChildSafely(node2D);
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
        await VFXUtil.Wait(time);
        NGame.Instance?.ScreenShake(strength, duration, degAngle);
    }

    public static Task Wait(float seconds, bool ignoreCombatEnd = false)
    {
        return VFXUtil.Wait(seconds, new CancellationToken(), ignoreCombatEnd);
    }

    public static async Task Wait(float seconds, CancellationToken cancelToken, bool ignoreCombatEnd = false)
    {
        if (NonInteractiveMode.IsActive || (double)seconds <= 0.0 || NGame.Instance != null && (SaveManager.Instance.PrefsSave.FastMode == FastModeType.Instant || !ignoreCombatEnd && CombatManager.Instance.IsEnding))
            return;
        await VFXUtil.WaitInternal(((SceneTree)Engine.GetMainLoop()).CreateTimer((double)seconds), cancelToken);
    }

    public static Task WaitInternal(SceneTreeTimer timer, CancellationToken cancellationToken)
    {
        TaskCompletionSource tcs = new TaskCompletionSource();
        timer.Timeout += () =>
        {
            tcs.TrySetResult();
        };
        if (cancellationToken.CanBeCanceled)
            cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
        return tcs.Task;
    }

    public static async Task CustomScaledWait(
        float fastSeconds,
        float standardSeconds,
        bool ignoreCombatEnd = false,
        CancellationToken cancellationToken = default)
    {
        if (NonInteractiveMode.IsActive || SaveManager.Instance.PrefsSave.FastMode == FastModeType.Instant || !ignoreCombatEnd && CombatManager.Instance.IsEnding)
            return;
        switch (SaveManager.Instance.PrefsSave.FastMode)
        {
            case FastModeType.Normal:
                await VFXUtil.Wait(standardSeconds, cancellationToken, ignoreCombatEnd);
                break;
            case FastModeType.Fast:
                await VFXUtil.Wait(fastSeconds, cancellationToken, ignoreCombatEnd);
                break;
            case FastModeType.Instant:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    public static readonly HashSet<ulong> StarryImpactNodes = new();

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
        if (Entry.ModSceneCache.TryGetValue(scenePath, out var modScene)) {
            return modScene.Instantiate<Node2D>();
        }
        return PreloadManager.Cache.GetScene(scenePath).Instantiate<Node2D>();
    }
    public static T GenVFXNode<T>(string scenePath) where T : Node2D {
        if (Entry.ModSceneCache.TryGetValue(scenePath, out var modScene)) {
            return modScene.Instantiate<T>();
        }
        return PreloadManager.Cache.GetScene(scenePath).Instantiate<T>();
    }

    public static void ActivateScaleAllParticles(Node2D? node, float scale) {
        if (node == null || !GodotObject.IsInstanceValid(node)) return;
        if (node is GpuParticles2D particles) {
            particles.LocalCoords = true;
            particles.Scale *= scale;
        }
        foreach (Node child in node.GetChildren()) {
            if (child is Node2D childNode) {
                ActivateScaleAllParticles(childNode, scale);
            }
        }
    }
    public static void ReplayAllParticles(Node2D? node) {
        if (node == null || !GodotObject.IsInstanceValid(node)) return;
        if (node is GpuParticles2D particles) {
            particles.Restart();
        }
        foreach (Node child in node.GetChildren()) {
            if (child is Node2D childNode) {
                ReplayAllParticles(childNode);
            }
        }
    }

    //为了兼容103和104
    public static IReadOnlyList<Creature>? GetHittableEnemiesFromCard(CardModel card) {
        var combatState = Traverse.Create(card).Property("CombatState").GetValue();
        if (combatState == null) return null;
        var enemiesRaw = Traverse.Create(combatState).Property("HittableEnemies").GetValue();
        if (enemiesRaw is IReadOnlyList<Creature> enemies) {
            return enemies;
        }
        return null;
    }


    public static bool IsCharacterFacingRight(Creature creature) {
        Node2D? body = NCombatRoom.Instance?.GetCreatureNode(creature)?.Body;
        if (body == null) return true;
        return body.Scale.X > 0;
    }
    
    public static Vector2? GetCombatSidePos(CardModel card) {
        var combatState = Traverse.Create(card).Property("CombatState").GetValue();
        if (combatState == null) return null;
        //首先反射获取SideCenter
        var method = Traverse.Create(typeof(VfxCmd)).Method("GetSideCenter", CombatSide.Enemy, combatState);
        if (method == null) return null;
        var combatSidePos = method.GetValue();
        if (combatSidePos is Vector2 pos) {
            // Entry.Logger.Info("GCSP:" + pos);
            return pos;
        }
        return null;
    }
    
    public static Node2D? PlayOriginalVfx(Vector2 position, string path) {
        if (!TestMode.IsOn && NCombatRoom.Instance != null) {
            string scenePath = SceneHelper.GetScenePath(path);
            Node2D node2D = PreloadManager.Cache.GetScene(scenePath).Instantiate<Node2D>(PackedScene.GenEditState.Disabled);
            NCombatRoom.Instance.CombatVfxContainer.AddChildSafely(node2D);
            node2D.GlobalPosition = position;
            return node2D;
        }
        return null;
    }

    // public static Vector2? GetEnemiesCenter(CardModel card) {
    //     try {
    //         IReadOnlyList<Creature>? enemies = GetHittableEnemiesFromCard(card);
    //         if (enemies == null)  return null;
    //         Vector2 posFin = Vector2.Zero;
    //         if (enemies.Count <= 0) return null;
    //         foreach (var creature in enemies) {
    //             NCreature? targetNode = NCombatRoom.Instance?.GetCreatureNode(creature);
    //             if (targetNode == null) continue;
    //             posFin += targetNode.VfxSpawnPosition;
    //         }
    //         posFin /= enemies.Count;
    //         return posFin;
    //     }
    //     catch (Exception ex) {
    //         return null;
    //     }
    // }
}
