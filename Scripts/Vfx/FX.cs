namespace RegentFX.Scripts.Vfx;

public abstract class FX: IWithFxLoad {
    
    
    public virtual string? VfxScenePath => null;

    public virtual List<string> AssetPaths {
        get {
            if (VfxScenePath != null) {
                return [VfxScenePath];
            }
            return [];
        }
    }
    
}