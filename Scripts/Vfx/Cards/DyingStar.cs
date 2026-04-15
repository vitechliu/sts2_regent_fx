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

public class DyingStar : CardFX {
    public override int StarCount => 3;

    // 更靠左上的位置
    public override Vector2 TargetOffset => new(-200f, -450f);
    
    private List<Vector2> starPos = new() {
        new Vector2(0f, 0f),
        new Vector2(13f, -24f),
        new Vector2(30f, -5f),
    };
    
    public override Vector2 CalculateTargetPosition(Vector2 basePosition, int index, int totalCount) {
        return basePosition + TargetOffset + (starPos[index] * 2.5f);
    }
    public override void OnStartHolding(Star star, int index) {
        star.PulseMinScale *= 2f;
        star.PulseMaxScale *= 1.8f;
    }
}

[HarmonyPatch]
public static class DyingStarPatch {
    
    private const string HitSFX = "res://RegentFX/sfx/dying_star.mp3";
    private const string HitSFX1 = "res://RegentFX/sfx/common_hold_4.mp3";
    private const string ScenePath = "res://RegentFX/scenes/dying_star.tscn";
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(MegaCrit.Sts2.Core.Models.Cards.DyingStar), "OnPlay")]
    public static bool OnPlay(
        MegaCrit.Sts2.Core.Models.Cards.DyingStar __instance,
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ref Task __result) {
        __result = MyOnPlay(__instance, choiceContext, cardPlay);
        return false;
    }

    private static async Task MyOnPlay(
        MegaCrit.Sts2.Core.Models.Cards.DyingStar card,
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay) {
        await CreatureCmd.TriggerAnim(card.Owner.Creature, "Cast", card.Owner.Character.CastAnimDelay);
        IReadOnlyList<Creature> enemies = card.CombatState.HittableEnemies;
        var cmd = DamageCmd.Attack(card.DynamicVars.Damage.BaseValue)
            .FromCard(card)
            .TargetingAllOpponents(card.CombatState)
            .WithHitFx("vfx/vfx_starry_impact")
            .SpawningHitVfxOnEachCreature()
            .BeforeDamage(async delegate {
                await PlayVFX(card.Owner.Creature, VFXUtil.GetEnemiesCenter(card.CombatState));
            });
        cmd._attackerAnimName = null;
        await cmd.Execute(choiceContext);
        foreach (Creature enemy in (IEnumerable<Creature>) enemies)
        {
            await PowerCmd.Apply<DyingStarPower>(enemy, 
                card.DynamicVars["StrengthLoss"].BaseValue, card.Owner.Creature, card);
        }
        enemies = null;
    }
    
    
    private static async Task PlayVFX(Creature owner, Vector2 targetPos) {
        if (TestMode.IsOn) {
            return;
        }
        NCreature? ownerNode = NCombatRoom.Instance?.GetCreatureNode(owner);
        if (ownerNode == null) {
            Entry.Logger.Info("Could not get creature nodes for VFX");
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
            var startPos = ownerNode.GlobalPosition + new DyingStar().TargetOffset;
            vfxNode.FitVFX(startNode.GlobalPosition, Vector2.Zero, startPos, targetPos);
            vfxNode.GlobalPosition = targetPos;

            Entry.StarEffectController?.OnCancelCard();
            // 添加到战斗特效容器
            NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(vfxNode);
            SimpleSfxUtil.Play(HitSFX1);
            TaskHelper.RunSafely(ClearAfter(vfxNode));
            // WorldEnvironmentUtil.TweenGlowIntensity(3f, .05f);
            await Cmd.Wait(0.4f);
            SimpleSfxUtil.Play(HitSFX);
            WorldEnvironmentUtil.TweenExposure(3f, .2f);
            await Cmd.Wait(0.2f);
            // WorldEnvironmentUtil.SetGlowIntensity(0);
            WorldEnvironmentUtil.TweenExposure(1f, .3f);

        } catch (Exception ex) {
            Entry.Logger.Info($"Error playing VFX: {ex.Message}");
        }
    }

    public static async Task ClearAfter(Node2D? node) {
        await Cmd.Wait(3f);
        if (node != null && GodotObject.IsInstanceValid(node)) node.QueueFreeSafely();
    }
}
