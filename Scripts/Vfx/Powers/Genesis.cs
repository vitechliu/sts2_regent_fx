using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using STS2RitsuLib.Audio;

#pragma warning disable CS4014

namespace RegentFX.Scripts.Vfx.Powers;

[PowerFx(typeof(GenesisPower))]
[HarmonyPatch]
public class Genesis : PowerFX {
    public override string? VfxScenePath => "res://RegentFX/scenes/vfx/genesis.tscn";
    
    public override void BeforeBeforeApplied(Creature target, Decimal amount) {
        NCreature n = NCombatRoom.Instance?.GetCreatureNode(target);
        if (n != null) {
            TaskHelper.RunSafely(PlayAnim(n.VfxSpawnPosition));
        }
    }

    async Task PlayAnim(Vector2 pos) {
        VFXUtil.PlaySimple(VfxScenePath,pos, 4f);
        Sts2SfxAlignedFmod.PlayOneShot("event:/RegentFx/sfx/genesis_1");
        await Cmd.Wait(0.5f);
        WorldEnvironmentUtil.TweenExposure(2.5f, 0.25f);
        await Cmd.Wait(0.25f);
        Sts2SfxAlignedFmod.PlayOneShot("event:/RegentFx/sfx/genesis_2");
        Node2D ntest = VFXUtil.PlaySimple(DISTORTION, pos);
        WorldEnvironmentUtil.TweenExposure(1f, 0.3f);
    }
}
