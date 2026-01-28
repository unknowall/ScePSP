# A Lightweight PlayStation Portable Emulator Fully Developed in C#
[![GPLv2 License](https://img.shields.io/badge/License-GPL%20v2-blue.svg)](https://www.gnu.org/licenses/old-licenses/gpl-2.0.html)
[![Latest Release](https://img.shields.io/github/v/release/unknowall/ScePSP?label=Release&color=orange)](https://github.com/unknowall/ScePSP/releases)
[![Build Status](https://img.shields.io/badge/Build-Passing-brightgreen.svg)](https://github.com/unknowall/ScePSP)
<details>
<summary><h3>中文说明</h3></summary>
  
这是一个完全使用 C# 开发的轻量级高性能 PlayStation Portable 模拟器。目前处于 alpha 阶段，仍存在许多兼容性、图形、音频问题。

## 功能说明
 - HLE模拟，无需固件/BIOS
 - 内部分辨率调整，支持4K原生输出
 - 材质增强，支持多倍率的 xBR,Jinc,Neighbor,Lanczos 
 - 自适应手柄支持，无需配置即插即用
 - 金手指支持，支持CWC格式金手指
 - 内存编辑器，可搜索、编辑内存

## 当前状态
 - 高效的动态重编译，游戏重编译为C#的IL运行, 对CPU及内存的占用极低。
 - 模拟器能够在 i3-3215u 处理器上以稳定的 60 FPS 运行。
 - 需要注意的是，**目前许多游戏存在图形和音频问题** 此 alpha 版本主要面向：
   - 想要探索模拟器实现的技术爱好者
   - 对 PSP 模拟器感兴趣的开发者
   - 愿意进行Alpha测试的用户

<details>
<summary><h3>开发进度</h3></summary>
## 开发进度

| 模块 | 状态 | 进度 | 说明 |
| :--- | :---: | :---: | :--- |
| CPU | ✅ 已完成 | 100% | 使用 AST 优化的动态重编译，速度约为原 CPU 的 12 倍 |
| GE | ⏳ 开发中 | 90% | BBOX、BJUMP、SIGNAL 指令尚未完成 |
| ME | ⏳ 开发中 | 90% | H264, VAG、ATRAC3、ATRAC3+ 和 AAC 解码已完成，通过 LightCodec 实现 |
| AUDIO | ✅ 已完成 | 100% | 使用 SDL2 作为音频输出 |
| Devices | ⏳ 开发中 | 95% | Kirk 支持商业游戏解码 |
| 图形后端 | ⏳ 开发中 | 90% | 支持 GE 所有渲染模式，130+ FPS，通过 LightGL 实现 |
| HLE 模块 | ⏳ 开发中 | 48% | 能够运行商业游戏 |
| 跨平台 | ⏳ 开发中 | ?% | 核心可编译为 Windows、Linux 和 Mac |
| UI 界面 | ⏳ 开发中 | ?% | 用于Alpha测试的基础UI |
</details>

## 技术规格
- **环境**：.NET 8.0
- **依赖项**：无特殊依赖
- **性能**：针对现代系统进行了优化，同时保持对旧硬件的兼容性

## 致谢
本项目建立在先前 PSP 模拟器工作的基础上。特别感谢该领域的先驱者：
- pspplayer on Http://code.google.com/p/pspplayer
- cspspemu on https://github.com/soywiz-archive/cspspemu

</details>

This is a lightweight and high-performance PlayStation Portable emulator developed entirely in C#. It is currently in the alpha stage and still has many compatibility, graphics, and audio issues.

## Feature Overview
- HLE emulation with no firmware/BIOS required
- Adjustable internal resolution supporting native 4K output
- Texture enhancement with multi-scale xBR, Jinc, Neighbor and Lanczos filters
- Adaptive controller support with plug-and-play functionality and no configuration needed
- Cheat code support compatible with CWCheat (CWC) format
- Memory editor enabling memory search and editing capabilities

## Current Status
- Efficient dynamic recompilation that translates game code to C# IL for execution, resulting in extremely low CPU and memory usage.
- The emulator runs at a stable 60 FPS on an i3-3215u processor.
- It should be noted that **many games currently have graphics and audio issues**. This alpha version is primarily intended for:
  - Technology enthusiasts looking to explore emulator implementation
  - Developers interested in PSP emulation
  - Users willing to participate in alpha testing

<details>
<summary><h3>Development Progress</h3></summary>
## Development Progress

| Module | Status | Progress | Description |
| :--- | :---: | :---: | :--- |
| CPU | ✅ Completed | 100% | Dynamic recompiler using AST optimization, approx 12x faster than original CPU |
| GE | ⏳ In Progress | 90% | BBOX, BJUMP, SIGNAL instructions not yet implemented |
| ME | ⏳ In Progress | 90% | H264, VAG, ATRAC3, ATRAC3+, and AAC decoding completed; implemented via LightCodec |
| AUDIO | ✅ Completed | 100% | Audio output via SDL2 |
| Devices | ⏳ In Progress | 95% | Kirk supports commercial game decoding |
| Graphics Backend | ⏳ In Progress | 90% | Supports all GE rendering modes, 130+ FPS, implemented via LightGL |
| HLE Modules | ⏳ In Progress | 48% | Capable of running commercial games |
| Cross-Platform | ⏳ In Progress | ?% | Core compiles on Windows, Linux, and Mac |
| UI | ⏳ In Progress | ?% | Alpha Version |

</details>
## Screenshots

### Figure 1: Alpha UI Interface
<img width="1113" height="742" alt="捕获" src="https://github.com/user-attachments/assets/62e89526-8f0f-449b-886c-953795fe2bb6" />

### Figure 2: Playing Guilty Gear XX Accent Core Plus
![Guilty Gear Gameplay](https://github.com/user-attachments/assets/c6914d4f-62c3-4b61-a67d-69d0bc680097)

### Figure 3: Running Castlevania - The Dracula X Chronicles (In-Game)
<img width="1113" height="742" alt="捕获02" src="https://github.com/user-attachments/assets/7afd01a7-c324-480c-8fc0-91a3588ecaef" />

### Figure 4: Running PSPDemo/lights.prx
![Lights Demo](https://github.com/user-attachments/assets/c435575f-419f-4214-abc5-721e94bb2079)

## Technical Specifications
- **Environment**: .NET 8.0
- **Dependencies**: None required
- **Performance**: Optimized for modern systems while maintaining compatibility with older hardware

## Acknowledgments
This project builds upon the foundations laid by previous PSP emulation efforts. Special thanks to the pioneers in this field:
- pspplayer on Http://code.google.com/p/pspplayer
- cspspemu on https://github.com/soywiz-archive/cspspemu

