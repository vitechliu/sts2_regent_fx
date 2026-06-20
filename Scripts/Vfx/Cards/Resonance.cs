using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using RegentFX.ThirdParty.Audio;
using Godot;
using MegaCrit.Sts2.Core.Extensions;


namespace RegentFX.Scripts.Vfx.Cards;

[CardFx(typeof(MegaCrit.Sts2.Core.Models.Cards.Resonance))]
public class Resonance : CardFX {
    public override int StarCount => 3;
    public override HoldingModes HoldingMode => HoldingModes.Custom;
    
    private static readonly List<Star> _borrowedStars = new();
    
    public override void HoldingCustom() {
        _borrowedStars.Clear();
        var c = Entry.StarRingController;
        if (c == null) return;
        var starData = c.OrbitStars.Take(StarCount).ToList();
        if (starData.Count < StarCount) return;
        foreach (var starDatum in starData) {
            _borrowedStars.Add(starDatum.Star);
            starDatum.Star.ToggleTrail(true);
            starDatum.Star.ChangeColorImmediate(new Color(14.551f, 14.551f, 0.0f));
            TweenRadius(starDatum, 1.5f, 0.15f);
        }
        TryPlayHoldingSfx();
    }
    
    public static void PlayStarVfx() {
        foreach (var star in _borrowedStars) {
            if (star != null && GodotObject.IsInstanceValid(star)) {
                VFXUtil.PlaySpecialStarAt(star.GlobalPosition);
            }
        }
        _borrowedStars.Clear();
        ResetOrbit();
    }

    static void ResetOrbit() {
        var c = Entry.StarRingController;
        if (c == null) return;
        foreach (var starDatum in c.OrbitStars) {
            starDatum.Star.ToggleTrail(false);
            starDatum.Star.ResetColor();
            if (Math.Abs(starDatum.RadiusMultiplier - 1f) > 0.01f) TweenRadius(starDatum, 1f, 0.15f);
        }
    }

    public override void OnCancel() {
        ResetOrbit();
        _borrowedStars.Clear();
    }

    private static void TweenRadius(StarRingController.StarData starDatum, float targetRadius, float duration) {
        starDatum.RadiusTween?.Kill();
        var tween = starDatum.Star.CreateTween();
        starDatum.RadiusTween = tween;
        tween.SetTrans(Tween.TransitionType.Expo);
        tween.SetEase(Tween.EaseType.Out);
        tween.TweenMethod(
            Callable.From<float>(v => starDatum.RadiusMultiplier = v),
            starDatum.RadiusMultiplier,
            targetRadius,
            duration
        );
    }

    public override string? VfxScenePath => "res://RegentFX/scenes/vfx/resonance.tscn";
}

[HarmonyPatch]
public static class ResonancePatch {
    [HarmonyPrefix]
    [HarmonyPatch(typeof(MegaCrit.Sts2.Core.Models.Cards.Resonance), "OnPlay")]
    public static void OnPlay(MegaCrit.Sts2.Core.Models.Cards.Resonance __instance) {
        if (!CardFX.IsTypeEnabled<Resonance>()) return;
        if (!LocalContext.IsMe(__instance.Owner)) return;
        _ = MyOnPlay(__instance);
    }
    
    private static async Task MyOnPlay(
        MegaCrit.Sts2.Core.Models.Cards.Resonance card) {
        NCreature? ownerNode = NCombatRoom.Instance?.GetCreatureNode(card.Owner.Creature);
        if (ownerNode != null) {
            FmodLite.Play("event:/RegentFx/sfx/Resonance");
            await VFXUtil.Wait( .1f);
            VFXUtil.PlaySimpleBack(CardFX.FromCard(card).VfxScenePath, ownerNode.VfxSpawnPosition, 2f);
            await VFXUtil.Wait( .1f);
            WorldEnvironmentUtil.TweenExposure(1.5f, .1f);
            await VFXUtil.Wait( .1f);
            WorldEnvironmentUtil.TweenExposure(1f, .3f);
        }
        Resonance.PlayStarVfx();
        Entry.StarEffectController?.OnPlayCard();
    }
}
