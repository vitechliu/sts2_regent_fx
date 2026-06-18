using System.Reflection;
using Godot;
using Godot.Bridge;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes;
using RegentFX.Scripts.Vfx;
using RegentFX.Scripts.Vfx.Cards;
using RegentFX.Scripts.Vfx.Powers;
using RegentFX.ThirdParty;
using RitsuFmodLite;

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

    /// <summary>
    /// Mod 独立的场景缓存，避免被游戏 PreloadManager 的 UnloadAssets 清理
    /// </summary>
    public static readonly System.Collections.Concurrent.ConcurrentDictionary<string, PackedScene> ModSceneCache = new();

    public const string VERSION = "0.4.0";
    
    // 初始化函数
    public static void Init() {
        try {
            // FMOD 资源注册
            // 注册 Bank 和 GUIDs
            FmodLite.TryLoadBankAndGuidMappings("res://RegentFX/banks/RegentFx.bank",
                "res://RegentFX/banks/GUIDs.txt");
            var harmony = new Harmony("sts2.vitech.regentFx");
            harmony.PatchAll();
            // 使得tscn可以加载自定义脚本
            ScriptManagerBridge.LookupScriptsInAssembly(typeof(Entry).Assembly);
            LoadScenes();
            RitsuLibModConfig.SetDefaults();
            Log.Info($"RegentFX Omnistar {VERSION} Load Complete![万象辉星]加载成功!");
        }
        catch (System.Exception ex) {
            Logger.Error($"RegentFX initialized failed! 错误详情: {ex.Message}");
        }
    }


    static void LoadScenes() {
        try {
            var paths = CollectAssetPathsSafely();
            if (paths.Count > 0) {
                Logger.Info($"Preloading {paths.Count} RegentFX assets synchronously");
                int success = 0, fail = 0;
                foreach (var path in paths) {
                    try {
                        if (ModSceneCache.ContainsKey(path)) continue;
                        var scene = ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.Reuse);
                        if (scene != null) {
                            ModSceneCache[path] = scene;
                            success++;
                        } else {
                            fail++;
                            Logger.Warn($"Failed to preload: {path}");
                        }
                    } catch (Exception ex) {
                        fail++;
                        Logger.Warn($"Error preloading {path}: {ex.Message}");
                    }
                }
                Logger.Info($"Preloading complete: {success} succeeded, {fail} failed");
            }
        }
        catch (Exception ex) {
            Logger.Warn($"Failed to preload RegentFX assets: {ex.Message}");
        }
    }

    private static List<string> CollectAssetPathsSafely() {
        var paths = new HashSet<string>() {
            FX.DISTORTION,
            Blade.Blade1Path,
            Blade.Blade2Path,
            Blackhole.VfxScenePath,
            Pillar.VfxScenePath,
            Pillar.BurstPath,
            Star.VfxScenePath,
            StardustVfx.ScenePath,
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
