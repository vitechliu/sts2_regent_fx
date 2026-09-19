# Regent FX Omnistar

**English** | [简体中文](README_CN.md)

[Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3747497501) · [RitsuLib](https://steamcommunity.com/sharedfiles/filedetails/?id=3747602295) · [Changelog](CHANGELOG_EN.md)

Regent FX Omnistar (万象辉星) is a visual and audio enhancement mod for the Regent in **Slay the Spire 2**. It brings the Regent's card artwork to life through orbiting stars, custom casting and impact effects, persistent power effects, and FMOD sound effects.

Created by **Vitech, SSSuika, and Margaritana**. The current source version is **0.5.1**.

## Features

- An orbiting star ring that reflects the local Regent player's current Stars and participates in card animations.
- Custom effects for 21 cards and 4 powers, including projectiles, beams, particles, distortion, and lighting.
- FMOD sound effects, with an option to return to the game's original attack sounds.
- Individual card and power effect toggles, adjustable light exposure, and optional effect preloading through RitsuLib.
- Client-side presentation for multiplayer: other players do not need to install RegentFX. Star rings and persistent power effects are scoped to the local player.

The mod is intended to change presentation without changing card stats or game balance. Its [manifest](RegentFX.json) declares `affects_gameplay: false`.

## Settings

With RitsuLib installed, open its mod settings interface and select **RegentFX / 万象辉星**.

| Setting | Default | Behavior |
| --- | --- | --- |
| Light Exposure | `1.0` | Adjusts effect exposure from `0` to `2`; `0` disables the exposure effect. |
| Preload Cache | On | Loads effect scenes during startup to reduce first-use stutter, at the cost of startup time and memory. Requires a game restart. |
| Disable Mod Sounds | Off | When enabled, falls back to the game's original attack sounds. |
| Cards / Powers | All on | Enables or disables each registered card or power effect individually. |
| Test Mode | Off | A developer setting; leave it off for normal play. |

Settings are stored in `SlayTheSpire2/RegentFX/ritsu_interop_state.json` under the operating system's .NET `LocalApplicationData` directory. On Windows, this is `%LOCALAPPDATA%\SlayTheSpire2\RegentFX\ritsu_interop_state.json`.

## Effect coverage

The current source registers the following effects. Card names follow the game's English localization.

| Type | Supported cards / powers |
| --- | --- |
| Card effects | Alignment, Astral Pulse, Big Bang, Comet, Crescent Spear, Dying Star, Falling Star, Glow, Guiding Star, Lunar Blast, Make It So, Particle Wall, Radiate, Resonance, Seven Stars, Shining Strike, Solar Strike, Stardust, Strike (Regent), Supermassive, Wrought in War |
| Power effects | Black Hole, Genesis, Pillar of Creation, Spectrum Shift |

Implementations are in [Scripts/Vfx/Cards](Scripts/Vfx/Cards) and [Scripts/Vfx/Powers](Scripts/Vfx/Powers).

## Compatibility and known issues

The current manifest specifies a minimum game version of **0.103.0**, and the **0.5.1** changelog records compatibility work for **beta 0.111**. This is not a guarantee that every intervening or future game build is compatible. See the [Chinese changelog](CHANGELOG.md) and [English changelog](CHANGELOG_EN.md) for release details; the English changelog currently stops at 0.5.0.

Windows and macOS support is recorded in the changelog. The wsdx233 mobile port has limited compatibility, including missing custom audio. Multiplayer allows a local-only installation, but interactions with other mods may still need investigation.

Known issues:

- Persistent power effects remain in place when the sandworm drags the player.
- Playing **Guiding Star** on the mobile port can crash the game.
- Startup preloading can cause stutter; low-VRAM systems and integrated GPUs may crash while loading effects.
- Background pillars can disappear during the **Vantom** encounter.

For startup or memory issues, try disabling **Preload Cache** and restarting the game; this may reintroduce stutter when an effect is first used. For audio issues, enable **Disable Mod Sounds**. If FMOD bank loading fails, RegentFX also falls back to the default attack sounds automatically.

If your progress appears missing after enabling mods, check whether the game has switched to its separate modded save profile. This does not by itself mean your original save was deleted.

## Building from source

This is a game mod, not a standalone Godot game. Run it inside Slay the Spire 2 for gameplay and visual verification.

### Prerequisites

- An installed copy of Slay the Spire 2, including `sts2.dll` and `0Harmony.dll`.
- **.NET 9 SDK** or a compatible newer SDK capable of targeting `net9.0`.
- **Godot 4.5.1 with .NET support** for resource editing and `.pck` export. The environment examples use MegaDot; configure the path to the compatible editor executable you use.
- **FMOD Studio** only if editing or rebuilding audio. The source project uses the `Studio.02.03.00` format; the runtime bank and GUID mappings are already included.

The project uses C# 12, `Godot.NET.Sdk/4.5.1`, Harmony from the game installation, and `Krafs.Publicizer` 2.3.0. Game assemblies are supplied by your installation, not by NuGet restore.

### 1. Configure local paths

Copy the appropriate template to `env.props` and edit it for your machine. Run commands from the repository root.

Windows / PowerShell:

```powershell
Copy-Item env.props.example_windows env.props
```

macOS:

```sh
cp env.props.example_mac env.props
```

| Property | Purpose |
| --- | --- |
| `GodotPath` | Full path to the Godot / MegaDot .NET executable used for export. |
| `GodotPreset` | Exact name of the export preset you create in Godot. |
| `Sts2Dir` | Game installation directory; on macOS, the template points to the app's `Contents` directory. |
| `Sts2DataDir` | Directory containing the game's `0Harmony.dll`, typically a platform-specific `data_sts2_*` directory. |
| `Sts2DllDir` | Directory containing `sts2.dll`. Verify its actual location. |
| `Sts2ModDir` | Destination `mods` directory; build output is copied to its `RegentFX` subdirectory. |

The templates contain example paths, not portable defaults. In particular, they set `Sts2DllDir` to `$(Sts2Dir)`; if your `sts2.dll` is in the data directory, change this to `$(Sts2DataDir)`.

`env.props` is ignored by Git and should remain local.

### 2. Configure resource export

`export_presets.cfg` is also ignored by Git, so a fresh clone needs a local export preset:

1. Open `project.godot` in the .NET editor and let resource imports finish.
2. In **Project → Export**, add a preset for your platform and install matching export templates if prompted.
3. Set `GodotPreset` in `env.props` to the preset's exact name. The Windows example uses `Windows`; the macOS example uses `mac`. If you name your preset `win`, use `win` in `env.props` too.
4. Export all project resources and add `*.bank,*.txt` to the non-resource include filter so the FMOD bank and `GUIDs.txt` are included.
5. Keep **Embed Build Outputs** disabled for the C# export: the mod loads `RegentFX.dll` alongside the resource pack.

### 3. Restore and build

```sh
dotnet restore RegentFX.sln
dotnet build RegentFX.sln --no-restore
```

The build copies `RegentFX.dll` and `RegentFX.json` to `$(Sts2ModDir)/RegentFX/`. It does **not** export the `.pck`; a first installation or a resource change also needs the publish step.

### 4. Publish the resource pack

```sh
dotnet publish RegentFX.csproj -c ExportRelease -f net9.0 -o ./bin/ExportRelease/net9.0/publish
```

The same command is available as `./publish.bat` on Windows and `sh ./publish.sh` on macOS. With a valid `GodotPath` and matching preset, publishing exports `RegentFX.pck` directly to `$(Sts2ModDir)/RegentFX/`.

Check the export output and confirm that the destination contains all three files shown in the installation section. A missing `GodotPath` skips resource export, and the current build target reports Godot export failures as warnings, so a successful `dotnet publish` alone does not prove the pack was updated.

## Contributing

Bug fixes, effect improvements, compatibility work, and documentation updates are welcome.

For a new effect, start with `CardFX` or `PowerFX`, register the corresponding game type using `CardFxAttribute` or `PowerFxAttribute`, and declare its preload resources through `AssetPaths`. The registries also supply the per-effect settings toggles. Keep multiplayer ownership checks and combat-end cleanup in mind.

Use UTF-8, four-space indentation, file-scoped namespaces, nullable annotations, and the existing C# brace style. Use `res://RegentFX/...` for mod resources, `Entry.Logger` for logging, and `AddChildSafely()` when attaching Godot nodes.

Before submitting a pull request:

1. Run `git diff --check` and `dotnet build RegentFX.sln --no-restore`.
2. For resource changes, publish an updated `.pck` and verify that scenes, shaders, and audio load without warnings.
3. For visual or gameplay-related changes, trigger the affected cards or powers in the actual game. Check ordinary play and multiplayer paths, and state which cases you tested. There is currently no standalone automated test project.
4. Explain the behavior change, link any related issue, and include screenshots or a short video for visual changes. Mention required game, Godot, or FMOD configuration changes.

Keep commits focused; existing subjects commonly use `feat:` or `fix:`. Keep generated `.godot/`, `bin/`, `obj/`, and machine-specific configuration out of commits. Keep both READMEs in sync when updating documentation.

## Feedback and credits

Report problems through the repository's issue tracker or the [Workshop page](https://steamcommunity.com/sharedfiles/filedetails/?id=3747497501). Include your game version and branch, RegentFX version, operating system, other enabled mods, reproduction steps, and `godot.log`. Screenshots or videos help with visual issues. Community QQ group: **1042906424**.

- Thanks to **SSSuika** and **Margaritana** for joining the project.
- Thanks to the **水产品交流群** community and its group owner **Reme** for their support.
- Thanks to **OLC** for technical guidance and **Dior** for their support.
- Thanks to **Nitablade, Gk, Cany0udance, Vex'd**, and the other Discord community members for their encouragement and support.

## License

MIT