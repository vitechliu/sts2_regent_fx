# AGENTS.md

使用中文思考和交流

## 项目结构
- Godot 4.5 + C# (.NET 9) Mod项目
- Mod入口: `Scripts/Entry.cs` - 使用 `[ModInitializer("Init")]`
- 脚本: `Scripts/Vfx/` 视觉效果, `Scripts/Patches/` Harmony补丁
- 资源: `RegentFX/scenes/`, `RegentFX/images/`

## 关键命令
```bash
dotnet build --no-restore  # 验证编译
```

## 规范
- 日志用 `Entry.Logger` (Info/Warn/Debug)，禁止 `GD.Print()`
- 资源路径: `res://RegentFX/...`
- 使用 `AddChildSafely()` 添加节点
- 检查 `NCombatRoom.Instance != null` 和 `Creature.IsDead`

## 记忆
- 详细规范: `.opencode/memory.md`
- 项目结构: `.opencode/rules/project.md`

## 技术栈
- Harmony 2.x (Lib.Harmony) - 代码注入
- 游戏源码: `D:\Github\raw109\src`
- 游戏资源: `D:\Github\raw109`