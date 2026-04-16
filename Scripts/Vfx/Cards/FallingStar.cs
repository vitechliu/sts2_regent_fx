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
    
    private List<Vector2> starPos = new() {
        new Vector2(0f, 0f),
        new Vector2(30f, -50f),
    };
    
    public override Vector2 CalculateTargetPosition(Vector2 basePosition, int index, int totalCount) {
        return basePosition + TargetOffset + (starPos[index] * 1.2f);
    }
    
    public override void OnStartHolding(Star star, int index) {
        if (index == 0) {
            star.ChangeColorTo(new Color(14.551f, 14.551f, 0.0f)); //yellow
        }
        if (index == 1) {
            star.ChangeColorTo(new Color(14.551f, 0.683f, 9.982f)); //pink
        }
    }
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
            Node2D vfxNode = VFXUtil.GenVFXNode(ScenePath);
            if (vfxNode == null) return;

            var startNode = vfxNode.FindChild("StartPos") as Node2D;
            if (startNode == null) {
                Entry.Logger.Error("no start pos find");
                return;
            }
            var startPos = ownerNode.GlobalPosition + new FallingStar().TargetOffset;
            var targetPos = targetNode.VfxSpawnPosition;
            vfxNode.FitVFX(startNode.GlobalPosition, Vector2.Zero, startPos, targetPos);
            vfxNode.GlobalPosition = targetPos;


            Entry.StarEffectController?.OnCancelCard();
            // 添加到战斗特效容器
            NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(vfxNode);

            SimpleSfxUtil.Play(HitSFX);
            TaskHelper.RunSafely(ClearAfter(vfxNode));
            // WorldEnvironmentUtil.TweenGlowIntensity(3f, .05f);
            WorldEnvironmentUtil.TweenExposure(3f, .1f);
            await Cmd.Wait(0.15f);
            // WorldEnvironmentUtil.SetGlowIntensity(0);
            WorldEnvironmentUtil.TweenExposure(1f, .5f);

        } catch (Exception ex) {
            Entry.Logger.Error($"[FallingStar] Error playing VFX: {ex.Message}");
        }
    }

    public static async Task ClearAfter(Node2D? node) {
        await Cmd.Wait(2f);
        if (node != null && GodotObject.IsInstanceValid(node)) node.QueueFreeSafely();
    }
}
