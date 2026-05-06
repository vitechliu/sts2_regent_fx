using System.Reflection;
using Godot.Bridge;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes;
using RegentFX.Scripts.Vfx;
using RegentFX.Scripts.Vfx.Cards;
using RegentFX.Scripts.Vfx.Powers;

namespace RegentFX.Scripts;

// 必须要加的属性，用于注册Mod。字符串和初始化函数命名一致。
[ModInitializer("Init")]
public class Entry {
    public const string ModId = "RegentFX";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new(ModId, LogType.Generic);

    /// <summary>
    /// 星星环绕控制器单例（仅本地玩家）
    /// </summary>
    public static StarRingController? StarRingController { get; set; }

    /// <summary>
    /// 星星特效控制器单例（仅本地玩家）
    /// </summary>
    public static StarEffectController? StarEffectController { get; set; }

    // 初始化函数
    public static void Init() {
        var harmony = new Harmony("sts2.vitech.regentFx");
        harmony.PatchAll();
        // 使得tscn可以加载自定义脚本
        ScriptManagerBridge.LookupScriptsInAssembly(typeof(Entry).Assembly);
        LoadScenes();
        Log.Debug("Regent Fx Mod initialized!");
    }


    static void LoadScenes() {
        try {
            var paths = CollectAssetPathsSafely();
            if (paths.Count > 0) {
                var session = PreloadManager.Cache.CreateSession("RegentFX", paths);
                NAssetLoader.Instance.LoadInTheBackground(session);
                Logger.Info($"Queued {paths.Count} assets for preloading");
            }
        }
        catch (Exception ex) {
            Logger.Warn($"Failed to queue RegentFX assets: {ex.Message}");
        }
    }

    private static List<string> CollectAssetPathsSafely() {
        var paths = new HashSet<string>() {
            Blade.Blade1Path,
            Blade.Blade2Path,
            Blackhole.VfxScenePath,
            Star.VfxScenePath,
        };
        var assembly = typeof(Entry).Assembly;

        // 安全收集 CardFX
        foreach (var type in assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(CardFX)) && !t.IsAbstract && !t.ContainsGenericParameters)) {
            try {
                var instance = Activator.CreateInstance(type);
                if (instance is CardFX cardFx) {
                    foreach (var path in cardFx.AssetPaths) {
                        if (!string.IsNullOrEmpty(path)) paths.Add(path);
                    }
                }
            }
            catch (Exception ex) {
                Logger.Debug($"Skip preloading for {type.Name}: {ex.Message}");
            }
        }

        // 安全收集 PowerFX
        foreach (var type in assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(PowerFX)) && !t.IsAbstract && !t.ContainsGenericParameters)) {
            try {
                var instance = Activator.CreateInstance(type);
                if (instance is PowerFX powerFx) {
                    foreach (var path in powerFx.AssetPaths) {
                        if (!string.IsNullOrEmpty(path)) paths.Add(path);
                    }
                }
            }
            catch (Exception ex) {
                Logger.Debug($"Skip preloading for {type.Name}: {ex.Message}");
            }
        }

        return paths.ToList();
    }
}
