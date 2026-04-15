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
/// GuidingStar 卡牌特效
/// </summary>
public class GuidingStar : CardFX {
    public override int StarCount => 3;

    // 更靠左上的位置
    public override Vector2 TargetOffset => new(-200f, -450f);
    
    private List<Vector2> starPos = new() {
        new Vector2(2f, 2f),
        new Vector2(0f, -5f),
        new Vector2(-5f, 0f),
    };
    
    public override Vector2 CalculateTargetPosition(Vector2 basePosition, int index, int totalCount) {
        return basePosition + TargetOffset + starPos[index];
    }
}

[HarmonyPatch]
public static class GuidingStarPatch {
    
    private const string ScenePath = "res://RegentFX/scenes/guiding_star.tscn";
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(MegaCrit.Sts2.Core.Models.Cards.GuidingStar), "OnPlay")]
    public static bool OnPlay(
        MegaCrit.Sts2.Core.Models.Cards.GuidingStar __instance,
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ref Task __result) {
        __result = MyOnPlay(__instance, choiceContext, cardPlay);
        return false;
    }

    private static async Task MyOnPlay(
        MegaCrit.Sts2.Core.Models.Cards.GuidingStar card,
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay) {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await CreatureCmd.TriggerAnim(card.Owner.Creature, "Cast", card.Owner.Character.CastAnimDelay);
        SfxCmd.Play("event:/sfx/characters/regent/regent_guiding_star");
        await DamageCmd.Attack(card.DynamicVars.Damage.BaseValue)
            .FromCard(card)
            .Targeting(cardPlay.Target)
            .BeforeDamage(async delegate {
                await PlayVFX(card.Owner.Creature, cardPlay.Target);
            })
            .WithNoAttackerAnim()
            .Execute(choiceContext);
        DrawCardsNextTurnPower cardsNextTurnPower = await PowerCmd.Apply<DrawCardsNextTurnPower>(card.Owner.Creature, card.DynamicVars.Cards.BaseValue, card.Owner.Creature, card);

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
            var startPos = ownerNode.GlobalPosition + new GuidingStar().TargetOffset;
            var targetPos = targetNode.VfxSpawnPosition;
            vfxNode.FitVFX(startNode.GlobalPosition, Vector2.Zero, startPos, targetPos);
            vfxNode.GlobalPosition = targetPos;


            Entry.StarEffectController?.OnCancelCard();
            // 添加到战斗特效容器
            NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(vfxNode);
            TaskHelper.RunSafely(ClearAfter(vfxNode));
            await Cmd.Wait(0.3f);
        } catch (Exception ex) {
            Entry.Logger.Info($"[GuidingStar] Error playing VFX: {ex.Message}");
        }
    }

    public static async Task ClearAfter(Node2D? node) {
        await Cmd.Wait(2f);
        if (node != null && GodotObject.IsInstanceValid(node)) node.QueueFreeSafely();
    }
}
