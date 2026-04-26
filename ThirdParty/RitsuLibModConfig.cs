using RegentFX.Scripts;

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
        Entry.Logger.Info("RitsuDir:" + DataDir);
        if (!File.Exists(SchemaPath)) {
            // 首次写默认 schema 文件，便于你手工编辑或版本控制
            File.WriteAllText(SchemaPath, BuildDefaultSchemaJson());
            SetRitsuLibSettingDouble("ExposureThreshold", 1);
        }

        // 也支持直接 return BuildDefaultSchemaJson() 或 Dictionary，无需文件
        return SchemaPath;
    }

    private static string BuildDefaultSchemaJson() {
        // 与 RuntimeInteropMirrorSource 解析器字段名一致；modId 必填
        return """
               {
                 "modId": "RegentFX",
                 "modDisplayName": "RegentFX 万象辉星",
                 "modSidebarOrder": 50,
                 "pages": [
                   {
                     "pageId": "main",
                     "title": "主设置",
                     "description": "RegentFX 主设置",
                     "sortOrder": 1000,
                     "sections": [
                       {
                         "id": "core",
                         "title": "特效",
                         "entries": [
                           {
                             "id": "master_vol",
                             "type": "slider",
                             "key": "ExposureThreshold",
                             "label": "光效强度 Light Exposure Setting",
                             "description": "设置为0将关闭光效",
                             "min": 0,
                             "max": 2,
                             "step": 0.05,
                             "scope": "global"
                           }
                         ]
                       }
                     ]
                   }
                 ]
               }
               """;
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
        Entry.Logger.Info("SetCore:" + key + "value:" + value);
    }

    private static object? GetCore(string key) {
        LoadIfNeeded();
        if (!Hot.TryGetValue(key, out var n) || n is null) return null;
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