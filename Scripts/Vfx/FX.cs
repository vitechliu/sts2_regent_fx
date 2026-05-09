namespace RegentFX.Scripts.Vfx;

public abstract class FX: IWithFxLoad {

    public const string DISTORTION = "res://RegentFX/scenes/vfx/distortions/vfx_outward_screen_distortion_ellipse.tscn";
    
    
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