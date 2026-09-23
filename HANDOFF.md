# HANDOFF.md — SpeakerGrillePro

> 最近更新：8 种孔型边界/对称性自动抽查完成 + 恢复 docs/LOG.md（2026-09-23）
> 最新 commit：`dc5574a` — docs: 记录 v27.4 实机验收完成

## 任务目标

维护并迭代 Windows x64 平台的 SOLIDWORKS 2025 插件 **SpeakerGrillePro**，在用户选定的平面/面上
按 8 种阵列样式批量生成喇叭孔并自动贯穿切除。

当前阶段目标：**v27.4 已定版（3Dconnexion / SpaceMouse 安全安装版）**，功能稳定，等待实机验证与新需求。

## 测试命令

本仓库**没有单元测试工程**（SOLIDWORKS 插件依赖宿主进程，无法脱离 SOLIDWORKS 做自动化测试），
因此"测试"= **编译验证 + 实机验收**。

### 1. 编译验证（本机已验证通过）

在**仓库根目录**（即本文件所在目录）下：

```powershell
"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe" src\SpeakerGrillePro.csproj `
  -t:Rebuild -p:Configuration=Release -p:Platform=AnyCPU `
  "-p:SldWorksInterop=D:\Program Files\SOLIDWORKS Corp\SOLIDWORKS\api\redist\SolidWorks.Interop.sldworks.dll" `
  "-p:SwConstInterop=D:\Program Files\SOLIDWORKS Corp\SOLIDWORKS\api\redist\SolidWorks.Interop.swconst.dll" `
  "-p:SwPublishedInterop=D:\Program Files\SOLIDWORKS Corp\SOLIDWORKS\api\redist\SolidWorks.Interop.swpublished.dll" `
  -v:minimal
```

- 通过判据：退出码 `0`，且 `bin\SpeakerGrillePro.dll` 被更新。
- 已知无害警告：`warning MSB3644`（未找到 .NETFramework v4.0 目标包，回退到 GAC 引用程序集），可忽略。
- 在 Git Bash 中执行时需加 `MSYS_NO_PATHCONV=1`，否则 `/t:`、`/p:` 会被 MSYS 改写成路径。

`build.bat` / `build.ps1` 现已可用（本机 exit 0）：Interop 探测已扩展到全部固定磁盘 + 注册表
（任意 `SOLIDWORKS 20xx` 年份）。构建前检测 .NET 4.0 目标包，本机缺失，因此自动追加
`FrameworkPathOverride=C:\WINDOWS\Microsoft.NET\Framework64\v4.0.30319`，使 VS Build Tools 的
MSBuild v18 第一次尝试即编译成功（不再需要退到老 MSBuild）；产物目标框架仍为 v4.0。
若 `FrameworkPathOverride` 也无能为力，仍会依次尝试其他 MSBuild 候选。备用方案：`build_manual.ps1`。

### 2. 实机验收（需人工）

1. 关闭 SOLIDWORKS。
2. 运行 `一键安装.bat`（管理员权限），安装过程按提示操作；安装器**不会**自动启动 SOLIDWORKS。
3. 从平常的快捷方式启动 SOLIDWORKS。
4. 确认工具栏出现蓝色喇叭"生成喇叭孔"按钮，且**启动无弹窗**。
5. 建二维草图点 → 选中点 → 点按钮 → 选孔型与参数 → 生成。
6. 复查：孔未越出区域边界、左右/上下镜像对称、含"同心声波"时肉厚 ≥ 设定值。
7. 若异常，查看 `bin\SpeakerGrillePro_runtime.log`。

### 3. 孔型几何回归抽查（不需 SOLIDWORKS）

自包含：自动编译验证器并跑完 17 组用例。

```bat
tools\verify\verify_patterns.bat                                        :: 双击，结尾 pause
powershell -NoProfile -ExecutionPolicy Bypass -File tools\verify\verify_patterns.ps1   :: 非交互
```

前提：先跑过 `build.bat` 或 `一键安装.bat`（需要 `bin\SpeakerGrillePro.dll` 与三个 Interop DLL 就位）。

- 覆盖：8 种孔型 × 圆角矩形/圆形区域 + `EdgeSkipPercent=15` 跳孔对称探测 + 验证器自检 = 17 组。
- 判定：配置区域边界（每个孔用**自身半径**）、关于 x=0 / y=0 / 双轴的镜像对称；
  圆孔类另报最小孔间肉厚（孔型 6 应 ≥ 设定值）。
- 退出码：`0` 全部通过；`1` 有违规或某次没生成任何孔；`2` 前置条件缺失；`3` 验证器编译失败。
- **不覆盖**：真实 Face / 裁剪开口的过滤（桩面恒答“点在面上”）、切除是否成功、SOLIDWORKS 内渲染效果。

文件：`tools/verify/Harness.cs`（判定器）、`verify_patterns.ps1`（编译+运行）、`verify_patterns.bat`（入口）、
`obj/`（编译产物，已忽略）。

关键实现细节（重做时容易踩坑，都已踩过）：

- `Face2` 与 `Entity` 是**互不继承**的接口，插件里 `(Entity)face` 靠运行时同时实现两者才成立；
  透明代理必须声明 `interface IFakeFace : Face2, Entity { }`，否则强转 `InvalidCastException`。
- 插件内部以**米**为单位（`Mm()` 除以 1000），独立判定器必须同样换算，否则边界判定恒真（假通过）。
- **自检只能认“边界判定器报了违规”**，不能把“任何失败”当作自检通过（否则零孔场景会假报 OK）。
- **反向用例**：用不产生任何孔的假插件（`_verify\FakeAddin.dll`）跑一次，必须 exit 1；
  否则说明“没生成孔”会被当成通过。
- 切除步骤在桩下必然失败，但异常发生在**孔全部写入之后**，捕获后仍保留已记录的孔集。
- 每次运行会向 `bin\SpeakerGrillePro_runtime.log` 追加诊断（该文件已忽略）。

## 当前状态

- **仓库根 = 项目根**（扁平结构）。本地路径：
  `K:\BaiduSyncdisk\C#\solidwork喇叭孔\SpeakerGrillePro_v27.4_3DconnexionSafe\SpeakerGrillePro_v27.4\`
- 已发布到 GitHub：`origin` → <https://github.com/zhubingzuo/SpeakerGrillePro>（public，主分支 `main`）。
  本次以**全新历史强制推送**替换原 v24 仓库：原 4 笔提交已不在 `main` 上；
  原 `docs/LOG.md`、`SpeakerGrillePro_V24_Project_Handoff.md`、旧 README/HANDOFF 已归档到本地
  `K:\BaiduSyncdisk\C#\solidwork喇叭孔\_old_repo_archive\`（不入库）。
- **不入库、仅本地保留**：`src\SpeakerGrillePro.snk`（强名称私钥）与 `bin\*.dll`（17 个 SolidWorks Interop
  引用 + 构建产物）。公开仓库不重分发专有 Interop 程序集，新克隆的机器从自己的 SOLIDWORKS 安装目录取。
- `bin\SpeakerGrillePro.dll` 已由编译验证重新生成（构建产物，已忽略）。
- `README.md` 已改写为**版本无关**框架（`# SpeakerGrillePro`，标题不再限定版本号，版本历史集中在文末），
  功能/参数/默认值/校验规则均对照源码 `GrilleDialog` 与 `one_click_install.ps1` 校正；
  GitHub 仓库描述已由 `7 patterns` 更新为 `8 patterns`。
- 本机 SOLIDWORKS 2025（`33.5.0.0053`）已安装于 `D:\Program Files\SOLIDWORKS Corp\SOLIDWORKS\`。
- **安装器与构建脚本已去硬编码**（本轮）：
  - `one_click_install.ps1`：多磁盘 + 注册表自动探测 SOLIDWORKS；未找到时给出可操作提示（exit 10）；
    支持 `-SolidWorksPath <exe 或目录>` 与环境变量 `SPEAKERGRILLE_SW_EXE` 覆盖；
    **恢复了强名称密钥自动生成**（缺 `src\SpeakerGrillePro.snk` 时用 CAPI `AT_SIGNATURE` 生成）。
    已实测：探测函数能正确找到本机 D 盘 SOLIDWORKS 与三个 Interop DLL；生成的 .snk 为 1172 字节
    （与出厂密钥同格式），csc 接受并签出强名称程序集。
  - `build.ps1`：Interop 探测扩展至全部固定磁盘 + 任意年份注册表；构建前预检 .NET 4.0 目标包，
    缺失时自动追加 `FrameworkPathOverride`（**不改目标框架**），并保留 MSBuild 候选依次尝试。
    已实测：正常构建 exit 0 且首次尝试即成功；故意破坏源码时正确报错并 exit 3（不再重复刷屏）。
- **实机验收已完成（2026-09-23）**：
  - 安装：用户以管理员运行 `一键安装.bat`，自动探测命中 D 盘 SOLIDWORKS（`33.5.0.0053`），csc 编译成功，
    RegAsm 注销/注册成功，`Registration verification: OK`，退出码 0；重跑后批处理输出已完全干净（无 BOM 报错、无命令回显）。
  - 功能：用户确认在 SOLIDWORKS 中使用正常。`bin\SpeakerGrillePro_runtime.log` 证据：
    `AddCommandManager ENTER` → `CommandManager creation OK` → `CONNECT_OK`（**静默启动，未弹窗**），
    随后 `FACE_FILTER candidates=319, kept=319, rejected=0` 与 `CUT_OK active-sketch-featurecut3: SpeakerGrille_Cut`
    （成功生成并切除一个 319 孔的阵列，边界过滤无剔除）。
- **8 种孔型几何回归抽查完成（2026-09-23）**：已并入仓库为 `tools/verify/`（一键运行）。
  17 组用例（8 孔型 × 圆角矩形/圆形区域，另含 `EdgeSkipPercent=15` 跳孔对称探测）结果均为
  **0 边界违规、0 镜像对称违规**；验证器自检 3/3 命中，反向用例（零孔假插件）exit 1。
  同心声波（孔型 6）实测最小孔间肉厚 1.228 / 1.243 mm ≥ 设定的 1.20 mm，v27.3 的硬约束成立。
  圆孔类最小肉厚：孔型 0 = 1.000 mm，孔型 2 = 1.100 mm，孔型 7 = 0.815 mm。
- 🔧 4 个 `.bat` 已去掉文件开头的 UTF-8 BOM（本次修复）：cmd.exe 按 GBK 解析批处理，BOM 会把首行
  `@echo off` 变成一个不存在的命令 —— 症状为安装一开始报 `'锘緻echo' 不是内部或外部命令`，
  且整份脚本命令被逐条回显。已实测：带 BOM 复现、无 BOM 干净，真实 `一键安装.bat` / `build.bat` /
  `install_admin.bat` / `uninstall_admin.bat` 均无该错误。该问题自 v24 公开仓库就存在。

## 下一步 TODO

- [x] ~~功能验收~~ → **已完成（2026-09-23）**：用户确认 SOLIDWORKS 中正常使用；运行日志确认静默启动成功、
      成功生成并切除 319 孔阵列。
- [ ] 待补充确认：**3Dconnexion / SpaceMouse 是否仍正常**（这是 v27.4 的全部意义所在，但 08:30 的日志无法体现）；
      以及 8 种孔型的边界/对称性逐个抽查（日志只能证明跑过一种）。
- [x] ~~本机以管理员跑一次 `一键安装.bat` 验证完整安装链路~~ → **已完成**：自动探测、csc 编译、RegAsm
      注销/注册、注册项校验全部通过（`Registration verification: OK`，exit 0）。
- [x] ~~评估是否把 `csproj` 的 `TargetFrameworkVersion` 由 `v4.0` 改为 `v4.8`~~ → **已评估并否决**：
      实测 v4.8 仍报 `MSB3644`（本机 `Reference Assemblies` 下无任何 4.x 目标包），且 `TargetFrameworkVersion`
      只影响 MSBuild 路径、csc 路径不读它。已改用 `FrameworkPathOverride` 绕开，保留 v4.0 作兼容性护栏。
- [x] ~~是否恢复原仓库的 `docs\LOG.md`（会话历史存档）惯例~~ → **已恢复**：`docs/LOG.md`，
      保留原 v24 条目并补全本次全部条目。
- [ ] **孔型 3「方形孔」与孔型 4「菱形孔」的朝向与名称疑似互换**（抽查发现，需你决定）：
      代码为 `ShapeMode == 3 ? Math.PI/4 : 0`，实测首顶点角 孔型 3 = **45°**（视觉为菱形）、
      孔型 4 = **0°**（视觉为方形），但日志标签为 `3 ? "SQUARE" : "DIAMOND"` 与 UI 名称一致。
      属外观/命名问题，**不影响边界与对称性**；修改会改变用户已习惯的观感，故未自行改动。
- [ ] 若后续需要版本迭代，建议在 README.md 中继续沿用“版本号 + 改动说明”的写法。

## 关键背景（避免踩坑）

- Add-in GUID `7A88B123-7C5D-4B8C-9E2B-7E7314B42650`、CommandGroup ID `48522` 不可随意改动。
- 语法上限 C# 5 / .NET Framework 4.0 / x64 / 强名称签名。
- 安装器不得自动启动或强杀 SOLIDWORKS，也不得清理其他插件的注册项——这是 v27.4 的全部要点。
- **公开仓库不得包含 `src\*.snk` 与 `bin\*.dll`**（安全/授权红线，详见 `AGENTS.md`「版本控制与发布」）。
- **编码红线**：`.bat` 必须无 BOM 且纯 ASCII；`.cs` / `.csproj` / `.ps1` 必须保留 BOM（详见 `AGENTS.md` 约定 8）。
- 详细约定见 `AGENTS.md`。
