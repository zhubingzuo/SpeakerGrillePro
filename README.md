# SpeakerGrillePro

> SOLIDWORKS 喇叭孔自动生成插件 · Speaker Grille Auto-Generation Add-in for SOLIDWORKS

一个面向 **SOLIDWORKS** 的喇叭孔（扬声器网罩孔）自动生成插件。用户在目标模型平面上放置一个**草图点**即可精确定位孔区中心，插件自动生成多种孔型并自动完成 **Cut-Extrude** 切除。

- 🎯 **8 种孔型**：六角错列渐变 / 真蜂窝 / 圆孔方阵 / 方孔 / 菱形孔 / 三角孔 / 同心声波 / 圆形连续渐变
- 📍 **草图点精确定位**孔区中心（配合 SOLIDWORKS 尺寸约束）
- 📏 **固定毫米尺寸**，全部参数手动输入，稳定可控
- ⚙️ **自动切除 + 多级 Fallback**，不依赖手动操作
- 🖱️ **一键安装**，不依赖 Visual Studio / MSBuild（用系统自带 `csc.exe`）
- 🔇 **静默启动**：SOLIDWORKS 加载插件时不弹任何窗口
- 🖐️ **安装器不自动启动 SOLIDWORKS**，避免权限继承导致 3Dconnexion / SpaceMouse 失效
- 📋 **完整日志**：运行时日志 + 安装日志，问题可溯源

---

## ✨ 功能特性

| 特性 | 说明 |
| --- | --- |
| 多孔型统一插件 | 8 种孔型在一个界面中选择，切换孔型自动套用推荐参数 |
| 草图点定位 | 选中草图点 → 插件以该点作为孔区中心生成 |
| 双区域形状 | 圆角矩形区域（宽 × 高 + 圆角）或圆形区域（按直径） |
| 固定尺寸模式 | 区域尺寸 / 孔尺寸 / 节距 / 圆角 / 渐变比例全部手动输入 mm 值 |
| 连续孔径渐变 | 孔径按半径余弦缓动平滑变化，无三级色带突变 |
| Face 边界过滤 | 逐孔按真实半径判定，自动剔除越出面边界的孔，避免切除失败 |
| 默认不跳孔 | 跳孔率默认 0%，保证网格完整、对称 |
| 对称性保证 | 每圈孔数取 4 的倍数 + 交替半步相位，水平/垂直双轴严格镜像 |
| 最小肉厚保护 | 同心声波模式把"最小孔间肉厚"作为硬约束，含相邻环之间 |
| 自动切除 | 保持活动草图直接切除，失败时自动尝试多级备用方案 |
| 样式联动 UI | 根据所选孔型自动启用/停用无关参数，避免误填 |
| Strong Name 签名 | 干净注册，无 RegAsm 警告 |
| 一键安装 | 解压 → 双击 `一键安装.bat`，自动完成编译 / 注册 / 自检 |
| 完整日志 | `bin\SpeakerGrillePro_runtime.log` + `install_log.txt` |

---

## 🕳️ 支持的 8 种孔型

| # | 孔型 | 适用区域 | 说明 |
| --- | --- | --- | --- |
| 1 | 圆孔 - 六角错列渐变（经典） | 圆角矩形 / 圆形 | 经典音箱喇叭孔，三角错列，视觉密度高，工程上最稳定 |
| 2 | 蜂窝 - 紧密六边形（真蜂窝） | 圆角矩形 / 圆形 | 真正的紧密蜂窝晶格（flat-top honeycomb），用「蜂窝筋宽」控制相邻六边形间的实体筋 |
| 3 | 圆孔 - 方阵极简 | 圆角矩形 / 圆形 | 规则矩形阵列，适合极简设计、规则电子产品 |
| 4 | 方形孔 - 错列阵列 | 圆角矩形 / 圆形 | 工业风、科技感、模块化外观 |
| 5 | 菱形孔 - 错列阵列 | 圆角矩形 / 圆形 | 方孔旋转 45°，有方向感的纹理、装饰性前面板 |
| 6 | 三角孔 - 交错阵列 | 圆角矩形 / 圆形 | 更激进的造型语言，视觉识别度高 |
| 7 | 圆孔 - 同心声波 | 圆角矩形 / 圆形 | 围绕中心产生声波感布局，含**最小孔间肉厚**硬约束 |
| 8 | 圆形渐变 - 六角错列连续孔径（推荐） | **仅圆形** | 圆形区域内六角错列 + 中心到外围连续孔径渐变，强制不跳孔 |

### 两种特殊孔型

**7 · 圆孔 - 同心声波**

- 连续平滑孔径过渡，消除明显的分级孔径分界。
- 每个完整圆环优先使用 **4 的倍数**孔数 + 交替半步相位，关于水平轴和垂直轴严格镜像。
- 边界判定按每个圆孔的实际半径计算，外围轮廓更整齐，同时保留实体边缘。
- **最小孔间肉厚**同时约束同一环相邻孔与相邻环之间的实体连接，不允许为了增加孔数而突破安全值。

**8 · 圆形渐变 - 六角错列连续孔径（推荐）**

严格实现：

- 圆形生成区域（自动锁定，区域选择器停用）
- 六角 / 三角错列圆孔网格
- 中心到外围**连续**孔径渐变（余弦缓动，中心与外围变化都柔和）
- 默认且**强制不跳孔**（代码层强制跳孔率为 0）
- 完整圆孔不会越过给定的圆形区域边界
- 仍然进行真实 Face 边界过滤

在该模式下：`中心孔特征尺寸` = 圆心处最大孔径，`外围孔特征尺寸` = 圆周附近最小孔径；
`中间孔特征尺寸`、两级区域比例、跳孔参数自动停用。要求 **中心孔尺寸 ≥ 外围孔尺寸**。

### 孔特征尺寸的定义

| 孔型 | "特征尺寸"的含义 |
| --- | --- |
| 圆孔 | 直径 |
| 蜂窝 / 方孔 / 菱形孔 | 对边尺寸 |
| 三角孔 | 外接圆直径 |

---

## 🖥️ 环境要求

| 项目 | 要求 |
| --- | --- |
| SOLIDWORKS | 2025 SP5 |
| .NET Framework | 4.x（使用系统自带 `csc.exe` 编译，**不依赖 Visual Studio / MSBuild**） |
| 系统 | Windows 10/11 x64，安装需要管理员权限 |
| 注册 | 64 位 `RegAsm.exe` + Strong Name 签名 |

> 🔍 **SOLIDWORKS 位置自动探测**：安装器与 `build.ps1` 不再写死盘符，会依次尝试注册表
> （`HKLM\SOFTWARE\SolidWorks\SOLIDWORKS 20xx\Setup`，含 32/64 位视图）与各固定磁盘下的
> `Program Files\SOLIDWORKS Corp` 等常见位置。若自动探测失败，可显式指定：
>
> ```bat
> powershell -ExecutionPolicy Bypass -File one_click_install.ps1 -SolidWorksPath "X:\...\SLDWORKS.exe"
> ```
>
> 也支持环境变量 `SPEAKERGRILLE_SW_EXE`，或直接指向安装目录（而非 exe）。

---

## 🚀 快速开始（一键安装）

```text
1. 克隆本仓库（或解压发布包）
2. 关闭 SOLIDWORKS（安装器检测到它在运行会直接中止，不会强行结束进程）
3. 右键「一键安装.bat」→ 以管理员身份运行
4. 等待自动完成
5. 关闭安装窗口，然后从你平常的快捷方式正常启动 SOLIDWORKS
```

安装器实际执行 5 步：

```text
[1/5] 确认 SOLIDWORKS 路径，并确认 SOLIDWORKS 未在运行
[2/5] 定位 SolidWorks.Interop.sldworks / swconst / swpublished 程序集
[3/5] 定位 .NET Framework csc.exe
[4/5] 用 csc.exe 编译（C# 5 / .NET Framework 4.x / x64 / Strong Name），并复制工具栏图标
[5/5] RegAsm /codebase 注册 COM，校验 CLSID 键，写入 AddInsStartup 启动项
```

安装日志保存在 `install_log.txt`。

> 🔒 **v27.4 安全约定**：安装器**不会**自动启动 SOLIDWORKS，也**不会**强杀正在运行的进程，
> 并且**只注册 / 清理本插件自己的 GUID**，绝不碰其他或旧插件的注册项
> （3Dconnexion / 3DxWare 使用同一片 `SolidWorks\AddIns` 注册区域，这是之前的故障根因）。

如果安装后工具栏没出现按钮：SOLIDWORKS → 工具 → 插件 → 勾选「喇叭孔生成器」。

### 卸载

```text
右键 uninstall_admin.bat → 以管理员身份运行
```

只反注册本插件（`7A88B123-...`）的 COM 键与启动项。

---

## 📖 使用方法

### 草图点定位（推荐流程）

```text
1. 在目标模型平面上创建二维草图
2. 在草图中放置一个草图点
3. 用尺寸约束把草图点定位到需要的位置
4. 选中该草图点
5. 点击 SOLIDWORKS 工具栏中的喇叭图标「生成喇叭孔」
6. 选择孔型与区域形状，输入参数，点击「生成」
7. 插件以该草图点为孔区中心自动生成并切除
```

### 参数说明

| 参数 | 默认值 | 单位 | 说明 |
| --- | --- | --- | --- |
| 区域宽度 | 80 | mm | 圆角矩形区域宽度 |
| 区域高度 | 44 | mm | 圆角矩形区域高度 |
| 外轮廓圆角（矩形区域） | 12 | mm | 圆角矩形外轮廓圆角 |
| 圆形区域直径 | 60 | mm | 圆形区域直径 |
| 孔中心距 / 基准节距 | 3.0 | mm | 相邻孔中心间距 |
| 蜂窝筋宽（蜂窝模式） | 0.55 | mm | 仅蜂窝模式启用，建议 0.4~0.8 mm，至少 0.2 mm |
| 同心声波最小孔间肉厚 | 1.20 | mm | 仅同心声波模式启用，硬下限 0.8 mm，上限 5.0 mm |
| 中心孔特征尺寸 | 2.0 | mm | 中心区域孔径 |
| 中间孔特征尺寸 | 1.5 | mm | 第 8 种模式停用 |
| 外围孔特征尺寸 | 1.0 | mm | 外围区域孔径 |
| 中心区域比例 | 0.42 | 0~1 | 第 8 种模式停用 |
| 中间区域比例 | 0.72 | 0~1 | 第 8 种模式停用 |
| 开始跳孔比例 | 0.94 | 0~1 | 第 8 种模式停用 |
| 边缘跳孔率 | 0 | % | 第 8 种模式强制为 0 |
| 水平偏移 | 0 | mm | 相对草图点的 X 偏移 |
| 垂直偏移 | 0 | mm | 相对草图点的 Y 偏移 |

**参数校验规则**

- 圆角矩形区域：宽度、高度必须 > 0；圆形区域：直径必须 > 0。
- 孔尺寸必须 > 0；除第 8 种外要求 **中心 ≥ 中间 ≥ 外围**；第 8 种要求 **中心 ≥ 外围**。
- 基准节距必须 > 0；蜂窝模式以外的节距必须 > 0.4 mm。
- 除第 8 种外要求 **0 < 中心比例 < 中间比例 < 1**，且 开始跳孔比例 ≥ 中间比例、≤ 1。
- 同心声波最小孔间肉厚须在 0.8~5.0 mm。

### 推荐参数

**第 8 种「圆形渐变 - 六角错列连续孔径」（推荐首测）**

```text
孔型：圆形渐变 - 六角错列连续孔径（推荐）
圆形区域直径：60 mm
孔中心距：3.0 mm
中心孔特征尺寸：2.2 mm
外围孔特征尺寸：1.0 mm
水平偏移：0 mm
垂直偏移：0 mm
```

圆形区域更大时可把中心距提到 3.2~3.5 mm；想更细腻可降到 2.6~2.9 mm，但孔数会明显增加。

**经典圆孔（圆角矩形区域）**

```text
孔型：圆孔 - 六角错列渐变（经典）
区域宽度：80 mm    区域高度：44 mm    外轮廓圆角：14 mm
孔中心距：3.0 mm
中心孔：2.2 mm    中间孔：1.6 mm    外围孔：1.05 mm
中心区域比例：0.42    中间区域比例：0.72
开始跳孔比例：0.94    边缘跳孔率：0%
```

> 💡 设计经验：不建议依靠"缺孔"实现视觉渐变。用 **Ø2.2 → Ø1.6 → Ø1.05** 的孔径变化产生渐隐，效果更好、更稳定。

**真蜂窝**

```text
孔型：蜂窝 - 紧密六边形（真蜂窝）
蜂窝六边形尺寸：约 2.4 mm    蜂窝筋宽：0.55 mm
```

标准蜂窝视觉：中心 = 中间 = 外围尺寸（如 2.4 / 2.4 / 2.4）。
渐变蜂窝：中心 2.4 / 中间 2.0 / 外围 1.6。

**同心声波**

```text
孔型：圆孔 - 同心声波
孔中心距：3.2 mm
中心孔：2.0 mm    外围孔：1.0 mm
同心声波最小孔间肉厚：1.20 mm    （塑料件通常建议 1.0~1.5 mm 或更大）
```

切换孔型时插件会自动套用该孔型的推荐参数，可以此为基础微调。

---

## 🗂️ 目录结构

```text
SpeakerGrillePro                        ← 仓库根 = 项目根
│
├─ src/
│  ├─ SpeakerGrillePro.cs               # 插件主源码（Add-in + 算法 + UI + 全部孔型）
│  ├─ SpeakerGrillePro.csproj           # MSBuild 工程（构建脚本亦可用 csc.exe）
│  ├─ SpeakerGrillePro.snk              # Strong Name 私钥（仅本地，不入库）
│  ├─ Icons/speaker_{20,32,40,64,96,128}.png   # 工具栏喇叭图标 6 档
│  └─ Properties/AssemblyInfo.cs        # 程序集信息
│
├─ one_click_install.ps1                # 一键安装核心脚本（编译 / 注册 / 自检）
├─ 一键安装.bat                         # 双击入口（自动提权）
├─ install_admin.bat                    # 仅注册已编译 DLL（管理员）
├─ uninstall_admin.bat                  # 卸载（管理员）
├─ build.bat / build.ps1                # 仅编译辅助脚本
├─ build_manual.ps1                     # 手动编译脚本（交互式指定 Interop 路径）
│
├─ bin/                                 # 编译输出（不入库）
│  ├─ SpeakerGrillePro.dll              #   插件本体（构建产物）
│  ├─ SpeakerGrillePro_runtime.log      #   运行时日志
│  └─ SolidWorks.Interop.*.dll          #   SOLIDWORKS 引用程序集（本机复制，不入库）
│
├─ AGENTS.md                            # 项目约定与模块地图（给 AI/协作者）
├─ HANDOFF.md                           # 交接文档：目标 / 测试命令 / 状态 / TODO
├─ CLAUDE.md                            # → @AGENTS.md
└─ LICENSE                              # MIT
```

> 说明：`bin\` 与 `src\*.snk` 已在 `.gitignore` 中排除。
> SOLIDWORKS Interop DLL 属于 SOLIDWORKS 自带运行库（专有程序集，**不再分发**），
> 由安装脚本从本机 SOLIDWORKS 安装目录取得；`src\SpeakerGrillePro.snk` 是私钥，
> 公开仓库不应包含，安装器在缺失时会自动生成新的签名密钥。

---

## 🛠️ 开发与编译

### 编译要求

- **C# 5** / .NET Framework 4.x 语法兼容
- 不使用 `nameof`、字符串插值、expression-bodied members 等新语法
- 平台：x64
- Strong Name 签名（`/keyfile:src\SpeakerGrillePro.snk`）

### 用 csc.exe 手动编译（安装器采用的方式）

```bat
"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:library /platform:x64 /optimize+ /langversion:5 ^
  /out:bin\SpeakerGrillePro.dll /keyfile:src\SpeakerGrillePro.snk ^
  /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll ^
  /reference:"<SOLIDWORKS>\api\redist\SolidWorks.Interop.sldworks.dll" ^
  /reference:"<SOLIDWORKS>\api\redist\SolidWorks.Interop.swconst.dll" ^
  /reference:"<SOLIDWORKS>\api\redist\SolidWorks.Interop.swpublished.dll" ^
  src\SpeakerGrillePro.cs src\Properties\AssemblyInfo.cs
```

### 用 MSBuild 编译

```bat
"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe" src\SpeakerGrillePro.csproj ^
  -t:Rebuild -p:Configuration=Release -p:Platform=AnyCPU ^
  "-p:SldWorksInterop=<SOLIDWORKS>\api\redist\SolidWorks.Interop.sldworks.dll" ^
  "-p:SwConstInterop=<SOLIDWORKS>\api\redist\SolidWorks.Interop.swconst.dll" ^
  "-p:SwPublishedInterop=<SOLIDWORKS>\api\redist\SolidWorks.Interop.swpublished.dll" ^
  -v:minimal
```

或直接 `build.bat` / `build.ps1`。两者的行为：

- 在全部固定磁盘与注册表中探测 Interop，不再依赖盘符或写死的版本年份。
- 构建前检测 **.NET Framework 4.0 目标包（引用程序集）** 是否存在。若缺失（新版开发工具已不再随附），
  自动追加 `FrameworkPathOverride=<运行时目录>`，让引用解析改用运行时程序集（老版 MSBuild 本来就是
  这么回退的）。**产物目标框架仍是 v4.0**，仅编译期参考来源不同。
- MSBuild 候选依次尝试：VS Build Tools 的 MSBuild → .NET Framework 自带的 MSBuild（兜底）。

若仍失败，用 `build_manual.ps1` 手动指定 Interop 路径。

---

## 🔍 日志与排障

所有问题请优先提供日志，不要盲猜：

| 日志 | 位置 | 用途 |
| --- | --- | --- |
| 安装日志 | `install_log.txt` | 编译 / 注册 / 自检全过程 |
| 运行时日志 | `bin\SpeakerGrillePro_runtime.log` | `ConnectToSW`、草图点定位、`FACE_FILTER`、`CIRCLE_CREATE`、`CUT_ATTEMPT` / `CUT_RESULT` 等 |

典型日志片段：

```text
ConnectToSW ENTER
SetAddinCallbackInfo2 returned True
CommandManager creation OK
CONNECT_OK

Selected sketch point center model XYZ = ...
FACE_FILTER candidates=... kept=... rejected=...
CIRCLE_CREATE requested=... created=...
ACTIVE_SKETCH_AFTER_CREATE manager=OK, model=OK
CUT_ATTEMPT ...
CUT_RESULT ...
CUT_OK ...
```

运行时日志也会记录静默启动阶段的异常（这些异常**不弹窗**，只写日志）。

---

## 📌 版本历史

### v27.4（当前）
- 🔧 **修复**：一键安装后 3Dconnexion / SpaceMouse 在 SOLIDWORKS 中失效。原因是旧安装器以管理员权限运行后直接启动 SOLIDWORKS，SOLIDWORKS 继承管理员权限，而 3DxWare 驱动仍是普通用户权限，Windows 权限隔离阻止两者通信。
- 安装器**不再自动启动 SOLIDWORKS**，安装后请从平常的快捷方式正常启动。
- 安装前检测到 SOLIDWORKS 正在运行会提示先关闭，**不会强制结束进程**。
- **不再删除**任何其他 / 旧插件的 SOLIDWORKS Add-in 注册项，只注册本插件自己的 GUID。
- 🔧 **安装器**：自动探测 SOLIDWORKS 安装位置（多磁盘 + 注册表 + 可显式指定），不再硬编码盘符；
  缺少强名称密钥时自动生成（公开仓库不携带私钥，让 clone 后一键安装可用）。
- 🔧 **构建脚本**：`build.ps1` 的 Interop 探测扩展到全部固定磁盘；缺少 .NET 4.0 目标包时
  自动启用 `FrameworkPathOverride`（目标框架保持 v4.0），并保留 .NET Framework 自带 MSBuild 兜底。

### v27.3
- 工具栏「生成喇叭孔」按钮改为蓝色喇叭图标，提供 20/32/40/64/96/128 px 六档 PNG，适配高 DPI 缩放；CommandGroup ID 更新并在加载时清理旧组，避免缓存旧图标。
- 「圆孔 - 同心声波」改为连续平滑孔径过渡，消除明显分级；每个完整圆环优先使用 4 的倍数孔数，保持双轴对称。
- 新增「同心声波最小孔间肉厚」参数（默认 1.20 mm，硬下限 0.80 mm），同时约束同环相邻孔与相邻环之间的实体连接。
- 修复圆角矩形区域中「同心声波」左右孔数不同、边缘孔不镜像的问题（孔数强制 4 的倍数 + 镜像位置联合剔除）。

### v27
- **静默启动**：SOLIDWORKS 自动加载插件时不再弹出"插件已成功加载"提示窗口；CommandManager 创建异常或 `ConnectToSW` 异常也不再弹模态窗口，诊断写入 `bin\SpeakerGrillePro_runtime.log`。
- 新增第 8 种孔型「圆形渐变 - 六角错列连续孔径（推荐）」，采用余弦缓动连续渐变，强制不跳孔。

### v25
- 稳定基线，7 种孔型。

### v24（首个公开版）
- ✅ SOLIDWORKS 2025 SP5、C# Add-in + Strong Name + 一键安装
- ✅ 7 种孔型统一插件、草图点精确定位 + 固定毫米尺寸
- ✅ 中心 / 中间 / 外围三级尺寸渐变、Face 边界过滤 + 默认不跳孔
- ✅ 自动 Cut-Extrude + 多级切除 Fallback、runtime log + install log
- 🔧 **移除** v23 的「自动适配模型」功能（简单 BoundingBox 百分比适配不可靠）

### v23 及更早
- 增加 / 验证了草图点定位、`FACE_FILTER`、自动切除等核心能力
- 修复了坐标转换错误（孔挤成一列 / 大圈套小圈）、缺孔、`FeatureCut` 状态等问题

---

## 🗺️ 后续规划

1. **生成前预览**：草图预览确认后再 Cut，避免反复撤销。
2. **性能优化**：500+ / 1000+ 孔时批量创建 Sketch Segment、暂停刷新、减少 Select 调用。
3. **自动化测试**：插件依赖 SOLIDWORKS 宿主进程，无法脱离宿主编译期单测；目前以“编译验证 + 实机验收”代替。
4. **目标框架：已评估，决定继续保留 v4.0，不改 v4.8。** 实测把 `TargetFrameworkVersion` 改成 `v4.8`
   仍会报 `MSB3644`（`Reference Assemblies` 下一个 4.x 目标包都没有）——缺的是目标包，不是版本号；
   而且 `TargetFrameworkVersion` 只影响 MSBuild 路径，一键安装器走的 `csc.exe` 并不读它，改了会让
   两条构建路径产出不一致的元数据。现用 `FrameworkPathOverride` 绕开，并把 v4.0 当作最低公共分母的
   兼容性护栏（防止误用只在新版框架里存在的 API）。除非同时改造 csc 构建路径，否则不动。

已完成（原规划）：样式参数动态 UI（按孔型启用/停用参数）、更严格的 Face 边界检测、
安装脚本与构建脚本去硬编码（多磁盘 / 注册表探测）、安装器自动生成强名称密钥、
构建脚本在缺目标包时自动使用 `FrameworkPathOverride`。

---

## 📄 许可证

MIT License — 详见 [LICENSE](LICENSE)。

---

## 🙏 致谢与说明

本插件为个人 SOLIDWORKS 二次开发工具，仅供学习与个人使用。SOLIDWORKS 为 Dassault Systèmes 公司的商标，本插件与其无任何隶属关系。
