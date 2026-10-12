# Claude Desktop Continuity

Windows 上的 Claude Desktop 账号与本地 Code 会话管理。继续使用原生 Desktop，在一个窗口中选择账号、查看最近对话，并点击 **切换账号并继续选中会话**。

Windows companion for the original Claude Desktop: saved accounts, local Code transcript previews, and switch-and-continue in one window. Unofficial; not affiliated with Anthropic.

## 现在可以做什么

- 复用 Desktop Switcher 的账号快照、锁、恢复及托盘添加/备份流程。
- 自动扫描本地 Code JSONL，按文件最近修改排序，显示最后对话时间及完整会话 ID 提示。
- 按项目和标题/ID 搜索，勾选多个会话；预览最近用户与 Claude 的文字对话。
- 恢复所选官方账号后验证身份和 Code 初始化，再通过 Desktop 原生入口接续选中记录。
- 显示扫描片段内发现的压缩时间，以及最近请求的输入 token 总量。
- 单独导入/打开当前 Desktop 的会话；显式配置后支持 CC Switch 的 Gateway 模式。

添加账号、备份仍使用附带的上游托盘界面，不是在主窗口内重新实现登录。点击 **添加账号 / 备份** 后，在 Windows 右下角托盘选择 **Add account…**。首次正常登录一次，工具自动保存；后续有效登录态可直接恢复。

## 下载与使用

从本仓库 Releases 下载 Windows x64 ZIP，解压到一个固定目录。先在原生 Claude Desktop 登录并打开 Code。

双击 `Start.cmd`。首次启动会检查依赖、生成仅供本机使用的 `config.local.json`，并创建桌面 **Claude账号与会话** 快捷方式。之后直接使用该快捷方式。

1. 首次在账号管理器中添加/保存账号，然后回到主窗口点刷新。
2. 根据最后对话和预览选择正确的分支，勾选需要接续的会话。
3. 选择目标账号，点击 **切换账号并继续选中会话**。

如果只想在当前账号打开记录，点击 **导入/打开选中**。运行中的任务请先完成或停止；恢复登录需要重启 Desktop。

账号快照复用 `%USERPROFILE%\.claude-swap\profiles`。已有 OpenScz Switcher 的账号可直接显示，勿同时操作两个管理器。若上游托盘已运行，使用现有托盘添加账号即可。

## 兼容性与边界

- 当前目标为 Windows 10/11 x64、.NET Framework 4.x 和本地 Code 会话。已在 Microsoft Store Desktop 2.31226.1.0 测试。
- `claude://resume?session=UUID` 是该 Desktop 版本的内置接续入口，不是保证长期兼容的公开导入 API。更新后需要回归验证。
- 不改写或合并原 JSONL，不直接修改 Desktop 索引；由原生 Desktop 完成登记。已压缩的上下文不会因此恢复成压缩前状态。
- “最近请求输入”是 transcript usage 中 input + cache read + cache creation 的总量；不是实时剩余额度，也不是全部历史 token 总和。
- 元数据和预览使用有限的头尾扫描窗口。“扫描片段未发现压缩”不代表整个历史从未压缩。
- 切换后无法保证沿用服务端缓存；登录过期、撤销或设备验证仍需重新认证。
- 不提供普通云端聊天跨个人账号迁移、配额自动轮换或多个 Desktop 同时登录。
- 如果账号恢复成功但 Code 初始化/导入失败，会明确报错。账号与会话导入不是一个可整体撤销的事务，此时先检查 Desktop 当前账号再操作。

## CC Switch / Gateway

Setup 在本机发现有效 CC Switch 设置时，将其绝对路径和 `desktopMode: "ccswitch"` 明确写入配置；没有安装 CC Switch 时写入 `desktopMode: "official"`。运行时仅使用这份配置，不在读取失败后改猜另一个数据源。已有配置可手动选择模式。

Gateway 接续另需配置 `gatewayDesktopRoot` 与 `gatewayLogPath`。当前 provider 的路径、账号或初始化日志不可用时立即报错，不猜测旧账号目录。Gateway 模式只支持当前模式的会话接续；官方账号切换按钮会拒绝执行，不代改 CC Switch 配置。

## 从源码构建

依赖：Windows、`go.mod` 指定的 Go 工具链、Windows .NET Framework 编译器。没有 Python 环境或 Python 依赖。

在仓库根目录的 PowerShell 中：

```powershell
.\scripts\continuity\Build.ps1
.\scripts\continuity\Setup.ps1
.\dist\LocalIntegrationTests.exe "$PWD\config.local.json"
```

Build 会验证 Go modules、运行上游 Go 测试、构建 CLI/托盘和主窗口。Setup 根据本机环境生成配置，已有配置不会覆盖。可执行 `Build.ps1 -ConfigPath <绝对路径>` 同时运行本机集成测试。

本机集成测试读取真实会话并驱动测试进程中的 WinForms 控件事件，不显示窗口、不调用 Computer Use、不切换登录、不发送模型请求。它需要实际账号快照和至少两条本地会话。CI 只进行不接触用户配置的上游测试与应用编译。

## 隐私与许可证

登录快照、原对话、配置和本机测试日志不属于发布包。认证值不进入前端账号 JSON 协议。诊断日志不记录对话正文；本地日志仍可能包含账号 UUID、项目路径或用户选择的账号名，分享前请审查。

本项目基于 [OpenScz/claude-desktop-switcher](https://github.com/OpenScz/claude-desktop-switcher)，保留 MIT 许可证及上游历史。新增 Windows 会话界面和整合代码同样采用 MIT。见 [THIRD_PARTY.md](THIRD_PARTY.md)、[SECURITY.md](SECURITY.md) 和 [VALIDATION.md](VALIDATION.md)。
