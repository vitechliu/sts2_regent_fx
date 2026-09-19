# Regent FX Omnistar · 万象辉星

[English](README.md) | **简体中文**

[Steam 创意工坊](https://steamcommunity.com/sharedfiles/filedetails/?id=3747497501) · [RitsuLib](https://steamcommunity.com/sharedfiles/filedetails/?id=3747602295) · [更新日志](CHANGELOG.md)

万象辉星是为 **《杀戮尖塔 2》储君**制作的视觉与音效增强 Mod。项目以卡牌插画为灵感，为储君加入环绕星星、施法与命中特效、常驻能力特效，以及 FMOD 音效。

作者：**Vitech、SSSuika、Margaritana**。当前源码版本：**0.5.1**。

## 主要功能

- 环绕本地储君的星环，随当前星星数量变化，并参与出牌演出。
- 为 21 张卡牌和 4 类能力提供专属特效，包括投射物、光束、粒子、扭曲和光效。
- 使用 FMOD 播放音效，也可切换回游戏原版攻击音效。
- 通过 RitsuLib 单独开关卡牌和能力特效、调整曝光光效强度、设置特效预加载。
- 支持联机时仅自己安装 RegentFX；星环与常驻能力特效限定于本地玩家。

Mod 的目标是增强表现，不改变卡牌数值或游戏平衡。[清单文件](RegentFX.json)中声明了 `affects_gameplay: false`。

## 设置说明

安装 RitsuLib 后，在其模组设置界面中选择 **RegentFX / 万象辉星**。

| 设置项 | 默认值 | 说明 |
| --- | --- | --- |
| 曝光光效强度 | `1.0` | 范围为 `0` 至 `2`；设为 `0` 可关闭曝光光效。 |
| 特效预加载 | 开启 | 启动时加载特效场景，减少首次播放卡顿，但会增加启动耗时和内存占用。修改后需要重启游戏。 |
| 禁用 Mod 音效 | 关闭 | 开启后回退到原版游戏的默认攻击音效。 |
| 卡牌 / 能力 | 全部开启 | 可分别启用或禁用已接入的卡牌、能力特效。 |
| 测试模式 | 关闭 | 开发调试选项，正常游玩时保持关闭。 |

设置保存在操作系统的 .NET `LocalApplicationData` 目录下，相对路径为 `SlayTheSpire2/RegentFX/ritsu_interop_state.json`。Windows 对应 `%LOCALAPPDATA%\SlayTheSpire2\RegentFX\ritsu_interop_state.json`。

## 已接入的特效

当前源码已注册以下特效，卡牌名称采用游戏简体中文本地化。

| 类型 | 支持的卡牌 / 能力 |
| --- | --- |
| 卡牌特效 | 星位序列、星界脉冲、大爆炸、彗星、新月长矛、星灭、陨星、辉光、引导之星、月面射击、如此甚好、粒子墙、辐射、共鸣、七星、明耀打击、太阳打击、星尘、打击（储君）、超质量体、战火铸就 |
| 能力特效 | 黑洞、创世纪、创世之柱、光谱偏移 |

对应实现位于 [Scripts/Vfx/Cards](Scripts/Vfx/Cards) 和 [Scripts/Vfx/Powers](Scripts/Vfx/Powers)。

## 兼容性与已知问题

当前清单声明的最低游戏版本为 **0.103.0**，**0.5.1** 更新日志记录了对 **beta 0.111** 的兼容修复。这不代表所有中间版本或未来版本都已通过验证。版本变动请查阅[中文更新日志](CHANGELOG.md)和[英文更新日志](CHANGELOG_EN.md)；英文日志目前更新至 0.5.0。

更新日志中记录了 Windows 和 macOS 支持。wsdx233 移动版的兼容性仍有限，存在自定义音效缺失等问题。联机允许仅本地安装，但与其他 Mod 的交互仍可能需要排查。

目前已知的问题：

- 沙虫拖拽玩家时，常驻能力特效会保持在原地。
- 在移动版打出**引导之星**可能导致游戏崩溃。
- 启动时大量预加载可能导致卡顿；显存较低或使用核显、集显的设备可能在加载特效时崩溃。
- 对战**墨影幻灵（Vantom）**时，背景柱子可能消失。

遇到启动卡顿或内存问题时，可尝试关闭**特效预加载**并重启游戏，但首次播放特效时可能重新出现卡顿。遇到音效问题时，可开启**禁用 Mod 音效**。FMOD 音频库加载失败时，RegentFX 也会自动回退到默认攻击音效。

如果启用 Mod 后发现原有进度似乎消失，请先确认游戏是否切换到了独立的 Mod 存档。这个现象本身不代表原存档已被删除。

## 从源码构建

本项目是游戏 Mod，不是独立运行的 Godot 游戏。玩法和视觉效果需要在《杀戮尖塔 2》中验证。

### 环境要求

- 已安装《杀戮尖塔 2》，并具备其中的 `sts2.dll` 和 `0Harmony.dll`。
- **.NET 9 SDK**，或支持构建 `net9.0` 的兼容新版 SDK。
- 支持 **.NET 的 Godot 4.5.1**，用于编辑资源和导出 `.pck`。环境示例使用 MegaDot，请填写实际使用的兼容编辑器可执行文件路径。
- 仅在编辑或重新构建音频时需要 **FMOD Studio**。源工程使用 `Studio.02.03.00` 格式；仓库已经包含运行所需的音频库和 GUID 映射。

项目使用 C# 12、`Godot.NET.Sdk/4.5.1`、游戏自带的 Harmony，以及 `Krafs.Publicizer` 2.3.0。游戏程序集由本地游戏安装提供，NuGet 还原不会下载这些文件。

### 1. 配置本机路径

将当前平台的模板复制为 `env.props`，再按本机环境修改。以下命令均在仓库根目录执行。

Windows / PowerShell：

```powershell
Copy-Item env.props.example_windows env.props
```

macOS：

```sh
cp env.props.example_mac env.props
```

| 配置项 | 用途 |
| --- | --- |
| `GodotPath` | 用于导出的 Godot / MegaDot .NET 可执行文件完整路径。 |
| `GodotPreset` | 在 Godot 中创建的导出预设名称，必须完全一致。 |
| `Sts2Dir` | 游戏安装目录；macOS 示例指向应用包内的 `Contents` 目录。 |
| `Sts2DataDir` | 游戏 `0Harmony.dll` 所在目录，通常为对应平台的 `data_sts2_*` 目录。 |
| `Sts2DllDir` | `sts2.dll` 所在目录，请核对实际位置。 |
| `Sts2ModDir` | 游戏的 `mods` 目录，构建结果会复制到其中的 `RegentFX` 子目录。 |

模板中的路径只是示例，不能直接用于所有设备。尤其是 `Sts2DllDir`，模板将其设为 `$(Sts2Dir)`；如果你的 `sts2.dll` 位于数据目录中，应改为 `$(Sts2DataDir)`。

`env.props` 已被 Git 忽略，应仅保留在本机。

### 2. 配置资源导出

`export_presets.cfg` 同样被 Git 忽略，因此首次克隆后需要创建本地导出预设：

1. 使用 .NET 编辑器打开 `project.godot`，等待资源导入完成。
2. 在**项目 → 导出**中添加对应平台的预设；如编辑器提示缺少导出模板，安装匹配的模板。
3. 将 `env.props` 中的 `GodotPreset` 设为预设的准确名称。Windows 示例使用 `Windows`，macOS 示例使用 `mac`；如果预设命名为 `win`，配置中也必须填写 `win`。
4. 选择导出全部项目资源，并在非资源文件包含过滤器中添加 `*.bank,*.txt`，确保 FMOD 音频库与 `GUIDs.txt` 一起打包。
5. 关闭 C# 导出选项中的 **Embed Build Outputs**；Mod 会单独加载资源包旁的 `RegentFX.dll`。

### 3. 还原依赖并构建

```sh
dotnet restore RegentFX.sln
dotnet build RegentFX.sln --no-restore
```

构建会将 `RegentFX.dll` 和 `RegentFX.json` 复制到 `$(Sts2ModDir)/RegentFX/`，但**不会**导出 `.pck`。首次安装或修改资源后，还需要执行发布步骤。

### 4. 发布资源包

```sh
dotnet publish RegentFX.csproj -c ExportRelease -f net9.0 -o ./bin/ExportRelease/net9.0/publish
```

Windows 下的 `./publish.bat` 和 macOS 下的 `sh ./publish.sh` 执行同一条命令。配置了有效的 `GodotPath` 和匹配的预设后，发布会直接将 `RegentFX.pck` 导出到 `$(Sts2ModDir)/RegentFX/`。

请检查导出日志，并确认目标目录中包含安装章节列出的三个文件。未配置有效 `GodotPath` 时会跳过资源导出；当前构建目标将 Godot 导出失败作为警告处理，因此仅凭 `dotnet publish` 成功不能确认资源包已更新。

## 参与贡献

欢迎提交问题修复、特效改进、兼容性适配和文档完善。

新增特效时，可从 `CardFX` 或 `PowerFX` 基类入手，使用 `CardFxAttribute` 或 `PowerFxAttribute` 关联游戏类型，并通过 `AssetPaths` 声明预加载资源。注册表也用于生成各特效的设置开关。实现时应考虑联机中的玩家归属判断，以及战斗结束后的资源清理。

代码请使用 UTF-8、四空格缩进、文件范围命名空间、可空标注及现有 C# 花括号风格。Mod 资源路径使用 `res://RegentFX/...`，日志使用 `Entry.Logger`，添加 Godot 节点时使用 `AddChildSafely()`。

提交拉取请求前：

1. 执行 `git diff --check` 和 `dotnet build RegentFX.sln --no-restore`。
2. 修改资源后，发布更新的 `.pck`，确认场景、着色器和音频加载时没有警告。
3. 涉及视觉或玩法的修改，应在真实游戏中触发对应卡牌或能力，检查普通流程及多人联机路径，并说明实际验证的范围。目前项目没有独立的自动化测试工程。
4. 说明行为变化、关联相关 Issue；视觉改动附截图或短视频。如需修改游戏、Godot 或 FMOD 配置，也请写明。

每个提交保持单一主题，历史提交常使用 `feat:` 或 `fix:` 前缀。不要提交 `.godot/`、`bin/`、`obj/` 等生成内容和本机配置。更新文档时请同步维护中英文 README。

## 反馈与鸣谢

可以通过仓库 Issue 或[创意工坊页面](https://steamcommunity.com/sharedfiles/filedetails/?id=3747497501)反馈问题。请提供游戏版本与分支、RegentFX 版本、操作系统、其他已启用的 Mod、复现步骤及 `godot.log`；视觉问题请尽量附截图或视频。交流 QQ 群：**1042906424**。

- 感谢 **SSSuika** 和 **Margaritana** 的加入。
- 感谢**水产品交流群**和群主 **Reme** 的支持。
- 感谢 **OLC** 的技术指导，以及 **Dior** 的支持。
- 感谢 **Nitablade、Gk、Cany0udance、Vex'd** 等 Discord 群友的鼓励与支持。

## 许可证

MIT