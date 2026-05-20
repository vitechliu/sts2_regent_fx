using System;
using System.Collections.Generic;
using Godot;
using GdFileAccess = Godot.FileAccess;

namespace RitsuFmodLite;

/// <summary>
/// Minimal standalone FMOD Studio toolchain extracted from RitsuLib.
/// It uses only FmodServer: bank loading, GUIDs.txt path mapping, one-shots, loops, and live parameters.
/// </summary>
public static class FmodLite
{
    private static readonly object Gate = new();
    private static readonly StringName FmodServerName = new("FmodServer");

    private static readonly StringName LoadBankMethod = new("load_bank");
    private static readonly StringName UnloadBankMethod = new("unload_bank");
    private static readonly StringName WaitForAllLoadsMethod = new("wait_for_all_loads");
    private static readonly StringName BanksStillLoadingMethod = new("banks_still_loading");
    private static readonly StringName CheckEventPathMethod = new("check_event_path");
    private static readonly StringName CheckEventGuidMethod = new("check_event_guid");
    private static readonly StringName CreateEventInstanceMethod = new("create_event_instance");
    private static readonly StringName CreateEventInstanceWithGuidMethod = new("create_event_instance_with_guid");

    private static readonly StringName StartMethod = new("start");
    private static readonly StringName StopMethod = new("stop");
    private static readonly StringName ReleaseMethod = new("release");
    private static readonly StringName SetVolumeMethod = new("set_volume");
    private static readonly StringName SetPitchMethod = new("set_pitch");
    private static readonly StringName SetPausedMethod = new("set_paused");
    private static readonly StringName SetParameterByNameMethod = new("set_parameter_by_name");
    private static readonly StringName LoopParameterName = new("loop");

    private static readonly StringName[] GuidMappingInjectCandidates =
    [
        new("register_guid_path_mappings_from_file"),
        new("inject_guid_mappings_from_file"),
        new("register_strings_from_guid_file"),
        new("load_guid_mapping_file"),
    ];

    private static readonly Dictionary<string, GodotObject> LoadedBankPins = new(StringComparer.Ordinal);
    private static Dictionary<string, string> EventPathToGuid = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, List<LoopSlot>> LoopQueues = new(StringComparer.Ordinal);

    public static int EventMappingCount
    {
        get
        {
            lock (Gate)
            {
                return EventPathToGuid.Count;
            }
        }
    }

    /// <summary>
    /// Loads a Studio bank and pins the returned FmodBank object so the addon does not unload it immediately.
    /// </summary>
    public static bool TryLoadBank(string resourcePath, int loadBankMode = 0)
    {
        if (string.IsNullOrWhiteSpace(resourcePath) || !GdFileAccess.FileExists(resourcePath))
            return false;

        if (!TryCallServer(out var result, LoadBankMethod, resourcePath, loadBankMode))
            return false;

        if (result.VariantType == Variant.Type.Bool)
            return result.AsBool();
        if (result.VariantType == Variant.Type.Nil)
            return false;

        var bank = result.AsGodotObject();
        if (bank is null || !GodotObject.IsInstanceValid(bank))
            return false;

        lock (Gate)
        {
            LoadedBankPins[resourcePath] = bank;
        }

        return true;
    }

    public static bool TryUnloadBank(string resourcePath)
    {
        lock (Gate)
        {
            LoadedBankPins.Remove(resourcePath);
        }

        return TryCallServer(UnloadBankMethod, resourcePath);
    }

    public static void TryWaitForAllLoads()
    {
        TryCallServer(WaitForAllLoadsMethod);
    }

    public static bool? TryBanksStillLoading()
    {
        return TryCallServer(out var result, BanksStillLoadingMethod) ? result.AsBool() : null;
    }

    /// <summary>
    /// Loads an FMOD Studio GUIDs.txt-style file. Only event:/ mappings are needed for playback fallback.
    /// Lines look like: {00000000-0000-0000-0000-000000000000} event:/path/name
    /// </summary>
    public static bool TryLoadGuidMappings(string resourcePath)
    {
        if (!TryParseGuidMappingsFromFile(resourcePath))
            return false;

        return TryCallNativeGuidInject(resourcePath) || EventMappingCount > 0;
    }

    /// <summary>
    /// Convenience setup for custom banks exported without a strings.bank path table.
    /// </summary>
    public static bool TryLoadBankAndGuidMappings(string bankResourcePath, string guidMapResourcePath,
        int loadBankMode = 0, bool waitForLoads = true)
    {
        var bankOk = TryLoadBank(bankResourcePath, loadBankMode);
        if (waitForLoads)
            TryWaitForAllLoads();

        var mapOk = TryLoadGuidMappings(guidMapResourcePath);
        return bankOk && mapOk;
    }

    public static bool IsMappedPath(string eventPath)
    {
        return TryGetMappedGuid(eventPath, out _);
    }

    public static bool? TryCheckEventPath(string eventPath)
    {
        if (TryGetMappedGuid(eventPath, out _))
            return true;

        return TryCheckEventPathByServerOnly(eventPath);
    }

    public static bool? TryCheckEventGuid(string eventGuid)
    {
        return TryNormalizeGuidForAddon(eventGuid, out var normalized) &&
               TryCallServer(out var result, CheckEventGuidMethod, normalized)
            ? result.AsBool()
            : null;
    }

    public static void RegisterGuidMapping(string eventPath, string eventGuid)
    {
        if (string.IsNullOrWhiteSpace(eventPath) || !TryNormalizeGuidForAddon(eventGuid, out var normalized))
            return;

        lock (Gate)
        {
            EventPathToGuid[eventPath] = normalized;
        }
    }

    public static void ClearGuidMappings()
    {
        lock (Gate)
        {
            EventPathToGuid = new Dictionary<string, string>(StringComparer.Ordinal);
        }
    }

    /// <summary>
    /// Fire-and-forget Studio event. Path-only events and GUID-mapped events go through the same instance path.
    /// </summary>
    public static bool Play(string eventPath, float volume = 1f)
    {
        return Play(eventPath, EmptyParameters, volume);
    }

    public static bool Play(string eventPath, string parameterName, float parameterValue, float volume = 1f)
    {
        return Play(eventPath, new Dictionary<string, float> { [parameterName] = parameterValue }, volume);
    }

    public static bool Play(string eventPath, IReadOnlyDictionary<string, float> parameters, float volume = 1f)
    {
        var instance = TryCreateRaw(eventPath);
        if (instance is null)
            return false;

        try
        {
            if (volume != 1f)
                instance.Call(SetVolumeMethod, volume);
            foreach (var kv in parameters)
                instance.Call(SetParameterByNameMethod, kv.Key, kv.Value);

            instance.Call(StartMethod);
            instance.Call(ReleaseMethod);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool PlayByGuid(string eventGuid, float volume = 1f)
    {
        return PlayByGuid(eventGuid, EmptyParameters, volume);
    }

    public static bool PlayByGuid(string eventGuid, IReadOnlyDictionary<string, float> parameters, float volume = 1f)
    {
        var instance = TryCreateRawFromGuid(eventGuid);
        if (instance is null)
            return false;

        try
        {
            if (volume != 1f)
                instance.Call(SetVolumeMethod, volume);
            foreach (var kv in parameters)
                instance.Call(SetParameterByNameMethod, kv.Key, kv.Value);

            instance.Call(StartMethod);
            instance.Call(ReleaseMethod);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Starts a managed loop. StopLoop uses vanilla convention: if usesLoopParam is true, set parameter "loop" to 1.
    /// </summary>
    public static bool PlayLoop(string eventPath, bool usesLoopParam = true,
        IReadOnlyDictionary<string, float>? parameters = null, float volume = 1f)
    {
        return PlayLoop(eventPath, out _, usesLoopParam, parameters, volume);
    }

    /// <summary>
    /// Starts a managed loop and returns the live event object for direct parameter control.
    /// StopLoop/StopAllLoops still own queue cleanup and release.
    /// </summary>
    public static bool PlayLoop(string eventPath, out FmodEventHandle? eventObject, bool usesLoopParam = true,
        IReadOnlyDictionary<string, float>? parameters = null, float volume = 1f)
    {
        var instance = TryCreateRaw(eventPath);
        eventObject = null;
        if (instance is null)
            return false;

        var handle = new FmodEventHandle(instance);
        try
        {
            if (volume != 1f)
                handle.SetVolume(volume);
            if (parameters is not null)
                foreach (var kv in parameters)
                    handle.SetParameter(kv.Key, kv.Value);

            if (!handle.Start())
            {
                handle.Release();
                return false;
            }
        }
        catch
        {
            handle.Release();
            return false;
        }

        lock (Gate)
        {
            if (!LoopQueues.TryGetValue(eventPath, out var list))
            {
                list = [];
                LoopQueues[eventPath] = list;
            }

            list.Add(new LoopSlot(handle, usesLoopParam));
        }

        eventObject = handle;
        return true;
    }

    public static bool StopLoop(string eventPath)
    {
        LoopSlot slot;
        lock (Gate)
        {
            if (!LoopQueues.TryGetValue(eventPath, out var list) || list.Count == 0)
                return false;

            slot = list[0];
            list.RemoveAt(0);
            if (list.Count == 0)
                LoopQueues.Remove(eventPath);
        }

        return StopLoopSlot(slot);
    }

    public static void StopAllLoops()
    {
        List<LoopSlot> slots = [];
        lock (Gate)
        {
            foreach (var list in LoopQueues.Values)
                slots.AddRange(list);
            LoopQueues.Clear();
        }

        foreach (var slot in slots)
            StopLoopSlot(slot);
    }

    public static bool SetParam(string eventPath, string parameterName, float value)
    {
        lock (Gate)
        {
            if (!LoopQueues.TryGetValue(eventPath, out var list) || list.Count == 0)
                return false;

            try
            {
                return list[0].Event.SetParameter(parameterName, value);
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Creates a controllable event instance. Caller owns Stop/Release/Dispose.
    /// </summary>
    public static FmodEventHandle? CreateEvent(string eventPath, bool start = true,
        IReadOnlyDictionary<string, float>? parameters = null, float volume = 1f)
    {
        return TryCreateEvent(eventPath, out var eventObject, start, parameters, volume) ? eventObject : null;
    }

    public static bool TryCreateEvent(string eventPath, out FmodEventHandle? eventObject, bool start = true,
        IReadOnlyDictionary<string, float>? parameters = null, float volume = 1f)
    {
        var instance = TryCreateRaw(eventPath);
        eventObject = null;
        if (instance is null)
            return false;

        var handle = new FmodEventHandle(instance);
        if (volume != 1f)
            handle.SetVolume(volume);
        if (parameters is not null)
            foreach (var kv in parameters)
                handle.SetParameter(kv.Key, kv.Value);
        if (start && !handle.Start())
        {
            handle.Release();
            return false;
        }

        eventObject = handle;
        return true;
    }

    public static FmodEventHandle? CreateEventByGuid(string eventGuid, bool start = true,
        IReadOnlyDictionary<string, float>? parameters = null, float volume = 1f)
    {
        return TryCreateEventByGuid(eventGuid, out var eventObject, start, parameters, volume) ? eventObject : null;
    }

    public static bool TryCreateEventByGuid(string eventGuid, out FmodEventHandle? eventObject, bool start = true,
        IReadOnlyDictionary<string, float>? parameters = null, float volume = 1f)
    {
        var instance = TryCreateRawFromGuid(eventGuid);
        eventObject = null;
        if (instance is null)
            return false;

        var handle = new FmodEventHandle(instance);
        if (volume != 1f)
            handle.SetVolume(volume);
        if (parameters is not null)
            foreach (var kv in parameters)
                handle.SetParameter(kv.Key, kv.Value);
        if (start && !handle.Start())
        {
            handle.Release();
            return false;
        }

        eventObject = handle;
        return true;
    }

    private static IReadOnlyDictionary<string, float> EmptyParameters { get; } =
        new Dictionary<string, float>(0);

    private static GodotObject? TryCreateRaw(string eventPath)
    {
        if (string.IsNullOrWhiteSpace(eventPath))
            return null;

        if (!TryGetMappedGuid(eventPath, out var mappedGuid))
            return TryCreateRawByPathOnly(eventPath);

        var byGuid = TryCreateRawFromGuid(mappedGuid);
        if (byGuid is not null)
            return byGuid;

        return TryCheckEventPathByServerOnly(eventPath) == true ? TryCreateRawByPathOnly(eventPath) : null;
    }

    private static GodotObject? TryCreateRawByPathOnly(string eventPath)
    {
        return TryCallServer(out var result, CreateEventInstanceMethod, eventPath) ? result.AsGodotObject() : null;
    }

    private static GodotObject? TryCreateRawFromGuid(string eventGuid)
    {
        if (!TryNormalizeGuidForAddon(eventGuid, out var normalized))
            return null;

        return TryCallServer(out var result, CreateEventInstanceWithGuidMethod, normalized)
            ? result.AsGodotObject()
            : null;
    }

    private static bool StopLoopSlot(LoopSlot slot)
    {
        try
        {
            if (slot.UsesLoopParam)
                slot.Event.RawInstance.Call(SetParameterByNameMethod, LoopParameterName, 1f);
            else
                slot.Event.Stop(allowFadeOut: false);

            slot.Event.Release();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryParseGuidMappingsFromFile(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath) || !GdFileAccess.FileExists(resourcePath))
            return false;

        using var file = GdFileAccess.Open(resourcePath, GdFileAccess.ModeFlags.Read);
        if (file is null)
            return false;

        ParseGuidMappings(file.GetAsText());
        return true;
    }

    private static void ParseGuidMappings(string text)
    {
        var lines = text.Replace("\r\n", "\n").Split('\n');
        Dictionary<string, string> next;
        lock (Gate)
        {
            next = new Dictionary<string, string>(EventPathToGuid, StringComparer.Ordinal);
        }

        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.Length == 0 || line[0] == '#')
                continue;

            var close = line.IndexOf('}', StringComparison.Ordinal);
            if (close <= 1 || line[0] != '{')
                continue;

            var guidFragment = line.Substring(1, close - 1).Trim();
            if (!Guid.TryParse(guidFragment, out var guid))
                continue;

            var path = close + 1 < line.Length ? line[(close + 1)..].TrimStart() : string.Empty;
            if (!path.StartsWith("event:", StringComparison.Ordinal))
                continue;

            next[path] = guid.ToString("B");
        }

        lock (Gate)
        {
            EventPathToGuid = next;
        }
    }

    private static bool TryCallNativeGuidInject(string resourcePath)
    {
        var server = TryGetServer();
        if (server is null)
            return false;

        foreach (var method in GuidMappingInjectCandidates)
        {
            if (!server.HasMethod(method))
                continue;

            try
            {
                var result = server.Call(method, resourcePath);
                if (result.VariantType == Variant.Type.Bool && !result.AsBool())
                    continue;

                return true;
            }
            catch
            {
                // Keep the managed fallback mappings even if native injection is absent or rejects the file.
            }
        }

        return false;
    }

    private static bool TryGetMappedGuid(string eventPath, out string guid)
    {
        guid = string.Empty;
        lock (Gate)
        {
            return EventPathToGuid.TryGetValue(eventPath, out guid!) && !string.IsNullOrEmpty(guid);
        }
    }

    private static bool? TryCheckEventPathByServerOnly(string eventPath)
    {
        return TryCallServer(out var result, CheckEventPathMethod, eventPath) ? result.AsBool() : null;
    }

    private static bool TryNormalizeGuidForAddon(string raw, out string bracedLowercase)
    {
        bracedLowercase = string.Empty;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var trimmed = raw.Trim();
        if (trimmed.Length >= 2 && trimmed[0] == '{' && trimmed[^1] == '}')
            trimmed = trimmed[1..^1].Trim();

        if (!Guid.TryParse(trimmed, out var guid))
            return false;

        bracedLowercase = guid.ToString("B");
        return true;
    }

    private static GodotObject? TryGetServer()
    {
        try
        {
            return Engine.HasSingleton(FmodServerName) ? Engine.GetSingleton(FmodServerName) : null;
        }
        catch
        {
            return null;
        }
    }

    private static bool TryCallServer(StringName method, params Variant[] args)
    {
        return TryCallServer(out _, method, args);
    }

    private static bool TryCallServer(out Variant result, StringName method, params Variant[] args)
    {
        result = default;
        var server = TryGetServer();
        if (server is null)
            return false;

        try
        {
            result = args.Length == 0 ? server.Call(method) : server.Call(method, args);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private sealed record LoopSlot(FmodEventHandle Event, bool UsesLoopParam);
}

public sealed class FmodEventHandle : IDisposable
{
    private readonly GodotObject _instance;
    private bool _released;

    internal FmodEventHandle(GodotObject instance)
    {
        _instance = instance;
    }

    public GodotObject RawInstance => _instance;
    public bool IsReleased => _released;

    public bool Start()
    {
        return TryCall("start");
    }

    public bool Stop(bool allowFadeOut = true)
    {
        return TryCall("stop", allowFadeOut ? 0 : 1);
    }

    public bool SetParameter(string name, float value)
    {
        return TryCall("set_parameter_by_name", name, value);
    }

    public bool SetVolume(float volume)
    {
        return TryCall("set_volume", volume);
    }

    public bool SetPitch(float pitch)
    {
        return TryCall("set_pitch", pitch);
    }

    public bool SetPaused(bool paused)
    {
        return TryCall("set_paused", paused);
    }

    public bool Release()
    {
        if (_released)
            return true;

        var ok = TryCall("release");
        _released = ok;
        return ok;
    }

    public void Dispose()
    {
        Stop();
        Release();
    }

    private bool TryCall(string method, params Variant[] args)
    {
        if (_released)
            return false;

        try
        {
            _instance.Call(method, args);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
