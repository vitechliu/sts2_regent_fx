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
    private static bool _isProcessing = false;
    
    
    [HarmonyPatch(typeof(AttackCommand), nameof(AttackCommand.Execute))]
    [HarmonyPrefix]
    public static bool ExecutePatch(AttackCommand __instance, PlayerChoiceContext? choiceContext, ref Task<AttackCommand> __result) {
        // 记录当前攻击来源，供动画链路末端（如NRegentVfx.Attack）读取
        
        if (_isProcessing) return true; 
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
            if (cardFX.HasOnBeforeDamage) {
                __instance.BeforeDamage(async delegate {
                    await cardFX.OnBeforeDamage(__instance);
                });
            }

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

    static async Task<AttackCommand> RunCustomFlow(CardFX cardFX, CardModel card, AttackCommand instance, PlayerChoiceContext? choiceContext) {
        _isProcessing = true;
        try {
            await BeforeExecute(cardFX, card, instance);
            return await instance.Execute(choiceContext);
        } finally {
            _isProcessing = false;
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
        __result = AsyncPostOnPlayPatch(__result);
    }

    static async Task AsyncPostOnPlayPatch(Task originalTask) {
        await originalTask;
        AttackVfxContext.ShouldDisableRegentWeaponAttack = false;
        AttackVfxContext.ShouldDisableRegentWeaponSFX = false;
        AttackVfxContext.CurrentModelSource = null;
    }
    
    //阻止群星动画
    [HarmonyPrefix]
    [HarmonyPatch(typeof(NRegentVfx), nameof(NRegentVfx.Attack))]
    static bool PreventRegentAnimPatch() {
        if (AttackVfxContext.ShouldDisableRegentWeaponAttack) {
            Entry.Logger.Info("DisableAttack2——1");
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
        if (AttackVfxContext.ShouldDisableRegentWeaponSFX) {
            if (sfx == CardFX.DEFAULT_REGENT_ATTACK_SFX)
                return false;
        }
        return true;
    }
}
