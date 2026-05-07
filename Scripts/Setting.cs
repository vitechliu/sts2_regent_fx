using RegentFX.ThirdParty;

namespace RegentFX.Scripts;

public static class Setting {

    public static float ExposureThreshold {
        get {
            var raw = RitsuLibModConfig.GetRitsuLibSettingDouble("ExposureThreshold");
            return (float)raw;
        }
    }

    public static bool ToggleEnabled(string key) {
        var raw = RitsuLibModConfig.GetRitsuLibSettingBool(key);
        return (bool)raw;
    }
}
