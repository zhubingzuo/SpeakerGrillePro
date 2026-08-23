# SpeakerGrillePro V24 项目介绍与后续开发交接文档

## 1. 项目概述

**SpeakerGrillePro** 是一个面向 **SOLIDWORKS 2025 SP5** 的喇叭孔自动生成插件。

当前稳定版本为：

```text
SpeakerGrillePro v24
```

版本定位：

> 多样式、固定尺寸、草图点定位、自动切除、适合继续扩展的稳定版本。

本项目最初只支持圆形渐变喇叭孔，后续经过多轮迭代，已经发展为一个可以生成多种喇叭孔样式的统一插件。

---

## 2. 当前 V24 的核心目标

V24 的设计目标有四个：

1. **支持多种喇叭孔样式**
2. **用户可以通过一个草图点精确控制喇叭孔区域中心**
3. **所有尺寸采用用户手动输入的固定 mm 参数**
4. **生成后自动完成切除**

V24 已经删除之前 V23 中不稳定的：

```text
自动适配模型
```

功能。

现在不同尺寸的模型，由用户直接输入：

- 孔区宽度
- 孔区高度
- 孔尺寸
- 节距
- 圆角
- 渐变区域比例
- 蜂窝筋宽
- 跳孔参数

这样更加可控，也更稳定。

---

# 3. 当前 SOLIDWORKS 环境

目标环境：

```text
SOLIDWORKS 2025 SP5
```

用户机器上的安装路径：

```text
D:\Program Files\SOLIDWORKS Corp\SOLIDWORKS\SLDWORKS.exe
```

SOLIDWORKS Interop DLL：

```text
D:\Program Files\SOLIDWORKS Corp\SOLIDWORKS\api\redist\SolidWorks.Interop.sldworks.dll
D:\Program Files\SOLIDWORKS Corp\SOLIDWORKS\api\redist\SolidWorks.Interop.swconst.dll
D:\Program Files\SOLIDWORKS Corp\SOLIDWORKS\api\redist\SolidWorks.Interop.swpublished.dll
```

本机使用的 C# 编译器：

```text
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
```

注意：

- 不依赖 Visual Studio
- 不依赖 MSBuild 15+
- 不依赖 .NET Framework 4.8 Targeting Pack
- 源码需保持 **C# 5 兼容**
- 插件通过 **64 位 RegAsm** 注册
- DLL 使用 **Strong Name** 签名
- 插件支持一键安装

---

# 4. V24 的主要功能

## 4.1 多种喇叭孔样式

V24 支持多种孔型，统一在同一个插件中选择。

当前包含：

### 1. 圆孔六角错列

典型效果：

```text
○   ○   ○   ○
  ○   ○   ○
○   ○   ○   ○
  ○   ○   ○
```

特点：

- 经典音箱喇叭孔
- 六角/三角错列
- 视觉密度高
- 工程上最稳定

---

### 2. 真蜂窝六边形孔

不是“六边形孔简单排成阵列”，而是真正的紧密蜂窝晶格。

支持：

```text
蜂窝筋宽
```

用于控制相邻六边形之间保留的实体材料。

特点：

- 更像真正蜂窝结构
- 适合硬朗、工业风设计
- 比普通圆孔更有视觉特征

---

### 3. 圆孔方阵

规则矩形阵列：

```text
○ ○ ○ ○
○ ○ ○ ○
○ ○ ○ ○
```

适合：

- 极简设计
- 规则电子产品
- 参数简单

---

### 4. 方形孔

规则方孔阵列。

适合：

- 工业风
- 科技感
- 模块化外观

---

### 5. 菱形孔

方孔旋转后形成菱形视觉效果。

适合：

- 更有方向感的纹理
- 装饰性较强的前面板

---

### 6. 三角孔

三角形闭合孔轮廓。

适合：

- 更激进的造型语言
- 视觉识别度高

---

### 7. 同心声波孔

围绕用户选择的中心点产生声波感布局。

特点：

- 视觉更有“声场”概念
- 适合智能音箱、智能屏等产品

注意：

V23 中这一模式曾出现固定大尺寸下自动切除失败，V24 已删除自动适配逻辑，但后续仍建议继续增强其几何安全性。

---

# 5. 草图点定位逻辑

这是项目中非常重要的功能。

用户的使用流程：

```text
1. 在目标模型平面上创建二维草图
2. 放置一个草图点
3. 用尺寸约束把草图点定位到需要的位置
4. 选中草图点
5. 点击 SpeakerGrillePro
6. 输入参数
7. 插件以该草图点为喇叭孔区域中心生成
```

这样用户可以通过 SOLIDWORKS 自己的尺寸约束精确控制孔区位置。

---

## 5.1 草图点坐标处理

插件会：

1. 读取当前选中的 `SketchPoint`
2. 获取所属草图
3. 将草图坐标转换到模型坐标
4. 获取草图所在目标平面 / Face
5. 在该目标面建立新的喇叭孔草图

历史上曾经出现过严重坐标错误：

```text
孔全部挤到右下角一列
大圈里面套小圈
```

根因是：

```text
局部草图坐标
→ 又转换成模型 XYZ
→ 再错误传给 CreateCircleByRadius
```

后续版本已经修复。

正确原则：

> 实际创建二维草图实体时，必须使用当前活动草图的局部 X/Y 坐标。

模型坐标转换主要用于：

- 判断目标 Face
- 边界过滤
- 空间位置判断

不要重新用模型 XYZ 直接作为 2D SketchManager 的局部坐标。

---

# 6. 圆孔模式的核心设计逻辑

当前圆孔模式已经经过大量实际验证。

推荐的工程化逻辑：

```text
固定六角网格
+
中心对称
+
三档孔径渐变
+
默认不跳孔
```

推荐参数示例：

```text
孔区宽度：80 mm
孔区高度：44 mm
圆角：14 mm

中心距：3.0 mm

中心孔：2.2 mm
中间孔：1.6 mm
外围孔：1.05 mm

中心区域比例：0.42
中间区域比例：0.72

开始跳孔比例：0.94
边缘跳孔率：0%
```

目前经验：

> 不建议依靠缺孔实现视觉渐变。

更好的效果是：

```text
Ø2.2
→ Ø1.6
→ Ø1.05
```

通过孔径变化产生渐隐。

---

# 7. 跳孔逻辑

早期版本采用随机或哈希式跳孔，出现过：

```text
局部大片没有孔
四个角缺孔
内部孤立缺孔
```

经过 V10、V19、V20 多次修复，现在稳定原则是：

```text
默认边缘跳孔率 = 0%
```

也就是说：

> 默认完全不跳孔。

参数仍然保留：

```text
开始跳孔比例
边缘跳孔率
```

但建议默认：

```text
开始跳孔比例：0.94
边缘跳孔率：0%
```

只有用户明确想要外围渐隐时才设置非 0 跳孔率。

即使以后重新优化跳孔，也必须保证：

- 中心区永不跳孔
- 中间区永不跳孔
- 四个圆角不能出现不对称缺孔
- 主体网格不能出现孤立空洞
- 跳孔必须严格对称
- 不能形成大片连续空白

---

# 8. 蜂窝模式设计逻辑

蜂窝模式经历过一次重要修正。

早期问题：

```text
只是一个一个六角形
不像真正蜂窝结构
```

之后改成：

```text
flat-top honeycomb axial lattice
```

即真正的紧密蜂窝晶格。

核心参数包括：

```text
蜂窝六边形尺寸
蜂窝筋宽
```

其中：

```text
蜂窝筋宽
```

表示相邻六边形孔之间保留的实体材料宽度。

推荐初始值：

```text
蜂窝六边形尺寸：约 2.4 mm
蜂窝筋宽：0.55 mm
```

若想标准蜂窝视觉，建议：

```text
中心尺寸 = 中间尺寸 = 外围尺寸
```

如果想渐变蜂窝，则可以使用：

```text
中心：2.4
中间：2.0
外围：1.6
```

---

# 9. 目标 Face 边界过滤

插件不是简单在一个理想矩形区域里生成孔，而是需要检查孔是否真正位于用户选择的模型表面。

历史上曾经出现：

```text
孔区进入屏幕开口
孔进入模型边界外
FeatureCut 失败
```

因此加入了：

```text
FACE_FILTER
```

日志中会出现：

```text
FACE_FILTER candidates=...
kept=...
rejected=...
```

作用：

- 检查孔中心是否位于真实 Face 内
- 排除超出模型边界的孔
- 避免孔生成到凹槽 / 开口 / 外壳外部

但是：

V23 的“自动适配模型”证明，仅依靠 Face 包围盒估算尺寸并不可靠。

因此 V24 已经：

```text
彻底删除自动适配模型
```

后续不要重新加入简单的 GetBox 百分比式自动适配。

如果以后重新设计自动适配功能，必须基于：

- 实际连续有效 Face 区域
- Trim Loop
- 真实边界距离
- 孔轮廓安全边距

而不是简单 Face Bounding Box。

---

# 10. 自动切除逻辑

插件创建所有草图孔后，会自动执行 Cut-Extrude。

历史上切除失败过很多次。

最终最稳定的流程来自 V18 之后：

```text
选择目标面
→ 进入新草图
→ 创建全部闭合轮廓
→ 保持活动草图
→ 立即 FeatureCut3
```

不要在切除之前：

```text
退出草图
→ 重建
→ 再重新找草图
```

这种方式过去经常导致：

```text
activeSketch=NULL
```

---

## 10.1 当前切除策略

优先：

```text
active sketch + FeatureCut3
```

如果失败，再进入备用方案：

```text
重新选择草图
→ FeatureCut3
→ FeatureCut4
→ 单向完全贯穿
→ 反向完全贯穿
→ 双向完全贯穿
→ Through All Both
→ 双向固定深度
```

运行日志会记录：

```text
CUT_ATTEMPT ...
CUT_RESULT ...
CUT_OK ...
```

---

# 11. 日志系统

运行日志：

```text
bin\SpeakerGrillePro_runtime.log
```

安装日志：

```text
install_log.txt
```

日志是后续调试的核心依据。

典型内容：

```text
ConnectToSW ENTER
SetAddinCallbackInfo2 returned True
CommandManager creation OK
CONNECT_OK
```

草图点定位：

```text
Selected sketch point center model XYZ = ...
Grille center sketch XY = ...
```

孔生成：

```text
FACE_FILTER candidates=...
CIRCLE_CREATE requested=..., created=...
```

切除：

```text
ACTIVE_SKETCH_AFTER_CREATE manager=OK, model=OK
CUT_ATTEMPT ...
CUT_RESULT ...
CUT_OK ...
```

后续 Codex / Claude Code 修改插件时，应该优先让用户提供：

```text
SpeakerGrillePro_runtime.log
```

不要盲猜。

---

# 12. CommandManager / 插件加载

插件通过：

```text
ISwAddin
```

接入 SOLIDWORKS。

关键流程：

```text
ConnectToSW
SetAddinCallbackInfo2
CommandManager
CreateCommandGroup2
AddCommandItem2
Activate
```

日志中正常情况应看到：

```text
CONNECT_OK
```

历史上蜂窝独立插件曾出现：

```text
安装成功
但 SOLIDWORKS 看不到按钮
```

以及：

```text
GetActiveObject
0x800401E3 MK_E_UNAVAILABLE
```

后来已经放弃依赖：

```text
Marshal.GetActiveObject()
```

的热加载方案。

目前统一插件应继续沿用稳定的一键安装 / 注册 / 启动流程。

---

# 13. 一键安装器

用户明确不希望手工配置环境。

标准使用：

```text
解压
→ 双击 一键安装.bat
```

安装器自动完成：

```text
1. 管理员权限
2. 检测 SOLIDWORKS
3. 找 Interop DLL
4. 使用本机 csc.exe 编译
5. 复制运行依赖
6. RegAsm 注册
7. 写 SOLIDWORKS Addins 注册表
8. 验证 Registration
9. 启动 SOLIDWORKS
10. 检测 ConnectToSW
11. 写 install_log
```

---

# 14. Strong Name

V21 开始，插件已经加入：

```text
Strong Name
```

原因：

此前使用：

```text
RegAsm /codebase
```

会出现：

```text
RA0000:
使用 /codebase 注册未签名程序集...
```

虽然只是 warning，但为了让安装器更干净，后续已经改成强名称签名。

V24 应继续保持签名。

---

# 15. 编译兼容要求

用户机器环境不能保证安装新 Visual Studio。

因此源码必须继续保持：

```text
C# 5
.NET Framework 4.x
```

不要使用：

```text
nameof
string interpolation
expression-bodied members
modern pattern matching
LangVersion=latest
```

等新语法。

特别注意：

SOLIDWORKS Interop 中存在：

```text
Environment
```

类。

因此：

```csharp
Environment.NewLine
```

可能与：

```text
System.Environment
SolidWorks.Interop.sldworks.Environment
```

发生歧义。

必须写：

```csharp
System.Environment.NewLine
```

---

# 16. SOLIDWORKS API 历史坑

## GetReferenceEntity

正确形式：

```csharp
int referenceType = 0;
object refEntity = sketch.GetReferenceEntity(ref referenceType);
```

参数必须：

```text
ref
```

---

## object → Feature

部分 API 返回：

```text
object
```

不能直接赋值给：

```csharp
Feature
```

需要：

```csharp
Feature feature = result as Feature;
```

或者显式转换。

---

## 2D 草图坐标

创建二维草图元素时：

```text
CreateCircleByRadius
CreateLine
...
```

必须使用当前草图的局部坐标。

不要错误传入转换后的模型 XYZ。

这是之前“全部孔压成一列”的根本原因。

---

# 17. V24 相比 V23 的关键变化

V23 曾增加：

```text
自动适配模型
```

日志示例：

```text
ADAPTIVE_SIZE available_mm=94.67x45.13
grille_mm=68.16x13.54
scale=0.350
```

虽然某些情况下切除成功，但在固定大尺寸和复杂表面时：

```text
FeatureCut3 = NULL
FeatureCut4 = NULL
```

说明简单自动适配策略不可靠。

因此 V24：

```text
完全删除自动适配模型功能
```

现在只有：

```text
固定尺寸模式
```

用户自己输入真实 mm 尺寸。

这也是当前稳定版的设计原则。

---

# 18. 当前稳定版本功能列表

SpeakerGrillePro V24：

- [x] SOLIDWORKS 2025 SP5
- [x] C# Add-in
- [x] 一键安装
- [x] Strong Name
- [x] 草图点定位
- [x] 固定尺寸
- [x] 圆孔六角错列
- [x] 真蜂窝孔
- [x] 圆孔方阵
- [x] 方形孔
- [x] 菱形孔
- [x] 三角孔
- [x] 同心声波孔
- [x] 中心 / 中间 / 外围三级尺寸
- [x] Face 边界过滤
- [x] 默认不跳孔
- [x] 自动 Cut-Extrude
- [x] 多级切除 fallback
- [x] runtime log
- [x] install log
- [x] 圆孔和蜂窝统一插件

---

# 19. 推荐的后续开发方向

建议从：

```text
SpeakerGrillePro V24
```

建立副本，再继续开发。

不要直接破坏 V24 稳定版。

下一版可以命名：

```text
V25
```

---

## V25 推荐优先级 1：参数预设

增加：

```text
高端精细圆孔
标准音箱圆孔
大孔音响风
标准蜂窝
细密蜂窝
工业方孔
```

用户可以一键选择模板。

---

## V25 推荐优先级 2：预览

增加：

```text
生成前草图预览
```

用户确认后再 Cut。

这样可以避免生成后撤销。

---

## V25 推荐优先级 3：样式参数动态 UI

根据当前孔型：

```text
只显示相关参数
```

例如：

圆孔模式：

```text
隐藏蜂窝筋宽
```

蜂窝模式：

```text
显示蜂窝筋宽
```

方形孔：

```text
显示旋转角度
```

同心声波：

```text
显示环距 / 波纹参数
```

---

## V25 推荐优先级 4：更加安全的边界检测

可考虑使用：

```text
Face Loop / Trim Curve
```

而不仅仅是中心点判断。

最终目标：

> 整个孔轮廓必须位于有效 Face 内。

尤其是：

- 六边形
- 方形
- 菱形
- 三角形

不能只检查中心点。

---

## V25 推荐优先级 5：性能优化

当孔数量达到：

```text
500+
1000+
```

时，SOLIDWORKS 草图和 Cut 可能变慢。

可优化：

- `AddToDB = true`
- 暂停图形刷新
- 批量 Sketch Segment
- 最后一次重建
- 减少 Select / ClearSelection
- 根据孔数量自动提示

---

# 20. 不建议重新加入的功能

当前阶段不要重新加入简单版：

```text
自动适配模型
```

尤其不要使用：

```text
Face.GetBox()
× 百分比
```

直接自动决定孔区尺寸。

如果以后真的要自动适配，应重新设计为一个独立高级功能。

---

# 21. 用户工作方式与开发偏好

用户偏好非常明确：

- 希望直接获得新的 ZIP
- 希望一键安装
- 不希望自己修改代码
- 不希望自己配置 DLL
- 出错时可以发送日志
- 希望 AI 直接根据日志修改并重新打包
- 重视实际运行效果
- 喇叭孔美观程度非常重要
- 同时考虑后续开模 / 工程可行性

推荐后续开发流程：

```text
用户测试
→ 发截图 / runtime.log
→ AI 分析
→ 修改源码
→ 升版本
→ 打 ZIP
→ 用户一键安装
```

---

# 22. 当前建议保存的稳定包

当前应作为稳定基线保存：

```text
SpeakerGrillePro_SOLIDWORKS2025_OneClick_v24_MultiStyle_Fixed.zip
```

后续开发建议：

```text
复制 V24
→ 修改 namespace / version / UI
→ V25
```

不要从 V23 或更早版本重新开发，否则可能重新引入已经修复的：

- 坐标转换问题
- 缺孔问题
- FeatureCut 状态问题
- 未签名 RegAsm warning
- 蜂窝排列问题
- 插件加载问题
- 自动适配问题

---

# 23. 最重要的开发原则

后续 Codex / Claude Code 请优先遵循：

1. **V24 是稳定基线**
2. **不要破坏草图点定位**
3. **不要破坏活动草图直接 FeatureCut3 流程**
4. **二维图元使用草图局部坐标**
5. **默认跳孔率保持 0%**
6. **不同模型尺寸由用户输入固定尺寸**
7. **保留 Face 边界过滤**
8. **保持 C# 5 兼容**
9. **保持一键安装**
10. **每次改动必须加强 runtime log**
11. **新功能尽量模块化，不要把所有逻辑继续堆进一个巨大方法**
12. **后续建议逐步拆分 PatternGenerator / SelectionService / CutService / Logging / UI**

---

# 24. 推荐的代码重构方向

如果 Codex / Claude Code 要继续长期维护，建议逐步重构成：

```text
SpeakerGrillePro
│
├─ Addin
│  └─ SwAddin.cs
│
├─ UI
│  ├─ GrilleSettingsForm.cs
│  └─ CommandManagerService.cs
│
├─ Core
│  ├─ GrilleSettings.cs
│  ├─ PatternType.cs
│  └─ HoleGeometry.cs
│
├─ Patterns
│  ├─ IGrillePatternGenerator.cs
│  ├─ HexCirclePatternGenerator.cs
│  ├─ HoneycombPatternGenerator.cs
│  ├─ SquarePatternGenerator.cs
│  ├─ DiamondPatternGenerator.cs
│  ├─ TrianglePatternGenerator.cs
│  └─ RadialWavePatternGenerator.cs
│
├─ SolidWorks
│  ├─ SketchPointResolver.cs
│  ├─ FaceBoundaryChecker.cs
│  ├─ SketchBuilder.cs
│  └─ CutFeatureService.cs
│
├─ Infrastructure
│  └─ RuntimeLogger.cs
│
└─ Installer
   ├─ 一键安装.bat
   └─ one_click_install.ps1
```

这样后续增加新喇叭孔样式时，只需要新增：

```text
PatternGenerator
```

而不会影响核心插件加载和切除逻辑。

---

# 25. 总结

SpeakerGrillePro V24 已经从最初的单一圆孔插件发展为：

> 一个支持多种孔型、草图点精确定位、固定尺寸、自动切除、一键安装、适用于 SOLIDWORKS 2025 SP5 的统一喇叭孔设计工具。

V24 当前最大的价值是：

```text
稳定
可控
多样式
容易继续扩展
```

后续开发建议以：

```text
参数预设
实时预览
更模块化代码
更严格几何边界检测
性能优化
```

为重点，而不是继续修改已经稳定的底层定位和切除流程。
