---
name: generate-tscn-scene
description: 根据用户资源生成Godot场景文件(.tscn)和配套C#代码。当用户提供了贴图、材质等资源并描述想要的特效效果时使用。
---

# 生成Godot场景文件

## 用途

根据用户提供的资源（图片、材质等）生成`.tscn`场景文件和配套的C#加载代码。

## 触发场景

- 用户提供了特效图片，想要生成粒子场景
- 用户想要创建特定类型的特效（爆炸、光环、烟雾等）
- 用户需要.tscn文件内容和对应的C#代码
- 用户想要将资源转换为可用的Godot场景

## 工作流程

1. **分析资源**
   - 确认资源类型（图片、材质、动画等）
   - 确认资源路径（应在 `RegentFX/` 目录下）

2. **确定特效类型**
   - 粒子特效（爆炸、火花、烟雾等）
   - 持续特效（光环、护盾等）
   - 动画特效（序列帧等）

3. **生成.tscn内容**
   - 创建符合Godot 4.x格式的场景文件
   - 配置合适的粒子参数
   - 设置纹理和材质引用

4. **生成C#代码**
   - 创建场景加载代码
   - 提供播放控制方法

## 资源路径规范

- 图片: `res://RegentFX/images/xxx.png`
- 场景: `res://RegentFX/scenes/xxx.tscn`
- 材质: `res://RegentFX/materials/xxx.tres`

## 场景类型模板

### 1. 爆炸/冲击特效

```
[gd_scene load_steps=3 format=3 uid="uid://xxxxxxxx"]

[ext_resource type="Texture2D" uid="uid://xxxxxxxx" path="res://RegentFX/images/explosion.png" id="1_xxxx"]

[sub_resource type="ParticleProcessMaterial" id="ParticleProcessMaterial_xxxx"]
emission_shape = 0
spread = 180.0
gravity = Vector3(0, 0, 0)
initial_velocity_min = 100.0
initial_velocity_max = 200.0
angular_velocity_min = -180.0
angular_velocity_max = 180.0
scale_min = 0.5
scale_max = 1.5
scale_curve = SubResource("Curve_xxxx")
color = Color(1, 0.8, 0.3, 1)

[node name="NExplosionVfx" type="Node2D"]

[node name="Particles" type="GPUParticles2D" parent="."]
amount = 32
process_material = SubResource("ParticleProcessMaterial_xxxx")
texture = ExtResource("1_xxxx")
one_shot = true
explosiveness = 1.0
lifetime = 0.8
```

### 2. 光环/持续特效

```
[gd_scene load_steps=3 format=3 uid="uid://xxxxxxxx"]

[ext_resource type="Texture2D" uid="uid://xxxxxxxx" path="res://RegentFX/images/aura.png" id="1_xxxx"]

[sub_resource type="ParticleProcessMaterial" id="ParticleProcessMaterial_xxxx"]
emission_shape = 1
emission_sphere_radius = 50.0
gravity = Vector3(0, -20, 0)
angular_velocity_min = -90.0
angular_velocity_max = 90.0
scale_min = 0.8
scale_max = 1.2
color = Color(0.3, 0.6, 1, 0.6)

[node name="NAuraVfx" type="Node2D"]

[node name="Particles" type="GPUParticles2D" parent="."]
amount = 16
process_material = SubResource("ParticleProcessMaterial_xxxx")
texture = ExtResource("1_xxxx")
lifetime = 2.0
preprocess = 2.0
```

### 3. 火花/飞溅特效

```
[gd_scene load_steps=3 format=3 uid="uid://xxxxxxxx"]

[ext_resource type="Texture2D" uid="uid://xxxxxxxx" path="res://RegentFX/images/spark.png" id="1_xxxx"]

[sub_resource type="ParticleProcessMaterial" id="ParticleProcessMaterial_xxxx"]
emission_shape = 0
spread = 90.0
gravity = Vector3(0, 200, 0)
initial_velocity_min = 150.0
initial_velocity_max = 300.0
scale_min = 0.3
scale_max = 0.8
color = Color(1, 0.9, 0.4, 1)

[node name="NSparkVfx" type="Node2D"]

[node name="Particles" type="GPUParticles2D" parent="."]
amount = 12
process_material = SubResource("ParticleProcessMaterial_xxxx")
texture = ExtResource("1_xxxx")
one_shot = true
explosiveness = 0.8
lifetime = 0.5
```

## C#配套代码模板

```csharp
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Helpers;

namespace RegentFX.Vfx;

/// <summary>
/// {特效名称} VFX
/// 场景路径: res://RegentFX/scenes/{scene_name}.tscn
/// </summary>
public static class N{SceneName}Vfx
{
    private const string ScenePath = "res://RegentFX/scenes/{scene_name}.tscn";
    
    /// <summary>
    /// 在指定位置播放特效
    /// </summary>
    public static void Play(Vector2 position)
    {
        if (NCombatRoom.Instance == null) return;
        
        var scene = GD.Load<PackedScene>(ScenePath);
        if (scene == null)
        {
            GD.PushError($"Failed to load scene: {ScenePath}");
            return;
        }
        
        var vfx = scene.Instantiate<Node2D>();
        NCombatRoom.Instance.CombatVfxContainer.AddChildSafely(vfx);
        vfx.GlobalPosition = position;
    }
    
    /// <summary>
    /// 在生物中心播放特效
    /// </summary>
    public static void PlayOnCreature(Creature creature)
    {
        if (creature == null || creature.IsDead) return;
        
        var node = NCombatRoom.Instance?.GetCreatureNode(creature);
        if (node != null)
        {
            Play(node.VfxSpawnPosition);
        }
    }
    
    /// <summary>
    /// 在多个生物上播放特效
    /// </summary>
    public static void PlayOnCreatures(IEnumerable<Creature> creatures)
    {
        foreach (var creature in creatures)
        {
            PlayOnCreature(creature);
        }
    }
}
```

## 输出格式

当用户请求生成场景时，输出以下内容：

1. **.tscn文件内容** - 可直接保存到 `RegentFX/scenes/`
2. **C#加载代码** - 保存到 `Scripts/Vfx/`
3. **保存路径建议**:
   - 场景: `RegentFX/scenes/{name}.tscn`
   - 代码: `Scripts/Vfx/N{name}Vfx.cs`
4. **参数调整说明** - 如何修改达到不同效果

## 参数说明

常用 `ParticleProcessMaterial` 参数:

| 参数 | 说明 | 常用值 |
|------|------|--------|
| `emission_shape` | 发射形状 (0=点, 1=球, 2=盒) | 0, 1 |
| `spread` | 扩散角度 | 0-180 |
| `gravity` | 重力影响 | Vector3(0, 0, 0) |
| `initial_velocity_min/max` | 初始速度范围 | 50-500 |
| `scale_min/max` | 缩放范围 | 0.1-2.0 |
| `color` | 粒子颜色 | Color(r, g, b, a) |
| `amount` | 粒子数量 | 1-100 |
| `lifetime` | 生命周期(秒) | 0.1-5.0 |
| `one_shot` | 是否只播放一次 | true/false |
| `explosiveness` | 爆发程度 | 0.0-1.0 |
