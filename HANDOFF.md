# HANDOFF.md — SpeakerGrillePro

> 最近更新：发布到 GitHub（强制替换 v24 历史）+ 仓库根扁平化
> 最新 commit：`c962615` — 项目初始化

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
（任意 `SOLIDWORKS 20xx` 年份），并会依次尝试多个 MSBuild——VS Build Tools 的 MSBuild 会把缺少
.NET 4.0 目标包当成硬错误 `MSB3644`，脚本会自动回退到 .NET Framework 自带的 MSBuild 并成功编译。
备用方案仍为 `build_manual.ps1`（手动粘贴 Interop 路径）。

### 2. 实机验收（需人工）

1. 关闭 SOLIDWORKS。
2. 运行 `一键安装.bat`（管理员权限），安装过程按提示操作；安装器**不会**自动启动 SOLIDWORKS。
3. 从平常的快捷方式启动 SOLIDWORKS。
4. 确认工具栏出现蓝色喇叭"生成喇叭孔"按钮，且**启动无弹窗**。
5. 建二维草图点 → 选中点 → 点按钮 → 选孔型与参数 → 生成。
6. 复查：孔未越出区域边界、左右/上下镜像对称、含"同心声波"时肉厚 ≥ 设定值。
7. 若异常，查看 `bin\SpeakerGrillePro_runtime.log`。

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
  - `build.ps1`：Interop 探测扩展至全部固定磁盘 + 任意年份注册表；MSBuild 候选改为依次尝试。
    已实测 exit 0：VS MSBuild 失败后自动回退到 `Framework64\v4.0.30319\MSBuild.exe` 成功编译。
- 编译链路已验证可用；**v27.4 尚未做安装后的实机功能验收**。

## 下一步 TODO

- [ ] 关闭 SOLIDWORKS 后跑一次 `一键安装.bat`，完成 v27.4 实机验收（重点：3Dconnexion/SpaceMouse 不再失效、启动无弹窗、喇叭图标正常）。
- [ ] 实机验证 8 种孔型的边界与对称性，特别是第 7 种同心声波的最小肉厚约束是否生效。
- [ ] 本机以管理员跑一次 `一键安装.bat`，验证改动后的完整安装链路（脚本前 4 步已单独验证，
      第 5 步 RegAsm 注册需管理员权限，尚未在本轮实执）。
- [ ] 评估是否把 `csproj` 的 `TargetFrameworkVersion` 由 `v4.0` 改为 `v4.8`：4.0 目标包已不随新版
      开发工具分发，是 `MSB3644` 的根因（当前靠 MSBuild 回退绕过）。
- [ ] 是否恢复原仓库的 `docs\LOG.md`（会话历史存档）惯例：原始内容已归档在 `_old_repo_archive\docs\LOG.md`，目前**未**纳入仓库。
- [ ] 若后续需要版本迭代，建议在 README.md 中继续沿用“版本号 + 改动说明”的写法。

## 关键背景（避免踩坑）

- Add-in GUID `7A88B123-7C5D-4B8C-9E2B-7E7314B42650`、CommandGroup ID `48522` 不可随意改动。
- 语法上限 C# 5 / .NET Framework 4.0 / x64 / 强名称签名。
- 安装器不得自动启动或强杀 SOLIDWORKS，也不得清理其他插件的注册项——这是 v27.4 的全部要点。
- **公开仓库不得包含 `src\*.snk` 与 `bin\*.dll`**（安全/授权红线，详见 `AGENTS.md`「版本控制与发布」）。
- 详细约定见 `AGENTS.md`。
