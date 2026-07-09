# 项目结构

这是 `my-gesture` 的项目地图。每次修改前先阅读；只要改动影响项目布局、启动流程、配置、构建/发布行为，或核心模块职责，就要同步更新这里。

## 产品方向

`Wu Gesture` 是一个 Windows 鼠标手势应用；仓库和工程目录仍沿用 `my-gesture` / `MyGesture.App` 命名。

当前目标：

- 原生后端负责全局鼠标钩子、手势识别、规则匹配和动作执行。
- WebView2 前端负责配置界面，当前是独立的 Vue3 + Vite 工程，构建产物由桌面宿主加载。
- 手势使用 8 个方向。
- 动作当前支持快捷键、窗口控制、音量控制和亮度控制；快捷键通过 `SendInput` 执行，窗口控制通过 Win32 窗口 API 执行，音量通过 Core Audio API 执行并带按键回退，静音状态下执行音量增减会先取消静音，亮度通过 DDC/CI、WMI、Gamma 三段回退执行。
- 规则当前支持 `global`、`category` 和 `app` 作用域，并按 `app > category > global` 优先级匹配。
- 边缘操作是独立的全局配置，支持触发角、摩擦边和边缘滚动。

## 根目录

```text
D:\workspace\my-gesture
├─ AGENTS.md
├─ PROJECT_STRUCTURE.md
├─ MyGesture.slnx
├─ README.md
├─ gesture.ahk
├─ scripts
├─ src
└─ tests
```

根目录重要文件：

- `MyGesture.slnx`：.NET 解决方案。
- `README.md`：面向用户的运行、构建和发布说明。
- `PROJECT_STRUCTURE.md`：本项目地图。
- `AGENTS.md`：后续会话的代理指令。
- `gesture.ahk`：早期 AutoHotkey 实验文件，当前未接入。
- `.gitignore`：忽略构建输出、WebView2 运行时缓存、IDE 状态和发布产物。

## 桌面应用

路径：

```text
src\MyGesture.App
```

技术栈：

- C# / .NET `net10.0-windows`
- WinForms
- WebView2

启动流程：

- `Program.cs` 通过命名互斥体保证单实例运行；再次启动时不会创建第二个实例，而是通知已运行实例弹出配置窗口。开机自启动会带 `--startup` 内部参数，默认只启动后台服务并驻留托盘，不打开配置窗口。
- `MainForm.cs` 加载配置、应用开机自启动和管理员启动设置、创建 `GestureService` / `EdgeActionService`、按需初始化 WebView2 配置界面、创建托盘图标，并桥接 WebView 消息；也会把配置里的 `uiSettings` 应用到轨迹窗、提示窗和应用行为。主窗口和托盘显示名为 `Wu Gesture`。
- `AppIdentity.cs` 集中应用显示名、AppData 子目录、自启动注册表值、单实例 IPC 名和内部启动参数。
- `ConfigStorageContract.cs` 集中本地配置文件名和窗口状态文件名。
- `WebViewHostContract.cs` 集中 WebView2 虚拟主机、入口 URL 和宿主输出目录中的 Web 前端路径片段。
- 配置窗口首次启动时默认占据主屏工作区的一半，并居中显示；关闭窗口时会保存窗口位置、大小和最大化状态，并按设置选择隐藏到托盘、最小化到任务栏或直接退出。隐藏到托盘会释放 WebView2 配置界面以降低后台内存占用，托盘恢复时重建 WebView2。通过托盘菜单“退出”始终会真正释放后台手势服务并结束进程。
- 托盘菜单提供“打开配置”、“暂停 Wu Gesture”和“退出”；暂停项会暂停手势识别和边缘操作，但保留后台进程和配置界面。暂停时仅系统托盘图标切换为灰阶图标，并在托盘提示文字中标记“已暂停”，任务栏窗口图标不变。

历史模板文件：

- `Form1.cs`
- `Form1.Designer.cs`

这两个文件是未使用的模板残留。

资源：

- `Resources\volume.png`、`Resources\sun.png`：音量和亮度 OSD 使用的嵌入图标资源。
- `Resources\wu.jpg`：应用图标来源图片。
- `Resources\wu.ico`：从 `wu.jpg` 生成的 Windows 应用图标，用于可执行文件、任务栏、窗口左上角和托盘。

## 手势引擎

路径：

```text
src\MyGesture.App\GestureEngine
```

关键文件：

- `MouseHook.cs`：低级全局鼠标钩子。
- `KeyboardShortcutRecorder.cs`：低级键盘 hook，用于配置界面录制快捷键并吞掉录制期间的原生键盘事件。
- `GestureService.cs`：跟踪右键和中键轨迹生命周期，调用识别器、匹配器和执行器，并向 UI 发送事件；也支持录制会话，把识别结果回传给前端。
- `EdgeActionService.cs`：轮询真实光标位置并监听滚轮，处理屏幕四角触发、四边摩擦计数和四边滚轮触发；摩擦边会排除角落区域，按沿边方向的反向位移计数并在触发后防重复，滚轮边命中时会吞掉原始滚轮事件。
- `GestureRecognizer.cs`：把鼠标轨迹转换为稳定的 8 方向模式。
- `GestureRuntimeDefaults.cs`：手势识别和边缘操作运行时阈值常量，包括最小移动距离、识别步数、边缘厚度、摩擦距离和超时。
- `GestureMatcher.cs`：将识别出的鼠标键和方向模式与已加载规则进行匹配，并按作用域优先级选择命中项。
- `GestureScopeContext.cs`：当前前台窗口的 app/category 上下文模型。
- `ForegroundWindowScopeContextProvider.cs`：读取前台窗口进程名。
- `ConfiguredScopeContextProvider.cs`：用配置里的应用程序列表把前台进程名映射到分类，供作用域匹配使用。
- `ActionExecutor.cs`：按动作类型执行命令；快捷键通过 Win32 `SendInput` 执行，窗口控制通过 `ShowWindow`、`SetWindowPos` 和窗口消息执行，音量/亮度会调用对应控制器并显示 OSD。
- `AudioController.cs`：通过 Windows Core Audio API 读取和设置系统主音量、静音状态。
- `BrightnessController.cs`：通过 DDC/CI、WMI、Gamma 三段回退读取和设置显示亮度。
- `BrightnessAdjustmentQueue.cs`：把亮度调节放到后台串行队列执行，并合并连续滚轮输入，避免 DDC/CI 等慢调用阻塞鼠标钩子或主 UI。
- `LevelOsdForm.cs`：音量和亮度调节后的置顶非激活弹窗提示。
- `ResourceNames.cs`：后端嵌入资源 manifest 名常量。
- `MouseInput.cs`：当移动距离太小，不足以构成手势时，重放一次普通右键或中键。
- `GestureDirection.cs`：8 方向枚举。
- `GestureRule.cs`：运行时规则和热键动作模型。
- `GestureUiSettings.cs`：持久化的运行时 UI 设置模型，包括轨迹窗和提示泡泡配置。
- `GestureHintForm.cs`：独立的全局命中提示窗，移动过程中匹配到规则时立即显示规则名。
- `MouseTrailForm.cs`：独立的全局透明覆盖窗，在按住中键或右键移动时绘制鼠标轨迹；启动后预热并在手势结束时隐藏复用，避免首次绘制和反复创建窗口造成卡顿。
- 这两个运行时窗体会从配置里的 `uiSettings` 读取并应用轨迹颜色、线宽、未激活/激活透明度，以及提示泡泡字体、文字颜色、背景透明度和尺寸。

手势流水线：

```text
MouseHook
-> GestureService
-> GestureRecognizer
-> GestureMatcher
-> ActionExecutor
-> GestureHintForm
-> WebView 状态
```

重要行为：

- 右键按下/抬起在手势跟踪期间会被吞掉。
- 中键按下/抬起在手势跟踪期间会被吞掉，避免触发目标程序的原生中键事件；轨迹窗在松开时立即隐藏，不做淡出，并保留资源供下一次手势复用。
- 如果移动太小，就会按原触发按钮重放一次普通右键或中键。
- 移动过程中会增量识别当前轨迹；一旦按当前鼠标键和方向匹配到规则，全局提示窗会立即显示规则名。
- 动作仍在右键抬起时执行；窗口控制动作会在执行时重新解析当前目标窗口。
- 窗口控制动作在执行时会先按鼠标当前位置重新解析顶层窗口，避免沿用上一轮手势的句柄；当无法解析时才回退到缓存目标窗口。
- `GestureService` 支持暂停；暂停时保留全局 hook，但不识别、不吞掉中/右键输入，并清理当前轨迹与预览提示，供配置界面录制手势使用。
- `GestureService` 会读取应用行为里的排除项；当前前台程序命中排除项时，不启动手势跟踪，也不吞掉原始鼠标输入。
- 托盘暂停和配置界面录制暂停是两个独立暂停来源，运行时按二者合并后的状态控制 `GestureService` 和 `EdgeActionService`。
- 快捷键录制由后端低级键盘 hook 完成；录制期间会阻止 `Win` 等系统级按键继续传递，松开所有按键后回传组合键。
- 钩子回调必须保持快速；动作会切回 WinForms 消息线程执行。
- 动作执行失败会被捕获，并通过 `GestureActionFailed` 上报。

## 配置

配置文件：

```text
%AppData%\MyGesture\gestures.json
```

配置相关文件：

- `GestureConfig.cs`：JSON DTO。
- `GestureConfigContract.cs`：配置和运行时共享的字符串契约常量，包括 scope、鼠标按键、动作类型、操作名、边缘触发类型/位置、滚轮方向和关闭按钮行为。
- `GestureConfigStore.cs`：加载、保存、重置默认配置。
- `ConfigStorageContract.cs`：配置文件名和窗口状态文件名常量。
- `GestureConfigMapper.cs`：把配置 DTO 映射为运行时 `GestureRule`。
- `DefaultGestureConfig.cs`：完整默认初始配置，包含默认全局规则、默认 UI/应用行为设置和默认关闭的边缘操作。
- `DefaultGestureRules.cs`：默认全局规则。
- `WebDavConfigSyncService.cs`：WebDAV 配置备份/恢复服务，负责测试连接、上传本地完整配置、下载远端配置并按远程路径规则创建目录。
- `WebDavProtocolContract.cs`：WebDAV 协议方法、请求头、认证 scheme、媒体类型和超时常量。
- `WebViewMessageTypes.cs`：桌面宿主侧 WebView 入站/出站消息类型常量。

当前支持的配置：

- `scope`：支持 `global`、`category:<分类名>`、`app:<进程名>`；运行时按 `app > category > global` 优先级匹配。
- `mouseButton`：支持 `right`、`middle`，运行时会按当前触发的鼠标键区分规则。
- `pattern`：手势方向列表，例如 `["Down", "Right"]`。
- `action.type`：支持 `hotkey`、`window`、`volume` 和 `brightness`。
- `action.keys`：按键列表，例如 `["Control", "W"]`；允许为空，表示先保存手势，之后再补命令，空命令规则不会参与运行时执行。
- `action.operation`：窗口控制操作，仅在 `action.type` 为 `window` 时使用；当前支持 `toggle-topmost`、`toggle-maximize`、`minimize`、`close`。
- `action.operation`：音量控制在 `action.type` 为 `volume` 时支持 `increase`、`decrease`、`mute`；亮度控制在 `action.type` 为 `brightness` 时支持 `increase`、`decrease`。
- `action.amount`：音量/亮度的 `increase`、`decrease` 步进值，范围 1-100。
- `edgeActions`：独立的全局边缘操作列表；每项包含 `enabled`、`triggerType`、`location`、`wheelDirection`、`frictionCount` 和 `action`。`triggerType` 支持 `corner`、`friction`、`wheel`；`corner` 的位置为四角，`friction/wheel` 的位置为四边，`wheel` 额外区分滚轮 `up/down`。边缘操作名称不再保存，由 UI 和运行时根据触发类型、位置与滚轮方向生成。
- `applications`：应用程序归属列表，每项包含 `name`、`displayName`、`path`、`category`，运行时通过前台进程名匹配 `name` 后得到分类；`displayName` 只用于 UI 展示和编辑。
- `uiSettings.mouseTrail`：轨迹窗设置，包含 `inactiveColor`、`activeColor`、`inactiveThickness`、`activeThickness`、`thickness`、`inactiveOpacity`、`activeOpacity`；`thickness` 保留用于兼容旧配置。
- `uiSettings.gestureHint`：提示泡泡设置，包含 `fontFamily`、`fontSize`、`textColor`、`backgroundColor`、`backgroundOpacity`、`width`、`widthPercent`、`autoWidth`、`height`、`heightPercent`、`cornerRadius`、`bottomOffset`、`bottomOffsetPercent`；百分比字段按当前屏幕工作区宽高换算，像素字段保留用于兼容旧配置。
- `uiSettings.appBehavior`：应用行为设置，包含 `launchAtStartup`、`runAsAdministrator`、`gesturePaused`、`closeButtonBehavior` 和 `excludedApplications`；关闭按钮行为支持 `minimize-to-tray`、`minimize-to-taskbar`、`exit`。排除项包含 `name`、`displayName`、`path` 和 `disableEdgeActions`；命中的程序不执行鼠标手势，勾选 `disableEdgeActions` 时也会禁用边缘操作。
- `uiSettings.webDav`：WebDAV 备份设置，包含 `address`、`userName`、`password` 和 `remotePath`。设置页可测试 WebDAV 连接；测试当前配置成功后，才允许把当前完整配置保存到 WebDAV，或从 WebDAV 下载配置并覆盖本地配置；恢复后会刷新规则匹配、边缘操作、应用行为和 UI 设置。

默认初始配置只包含全局规则和边缘操作，不包含应用程序归属或分类规则。边缘操作会预置触发角、摩擦边和边缘滚动项，但默认全部关闭。

默认全局规则：

```text
Left        -> 返回 / Alt + Left
Right       -> 前进 / Alt + Right
Down,Right  -> 关闭窗口
DownLeft    -> 最小化
Down,Left   -> Enter
UpRight     -> 最大化
```

## WebView 前端

路径：

```text
src\MyGesture.App\Web
```

Web 前端有独立项目地图：

```text
src\MyGesture.App\Web\PROJECT_STRUCTURE.md
```

根文档只记录桌面宿主和 Web 子项目之间的集成关系；页面、路由、组件、图标、前端状态模块和 WebView 消息细节以 Web 子项目文档为准。

宿主集成：

- 前端使用 `pnpm build` 生成根目录下的 `dist\web`。
- 前端 `pnpm` 构建脚本通过 `src\MyGesture.App\Web\pnpm-workspace.yaml` 放行 `@parcel/watcher` 的本地构建脚本，避免非交互环境下的依赖安装中断。
- `MyGesture.App.csproj` 会在 `.NET` 构建前自动执行前端构建。
- `MyGesture.App.csproj` 会在前端构建后把 `dist\web` 复制到宿主输出目录中的 `Web\dist`。
- 桌面宿主通过 WebView2 虚拟主机 `https://appassets.local/` 加载宿主输出目录中的 `Web\dist`。

## 测试

当前仓库没有独立的测试项目。`MyGesture.slnx` 目前只包含桌面应用工程。

如果后续补回测试工程，可在此补充对应路径、覆盖范围和运行命令。

## 构建与发布

构建：

```powershell
dotnet build MyGesture.slnx
```

默认 Debug 构建输出：

```text
artifacts\debug\WuGesture
```

默认 Debug 可执行文件名：

```text
WuGesture.exe
```

测试：

```powershell
dotnet test MyGesture.slnx
```

当前会因没有测试项目而没有可执行测试目标。

发布：

```powershell
.\scripts\publish.ps1
```

默认发布输出：

```text
artifacts\publish\WuGesture
```

默认发布可执行文件名：

```text
WuGesture.exe
```

`artifacts/` 已被 Git 忽略，用于统一存放 Debug 构建和发布产物。

## 何时更新此文件

在修改以下内容时，请更新此文件：

- 项目文件夹或解决方案布局
- 启动流程
- 手势引擎职责
- 配置文件路径或 schema
- WebView 消息契约
- 构建/发布/测试命令
- 动作类型或规则作用域
- 测试结构

如果只是某个现有文件里的小实现改动，请先检查这里；只有当文档中描述的行为真的变了，才需要更新。
