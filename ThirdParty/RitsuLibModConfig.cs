using MegaCrit.Sts2.Core.Models;
using RegentFX.Scripts;
using RegentFX.Scripts.Vfx.Cards;
using RegentFX.Scripts.Vfx.Powers;

namespace RegentFX.ThirdParty;

using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;

/// <summary>由 Ritsu 经 AssemblyMetadata: RitsuLib.ModSettingsInterop.ProviderType 发现；全部为 static，且无 STS2RitsuLib 引用。</summary>
public static class RitsuLibModConfig {
    // —— 与 mod_manifest 及游戏内 id 一致（与 BaseGameData / 持久化中使用的 mod id 相同字符串）
    private const string ModId = Entry.ModId;

    // 仅为示例路径；真模组请与你在 ModConfig / 自带存档里使用的一致
    // Windows 常见: %LocalAppData%\SlayTheSpire2\... 或 游戏 mod_data 目录
    private static string DataDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SlayTheSpire2", ModId);

    private static string SchemaPath => Path.Combine(DataDir, "ritsu_interop_schema.json");
    private static string StatePath => Path.Combine(DataDir, "ritsu_interop_state.json");

    // —— 多线程下避免读写交错（Ritsu 可能在不同线程回调）
    private static readonly object FileLock = new();
    private static readonly ConcurrentDictionary<string, JsonNode?> Hot = new(StringComparer.Ordinal);

    /// <summary>Ritsu 调用：返回“schema”。返回文件路径时，Ritsu 会读文件内容解析 JSON。</summary>
    public static object CreateRitsuLibSettingsSchema() {
        Directory.CreateDirectory(DataDir);
        File.WriteAllText(SchemaPath, BuildDefaultSchemaJson());
        // if (!File.Exists(SchemaPath)) {
        //     // 首次写默认 schema 文件，便于你手工编辑或版本控制
        //     SetDefaults();
        // }
        // 也支持直接 return BuildDefaultSchemaJson() 或 Dictionary，无需文件
        return SchemaPath;
    }

    private static readonly Dictionary<string, object> Defaults = new(){
        ["ExposureThreshold"] = 1,
        ["DevTestStartMode"] = false,
        ["PreloadEffects"] = true,
        ["DisableModSounds"] = false,
    };

    public static void SetDefaults() {
        CardFX.EnsureRegistry();
        var cardFxTypes = CardFX.Registry.Values;
        foreach (var VARIABLE in cardFxTypes) {
            Defaults["card_" + VARIABLE.Name] = true;
        }
        PowerFX.EnsureRegistry();
        var powerFxTypes = PowerFX.Registry.Values;
        foreach (var VARIABLE in powerFxTypes) {
            Defaults["power_" + VARIABLE.Name] = true;
        }
    }

    private static string BuildDefaultSchemaJson() {
        // 与 RuntimeInteropMirrorSource 解析器字段名一致；modId 必填
        var Schema = new RitsuLibModConfigEntity {
            modDisplayName = SimpleLocUtil.Simple("万象辉星", "RegentFX")
        };

        var MainPage = new RLMCPage();
        MainPage.pageId = "main";
        MainPage.title = SimpleLocUtil.Simple("主要设置", "Main");
        MainPage.description = SimpleLocUtil.Simple("主要设置", "Main");
        MainPage.sortOrder = 1;

        var MainSection = new RLMCSection();
        MainSection.id = "core";
        MainSection.title = SimpleLocUtil.Simple("基础", "Basics");

        var ExposureEntry = new SliderEntry();
        ExposureEntry.id = "master_vol";
        ExposureEntry.key = "ExposureThreshold";
        ExposureEntry.label = SimpleLocUtil.Simple("曝光光效强度", "Light Exposure");
        ExposureEntry.description = SimpleLocUtil.Simple("设置为0将关闭光效", "Set 0 to disable exposure");
        ExposureEntry.min = 0;
        ExposureEntry.max = 2;
        ExposureEntry.step = 0.05;
        MainSection.entries.Add(ExposureEntry);
        
        var PreloadEntry = new ToggleEntry();
        PreloadEntry.id = "PreloadEffects";
        PreloadEntry.key = PreloadEntry.id;
        PreloadEntry.description = SimpleLocUtil.Simple("需重启游戏生效，能解决第一次打出特效卡顿问题，但是内存占用会提升。(Mac系统如果卡顿建议关闭此选项)", "Require game restart. Can resolve the lag issue when playing effects for the first time, but will increase memory usage");
        PreloadEntry.label = SimpleLocUtil.Simple("特效预加载", "Preload Cache");
        
        MainSection.entries.Add(PreloadEntry);
        
        var SoundEntry = new ToggleEntry();
        SoundEntry.id = "DisableModSounds";
        SoundEntry.key = PreloadEntry.id;
        SoundEntry.description = SimpleLocUtil.Simple("禁用后会恢复至原版游戏默认攻击音效", "Fallback to original attack sound effects.");
        SoundEntry.label = SimpleLocUtil.Simple("禁用mod音效", "Disable Mod Sounds");
        
        MainSection.entries.Add(SoundEntry);
        
        var CardSection = new RLMCSection();
        CardSection.id = "cards";
        CardSection.title = SimpleLocUtil.Simple("卡牌", "Cards");
        var PowerSection = new RLMCSection();
        PowerSection.id = "powers";
        PowerSection.title = SimpleLocUtil.Simple("能力", "Powers");
        
        
        CardFX.EnsureRegistry();
        foreach (Type cardModelType in CardFX.Registry.Keys) {
            Type cardFxType = CardFX.Registry[cardModelType];
            var CardEntry = new ToggleEntry();
            CardModel card = null!;
            try {
                card = ModelDb.Get(cardModelType) as CardModel;
                
            }
            catch (Exception) {
                Entry.Logger.Warn("[RitsuConfig] Cannot find cardModel: " + cardModelType.Name);
                continue;
            }
            CardEntry.id = CardFX.GetToggleKey(cardFxType);
            CardEntry.key = CardEntry.id;
            CardEntry.label = card.TitleLocString.GetFormattedText();
            CardEntry.description = null;
            CardSection.entries.Add(CardEntry);
        }
        
        PowerFX.EnsureRegistry();
        foreach (Type powerModelType in PowerFX.Registry.Keys) {
            Type powerFxType = PowerFX.Registry[powerModelType];
            var PowerEntry = new ToggleEntry();
            PowerModel power = null!;
            try {
                power = ModelDb.Get(powerModelType) as PowerModel;
            }
            catch (Exception) {
                Entry.Logger.Warn("[RitsuConfig] Cannot find powerModel: " + powerModelType.Name);
                continue;
            }
            PowerEntry.id = PowerFX.GetToggleKey(powerFxType);
            PowerEntry.key = PowerEntry.id;
            PowerEntry.label = power.Title.GetFormattedText();
            PowerEntry.description = null;
            PowerSection.entries.Add(PowerEntry);
        }
        
        MainPage.sections.Add(MainSection);
        MainPage.sections.Add(CardSection);
        MainPage.sections.Add(PowerSection);
        
        Schema.pages.Add(MainPage);
        
        var DebugPage = new RLMCPage();
        DebugPage.pageId = "debug";
        DebugPage.title = SimpleLocUtil.Simple("调试设置", "Debug Settings");
        DebugPage.description = SimpleLocUtil.Simple("测试Mod使用，会影响游戏性，请勿修改！", "Only for debugging. Do not change!");
        DebugPage.sortOrder = 3;
        var DebugSection = new RLMCSection();
        DebugSection.id = "debug_core";
        DebugSection.title = SimpleLocUtil.Simple("基础", "Basics");

        var DebugModeEntry = new ToggleEntry();
        DebugModeEntry.id = "DevTestStartMode";
        DebugModeEntry.key = DebugModeEntry.id;
        DebugModeEntry.label = SimpleLocUtil.Simple("测试模式", "Test Mode");
        
        DebugSection.entries.Add(DebugModeEntry);
        DebugPage.sections.Add(DebugSection);
        
        Schema.pages.Add(DebugPage);
        
        
        var res = JsonSerializer.Serialize(Schema);
        // Entry.Logger.Info("[RitsuConfigExport] " + res);
        return res;
    }

    public static void SetRitsuLibSettingValue(string key, object? value) => SetCore(key, value);

    public static object? GetRitsuLibSettingValue(string key) => GetCore(key);

    public static void SaveRitsuLibSettings() {
        lock (FileLock) {
            var path = StatePath;
            var obj = new JsonObject();
            foreach (var kv in Hot)
                obj[kv.Key] = kv.Value is null ? null : JsonNode.Parse(kv.Value.ToJsonString());
            File.WriteAllText(path, obj.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    // 可选强类型，解析器会优先用它们（见 RuntimeInteropMirrorSource.BuildAccessor）
    public static bool GetRitsuLibSettingBool(string key) => CoerceBool(GetCore(key));
    public static void SetRitsuLibSettingBool(string key, bool value) => SetCore(key, value);

    public static double GetRitsuLibSettingDouble(string key) => CoerceDouble(GetCore(key));
    public static void SetRitsuLibSettingDouble(string key, double value) => SetCore(key, value);

    public static int GetRitsuLibSettingInt(string key) => CoerceInt(GetCore(key));
    public static void SetRitsuLibSettingInt(string key, int value) => SetCore(key, value);

    public static string? GetRitsuLibSettingString(string key) => GetCore(key)?.ToString();

    public static void SetRitsuLibSettingString(string key, string value) => SetCore(key, value);

    public static void InvokeRitsuLibSettingAction(string key) {
        if (key == "reset_all") {
            Hot.Clear();
            try {
                if (File.Exists(StatePath)) File.Delete(StatePath);
            }
            catch {
                // 忽略
            }
        }
    }

    // —— 内部
    private static void SetCore(string key, object? value) {
        LoadIfNeeded();
        Hot[key] = JsonSerializer.SerializeToNode(value);
        // Entry.Logger.Info("SetCore:" + key + "value:" + value);
    }

    private static object? GetCore(string key) {
        LoadIfNeeded();
        if (!Hot.TryGetValue(key, out var n) || n is null) {
            return Defaults.GetValueOrDefault(key);
        }
        if (n is JsonValue jv) return jv.GetValue<object>();
        return n;
    }

    private static void LoadIfNeeded() {
        if (Hot.Count > 0) return;
        lock (FileLock) {
            if (Hot.Count > 0) return;
            if (!File.Exists(StatePath)) return;
            var root = JsonNode.Parse(File.ReadAllText(StatePath))?.AsObject();
            if (root is null) return;
            foreach (var p in root) Hot[p.Key] = p.Value;
        }
    }

    private static bool CoerceBool(object? o) => o switch {
        true => true,
        false => false,
        null => false,
        JsonValue jv => jv.TryGetValue<bool>(out var b) && b,
        _ => bool.TryParse(o.ToString(), out var x) && x
    };

    private static double CoerceDouble(object? o) => o switch {
        null => 0d,
        JsonValue jv => jv.GetValue<double>(),
        IConvertible c => Convert.ToDouble(c),
        _ => double.TryParse(o.ToString(), out var x) ? x : 0d
    };

    private static int CoerceInt(object? o) => o switch {
        null => 0,
        JsonValue jv => jv.GetValue<int>(),
        IConvertible c => Convert.ToInt32(c),
        _ => int.TryParse(o.ToString(), out var x) ? x : 0
    };
}