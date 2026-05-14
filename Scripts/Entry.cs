using System;
using System.Collections.Generic;
using System.Linq;
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
using RegentFX.ThirdParty.Audio; 

namespace RegentFX.Scripts;

[ModInitializer("Init")]
public class Entry {
    public const string ModId = "RegentFX";
    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new(ModId, LogType.Generic);

    public static StarRingController? StarRingController { get; set; }
    public static StarEffectController? StarEffectController { get; set; }
    
    /// <summary>
    /// Mod 独立的场景缓存，完全还原线程安全的 ConcurrentDictionary，避免被游戏 PreloadManager 的 UnloadAssets 清理
    /// </summary>
    public static readonly System.Collections.Concurrent.ConcurrentDictionary<string, PackedScene> ModSceneCache = new();

    // 初始化函数
    public static void Init() {
        try {
            var harmony = new Harmony("sts2.vitech.regentFx");
            harmony.PatchAll();
            
            ScriptManagerBridge.LookupScriptsInAssembly(typeof(Entry).Assembly);
            
            InitModAudioSystem();
            
            LoadScenes();
            
            CheckAndInitRitsuSoftDependency();

            Logger.Info("Regent Fx Mod initialized successfully!");
        }
        catch (System.Exception ex) {
            Logger.Error($"RegentFX initialized failed! 错误详情: {ex.Message}");
        }
    }
    
    private static void InitModAudioSystem() {
        string resBankPath = "res://mods/RegentFX/banks/RegentFx.bank";
        string resGuidPath = "res://mods/RegentFX/banks/GUIDs.txt";

        try {
            RegentFxGuidMap.LoadFromFile(resGuidPath);

            // 检测当前运行环境是否存在 RitsuLib 程序集
            var ritsuAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "STS2-RitsuLib");
            
            if (ritsuAssembly != null) {
                // 模式一：环境存在 RitsuLib。在 Init 黄金期通过反射无缝挂载到其官方延迟加载队列中
                var regType = ritsuAssembly.GetType("STS2RitsuLib.Audio.FmodStudioDeferredBankRegistration");
                if (regType != null) {
                    var regBankMethod = regType.GetMethod("RegisterBank", new[] { typeof(string) });
                    var regGuidMethod = regType.GetMethod("RegisterStudioGuidMappings", new[] { typeof(string) });
                    
                    regBankMethod?.Invoke(null, new object[] { resBankPath });
                    regGuidMethod?.Invoke(null, new object[] { resGuidPath });
                    
                    Logger.Info("[RegentFX Audio] 启动阶段：已成功无缝对接 RitsuLib 官方音频加载通道。");
                    return; // 成功对接前置，直接返回，不再执行独立加载
                }
            }

            // 模式二：零前置独立运行模式。直接接管底层的全局 FmodServer 单例
            GodotObject fmodServer = Engine.GetSingleton("FmodServer");
            if (fmodServer != null) {
                if (fmodServer.HasMethod("load_bank")) {
                    // 1. 尝试原生的 res:// 虚拟路径加载
                    fmodServer.Call("load_bank", resBankPath, 0);
                    Logger.Info($"[RegentFX Audio] 独立模式：已通过虚拟路径加载音频库: {resBankPath}");

                    // 2. 将其转换为原生 C++ 核心绝对认可的操作系统绝对路径再次注入
                    try {
                        string absoluteBankPath = ProjectSettings.GlobalizePath(resBankPath);
                        fmodServer.Call("load_bank", absoluteBankPath, 0);
                        Logger.Info($"[RegentFX Audio] 独立模式双保险：已通过绝对路径加载音频库: {absoluteBankPath}");
                    }
                    catch {
                        // 忽略转换失败
                    }
                }
            }
            else {
                Logger.Warn("[RegentFX Audio] 找不到全局 FmodServer 单例，独立模式下音效可能无法加载。");
            }
        }
        catch (Exception ex) {
            Logger.Error($"[RegentFX Audio] 初始化音频系统时发生严重异常: {ex.Message}");
        }
    }

    private static void CheckAndInitRitsuSoftDependency() {
        try {
            bool hasRitsuLib = AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "STS2-RitsuLib")
                               || Type.GetType("STS2RitsuLib.RitsuLibFramework, STS2-RitsuLib") != null;

            if (hasRitsuLib) {
                ExecuteRitsuSetup();
                Logger.Info("检测到 RitsuLib 环境，已成功恢复默认配置。");
            }
        }
        catch (Exception ex) {
            Logger.Warn($"初始化 RitsuLib 软依赖配置失败: {ex.Message}");
        }
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static void ExecuteRitsuSetup() {
        RitsuLibModConfig.SetDefaults();
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
        };
        var assembly = typeof(Entry).Assembly;

        foreach (var type in assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(CardFX)) && !t.IsAbstract && !t.ContainsGenericParameters)) {
            try {
                var instance = Activator.CreateInstance(type);
                if (instance is CardFX cardFx) {
                    foreach (var path in cardFx.AssetPaths) if (!string.IsNullOrEmpty(path)) paths.Add(path);
                }
            } catch {}
        }

        foreach (var type in assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(PowerFX)) && !t.IsAbstract && !t.ContainsGenericParameters)) {
            try {
                var instance = Activator.CreateInstance(type);
                if (instance is PowerFX powerFx) {
                    foreach (var path in powerFx.AssetPaths) if (!string.IsNullOrEmpty(path)) paths.Add(path);
                }
            } catch {}
        }

        return paths.ToList();
    }
}