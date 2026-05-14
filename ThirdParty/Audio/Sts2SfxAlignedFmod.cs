using System;
using System.Collections.Generic;
using RegentFX.Scripts;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Combat;

namespace RegentFX.ThirdParty.Audio;

public static class Sts2SfxAlignedFmod
{
    public static void PlayOneShot(string eventPath, float volume = 1f)
    {
        PlayOneShot(eventPath, null, volume);
    }

    public static void PlayOneShot(string eventPath, IReadOnlyDictionary<string, float>? parameters, float volume = 1f)
    {
        if (string.IsNullOrEmpty(eventPath)) return;

        if (NonInteractiveMode.IsActive || CombatManager.Instance.IsEnding)
            return;

        try
        {
            string exactRealPath = RegentFxGuidMap.Resolve(eventPath);

            float finalVolume = volume * RegentFxConfig.SfxVolume;

            if (parameters != null && parameters.Count > 0)
            {
                var passParams = new Dictionary<string, float>(parameters);
                MegaCrit.Sts2.Core.Nodes.Audio.NAudioManager.Instance.PlayOneShot(exactRealPath, passParams, finalVolume);
            }
            else
            {
                MegaCrit.Sts2.Core.Nodes.Audio.NAudioManager.Instance.PlayOneShot(exactRealPath, finalVolume);
            }
        }
        catch (Exception ex)
        {
            Entry.Logger.Warn($"[RegentFX] FMOD 播放失败: {eventPath}。原因: {ex.Message}");
        }
    }
}