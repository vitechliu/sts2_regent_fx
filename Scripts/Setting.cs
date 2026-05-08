using RegentFX.ThirdParty;

namespace RegentFX.Scripts;

public static class Setting {

    public static float ExposureThreshold => (float)RitsuLibModConfig.GetRitsuLibSettingDouble("ExposureThreshold");
    public static bool DevTestStartMode => RitsuLibModConfig.GetRitsuLibSettingBool("DevTestStartMode");
    
    

    public static bool ToggleEnabled(string key) {
        var raw = RitsuLibModConfig.GetRitsuLibSettingBool(key);
        return (bool)raw;
    }
}
