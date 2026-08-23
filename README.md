# SpeakerGrillePro

> SOLIDWORKS 喇叭孔自动生成插件 · Speaker Grille Auto-Generation Add-in for SOLIDWORKS

一个面向 **SOLIDWORKS** 的喇叭孔（扬声器网罩孔）自动生成插件。用户在目标模型平面上放置一个**草图点**即可精确定位孔区中心，插件自动生成多种孔型并自动完成 **Cut-Extrude** 切除。

- 🎯 7 种孔型：圆孔六角错列 / 真蜂窝 / 圆孔方阵 / 方孔 / 菱形孔 / 三角孔 / 同心声波孔
- 📍 草图点精确定位孔区中心（配合 SOLIDWORKS 尺寸约束）
- 📏 固定毫米尺寸，全部参数手动输入，稳定可控
- ⚙️ 自动切除 + 多级 Fallback，稳定不依赖手动操作
- 🖱️ 一键安装，不依赖 Visual Studio / MSBuild
- 📋 完整运行时日志与安装日志，问题可溯源

---

## ✨ 功能特性

| 特性 | 说明 |
| --- | --- |
| 多孔型统一插件 | 圆形、蜂窝、方形、菱形、三角形、声波孔在一个界面中选择 |
| 草图点定位 | 选中草图点 → 插件以该点作为孔区中心生成 |
| 固定尺寸模式 | 孔区宽度 / 高度 / 孔尺寸 / 节距 / 圆角 / 渐变比例全部手动输入 mm 值 |
| 三级尺寸渐变 | 中心 / 中间 / 外围三档特征尺寸，实现由密到疏的渐隐视觉 |
| Face 边界过滤 | 自动剔除落在模型表面之外的孔，避免切除失败 |
| 默认不跳孔 | 跳孔率默认 0%，保证网格完整、对称、无孤立空洞 |
| 自动切除 | 保持活动草图直接 `FeatureCut3`，失败时自动尝试多级备用方案 |
| Strong Name 签名 | 干净注册，无 RegAsm 警告 |
| 一键安装 | 解压 → 双击 `一键安装.bat`，自动完成编译 / 注册 / 启动 / 自检 |
| 完整日志 | `bin\SpeakerGrillePro_runtime.log` + `install_log.txt` |

---

## 🕳️ 支持的 7 种孔型

### 1. 圆孔六角错列（经典音箱风格）
```
○   ○   ○   ○
  ○   ○   ○
○   ○   ○   ○
  ○   ○   ○
```
经典音箱喇叭孔，六角 / 三角错列，视觉密度高，工程上最稳定。

### 2. 真蜂窝六边形孔
不是"六边形孔简单排成阵列"，而是**真正的紧密蜂窝晶格**（flat-top honeycomb axial lattice），通过 `蜂窝筋宽` 控制相邻六边形之间保留的实体材料。适合硬朗、工业风设计。

### 3. 圆孔方阵
```
○ ○ ○ ○
○ ○ ○ ○
○ ○ ○ ○
```
规则矩形阵列，适合极简设计、规则电子产品。

### 4. 方形孔
规则方孔阵列，适合工业风、科技感、模块化外观。

### 5. 菱形孔
方孔旋转 45° 形成的菱形视觉效果，适合有方向感的纹理、装饰性前面板。

### 6. 三角孔
三角形闭合孔轮廓，更激进的造型语言，视觉识别度高。

### 7. 同心声波孔
围绕中心点产生声波感布局，适合智能音箱、智能屏等产品。

---

## 🖥️ 环境要求

| 项目 | 要求 |
| --- | --- |
| SOLIDWORKS | 2025 SP5（`D:\Program Files\SOLIDWORKS Corp\SOLIDWORKS\SLDWORKS.exe`） |
| .NET Framework | 4.x（使用系统自带 `csc.exe` 编译，**不依赖 Visual Studio / MSBuild**） |
| 系统 | Windows 10/11 x64，需要管理员权限运行安装 |
| 注册 | 64 位 `RegAsm.exe` + Strong Name 签名 |

---

## 🚀 快速开始（一键安装）

```text
1. 解压 / 克隆本仓库
2. 右键单击「一键安装.bat」→ 以管理员身份运行
3. 等待自动完成：检测 SOLIDWORKS → 定位 Interop DLL → csc.exe 编译 → RegAsm 注册 → 写入注册表 → 启动 SOLIDWORKS → 检测 ConnectToSW
4. 安装日志保存在 install_log.txt
```

安装器自动完成：

```text
1. 管理员权限
2. 检测 SOLIDWORKS 安装路径
3. 查找 SolidWorks.Interop.sldworks.dll / swconst.dll / swpublished.dll
4. 使用本机 .NET Framework csc.exe 编译（C# 5 / x64 / Strong Name）
5. 复制运行依赖到 bin\
6. RegAsm /codebase 注册 COM
7. 写入 SOLIDWORKS Addins 注册表
8. 验证 Registration（CLSID + Addins 键 + 启动项）
9. 启动 SOLIDWORKS 并检测 ConnectToSW
10. 生成 install_log.txt
```

---

## 📖 使用方法

### 草图点定位（推荐流程）

```text
1. 在目标模型平面上创建二维草图
2. 在草图中放置一个草图点
3. 用尺寸约束把草图点定位到需要的位置
4. 选中该草图点
5. 点击 SOLIDWORKS 工具栏中的「喇叭孔生成器 Pro」
6. 输入参数，点击生成
7. 插件以该草图点为孔区中心自动生成并切除
```

### 参数说明

| 参数 | 默认值 | 说明 |
| --- | --- | --- |
| 区域宽度 | 80 mm | 孔区宽度 |
| 区域高度 | 44 mm | 孔区高度 |
| 外轮廓圆角 | 12 mm | 孔区外轮廓圆角（参考值） |
| 孔中心距 / 基准节距 | 3.0 mm | 相邻孔中心间距 |
| 蜂窝筋宽 | 0.55 mm | 蜂窝模式下相邻六边形间的实体筋宽 |
| 中心孔特征尺寸 | 2.0 mm | 中心区域孔径 |
| 中间孔特征尺寸 | 1.5 mm | 中间区域孔径 |
| 外围孔特征尺寸 | 1.0 mm | 外围区域孔径 |
| 中心区域比例 | 0.42 | 中心渐变区占比（0~1） |
| 中间区域比例 | 0.72 | 中间渐变区占比（0~1） |
| 开始跳孔比例 | 0.94 | 距中心多远开始允许跳孔（0~1） |
| 边缘跳孔率 | 0% | 边缘跳孔百分比（默认 0，不跳孔） |
| X / Y 偏移 | 0 mm | 相对草图点的偏移量 |

### 推荐参数示例（圆孔模式）

```text
孔区宽度：80 mm    孔区高度：44 mm    圆角：14 mm
中心距：3.0 mm
中心孔：2.2 mm    中间孔：1.6 mm    外围孔：1.05 mm
中心区域比例：0.42    中间区域比例：0.72
开始跳孔比例：0.94    边缘跳孔率：0%
```

> 💡 设计经验：不建议依靠"缺孔"实现视觉渐变。用 **Ø2.2 → Ø1.6 → Ø1.05** 的孔径变化产生渐隐，效果更好、更稳定。

### 蜂窝模式推荐参数

```text
蜂窝六边形尺寸：约 2.4 mm
蜂窝筋宽：0.55 mm
```

标准蜂窝视觉：中心 = 中间 = 外围尺寸（如 2.4 / 2.4 / 2.4）。
渐变蜂窝：中心 2.4 / 中间 2.0 / 外围 1.6。

---

## 🗂️ 目录结构

```text
SpeakerGrillePro
│
├─ src/
│  ├─ SpeakerGrillePro.cs          # 插件主源码（Add-in + UI + 全部模式）
│  ├─ SpeakerGrillePro.csproj      # VS 工程文件（参考用，实际用 csc.exe 编译）
│  ├─ SpeakerGrillePro.snk         # Strong Name 签名密钥
│  └─ Properties/AssemblyInfo.cs   # 程序集信息
│
├─ one_click_install.ps1           # 一键安装核心脚本（编译 / 注册 / 自检）
├─ 一键安装.bat                    # 双击入口（自动提权）
├─ install_admin.bat               # 仅注册已编译 DLL（管理员）
├─ uninstall_admin.bat             # 卸载（管理员）
├─ build.bat / build.ps1           # 仅编译辅助脚本
├─ build_manual.ps1                # 手动编译脚本
│
├─ bin/                            # 编译输出（构建时生成，不入库）
│  ├─ SpeakerGrillePro.dll
│  └─ SpeakerGrillePro_runtime.log
│
└─ SpeakerGrillePro_V24_Project_Handoff.md   # V24 开发交接文档（含历史坑与后续规划）
```

> 说明：`bin\` 中的 SOLIDWORKS Interop DLL 属于 SOLIDWORKS 自带运行库，由安装脚本自动从本机 SOLIDWORKS 安装目录复制，无需提交到仓库（已在 `.gitignore` 中排除）。

---

## 🛠️ 开发与编译

### 编译要求

- **C# 5** / .NET Framework 4.x 语法兼容
- 不使用 `nameof`、字符串插值、expression-bodied members 等新语法
- 平台：x64
- Strong Name 签名（`/keyfile:src\SpeakerGrillePro.snk`）

### 手动编译

```bat
build.bat
```

或直接使用本机编译器：

```bat
"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:library /platform:x64 /optimize+ /langversion:5 ^
  /out:bin\SpeakerGrillePro.dll /keyfile:src\SpeakerGrillePro.snk ^
  /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll ^
  /reference:"D:\Program Files\SOLIDWORKS Corp\SOLIDWORKS\api\redist\SolidWorks.Interop.sldworks.dll" ^
  /reference:"D:\Program Files\SOLIDWORKS Corp\SOLIDWORKS\api\redist\SolidWorks.Interop.swconst.dll" ^
  /reference:"D:\Program Files\SOLIDWORKS Corp\SOLIDWORKS\api\redist\SolidWorks.Interop.swpublished.dll" ^
  src\SpeakerGrillePro.cs src\Properties\AssemblyInfo.cs
```

### 卸载

```text
右键 uninstall_admin.bat → 以管理员身份运行
```

---

## 🔍 日志与排障

所有问题请优先提供日志，不要盲猜：

| 日志 | 位置 | 用途 |
| --- | --- | --- |
| 安装日志 | `install_log.txt` | 编译 / 注册 / 自检全过程 |
| 运行时日志 | `bin\SpeakerGrillePro_runtime.log` | ConnectToSW、草图点定位、FACE_FILTER、CIRCLE_CREATE、CUT_ATTEMPT / CUT_RESULT 等 |

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

---

## 📌 版本历史

### V24（当前稳定版）
- ✅ SOLIDWORKS 2025 SP5
- ✅ C# Add-in + Strong Name + 一键安装
- ✅ 7 种孔型统一插件（圆孔六角错列 / 真蜂窝 / 圆孔方阵 / 方孔 / 菱形孔 / 三角孔 / 同心声波孔）
- ✅ 草图点精确定位 + 固定毫米尺寸
- ✅ 中心 / 中间 / 外围三级尺寸渐变
- ✅ Face 边界过滤 + 默认不跳孔
- ✅ 自动 Cut-Extrude + 多级切除 Fallback
- ✅ runtime log + install log
- 🔧 **移除** V23 的「自动适配模型」功能（简单 BoundingBox 百分比适配不可靠，已彻底删除）

### V23 及更早
- 增加 / 验证了草图点定位、FACE_FILTER、自动切除等核心能力
- 修复了坐标转换错误（孔挤成一列 / 大圈套小圈）、缺孔、FeatureCut 状态等问题

---

## 🗺️ 后续规划（V25 方向）

1. **参数预设**：高端精细圆孔 / 标准音箱圆孔 / 大孔音响风 / 标准蜂窝 / 细密蜂窝 / 工业方孔等一键模板
2. **生成前预览**：草图预览确认后再 Cut，避免反复撤销
3. **样式参数动态 UI**：根据孔型只显示相关参数（如蜂窝模式才显示筋宽）
4. **更严格的边界检测**：基于 Face Loop / Trim Curve，保证整个孔轮廓都在有效面内
5. **性能优化**：500+ / 1000+ 孔时批量 Sketch Segment、暂停刷新、减少 Select

---

## 📄 许可证

MIT License — 详见 [LICENSE](LICENSE)。

---

## 🙏 致谢与说明

本插件为个人 SOLIDWORKS 二次开发工具，仅供学习与个人使用。SOLIDWORKS 为 Dassault Systèmes 公司的商标，本插件与其无任何隶属关系。
