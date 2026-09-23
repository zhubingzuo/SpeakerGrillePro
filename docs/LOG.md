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
| 2026-09-23 | <待回填> | 几何回归验证器并入仓库（`tools/verify/`）：`Harness.cs` + `verify_patterns.ps1` + `.bat`，一键运行且不需 SOLIDWORKS；修正自检逻辑（只认边界判定器报出的违规，避免零孔场景假报通过），补反向用例（零孔假插件必须 exit 1） |

## 待确认事项（2026-09-23 抽查发现）

**孔型 3「方形孔」与孔型 4「菱形孔」的实际朝向疑似互换。**

- 代码：`double angle = s.ShapeMode == 3 ? Math.PI / 4.0 : 0.0;`
- 实测首顶点角：孔型 3 = **45°**（旋转 45°，视觉上是菱形），孔型 4 = **0°**（轴对齐，视觉上是方形）
- 而源码内日志标签为 `ShapeMode == 3 ? "SQUARE" : "DIAMOND"`，与 UI 名称一致
- 结论：UI 名称与绘制朝向不一致，表现为两个孔型的观感互换；属外观/命名问题，不影响边界与对称性。
  是否调整需用户决定（改朝向会改变用户已习惯的观感）。
