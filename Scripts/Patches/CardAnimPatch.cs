using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using RegentFX.Scripts.Vfx;
using RegentFX.Scripts.Vfx.Cards;

namespace RegentFX.Scripts.Patches;

[HarmonyPatch]
public static class CardAnimPatch {
    private static readonly AsyncLocal<AttackCommand?> _processingCommand = new();

    private sealed record AttackVfxState(
        CardModel? ModelSource,
        bool DisableWeaponAttack,
        bool DisableWeaponSfx,
        AttackCommand? AttackCommand);
    
    
    [HarmonyPatch(typeof(AttackCommand), nameof(AttackCommand.Execute))]
    [HarmonyPrefix]
    public static bool ExecutePatch(
        AttackCommand __instance,
        PlayerChoiceContext? choiceContext,
        ref Task<AttackCommand> __result,
        out object? __state) {
        // 记录当前攻击来源，供动画链路末端（如NRegentVfx.Attack）读取

        __state = null;
        if (ReferenceEquals(_processingCommand.Value, __instance)) return true;

        __state = new AttackVfxState(
            AttackVfxContext.CurrentModelSource,
            AttackVfxContext.ShouldDisableRegentWeaponAttack,
            AttackVfxContext.ShouldDisableRegentWeaponSFX,
            AttackVfxContext.CurrentAttackCommand.Value);
        AttackVfxContext.ShouldDisableRegentWeaponAttack = false;
        AttackVfxContext.ShouldDisableRegentWeaponSFX = false;
        AttackVfxContext.CurrentAttackCommand.Value = __instance;
        if (__instance.ModelSource == null) return true;
        try {
            var card = __instance.ModelSource as CardModel;
            AttackVfxContext.CurrentModelSource = card;
            var cardFX = CardFX.FromCard(card);
            if (cardFX == null) return true;
            if (!cardFX.UseV2Patch) return true;
            if (!LocalContext.IsMe(card.Owner)) return true;


            if (cardFX.ShouldDisableRegentWeaponAttack) {
                // Entry.Logger.Info("DisableAttack1");
                AttackVfxContext.ShouldDisableRegentWeaponAttack = true;
            }
            if (cardFX.ShouldDisableRegentWeaponSFX) {
                AttackVfxContext.ShouldDisableRegentWeaponSFX = true;
            }

            // if (cardFX.DisableAttackAnim) {
            //     __instance.WithNoAttackerAnim();
            // }
            // 旧版无参 BeforeDamage 通过 DamageTargetPatch 统一处理；这里不再重复注册
            if (cardFX.ChangeHitFx != null) {
                __instance.WithHitFx(cardFX.ChangeHitFx);
            }
            if (cardFX.RemoveHitFx) {
                __instance.WithHitFx();
            }
            if (cardFX.HasOnBeforeExecute) {
                __result = RunCustomFlow(cardFX, card, __instance, choiceContext);
                return false;
            }
        }
        catch (InvalidCastException) {
        }
        return true;
    }

    [HarmonyPatch(typeof(AttackCommand), nameof(AttackCommand.Execute))]
    [HarmonyPostfix]
    public static void ExecutePostfix(object? __state) {
        if (__state is not AttackVfxState state) return;

        AttackVfxContext.CurrentModelSource = state.ModelSource;
        AttackVfxContext.ShouldDisableRegentWeaponAttack = state.DisableWeaponAttack;
        AttackVfxContext.ShouldDisableRegentWeaponSFX = state.DisableWeaponSfx;
        AttackVfxContext.CurrentAttackCommand.Value = state.AttackCommand;
    }

    static async Task<AttackCommand> RunCustomFlow(CardFX cardFX, CardModel card, AttackCommand instance, PlayerChoiceContext? choiceContext) {
        AttackCommand? previousCommand = _processingCommand.Value;
        _processingCommand.Value = instance;
        try {
            await BeforeExecute(cardFX, card, instance);
            return await instance.Execute(choiceContext);
        } finally {
            _processingCommand.Value = previousCommand;
        }
    }
    
    
    static async Task BeforeExecute(CardFX cardFX, CardModel card, AttackCommand command) {
        // if (cardFX.PlayCastAnim) {
        //     await CreatureCmd.TriggerAnim(card.Owner.Creature, "Cast", card.Owner.Character.CastAnimDelay);
        // }
        await cardFX.OnBeforeExecute();
    }

    [HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper))]
    [HarmonyPostfix]
    public static void PostOnPlayPatch(CardModel __instance, ref Task __result) {
        __result = AsyncPostOnPlayPatch(__result, __instance);
    }

    static async Task AsyncPostOnPlayPatch(Task originalTask, CardModel card) {
        try {
            await originalTask;
        }
        finally {
            Entry.StarEffectController?.OnCardPlayed(card);
            AttackVfxContext.ShouldDisableRegentWeaponAttack = false;
            AttackVfxContext.ShouldDisableRegentWeaponSFX = false;
            AttackVfxContext.CurrentModelSource = null;
        }
    }
    
    //阻止群星动画
    [HarmonyPrefix]
    [HarmonyPatch(typeof(NRegentVfx), nameof(NRegentVfx.Attack))]
    static bool PreventRegentAnimPatch() {
        if (AttackVfxContext.ShouldDisableRegentWeaponAttack) {
            // Entry.Logger.Info("DisableAttack2——1");
            // AttackVfxContext.ShouldDisableRegentWeaponAttack = false;
            return false;
        }
        // Entry.Logger.Info("DisableAttack2——2");
        return true;
    }
    
    
    //阻止默认动画音效
    [HarmonyPrefix]
    [HarmonyPatch(typeof(SfxCmd), nameof(SfxCmd.Play), [typeof(string), typeof(float)])]
    static bool PreventRegentSfx(string sfx, float volume) {
        if (AttackVfxContext.ShouldDisableRegentWeaponSFX && !Setting.DisableModSounds) {
            if (sfx == CardFX.DEFAULT_REGENT_ATTACK_SFX)
                return false;
        }
        return true;
    }
}
