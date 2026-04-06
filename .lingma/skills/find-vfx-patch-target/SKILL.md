---
name: find-vfx-patch-target
description: 分析STS2游戏源码找到VFX特效注入点。当用户想要为攻击、受击、角色周围等场景添加特效时使用。自动生成Harmony补丁代码框架。
---

# 查找VFX注入点

## 用途

分析杀戮尖塔2(STS2)游戏源码，找到合适的Harmony补丁注入点，为特效(VFX)提供触发时机。

## 触发场景

- 用户想要为攻击动作添加特效
- 用户想要为受击动作添加特效
- 用户想要创建围绕角色的持续特效（光环等）
- 用户想要为特定卡牌添加特效
- 用户想要找到合适的VFX播放时机

## 工作流程

1. **理解需求**
   - 确定特效触发时机（攻击/受击/围绕角色/特定卡牌等）
   - 了解是否有特定目标（特定怪物、卡牌等）

2. **搜索游戏源码**
   - 在 `D:\Github\sts2\decompiled` 目录搜索相关代码
   - 使用Grep搜索类名、方法名、VFX相关调用

3. **分析注入点**
   - 识别合适的Patch策略（Prefix/Postfix）
   - 分析方法参数和返回值
   - 确定如何获取必要的上下文

4. **生成补丁代码**
   - 生成Harmony补丁类框架
   - 提供VFX播放代码示例

## 注入模式参考

### 攻击触发
- **目标类**: `AttackCommand` (`Core/Commands/Builders/AttackCommand.cs`)
- **方法**: `Execute`
- **策略**: Postfix
- **获取攻击者**: `___Attacker` 或 `__instance.Attacker`
- **获取目标**: `___singleTarget` 或分析 `GetPossibleTargets()`

### 受击触发
- **目标类**: `Creature` (`Core/Entities/Creatures/Creature.cs`)
- **方法**: `TakeDamage`
- **策略**: Postfix
- **获取目标**: `__instance`

### 围绕角色（光环类）
- **目标类**: `NCreature` (`Core/Nodes/Combat/NCreature.cs`)
- **方法**: `_Ready` 或相关初始化
- **策略**: Postfix
- **实现**: 创建跟随逻辑或定时更新位置

### 特定卡牌
- **目标类**: 具体卡牌类 (`Core/Models/Cards/xxx.cs`)
- **方法**: `OnPlay` 或 `Use`
- **策略**: Postfix

## 输出格式

```csharp
// ============================================
// 分析结果
// ============================================
// 目标类: AttackCommand
// 方法: Execute
// 策略: Postfix
// 获取上下文: ___Attacker (攻击者), ___singleTarget (目标)

// ============================================
// Harmony补丁代码
// ============================================
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using Godot;

namespace RegentFX.Patches;

[HarmonyPatch(typeof(AttackCommand))]
public class AttackCommandVfxPatch
{
    [HarmonyPostfix]
    [HarmonyPatch("Execute")]
    static void Postfix(
        AttackCommand __instance,
        Creature ___Attacker,
        Creature ___singleTarget)
    {
        // 检查有效性
        if (___Attacker == null || ___Attacker.IsDead) return;
        
        // 在攻击者位置播放特效
        // 方式1: 使用VfxCmd
        // VfxCmd.PlayOnCreatureCenter(___Attacker, "RegentFX/scenes/my_effect");
        
        // 方式2: 加载自定义场景
        var scene = GD.Load<PackedScene>("res://RegentFX/scenes/my_effect.tscn");
        if (scene != null && NCombatRoom.Instance != null)
        {
            var vfx = scene.Instantiate<Node2D>();
            NCombatRoom.Instance.CombatVfxContainer.AddChildSafely(vfx);
            
            var creatureNode = NCombatRoom.Instance.GetCreatureNode(___Attacker);
            if (creatureNode != null)
            {
                vfx.GlobalPosition = creatureNode.VfxSpawnPosition;
            }
        }
    }
}

// ============================================
// 使用说明
// ============================================
// 1. 将此代码保存到 Mod项目的 Patches/AttackCommandVfxPatch.cs
// 2. 确保Harmony在Entry.cs中已初始化
// 3. 创建对应的.tscn场景文件到 RegentFX/scenes/my_effect.tscn
// 4. 编译并测试
```

## VFX播放方式

### 方式1: 使用VfxCmd（推荐简单特效）
```csharp
VfxCmd.PlayOnCreatureCenter(creature, "path/to/vfx");
VfxCmd.PlayOnCreatures(targets, "path/to/vfx");
VfxCmd.PlayOnSide(CombatSide.Enemy, "path/to/vfx", combatState);
```

### 方式2: 加载自定义场景（推荐复杂特效）
```csharp
var scene = GD.Load<PackedScene>("res://RegentFX/scenes/my_effect.tscn");
var vfx = scene.Instantiate<Node2D>();
NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(vfx);
vfx.GlobalPosition = position;
```

## 注意事项

- 始终检查 `NCombatRoom.Instance` 不为null
- 检查目标 `Creature` 不为null且未死亡 (`!IsDead`)
- 使用 `AddChildSafely()` 而非直接 `AddChild()`
- 资源路径使用 `res://RegentFX/` 前缀
- 优先使用Postfix避免干扰原逻辑
