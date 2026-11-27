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
    [ThreadStatic]
    private static AttackCommand? _resumingAttack;
    
    
    [HarmonyPatch(typeof(AttackCommand), nameof(AttackCommand.Execute))]
    [HarmonyPrefix]
    public static bool ExecutePatch(AttackCommand __instance, PlayerChoiceContext? choiceContext,
        ref Task<AttackCommand> __result, out AttackCommand? __state) {
        // 记录当前攻击来源，供动画链路末端（如NRegentVfx.Attack）读取
        __state = AttackVfxContext.CurrentAttackCommand.Value;
        AttackVfxContext.CurrentAttackCommand.Value = __instance;
        if (ReferenceEquals(_resumingAttack, __instance)) return true;

        AttackVfxContext.ShouldDisableRegentWeaponAttack = false;
        AttackVfxContext.ShouldDisableRegentWeaponSFX = false;
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
                __result = RunCustomFlow(cardFX, __instance, choiceContext);
                return false;
            }
        }
        catch (InvalidCastException) {
        }
        return true;
    }

    [HarmonyPatch(typeof(AttackCommand), nameof(AttackCommand.Execute))]
    [HarmonyFinalizer]
    static void RestoreAttackContext(AttackCommand? __state) {
        AttackVfxContext.CurrentAttackCommand.Value = __state;
    }

    static async Task<AttackCommand> RunCustomFlow(CardFX cardFX, AttackCommand instance, PlayerChoiceContext? choiceContext) {
        try {
            await cardFX.OnBeforeExecute();
        }
        catch (Exception ex) {
            Entry.Logger.Warn($"[{cardFX.GetType().Name}] 攻击前特效失败: {ex}");
        }

        return await ResumeAttack(instance, choiceContext);
    }

    static Task<AttackCommand> ResumeAttack(AttackCommand instance, PlayerChoiceContext? choiceContext) {
        var previous = _resumingAttack;
        _resumingAttack = instance;
        try {
            return instance.Execute(choiceContext);
        } finally {
            _resumingAttack = previous;
        }
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
            AttackVfxContext.ShouldDisableRegentWeaponAttack = false;
            AttackVfxContext.ShouldDisableRegentWeaponSFX = false;
            AttackVfxContext.CurrentModelSource = null;
            try {
                Entry.StarEffectController?.OnCardPlayed(card);
            }
            catch (Exception ex) {
                Entry.Logger.Warn($"[{card.Id}] 出牌后特效清理失败: {ex}");
            }
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
