# HANDOFF — SpeakerGrillePro

> 会话存档（当前状态）· 历史见 `docs/LOG.md` · 项目详细规格见 `SpeakerGrillePro_V24_Project_Handoff.md`

## 任务目标
SpeakerGrillePro：面向 SOLIDWORKS 的喇叭孔自动生成 C# Add-in（V24 稳定版，7 种孔型 / 草图点定位 / 固定毫米尺寸 / 自动切除 / 一键安装）。
本会话完成：发布到 GitHub、完善项目介绍、安全审查与修复。

## 测试命令
无自动化测试框架。验证方式 = 编译检查（csc.exe + 本机 SOLIDWORKS Interop，见 one_click_install.ps1 的 [4/5] 段）。
注意：必须用 PowerShell 执行 csc（Git Bash 会把 /nologo 等参数转成路径导致 CS2001）。
本会话实测：`CSC_EXIT=0`、`BUILD_OK`（DLL 37888 字节），新密钥签名 token=`1f71b6e6b17f50e9`。

## 本次完成及验证结果
- [x] GitHub 新建公共仓库 `zhubingzuo/SpeakerGrillePro`（main 分支，git push 正常）
- [x] README.md（完整项目介绍）/ LICENSE(MIT) / .gitignore 已入库并推送
- [x] 安全审查：提交历史与内容无令牌/密码泄漏；其他公共仓库干净
- [x] 移除强名称私钥 `src/SpeakerGrillePro.snk` 出库并轮换（本机新钥，token=1f71b6e6b17f50e9）
- [x] one_click_install.ps1 新增：snk 缺失时用 PowerShell/CAPI（AT_SIGNATURE）自动生成，实测 csc 兼容
- [x] 删除仓库 `seatclean`、`seatcleanen`；删除本机 `K:\BaiduSyncdisk\C#\github.txt`
- [x] 编译验证通过（SOLIDWORKS 2025 真实 Interop 编译）

## 下一步 TODO（最多 5 条）
1. 用户重跑「一键安装.bat」（管理员）→ 用新密钥重新编译/注册插件（当前注册 DLL 仍为旧密钥签名）
2. 到 GitHub settings/tokens 撤销不用的旧令牌；建议开启 2FA（不影响令牌/SSH 认证）
3. （可选）V25：参数预设 / 生成前预览 / 样式动态 UI / 更严格边界检测 / 性能优化
4. 如需恢复已删仓库（90 天内）需联系 GitHub Support

## 当前的坑
- 本机直连 github.com 需 VPN；api.github.com 一般可直连
- git push 走 GCM + fine-grained 令牌（过期需重新生成）
- 一键安装要求 SOLIDWORKS 在 `D:\Program Files\SOLIDWORKS Corp\SOLIDWORKS` 且需管理员权限
- csc 编译必须经 PowerShell 调用（Git Bash 参数路径转换问题）
- 源码须保持 C# 5 / x64 / Strong Name；`System.Environment.NewLine` 防 Interop Environment 歧义
- 2D 草图实体必须用草图局部坐标（勿传模型 XYZ）；默认跳孔率保持 0%

## 下次恢复需打开的关键文件
- `SpeakerGrillePro_V24_Project_Handoff.md`（项目规格与全部历史坑）
- `README.md`（项目介绍 / 参数 / 安装说明）
- `src/SpeakerGrillePro.cs`（全部插件逻辑）
- `one_click_install.ps1`（安装/编译/注册，含 snk 自动生成）
- `docs/LOG.md`（会话历史）

## 最新 commit hash
`8f53b51`（Security: remove strong-name private key from public repo）
