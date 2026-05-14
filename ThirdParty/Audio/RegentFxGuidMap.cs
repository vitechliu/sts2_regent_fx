using System;
using System.Collections.Generic;
using Godot;

namespace RegentFX.ThirdParty.Audio;

public static class RegentFxGuidMap
{
    // Key: 不区分大小写的路径, Value: GUIDs.txt 中录入的【精确大小写真实路径】
    private static readonly Dictionary<string, string> CaseCorrectedPaths = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 在游戏启动时，把 GUIDs.txt 里的标准路径存入纠错字典
    /// </summary>
    public static void LoadFromFile(string resPath)
    {
        try
        {
            if (!Godot.FileAccess.FileExists(resPath))
            {
                GD.PrintErr($"[RegentFX Audio] 找不到映射文件: {resPath}");
                return;
            }

            using var file = Godot.FileAccess.Open(resPath, Godot.FileAccess.ModeFlags.Read);
            if (file == null) return;

            CaseCorrectedPaths.Clear();
            int count = 0;

            while (!file.EofReached())
            {
                string line = file.GetLine().Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;

                string[] parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2) continue;

                string? officialPath = null;
                foreach (var part in parts)
                {
                    if (part.StartsWith("event:/", StringComparison.OrdinalIgnoreCase) || 
                        part.StartsWith("snapshot:/", StringComparison.OrdinalIgnoreCase))
                    {
                        officialPath = part;
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(officialPath))
                {
                    CaseCorrectedPaths[officialPath] = officialPath;
                    count++;
                }
            }

            GD.Print($"[RegentFX Audio] 成功载入 {count} 个标准音频事件的大小写纠错映射！");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[RegentFX Audio] 解析 GUIDs.txt 路径发生异常: {ex.Message}");
        }
    }
    
    public static string Resolve(string eventPath)
    {
        if (string.IsNullOrEmpty(eventPath)) return eventPath;
        
        if (CaseCorrectedPaths.TryGetValue(eventPath, out var exactPath))
        {
            return exactPath;
        }

        return eventPath; // 
    }
}