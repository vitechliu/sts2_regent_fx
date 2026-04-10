---
trigger: manual
alwaysApply: false
---

### 技术栈

本项目为2D卡牌游戏 杀戮尖塔2 的mod
杀戮尖塔2采用Godot+C#开发，原生支持mod
本项目作为一个Godot项目，主要使用Harmony库去注入行为
特效、音效素材先制作成tscn场景，后续在游戏中动态导入或者替换

### 需求
本项目主要是为杀戮尖塔2中原生的角色储君(Regent)的卡牌设计特效
原版特效过于单一，因此本项目为特色卡牌设计独有的出手、和受击特效、音效

日志输出使用Entry.cs中的Logger,不要使用GD.Print()