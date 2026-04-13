using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;
using RegentFx.Core.Audio;

#pragma warning disable CS4014


namespace RegentFX.Scripts.Vfx.Cards;

/// <summary>
/// FallingStar 卡牌特效
/// </summary>
public class FallingStar : CardFX {
    public override int StarCount => 2;

    // 更靠左上的位置
    public override Vector2 TargetOffset => new(-200f, -450f);
}

[HarmonyPatch]
public static class FallingStarPatch {
    
    private const string HitSFX = "res://RegentFX/sfx/falling_star.mp3";
    private const string ScenePath = "res://RegentFX/scenes/falling_star.tscn";
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(MegaCrit.Sts2.Core.Models.Cards.FallingStar), "OnPlay")]
    public static bool OnPlay(
        MegaCrit.Sts2.Core.Models.Cards.FallingStar __instance,
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ref Task __result) {
        __result = MyOnPlay(__instance, choiceContext, cardPlay);
        return false;
    }

    private static async Task MyOnPlay(
        MegaCrit.Sts2.Core.Models.Cards.FallingStar card,
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay) {
        
        
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await CreatureCmd.TriggerAnim(card.Owner.Creature, "Cast", card.Owner.Character.CastAnimDelay);
        var cmd = DamageCmd.Attack(card.DynamicVars.Damage.BaseValue)
            .FromCard(card)
            .Targeting(cardPlay.Target)
            .BeforeDamage(async delegate {
                await PlayVFX(card.Owner.Creature, cardPlay.Target);
            });
        cmd._attackerAnimName = null;
        // Entry.Logger.Info("HasHitVFX?:" + cmd.HitVfx);
        await cmd.Execute(choiceContext);
        WeakPower weakPower = await PowerCmd.Apply<WeakPower>(cardPlay.Target, card.DynamicVars.Weak.BaseValue, card.Owner.Creature, card);
        VulnerablePower vulnerablePower = await PowerCmd.Apply<VulnerablePower>(cardPlay.Target, card.DynamicVars.Vulnerable.BaseValue, card.Owner.Creature, card);
    }
    
    
    private static async Task PlayVFX(Creature owner, Creature target) {
        if (TestMode.IsOn) {
            return;
        }
        NCreature? ownerNode = NCombatRoom.Instance?.GetCreatureNode(owner);
        NCreature? targetNode = NCombatRoom.Instance?.GetCreatureNode(target);

        if (ownerNode == null || targetNode == null) {
            Entry.Logger.Info("[CrescentSpear] Could not get creature nodes for VFX");
            return;
        }
        try {
            Node2D vfxNode = CardFX.GenVFXNode(ScenePath);
            if (vfxNode == null) return;

            var startNode = vfxNode.FindChild("StartPos") as Node2D;
            if (startNode == null) {
                Entry.Logger.Error("no start pos find");
                return;
            }
            Vector2 starPos = ownerNode.GlobalPosition + new FallingStar().TargetOffset;
            Vector2 targetPos = targetNode.VfxSpawnPosition;

            Vector2 originalVec = startNode.GlobalPosition - Vector2.Zero;
            Vector2 targetVec = starPos - targetPos;
            
            // 计算旋转角度（弧度）
            float angle = targetVec.Angle() - originalVec.Angle();
            // 计算均匀缩放因子
            float scale = targetVec.Length() / originalVec.Length();

            vfxNode.Rotation = angle;
            vfxNode.Scale = Vector2.One * scale;
            // startPos: 玩家位置 + TargetOffset
            vfxNode.GlobalPosition = targetPos;

            // 添加到战斗特效容器
            NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(vfxNode);

            SimpleSfxUtil.Play(HitSFX);
            TaskHelper.RunSafely(ClearAfter(vfxNode));
            await Cmd.Wait(0.15f);
        } catch (Exception ex) {
            Entry.Logger.Info($"[FallingStar] Error playing VFX: {ex.Message}");
        }
    }

    public static async Task ClearAfter(Node2D? node) {
        await Cmd.Wait(2f);
        if (node != null && GodotObject.IsInstanceValid(node)) node.QueueFreeSafely();
    }
}
