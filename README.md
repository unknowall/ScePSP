# A Lightweight PlayStation Portable Emulator Fully Developed in C#
<details>
<summary><h3>中文说明</h3></summary>
  
## 项目概述
这是一个完全使用 C# 开发的轻量级 PlayStation Portable 模拟器。目前处于 alpha 阶段，核心模拟引擎已取得显著进展，但仍存在游戏兼容性限制。

## 当前状态
核心模拟引擎已实现相对稳定，能够在 i3-3215u 处理器上以稳定的 60 FPS 运行。性能指标相当不错：
- CPU 使用率：约 20-28%
- 内存使用：约 400MB（2倍内部分辨率，最大材质缓存 300，无材质缩放）
需要注意的是，**目前仅能运行少量游戏**，且许多游戏存在显著的图形和音频问题。此 alpha 版本主要面向：
- 想要探索模拟器实现的技术爱好者
- 对 PSP 模拟器内部工作原理感兴趣的开发者
- 愿意测试早期阶段功能的用户

## 开发进度

| 模块 | 状态 | 进度 | 说明 |
| :--- | :---: | :---: | :--- |
| CPU | ✅ 已完成 | 100% | 使用 AST 优化的动态重编译，速度约为原 CPU 的 12 倍 |
| GE | ⏳ 开发中 | 90% | BBOX、BJUMP、SIGNAL 指令尚未完成 |
| ME | ⏳ 开发中 | 70% | VAG、ATRAC3、ATRAC3+ 和 AAC 解码已完成，通过 LightCodec 实现 |
| AUDIO | ✅ 已完成 | 100% | 使用 SDL2 作为音频输出 |
| Devices | ⏳ 开发中 | 95% | Kirk 支持商业游戏解码 |
| 图形后端 | ⏳ 开发中 | 90% | 支持 GE 所有渲染模式，130+ FPS，通过 LightGL 实现 |
| HLE 模块 | ⏳ 开发中 | 48% | 能够运行商业游戏 |
| 跨平台 | ⏳ 开发中 | ?% | 核心可编译为 Windows、Linux 和 Mac |
| UI 界面 | 🚫 计划中 | 0% | 核心功能稳定后开始编写 |


## 技术规格
- **环境**：.NET 8.0
- **依赖项**：无特殊依赖
- **性能**：针对现代系统进行了优化，同时保持对旧硬件的兼容性

## 致谢
本项目建立在先前 PSP 模拟器工作的基础上。特别感谢该领域的先驱者：
- pspplayer on Http://code.google.com/p/pspplayer
- cspspemu on https://github.com/soywiz-archive/cspspemu

## 免责声明
这是一个 alpha 版本，主要用于技术评估和教育目的。并非所有游戏都兼容，且应预期存在图形/音频问题。随着开发的继续，将定期更新和改进。
</details>

## Overview
A lightweight PSP emulator written entirely in C#. Currently in alpha stage, this project demonstrates significant progress in core emulation while still facing limitations in game compatibility.
## Current Status
The core emulation engine has achieved relative stability, capable of running at a consistent 60 FPS on an i3-3215u processor. Performance metrics are impressive:
- CPU Usage: ~20-28%
- Memory Usage: ~400MB (2x Internal Resolution, max texture cache 300, no texture scaling)
However, please note that **only a limited number of games are currently playable**, and many titles exhibit significant graphics and audio issues. This alpha release is primarily intended for:
- Technical enthusiasts wanting to explore emulator implementation
- Developers interested in the inner workings of PSP emulation
- Those willing to test and provide feedback on early-stage functionality

## Development Progress

| Module | Status | Progress | Description |
| :--- | :---: | :---: | :--- |
| CPU | ✅ Completed | 100% | Dynamic recompiler using AST optimization, approx 12x faster than original CPU |
| GE | ⏳ In Progress | 90% | BBOX, BJUMP, SIGNAL instructions not yet implemented |
| ME | ⏳ In Progress | 70% | VAG, ATRAC3, ATRAC3+, and AAC decoding completed; implemented via LightCodec |
| AUDIO | ✅ Completed | 100% | Audio output via SDL2 |
| Devices | ⏳ In Progress | 95% | Kirk supports commercial game decoding |
| Graphics Backend | ⏳ In Progress | 90% | Supports all GE rendering modes, 130+ FPS, implemented via LightGL |
| HLE Modules | ⏳ In Progress | 48% | Capable of running commercial games |
| Cross-Platform | ⏳ In Progress | ?% | Core compiles on Windows, Linux, and Mac |
| UI | 🚫 Planned | 0% | Development to begin after core features are stable |

## Screenshots

### Figure 1: Alpha UI Interface
<img src="https://github.com/user-attachments/assets/9d47f5b8-0338-443b-a591-67305e91c0e1" />

### Figure 2: Playing Guilty Gear XX Accent Core Plus
![Guilty Gear Gameplay](https://github.com/user-attachments/assets/c6914d4f-62c3-4b61-a67d-69d0bc680097)

### Figure 3: Running Castlevania - The Dracula X Chronicles (Title Screen)
![Castlevania Title](https://github.com/user-attachments/assets/0d707833-22e3-480f-8fe7-1751e12d330a)

### Figure 4: Running Castlevania - The Dracula X Chronicles (In-Game)
![Castlevania Gameplay](https://github.com/user-attachments/assets/ed22a0c8-2d8d-4698-8c1f-543399c8c323)

### Figure 5: Running PSPDemo/lights.prx
![Lights Demo](https://github.com/user-attachments/assets/c435575f-419f-4214-abc5-721e94bb2079)

## Technical Specifications
- **Environment**: .NET 8.0
- **Dependencies**: None required
- **Performance**: Optimized for modern systems while maintaining compatibility with older hardware

## Acknowledgments
This project builds upon the foundations laid by previous PSP emulation efforts. Special thanks to the pioneers in this field:
- pspplayer on Http://code.google.com/p/pspplayer
- cspspemu on https://github.com/soywiz-archive/cspspemu

## Disclaimer
This is an alpha release intended for technical evaluation and educational purposes. Not all games are compatible, and graphical/audio issues should be expected. Regular updates and improvements are planned as development continues.
