# HANDOFF.md — SpeakerGrillePro

> 历史进展见 `docs/LOG.md`；项目约定与模块地图见 `AGENTS.md`。最新 commit：`cea4642`

## 任务目标

维护 Windows x64 的 SOLIDWORKS 2025 插件 **SpeakerGrillePro**：按 8 种阵列样式在选定平面/面上批量生成
喇叭孔并自动贯穿切除。v27.4（3Dconnexion 安全安装版）已定版、实机验收通过。仓库根 = 项目根（扁平）；公开仓库 <https://github.com/zhubingzuo/SpeakerGrillePro>。

## 测试命令

**1. 编译验证** — `build.bat`（仓库根；Git Bash 需 `MSYS_NO_PATHCONV=1`；等价 MSBuild 命令见 `build.ps1`）。
判据：exit 0 且 `bin\SpeakerGrillePro.dll` 更新；`warning MSB3644` 为已知无害警告。

**2. 孔型几何回归**（改了孔阵算法后必跑；17 组用例，不需 SOLIDWORKS）

```bat
build.bat && tools\verify\verify_patterns.bat     :: 非交互用 verify_patterns.ps1
```

判据：exit 0，且输出末段出现 `VERDICT: PASS` 与 `SELF-TEST OK 3/3`。覆盖 8 孔型 × 圆/矩形区域的边界与
镜像对称、跳孔对称、最小肉厚；**不覆盖**真实 Face 过滤与切除（见 `Harness.cs` 头部注释）。

**3. 实机验收**（发版前人工）：关闭 SOLIDWORKS → 管理员运行 `一键安装.bat` → 从平常快捷方式启动 →
确认无弹窗 / 喇叭按钮 / 3Dconnexion 正常 → 建草图点生成孔 → 异常查 `bin\SpeakerGrillePro_runtime.log`。

## 本次会话完成及验证结果

- **仓库重建**：全新历史强制推送替换原 v24；根扁平化；`src/*.snk` 私钥与 `bin/*.dll` Interop 不入库；
  取回 LICENSE；恢复 `docs/LOG.md` 惯例。
- **去硬编码**：多磁盘 + 注册表探测 SOLIDWORKS 与 Interop，`-SolidWorksPath` / `SPEAKERGRILLE_SW_EXE` 可覆盖；
  **恢复强名称密钥自动生成**（clone 后仍能一键安装）；`build.ps1` 缺目标包时自动 `FrameworkPathOverride`。
- **修复 4 个 `.bat` 的 UTF-8 BOM**（自 v24 起存在）：cmd 按 GBK 把 `@echo off` 解成 `锘緻echo`，安装首行报错并回显全部命令。
- **新增 `tools/verify/`** 几何回归工具（含判定器自检与"零孔必失败"反向用例）。
- **验证结果**：编译 exit 0；回归 17 组 exit 0（0 越界 / 0 对称违规，自检 3/3，零孔假插件 exit 1）；
  实机 `Registration verification: OK`，日志 `CONNECT_OK`、`FACE_FILTER 319/319`、`CUT_OK`。

## 下一步 TODO

- [ ] 确认 SpaceMouse / 3Dconnexion 单独可用（v27.4 的核心目的，日志无法体现）。
- [ ] 补 `InsideConfiguredRegion` 等纯函数的参数化边界用例。
- [ ] 可选：合并 `one_click_install.ps1` 与 `build.ps1` 重复的 Interop 探测。

## 当前的坑

1. **勿用“首顶点角”判断多边形朝向**：顶点 45° 表示**边水平/垂直 = 轴对齐正方形**（方形孔，正确），
   顶点 0° 才是菱形。`ShapeMode==3 ? Math.PI/4 : 0` 曾被误读为“3/4 朝向互换”；`tools/verify` 现已
   直接分类形状，实测 `square(axis-aligned)` / `diamond(45 deg)` 与 UI 名称一致，**无需改动**。
2. **插件内部以米为单位**（`Mm()` 除以 1000）。外部判定器或脚本必须换算，否则边界判定恒真（曾因此假通过）。
3. **`.bat` 不得带 BOM 且须纯 ASCII；`.cs` / `.csproj` / `.ps1` 必须保留 BOM**（`AGENTS.md` 约定 8）。
4. `one_click_install.ps1` 与 `build.ps1` 的 Interop 探测重复（改动需同步）；本机无 4.x 目标包，须保留 MSBuild 回退 / `FrameworkPathOverride`。

## 下次恢复需打开的关键文件

- `AGENTS.md` — 约定、模块地图、8 种孔型、红线（GUID / 编码 / 入库）
- `src/SpeakerGrillePro.cs` — `SwAddin`(L18) / `CreateGrille`(L272) / 边界与区域判定(L791–1374) / `GrilleDialog`(L1470)
- `tools/verify/Harness.cs` — 几何回归判定器（头部注释写明覆盖范围与要点）
- `one_click_install.ps1` — 安装器，v27.4 安全规则所在
- `docs/LOG.md` — 会话历史存档
