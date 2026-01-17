<h2> A Lightweight PlayStation Portable emulator Fully Developed in C# </h2>

#### At present, the core can only run a small number of games, and there is still a considerable distance to go before releasing a version available for end users. 
<details>
<summary><h3>中文进度表</h3></summary>
  
| 模块 | 状态 | 进度 | 说明 |
| :--- | :---: | :---: | :--- |
| CPU | ✅ 已完成 | 100% | 使用AST优化的动态重编译，速度约为原CPU的12倍 |
| GE | ⏳ 开发中 | 90% | BBOX, BJUMP,SIGNAL 指令未完成 |
| ME | ⏳ 开发中 | 70% | Vag,AT3,AT3+,AAC 完成，使用 LightCodec 实现 |
| AUDIO | ✅ 已完成 | 100% | 使用SDL2作为音频输出 |
| Devices | ⏳ 开发中 | 95% | Kirk 已支持商业游戏解码|
| 图形后端 | ⏳ 开发中 | 90% | 支持所有GE渲染模式 130+ FPS，使用 LightGL 实现 |
| HLE模块 | ⏳ 开发中 | 48% | 已能支持商业游戏运行 |
| 跨平台 | ⏳ 开发中 | ?% | 核心已可编译为Win,Linux,Mac |
| UI 界面 | 🚫 计划中 | 0% | 核心功能稳定后开始编写 |

</details>

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

_(Figure 1: runing Castlevania - The Dracula X Chronicles (USA) on title )_  
<img width="962" height="576" alt="捕获" src="https://github.com/user-attachments/assets/0d707833-22e3-480f-8fe7-1751e12d330a" />

_(Figure 2: runing Castlevania - The Dracula X Chronicles (USA) in game )_  
<img width="962" height="576" alt="捕获1" src="https://github.com/user-attachments/assets/ed22a0c8-2d8d-4698-8c1f-543399c8c323" />

_(Figure 3: playing Guilty Gear XX Accent Core Plus)_  
<img width="962" height="576" alt="捕获03" src="https://github.com/user-attachments/assets/e2fb67a0-1944-4805-aabd-847da2f8acab" />

_(Figure 4: runing PSPDemo/lights.prx)_  
<img width="962" height="576" alt="捕获" src="https://github.com/user-attachments/assets/b80b1d4d-26ed-4664-8abb-0bf42749d7f8" />

#### Compilation 
- Environment: .NET 8.0
- Dependencies: None

#### This project is based on the following projects. Special thanks to the pioneers for their contributions.
- pspplayer on Http://code.google.com/p/pspplayer
- cspspemu on https://github.com/soywiz-archive/cspspemu
