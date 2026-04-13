using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Saves;
using RegentFX.Scripts;

namespace RegentFx.Core.Audio;

/// <summary>
/// 简易音效播放工具类 - 使用 Godot 原生 AudioStreamPlayer 播放音频文件
/// 自动跟随游戏 SFX 音量设置
/// </summary>
public static class SimpleSfxUtil
{
    private static readonly StringName SfxBusName = new("SFX");

    /// <summary>
    /// 播放音频文件，自动应用游戏 SFX 音量设置
    /// </summary>
    /// <param name="path">音频文件路径，如 "res://sounds/my_sound.wav"</param>
    /// <param name="volume">额外音量倍数 (0-1)，会与游戏 SFX 音量相乘</param>
    /// <returns>AudioStreamPlayer 实例，可用于后续控制</returns>
    public static AudioStreamPlayer? Play(string path, float volume = 1f)
    {
        Entry.Logger.Info($"[SimpleSfxUtil] Attempting to play: {path}");

        if (NGame.Instance == null)
        {
            Entry.Logger.Error("[SimpleSfxUtil] NGame.Instance is null!");
            return null;
        }

        if (!ResourceLoader.Exists(path))
        {
            Entry.Logger.Error($"[SimpleSfxUtil] Audio file not found: {path}");
            return null;
        }

        var stream = ResourceLoader.Load<AudioStream>(path);
        if (stream == null)
        {
            Entry.Logger.Error($"[SimpleSfxUtil] Failed to load audio stream: {path}");
            return null;
        }

        Entry.Logger.Info($"[SimpleSfxUtil] Audio loaded successfully: {path}");
        return PlayStream(stream, volume);
    }

    /// <summary>
    /// 播放音频流，自动应用游戏 SFX 音量设置
    /// </summary>
    /// <param name="stream">音频流</param>
    /// <param name="volume">额外音量倍数 (0-1)，会与游戏 SFX 音量相乘</param>
    /// <returns>AudioStreamPlayer 实例，可用于后续控制</returns>
    public static AudioStreamPlayer? PlayStream(AudioStream stream, float volume = 1f)
    {
        if (NGame.Instance == null)
        {
            GD.PrintErr("[SimpleSfxUtil] NGame.Instance is null, cannot create player!");
            return null;
        }

        var player = new AudioStreamPlayer();
        NGame.Instance.AddChildSafely(player);

        player.Stream = stream;
        player.Bus = SfxBusName;

        var finalVolume = CalculateVolume(volume);
        player.VolumeLinear = finalVolume;

        GD.Print($"[SimpleSfxUtil] Playing with volume: {finalVolume} (user: {volume}, gameSfx: {GetGameSfxVolume()})");

        // 播放完成后自动释放
        player.Finished += () =>
        {
            player.QueueFree();
        };

        player.Play();
        GD.Print($"[SimpleSfxUtil] Player started playing");
        return player;
    }

    /// <summary>
    /// 播放音频并随机音高变化
    /// </summary>
    /// <param name="path">音频文件路径</param>
    /// <param name="volume">音量倍数</param>
    /// <param name="pitchVariance">音高变化幅度</param>
    /// <returns>AudioStreamPlayer 实例</returns>
    public static AudioStreamPlayer? PlayWithPitch(string path, float volume = 1f, float pitchVariance = 0.05f)
    {
        var player = Play(path, volume);
        if (player != null && pitchVariance > 0)
        {
            var random = new System.Random();
            var pitch = 1f + (float)(random.NextDouble() * 2 - 1) * pitchVariance;
            player.PitchScale = Mathf.Max(0.1f, pitch);
        }
        return player;
    }

    /// <summary>
    /// 获取当前游戏 SFX 音量设置 (0-1)
    /// </summary>
    public static float GetGameSfxVolume()
    {
        return SaveManager.Instance?.SettingsSave?.VolumeSfx ?? 0.5f;
    }

    /// <summary>
    /// 计算最终音量 - 将用户传入的音量与游戏 SFX 音量相乘
    /// </summary>
    private static float CalculateVolume(float userVolume)
    {
        var gameSfxVolume = GetGameSfxVolume();
        return Mathf.Clamp(userVolume * gameSfxVolume, 0f, 1f);
    }
}
