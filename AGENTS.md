# AGENTS.md — SpeakerGrillePro（SOLIDWORKS 喇叭孔插件）

## 项目简介

SOLIDWORKS 插件（Add-in），在选定的平面/面上按 8 种阵列样式批量生成"喇叭孔"（扬声器出音孔），
并自动执行贯穿切除（Cut-Extrude）。当前版本 **v27.4（3Dconnexion / SpaceMouse 安全安装版）**。

目标运行环境：SOLIDWORKS 2025 SP5，Windows x64。

仓库：<https://github.com/zhubingzuo/SpeakerGrillePro>（public）。
**仓库根 = 项目根**（扁平结构，与 v24 起的历史布局一致），不再使用外层版本号包装目录。

## 技术栈

| 项 | 值 |
| --- | --- |
| 语言 | C# 5（`LangVersion 5`，**不可使用 C# 6+ 语法**） |
| 框架 | .NET Framework 4.0（`TargetFrameworkVersion v4.0`） |
| 平台 | x64（`PlatformTarget x64`，AnyCPU 配置） |
| 强名称 | 是，签名文件 `src/SpeakerGrillePro.snk` |
| 输出 | `bin\SpeakerGrillePro.dll` |
| UI | Windows Forms（`System.Windows.Forms`），全中文界面 |
| SOLIDWORKS 互操作 | COM Interop，RegAsm 注册 |

## 主要模块

单文件实现，主体在 `src/SpeakerGrillePro.cs`（约 86 KB），逻辑分区如下：

| 位置 | 内容 |
| --- | --- |
| `SwAddin`（L18） | `ISwAddin` 实现，插件入口。SOLIDWORKS 回调：`ConnectToSW` / `DisconnectFromSW` |
| `SwAddin.AddCommandManager`（L83） | 创建 CommandManager 选项卡与工具栏按钮，加载 `bin\Icons` 下的喇叭图标 |
| `GetToolbarIconPaths`（L132） | 按 20/32/40/64/96/128 px 六档解析图标路径 |
| `CanGenerateSpeakerGrille` / `GenerateSpeakerGrille`（L158 / L171） | 命令回调：校验选择集、取面与草图中心、弹出参数对话框 |
| `CreateGrille`（L272） | **核心算法**：按孔型生成孔阵列，含渐变、跳孔、镜像对称、边界过滤 |
| 边界过滤族（L791–L907） | `IsCircleFullySupportedByFace` / `IsHexFullySupportedByFace` / `IsRegularPolygonFullySupportedByFace`：逐孔做真实 Face 边界判定 |
| 区域判定族（L1253–L1374） | `InsideConfiguredRegion` / `RegionRho` / `InsideRoundedRect` / `IsProtectedRoundedCorner` / `ShouldSkipSparseSymmetric`：圆角矩形与圆形区域、圆角保护、对称跳孔 |
| 切除族（L1003–L1253） | `FindNewestSketchFeature` / `CreateRobustCut` / `TryCut` 等：多级兜底的贯穿切除 |
| `Logger`（L1377–L1436） | 注册/注销辅助 + 运行日志，日志固定写到 `bin\SpeakerGrillePro_runtime.log` |
| `Hole`（L1436） | 孔的轻量结构（坐标、半径、角度） |
| `GrilleSettings`（L1443） | 全部参数模型 |
| `GrilleDialog`（L1470） | 参数对话框，含孔型选择、区域选择、预设与校验 |

### 8 种孔型（`GrilleDialog` 中的 `ApplyPreset` / `ApplyShapeModeUi`）

1. 圆孔 - 六角错列渐变
2. 蜂窝 - 紧密六边形
3. 圆孔 - 方阵
4. 方形孔
5. 菱形孔
6. 三角孔
7. 圆孔 - 同心声波（含"最小孔间肉厚"约束，默认 1.20 mm，硬下限 0.80 mm）
8. 圆形渐变 - 六角错列连续孔径（推荐；余弦缓动连续渐变，强制不跳孔）

第 1–7 种可选圆角矩形或圆形区域，第 8 种为圆形区域。

## 常用命令

### 编译（本机已验证可用）

```powershell
# 在仓库根目录执行
"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe" src\SpeakerGrillePro.csproj `
  -t:Rebuild -p:Configuration=Release -p:Platform=AnyCPU `
  "-p:SldWorksInterop=D:\Program Files\SOLIDWORKS Corp\SOLIDWORKS\api\redist\SolidWorks.Interop.sldworks.dll" `
  "-p:SwConstInterop=D:\Program Files\SOLIDWORKS Corp\SOLIDWORKS\api\redist\SolidWorks.Interop.swconst.dll" `
  "-p:SwPublishedInterop=D:\Program Files\SOLIDWORKS Corp\SOLIDWORKS\api\redist\SolidWorks.Interop.swpublished.dll" `
  -v:minimal
```

产物：`bin\SpeakerGrillePro.dll`。

> ✅ `build.bat` / `build.ps1` 亦可直接使用（已去硬编码）：Interop 探测覆盖全部固定磁盘与任意
> `SOLIDWORKS 20xx` 注册表年份；构建前会检查 .NET Framework 4.0 目标包，缺失时自动追加
> `FrameworkPathOverride=<运行时目录>`（**目标框架保持 v4.0 不变**），并保留 .NET Framework 自带的
> MSBuild 作为兜底。需要手动指定时用 `build_manual.ps1`。
>
> 安装器侧同理：`one_click_install.ps1` 自动探测 SOLIDWORKS，可用 `-SolidWorksPath <exe|目录>`
> 或环境变量 `SPEAKERGRILLE_SW_EXE` 覆盖；`src\SpeakerGrillePro.snk` 缺失时会自动生成。

### 安装 / 卸载（需管理员权限）

```text
一键安装.bat            # 推荐入口，内部调用 one_click_install.ps1
install_admin.bat       # 直接注册（RegAsm x64）
uninstall_admin.bat     # 反注册
```

安装前必须关闭 SOLIDWORKS，否则注册会失败或插件行为异常。

## 版本控制与发布

- 远程：`origin` → <https://github.com/zhubingzuo/SpeakerGrillePro>，主分支 `main`。
- **绝不提交**以下内容（已写入 `.gitignore`）：
  - `src/*.snk` —— **强名称私钥**。历史上有过因私钥进入公开仓库而专门移除的提交，属安全红线；
    安装器在密钥缺失时会自动生成新的（`AT_SIGNATURE`，格式同 `sn.exe -k`），因此公开仓库不需要它。
  - `bin/` 与 `*.dll` —— 既含构建产物，也含 17 个 `SolidWorks.Interop.*.dll` 专有引用程序集，
    **不要再分发**。它们在本地保留以便编译，新克隆的机器从自己的 SOLIDWORKS 安装目录取得。
  - `*.log`、`install_log.txt`、`regasm_*.txt` —— 运行/注册日志，可能含本机路径与用户名。
- 发布新版本时保持仓库根布局不变，版本历史写在 `README.md` 与 `HANDOFF.md` 中。

## 项目约定

1. **不要修改 Add-in GUID** `7A88B123-7C5D-4B8C-9E2B-7E7314B42650`（见 `src/SpeakerGrillePro.cs` L16）——
   它是注册表登记键，改动会导致旧注册残留、插件重复加载或加载失败。
2. **不要重命名 SOLIDWORKS 回调方法**（`ConnectToSW` / `DisconnectFromSW` / `CanGenerateSpeakerGrille` /
   `GenerateSpeakerGrille`）——它们通过 COM 暴露给 SOLIDWORKS，改名即插件失效。
3. **CommandGroup ID 为 48522**。修改按钮图标后必须清理旧 CommandGroup，否则 SOLIDWORKS 会缓存旧图标。
4. **静默启动**：SOLIDWORKS 启动加载插件时不得弹出模态窗口（含异常路径）。诊断信息写入
   `bin\SpeakerGrillePro_runtime.log`。用户主动点击按钮后的交互提示保留。
5. **安装器不得结束 SOLIDWORKS 进程，也不得自动启动 SOLIDWORKS**（v27.4 的核心修复：避免 SOLIDWORKS
   继承管理员权限导致 3Dconnexion/3DxWare 无法通信）。
6. **只注册/清理本插件自己的 GUID 与注册项**，绝不删除其他或旧插件的 Add-in 注册项。
7. 保持 **C# 5 语法**；新增引用请沿用 `$(SldWorksInterop)` 等可注入属性，**不要在 csproj 里写死绝对路径**。
8. 编码规则（改错会导致很难看出的故障，务必遵守）：
   - `.cs` / `.csproj` / `.ps1`：**UTF-8 带 BOM**。中文字面量与 PowerShell 5.1 的读取都依赖它，编辑时勿去 BOM。
   - `.bat`：**UTF-8 无 BOM，且保持纯 ASCII**。cmd.exe 按 GBK 解析批处理，文件开头的 BOM 会被解码成
     `锘緻` 并吞掉行首的 `@`，使 `@echo off` 变成一个不存在的命令 —— 症状是安装一开始就报
     `'锘緻echo' 不是内部或外部命令`，且因为 `@echo off` 未生效，整份脚本的命令会被逐条回显。
   - `.md` / `.gitignore` / `LICENSE`：UTF-8 无 BOM。
9. 孔阵列算法改动后需自查：左右/上下镜像对称、孔不越出区域边界、满足最小肉厚。
10. **不要把 `TargetFrameworkVersion` 改成 v4.8**（已评估并否决，理由见 `HANDOFF.md`）：改它既修不了
    `MSB3644`（缺的是目标包），又只影响 MSBuild 路径、会让 csc 路径产出与之一致性不同的元数据。
