# 会话历史存档

| 日期 | commit | 摘要 |
| --- | --- | --- |
| 2026-08-23 | 8f53b51 | 发布 SpeakerGrillePro V24 到 GitHub（新建公共仓库、README/LICENSE/.gitignore）；安全修复（强名称私钥移出仓库、安装器自动生成新密钥）；删除 seatclean/seatcleanen 仓库；编译验证通过 |
| 2026-09-23 | c962615 | 仓库重建为 v27.4：在项目目录重新 `git init` 得到全新历史，强制推送替换原 v24 的 4 笔提交；仓库根扁平化（与旧布局一致）；`src/*.snk` 与 `bin/*.dll` 不入库；取回并保留 LICENSE；新增 AGENTS.md / CLAUDE.md / HANDOFF.md；恢复 `docs/LOG.md`（本文件） |
| 2026-09-23 | 1c8f830 | README 改写为版本无关框架（标题不再限定版本号，版本历史集中文末），功能/参数/校验规则对照源码校正；GitHub 仓库描述 `7 patterns` → `8 patterns` |
| 2026-09-23 | 90b2836 | 安装器与构建脚本去硬编码：多磁盘 + 注册表自动探测 SOLIDWORKS 与 Interop；`-SolidWorksPath` / `SPEAKERGRILLE_SW_EXE` 覆盖；恢复强名称密钥自动生成（CAPI AT_SIGNATURE），使公开仓库 clone 后仍可一键安装 |
| 2026-09-23 | 29d5f91 | `build.ps1` 在缺少 .NET Framework 4.0 目标包时自动启用 `FrameworkPathOverride`（目标框架保持 v4.0），MSBuild 候选依次尝试；评估后**否决**把目标框架改为 v4.8 |
| 2026-09-23 | 0f5a08a | 修复 4 个 `.bat` 的 UTF-8 BOM：cmd.exe 按 GBK 解析会把 `@echo off` 变成 `锘緻echo`，导致安装一开始报错且全脚本命令被逐条回显（该缺陷自 v24 起存在） |
| 2026-09-23 | dc5574a | 记录 v27.4 实机验收完成：安装链路 5 步全过（`Registration verification: OK`），SOLIDWORKS 内正常使用；运行时日志证据 `CONNECT_OK`（静默启动）、`FACE_FILTER 319/319`、`CUT_OK` |
| 2026-09-23 | 6e6fa91 | 8 种孔型边界/对称性自动化抽查：15 组用例（8 孔型 × 圆角矩形/圆形区域，另含跳孔对称探测）全部 0 边界违规、0 镜像对称违规；验证器自检通过。方法为用真实 `bin\SpeakerGrillePro.dll` 的 `CreateGrille` 配合 RealProxy 桩化 SOLIDWORKS API 并记录每个孔的坐标与半径 |
| 2026-09-23 | 2a015a6 | 几何回归验证器并入仓库（`tools/verify/`）：`Harness.cs` + `verify_patterns.ps1` + `.bat`，一键运行且不需 SOLIDWORKS；修正自检逻辑（只认边界判定器报出的违规，避免零孔场景假报通过），补反向用例（零孔假插件必须 exit 1） |

## 更正记录（2026-09-23）

**此前记录的「孔型 3/4 朝向与名称疑似互换」是错误结论，已作废 —— 代码本来就是对的，无任何改动。**

- 误判来源：按 `Harness.cs` 输出的“首顶点角”判断，把“顶点 45°”误当成“形状旋转了 45°”。
- 正确几何：顶点在 45°/135°/225°/315° 时坐标为 `(±0.707r, ±0.707r)`，相邻顶点连线的边为
  **水平/垂直** → **轴对齐正方形**，正是「方形孔」；顶点在 0°/90°/180°/270° 时才是菱形。
- 实测（验证器已改为直接分类形状，不再输出有歧义的顶点角）：孔型 3 = `square(axis-aligned)`，
  孔型 4 = `diamond(45 deg)`，孔型 1 = `hexagon(flat-top)`，孔型 5 = `triangle` —— 均与 UI 名称一致。
- `src/SpeakerGrillePro.cs` **未作任何改动**。

2026-09-23 | cea4642 | HANDOFF.md 重写为精简交接文档（≤60 行）；本次会话全部进展已逐条存于上表，无信息丢失
