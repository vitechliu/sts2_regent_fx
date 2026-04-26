using RegentFX.ThirdParty;

namespace RegentFX.Scripts;

public static class Setting {

    public static float ExposureThreshold {
        get {
            var raw = RitsuLibModConfig.GetRitsuLibSettingDouble("ExposureThreshold");
            if (raw == null) return 1f;
            return (float)raw;
        }
    }
}
