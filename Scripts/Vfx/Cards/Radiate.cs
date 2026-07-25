using Godot;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.TestSupport;
using RegentFX.ThirdParty.Audio;

namespace RegentFX.Scripts.Vfx.Cards;

[CardFx(typeof(MegaCrit.Sts2.Core.Models.Cards.Radiate))]
public class Radiate : CardFX {
    public const string ConvergeScenePath = "res://RegentFX/scenes/vfx/radiate_converge.tscn";
    public const string PulseScenePath = "res://RegentFX/scenes/vfx/radiate_outward_pulse.tscn";

    private const float ConvergeDuration = 0.23f;
    private const float PulseLifetime = 0.28f;
    private const float PulsePositionJitter = 12f;
    private const float PulseRotationJitterDegrees = 6f;

    public override HoldingModes HoldingMode => HoldingModes.None;
    public override string VfxScenePath => ConvergeScenePath;
    public override List<string> AssetPaths => [ConvergeScenePath, PulseScenePath];

    public override bool UseV2Patch => true;
    public override bool HasOnBeforeExecute => true;
    public override bool HasOnBeforeDamageTargeted => true;

    public override async Task OnBeforeExecute() {
        if (TestMode.IsOn || card == null || NCombatRoom.Instance == null) return;
        if (card.Owner.Creature.IsDead || GetCalculatedHits() <= 0) return;

        NCreature? ownerNode = NCombatRoom.Instance.GetCreatureNode(card.Owner.Creature);
        if (ownerNode == null) {
            Entry.Logger.Warn("[Radiate] Could not get owner creature node for converge VFX");
            return;
        }
        FmodLite.Play("event:/RegentFx/sfx/common_hold_3");
        VFXUtil.PlaySimple(ConvergeScenePath, ownerNode.VfxSpawnPosition, 0.34f);
        await VFXUtil.Wait(ConvergeDuration);
        if (NCombatRoom.Instance == null || !GodotObject.IsInstanceValid(ownerNode)) return;

        Node2D? energyVfx = VFXUtil.PlaySimple(
            "res://scenes/vfx/energy/regent/regent_energy_vfx_back.tscn",
            ownerNode.VfxSpawnPosition,
            3f);
        if (energyVfx == null) return;

        VFXUtil.ActivateScaleAllParticles(energyVfx, 3f);
        VFXUtil.ReplayAllParticles(energyVfx);
    }

    public override Task OnBeforeDamage(AttackCommand command, IReadOnlyList<Creature> targets) {
        if (TestMode.IsOn || card == null || NCombatRoom.Instance == null) {
            return Task.CompletedTask;
        }

        Creature owner = card.Owner.Creature;
        if (owner.IsDead) return Task.CompletedTask;

        NCreature? ownerNode = NCombatRoom.Instance.GetCreatureNode(owner);
        if (ownerNode == null) {
            Entry.Logger.Warn("[Radiate] Could not get owner creature node for pulse VFX");
            return Task.CompletedTask;
        }

        Vector2 pulsePosition = ownerNode.VfxSpawnPosition + VFXUtil.RandVec2(PulsePositionJitter);
        FmodLite.Play("event:/RegentFx/sfx/genesis_2");
        Node2D? pulse = VFXUtil.PlaySimple(PulseScenePath, pulsePosition, PulseLifetime);
        if (pulse != null) {
            pulse.RotationDegrees = (float)GD.RandRange(
                -PulseRotationJitterDegrees,
                PulseRotationJitterDegrees);
            RandomizePulseShader(pulse);
            VFXUtil.ReplayAllParticles(pulse);
        }
        Node2D? energyVfx = VFXUtil.PlaySimple(
            "res://scenes/vfx/energy/regent/regent_energy_vfx_back.tscn",
            ownerNode.VfxSpawnPosition,
            3f);
        VFXUtil.ActivateScaleAllParticles(energyVfx, 2f);
        VFXUtil.ReplayAllParticles(energyVfx);
        NGame.Instance?.ScreenShakeTrauma(ShakeStrength.VeryWeak);
        return Task.CompletedTask;
    }

    private static void RandomizePulseShader(Node2D pulse) {
        GpuParticles2D? distortionRing = pulse.GetNodeOrNull<GpuParticles2D>("DistortionRing");
        ShaderMaterial? material = distortionRing?.Material as ShaderMaterial;
        ShaderMaterial? uniqueMaterial = material?.Duplicate() as ShaderMaterial;
        if (distortionRing == null || uniqueMaterial == null) return;

        uniqueMaterial.SetShaderParameter("noise_seed", (float)GD.RandRange(0.0, 1000.0));
        uniqueMaterial.SetShaderParameter(
            "distortion_strength",
            (float)GD.RandRange(0.014, 0.021));
        uniqueMaterial.SetShaderParameter(
            "swirl_strength",
            (float)GD.RandRange(-0.012, 0.012));
        uniqueMaterial.SetShaderParameter(
            "chromatic_aberration",
            (float)GD.RandRange(0.0025, 0.0045));
        distortionRing.Material = uniqueMaterial;
    }

    private int GetCalculatedHits() {
        if (card?.DynamicVars["CalculatedHits"] is not CalculatedVar calculatedHits) return 0;
        return (int)calculatedHits.Calculate(null);
    }
}
