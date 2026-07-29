# 项目结构

这是 `my-gesture` 的项目地图。每次修改前先阅读；只要改动影响项目布局、启动流程、配置、构建/发布行为，或核心模块职责，就要同步更新这里。

## 产品方向

`WuGesture` 是一个 Windows 鼠标手势应用；仓库和工程目录仍沿用 `my-gesture` / `WuGesture.App` 命名。

当前目标：

- 原生后端负责全局鼠标钩子、手势识别、规则匹配和动作执行。
- WebView2 前端负责配置界面，当前是独立的 Vue3 + Vite 工程，构建产物由桌面宿主加载。
- 手势使用 8 个方向。
- 动作当前支持快捷键、窗口控制、音量控制和亮度控制；快捷键通过 `SendInput` 执行，窗口控制通过 Win32 窗口 API 执行，音量通过 Core Audio API 执行并带按键回退，静音状态下执行音量增减会先取消静音，亮度通过 DDC/CI、WMI、Gamma 三段回退执行。
- 规则当前支持 `global`、`category` 和 `app` 作用域；程序可按关联顺序归属多个分类，分类同手势以后关联的分类覆盖前者，`app` 规则仍高于分类和全局规则。
- 边缘操作是独立的全局配置，支持触发角、摩擦边和边缘滚动。
- UI 设置里的轨迹线、手势提示和音量/亮度 OSD 都支持单独关闭，运行时会按对应 `uiSettings` 节点的 `enabled` 决定是否显示。

## 根目录

```text
D:\workspace\my-gesture
├─ AGENTS.md
├─ docs
│  ├─ architecture.md
│  └─ releasing.md
├─ PROJECT_STRUCTURE.md
├─ WuGesture.slnx
├─ README.md
├─ scripts
├─ src
│  └─ WuGesture.App
└─ tests
   └─ WuGesture.App.Tests
```

根目录重要文件：

- `WuGesture.slnx`：.NET 解决方案。
- `README.md`：面向用户的运行、构建和发布说明。
- `docs\architecture.md`：面向开发者的架构说明和 WebView2 集成概览。
- `docs\releasing.md`：面向维护者的本地发布和自动发行流程。
- `PROJECT_STRUCTURE.md`：本项目地图。
- `AGENTS.md`：后续会话的代理指令。
- `.gitignore`：忽略构建输出、WebView2 运行时缓存、IDE 状态和发布产物。

## 桌面应用

路径：

```text
src\WuGesture.App
```

技术栈：

- C# / .NET `net10.0-windows`
- WinForms
- WebView2

启动流程：

- `Program.cs` 会在 .NET Host 成功启动后检查 WebView2 Runtime，缺少时显示官方下载引导；随后通过命名互斥体保证单实例运行。再次启动时不会创建第二个实例，而是先将前台切换权限授予已运行实例，再通知它在 UI 线程弹出并激活配置窗口，且等待该 UI 操作通过命名自动重置事件确认或超时后才退出。已有且可见的配置窗口收到单实例唤醒时，会临时关联当前前台线程的输入队列后激活，并在完成后立即解除关联；没有配置窗口时则保持普通启动与恢复流程，避免使用短暂置顶造成焦点回退。开机自启动会带 `--startup` 内部参数，始终只启动后台服务并驻留托盘，不打开配置窗口；普通启动是否显示配置窗口由应用行为设置控制，默认显示。
- `MainForm.cs` 会在首次显示前加载配置并决定是否后台启动，以避免后台启动时短暂绘制主窗口；普通启动显示配置窗口、托盘恢复和单实例唤醒都会将配置窗口前置并激活，后台启动则继续隐藏。随后应用开机自启动和管理员启动设置、创建 `GestureService` / `EdgeActionService` / `GestureFeedbackCoordinator`、创建托盘图标，并处理 WebView 业务消息；也会把配置里的 `uiSettings` 应用到轨迹窗、提示窗和应用行为。主窗口和托盘显示名为 `WuGesture`。
- `WebViewHost.cs`：负责 WebView2 控件的创建、销毁、虚拟主机映射、导航和消息事件订阅；入站消息仍同步交由 `MainForm` 的业务处理器执行，宿主不存在时出站消息直接丢弃。
- `WebViewMessageDtos.cs`：集中 WebView 入站消息 DTO。
- `WebViewRulesPayloadFactory.cs`：把已加载的手势配置转换为 WebView `rules` 消息 payload，并补充应用图标数据。
- `ApplicationIconDataUrl.cs`：从可执行文件提取图标并转换为 WebView 可用的 PNG data URL。
- `WindowStateStore.cs`：负责配置窗口位置、尺寸和最大化状态的读写、校验与屏幕边界规范化。
- `AppIdentity.cs` 集中应用显示名、AppData 子目录、自启动注册表值、单实例 IPC 请求/确认事件名和内部启动参数；开机自启动和管理员重启会直接调用当前 `WuGesture.exe`。
- `ConfigStorageContract.cs` 集中本地配置文件名和窗口状态文件名；本地导入/导出只处理主配置文件，不包含窗口状态文件。
- `WebViewHostContract.cs` 集中 WebView2 虚拟主机、入口 URL 和宿主输出目录中的 Web 前端路径片段。
- 配置窗口最小尺寸为 1280x720；首次启动会以至少该尺寸居中显示，关闭窗口时会保存窗口位置、大小和最大化状态，并拒绝恢复小于该尺寸（包括 0x0）的无效状态。窗口按设置选择隐藏到托盘、最小化到任务栏或直接退出。隐藏到托盘会释放 WebView2 配置界面以降低后台内存占用，托盘恢复时重建 WebView2。通过托盘菜单“退出”始终会真正释放后台手势服务并结束进程。
- 托盘菜单提供“打开配置”、“暂停 WuGesture”和“退出”；暂停项会暂停手势识别和边缘操作，但保留后台进程和配置界面。暂停时仅系统托盘图标切换为灰阶图标，并在托盘提示文字中标记“已暂停”，任务栏窗口图标不变。

资源：

- `Resources\volume.png`、`Resources\sun.png`：音量和亮度 OSD 使用的嵌入图标资源。
- `Resources\wu.jpg`：应用图标来源图片。
- `Resources\wu.ico`：从 `wu.jpg` 生成的 Windows 应用图标，用于可执行文件、任务栏、窗口左上角和托盘。

## 手势引擎

路径：

```text
src\WuGesture.App\GestureEngine
```

关键文件：

- `MouseHook.cs`：低级全局鼠标钩子。
- `KeyboardShortcutRecorder.cs`：低级键盘 hook，用于配置界面录制快捷键并吞掉录制期间的原生键盘事件。
- `GestureService.cs`：接入右键和中键低级 hook，协调会话、识别器、匹配器和执行器，并向 UI 发送事件；也支持录制会话，把识别结果回传给前端。
- `GestureSession.cs`：保存单次手势的 `Tracking -> Completing -> Idle` 生命周期、会话编号、轨迹和预览/进度缓存。完成阶段不会开始下一笔；跟踪中收到任意新的手势按键按下时会先取消旧会话，再以新按下开始下一笔，且会吞掉被取消旧按键迟到的抬起事件。
- `GestureFeedbackCoordinator.cs`：订阅手势 UI 事件，在 UI 线程按会话编号过滤陈旧更新，负责轨迹/提示覆盖层及手势相关 WebView 消息；`MainForm` 仅提供配置和宿主资源回调。
- `EdgeActionService.cs`：轮询真实光标位置并监听滚轮，负责暂停/排除/全屏门控、触发顺序、动作筛选和 UI 线程调度；摩擦边会排除角落区域，按沿边方向的反向位移计数并在触发后防重复，滚轮边命中时会吞掉原始滚轮事件。
- `EdgeHitTester.cs`：集中四角、普通边缘和摩擦边的屏幕几何命中计算，并提供到边距离计算；保留屏幕遍历和边界优先级。
- `FrictionTracker.cs`：维护单次摩擦边的位移方向、计数、触发后冷却和超时重置状态。
- `EdgeActionConfigNormalizer.cs`：将边缘动作配置规范为运行时所需形态，并迁移旧版摩擦边角落位置。
- `EdgeActionFormatting.cs` 和 `EdgeLocation.cs`：分别提供边缘动作失败名称格式化及运行时边缘位置模型/配置位置映射。
- `ForegroundWindowFullscreenDetector.cs`：判断当前前台窗口是否处于无边框全屏，供手势和边缘操作的全屏禁用设置共用。
- `GestureRecognizer.cs`：把鼠标轨迹转换为稳定的方向模式；识别前按有效移动距离抽样，单笔手势保留 8 方向，多笔手势默认回退到更宽容的横/竖方向以贴近 WGestures 手感。
- `GestureRuntimeDefaults.cs`：手势识别和边缘操作运行时阈值常量，包括最小移动距离、识别步数、边缘厚度、摩擦距离和超时。
- `GestureMatcher.cs`：将识别出的鼠标键和方向模式与已加载规则进行匹配，并按作用域优先级选择命中项。
- `GestureScopeContext.cs`：用于规则匹配的窗口 app/有序分类上下文模型。
- `ForegroundWindowScopeContextProvider.cs`：读取前台窗口或指定窗口句柄的进程名。
- `ConfiguredScopeContextProvider.cs`：用配置里的应用程序列表把窗口进程名映射到有序分类列表，供作用域匹配使用。
- `ActionExecutor.cs`：按动作类型执行命令；快捷键通过 Win32 `SendInput` 执行，窗口控制通过 `ShowWindow`、`SetWindowPos` 和窗口消息执行。鼠标手势关闭桌面或桌面托管的 Wallpaper Engine 窗口时会让 Explorer 显示系统关机选择对话框；其余动作按原目标窗口流程执行；音量/亮度会调用对应控制器并显示 OSD。
- `DesktopWindowClassifier.cs`：通过桌面 Shell 窗口类以及父/拥有者链识别 `Progman`、`WorkerW` 和桌面视图，供鼠标手势关闭窗口动作区分桌面与普通应用窗口。
- `AudioController.cs`：通过 Windows Core Audio API 读取和设置系统主音量、静音状态。
- `BrightnessController.cs`：通过 DDC/CI、WMI、Gamma 三段回退读取和设置显示亮度。
- `BrightnessAdjustmentQueue.cs`：把亮度调节放到后台串行队列执行，并合并连续滚轮输入，避免 DDC/CI 等慢调用阻塞鼠标钩子或主 UI。
- `LevelOsdOverlay.cs`：音量和亮度 OSD 的线程安全请求入口，把手势、边缘操作和后台亮度队列的显示请求投递到主 UI 线程。
- `ResourceNames.cs`：后端嵌入资源 manifest 名常量。
- `MouseInput.cs`：当移动距离太小，不足以构成手势时，重放一次普通右键或中键。
- `GestureDirection.cs`：8 方向枚举。
- `GestureRule.cs`：运行时规则和热键动作模型。
- `GestureUiSettings.cs`：持久化的运行时 UI 设置模型，包括轨迹窗、提示泡泡、音量/亮度 OSD 和手势灵敏度配置。
- `MouseTrailForm.cs`：全虚拟桌面的透明分层覆盖窗，负责窗口生命周期、鼠标穿透、DIB/HDC 缓冲和画面提交；启动后预热并在手势结束时隐藏复用，避免独立提示窗的创建和定位时序问题。即使轨迹线关闭，提示仍可单独使用此覆盖层显示。
- `MouseTrailRenderer.cs`：管理手势路径、轨迹画笔和增量轨迹绘制。
- `GestureHintRenderer.cs`：管理规则命中提示的样式、尺寸定位、显示/淡出计时和同层绘制；最终提示无缝接续预览，并按配置时长淡出。
- `LevelOsdRenderer.cs`：管理音量/亮度 OSD 的图标、轨道、数值、位置、显示/淡出计时和同层绘制。
- 运行时窗体会从配置里的 `uiSettings` 读取并应用轨迹颜色、线宽、未激活/激活透明度、提示泡泡外观和显示/淡出时长，以及音量/亮度 OSD 的显示时长、尺寸、圆角和位置。三个渲染器独立维护内容和淡出 alpha，任一层结束都不会清空其他层。

手势流水线：

```text
MouseHook
-> GestureService
-> GestureRecognizer
-> GestureMatcher
-> ActionExecutor
-> MouseTrailForm
-> WebView 状态
```

重要行为：

- 右键按下/抬起在手势跟踪期间会被吞掉。
- 中键按下/抬起在手势跟踪期间会被吞掉，避免触发目标程序的原生中键事件；轨迹线会在松开时隐藏并保留资源供下一次手势复用，若命中规则则提示会无缝保留至配置的显示时长结束，再按淡出时长消失。
- 如果移动太小，就会按原触发按钮重放一次普通右键或中键。
- 移动过程中会增量识别当前轨迹；一旦按当前鼠标键和方向匹配到规则，透明轨迹覆盖层会立即在当前鼠标屏幕底部居中显示规则名。
- 动作仍在右键抬起时执行；窗口控制动作的目标窗口由应用行为设置决定。
- 窗口控制动作的目标窗口由 `uiSettings.appBehavior.targetWindowMode` 决定：默认 `start-window` 会在鼠标按下时按起点坐标解析顶层窗口，并使用该窗口的进程和分类上下文匹配 `app > category > global` 规则；松开后会直接将该起始窗口设为当前激活窗口，再执行动作。`current-window` 会保留当前活动窗口的规则上下文，并直接对动作执行时的当前窗口执行。两种模式都不依赖鼠标结束位置，起始窗口无法解析时不会执行动作。
- 鼠标手势命中桌面 Shell 或其托管窗口（包括 Wallpaper Engine 的桌面宿主）时，只有关闭窗口动作会触发 Explorer 的桌面关机选择对话框。`start-window` 仅在这种情况下不激活桌面目标；`current-window` 则在执行时按当前窗口识别桌面。快捷键、其他窗口控制、音量和亮度都按原流程执行，边缘操作不使用该桌面关闭策略。
- `GestureService` 支持暂停；暂停时保留全局 hook，但不识别、不吞掉中/右键输入，并清理当前轨迹与预览提示，供配置界面录制手势使用。
- `GestureService` 会读取应用行为里的排除项；`start-window` 在鼠标按下时按起始窗口判断排除项，`current-window` 则按当前前台窗口判断。命中排除项时不启动手势跟踪，也不吞掉原始鼠标输入。
- 托盘暂停和配置界面左上角状态标识共用同一个临时用户暂停状态，配置界面录制暂停则是独立暂停来源；运行时按用户、配置和录制暂停合并后的状态控制 `GestureService` 和 `EdgeActionService`。
- 快捷键录制由后端低级键盘 hook 完成；录制期间会阻止 `Win` 等系统级按键继续传递，松开所有按键后回传组合键。
- 钩子回调必须保持快速；动作会切回 WinForms 消息线程执行。
- 动作执行失败会被捕获，并通过 `GestureActionFailed` 上报。

## 配置

配置文件：

```text
%AppData%\WuGesture\gestures.json
```

配置相关文件：

- `GestureConfig.cs`：JSON DTO。
- `GestureConfigContract.cs`：配置和运行时共享的字符串契约常量，包括 scope、鼠标按键、动作类型、操作名、边缘触发类型/位置、滚轮方向和关闭按钮行为。
- `GestureConfigStore.cs`：加载、保存、重置默认配置，并处理本地/WebDAV 配置的 JSON 读写与 `LoadedGestureConfig` 组装。
- `GestureConfigNormalizer.cs`：集中配置 schema 的空值补全、旧字段迁移、UI 设置范围校验和排除项规范化；后端仍是配置校验的最终权威。
- `ApplicationIdentityNormalizer.cs`：集中可执行文件名/进程名的规范化，供配置、排除项和应用分类匹配复用。
- `ConfigStorageContract.cs`：配置文件名和窗口状态文件名常量。
- `GestureConfigMapper.cs`：把配置 DTO 映射为运行时 `GestureRule`。
- `DefaultGestureConfig.cs`：完整默认初始配置，包含默认全局规则、默认 UI/应用行为设置和默认关闭的边缘操作。
- `DefaultGestureRules.cs`：默认全局规则。
- `WebDavConfigSyncService.cs`：WebDAV 配置备份/恢复服务，负责测试连接、上传本地完整配置、下载远端配置并按远程路径规则创建目录。
- `WebDavProtocolContract.cs`：WebDAV 协议方法、请求头、认证 scheme、媒体类型和超时常量。
- `WebViewMessageTypes.cs`：桌面宿主侧 WebView 入站/出站消息类型常量。
- `config-result` 消息会携带操作来源；前端仅静默 `save` 的成功结果，导入、导出、重载和恢复等明确操作仍显示结果提示，任意失败都会显示错误提示。

当前支持的配置：

- `scope`：支持 `global`、`category:<分类名>`、`app:<进程名>`；运行时优先匹配 app，其次按程序分类关联顺序匹配 category（后关联者覆盖前者），最后匹配 global。
- `mouseButton`：支持 `right`、`middle`，运行时会按当前触发的鼠标键区分规则。
- `pattern`：手势方向列表，例如 `["Down", "Right"]`。
- `action.type`：支持 `hotkey`、`window`、`volume` 和 `brightness`。
- `action.keys`：按键列表，例如 `["Control", "W"]`；允许为空，表示先保存手势，之后再补命令，空命令规则不会参与运行时执行。
- `action.operation`：窗口控制操作，仅在 `action.type` 为 `window` 时使用；当前支持 `toggle-topmost`、`toggle-maximize`、`minimize`、`close`。
- `action.operation`：音量控制在 `action.type` 为 `volume` 时支持 `increase`、`decrease`、`mute`；亮度控制在 `action.type` 为 `brightness` 时支持 `increase`、`decrease`。
- `action.amount`：音量/亮度的 `increase`、`decrease` 步进值，范围 1-100。
- `edgeActions`：独立的全局边缘操作列表；每项包含 `enabled`、`triggerType`、`location`、`wheelDirection`、`frictionCount` 和 `action`。`triggerType` 支持 `corner`、`friction`、`wheel`；`corner` 的位置为四角，`friction/wheel` 的位置为四边，`wheel` 额外区分滚轮 `up/down`。边缘操作名称不再保存，由 UI 和运行时根据触发类型、位置与滚轮方向生成。
- `applications`：应用程序归属列表，每项包含 `name`、`displayName`、`path`、`categories`；`categories` 是有序分类列表，运行时通过前台进程名匹配 `name`，分类同手势以后关联者覆盖前者。旧配置的单个 `category` 会在加载时迁移；`displayName` 只用于 UI 展示和编辑。
- `uiSettings.mouseTrail`：轨迹窗设置，包含 `enabled`、`inactiveColor`、`activeColor`、`inactiveThickness`、`activeThickness`、`thickness`、`inactiveOpacity`、`activeOpacity`；`enabled` 关闭时不再绘制轨迹线，`thickness` 保留用于兼容旧配置。
- `uiSettings.gestureHint`：提示泡泡设置，包含 `enabled`、`displayDurationMs`、`fadeDurationMs`、`fontFamily`、`fontSize`、`textColor`、`backgroundColor`、`backgroundOpacity`、`width`、`widthPercent`、`autoWidth`、`height`、`heightPercent`、`cornerRadius`、`bottomOffset`、`bottomOffsetPercent`；提示绘制在全虚拟桌面轨迹覆盖层而非独立窗体，`enabled` 关闭时不再显示手势命中文本，`displayDurationMs` 为停留时长、`fadeDurationMs` 为 0 时立即消失，百分比字段按当前鼠标屏幕工作区宽高换算，像素字段保留用于兼容旧配置。
- `uiSettings.levelOsd`：音量/亮度 OSD 设置，包含 `enabled`、`displayDurationMs`、`fadeDurationMs`、`backgroundColor`、`backgroundOpacity`、`textColor`、`trackColor`、`volumeColor`、`brightnessColor`、`width`、`height`、`cornerRadius`、`position`、`offsetX` 和 `offsetY`；OSD 由全虚拟桌面透明覆盖层绘制，不再创建独立窗体；当前支持相对于鼠标所在屏幕工作区的居中、上/下居中和四角位置预设，`fadeDurationMs` 为 0 时立即消失。
- `uiSettings.gestureSensitivity`：手势灵敏度配置，包含 `percent`，范围 0-200，默认 110；100 对应标准手感，数值越高越容易识别短距离手势。
- `uiSettings.appBehavior`：应用行为设置，包含 `launchAtStartup`、`showConfigWindowOnLaunch`、`runAsAdministrator`、`gesturePaused`、`closeButtonBehavior`、`targetWindowMode`、`disableGesturesInFullscreen`、`disableEdgeActionsInFullscreen` 和 `excludedApplications`；`showConfigWindowOnLaunch` 默认开启，控制普通启动时是否显示配置窗口，关闭后仅后台驻留并可从托盘打开，开机自启动始终后台运行。关闭按钮行为支持 `minimize-to-tray`、`minimize-to-taskbar`、`exit`。`targetWindowMode` 支持默认的 `start-window` 和 `current-window`。全屏禁用开关默认关闭，分别停止手势识别和边缘操作。排除项包含 `name`、`displayName`、`path` 和 `disableEdgeActions`；命中的程序不执行鼠标手势，勾选 `disableEdgeActions` 时也会禁用边缘操作。
- `uiSettings.webDav`：WebDAV 备份设置，包含 `address`、`userName`、`password` 和 `remotePath`。设置页可把当前完整配置导出到本地 JSON，或从本地 JSON 导入并覆盖主配置文件；本地导入/导出不包含窗口状态。设置页也可测试 WebDAV 连接；测试当前配置成功后，才允许把当前完整配置保存到 WebDAV，或从 WebDAV 下载配置并覆盖本地配置；恢复后会刷新规则匹配、边缘操作、应用行为和 UI 设置。

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
src\WuGesture.App\Web
```

Web 前端有独立项目地图：

```text
src\WuGesture.App\Web\PROJECT_STRUCTURE.md
```

根文档只记录桌面宿主和 Web 子项目之间的集成关系；页面、路由、组件、图标、前端状态模块和 WebView 消息细节以 Web 子项目文档为准。

宿主集成：

- 前端使用 `pnpm build` 生成根目录下的 `dist\web`。
- `Web\src\components\BaseDialog.vue` 统一前端对话框的遮罩关闭、可选右上关闭按钮、默认操作区和关闭动画；自动保存或即时选择类弹层可复用外壳并关闭默认操作区。
- `Web\src\components\BaseInput.vue` 和 `BaseRange.vue` 统一前端原生输入控件的 `v-model` 事件、宽度约束和滑块进度填充；页面继续保留各自的配置约束与保存时机。
- 前端 `pnpm` 构建脚本通过 `src\WuGesture.App\Web\pnpm-workspace.yaml` 放行 `@parcel/watcher` 的本地构建脚本，避免非交互环境下的依赖安装中断。
- `WuGesture.App.csproj` 会在 `.NET` 构建前自动执行前端构建。
- `WuGesture.App.csproj` 会在前端构建后把 `dist\web` 复制到宿主输出目录中的 `Web\dist`。
- 桌面宿主通过 WebView2 虚拟主机 `https://gesture.wu.philosophy/` 加载宿主输出目录中的 `Web\dist`。

## 测试

测试工程路径：

```text
tests\WuGesture.App.Tests
```

`WuGesture.slnx` 包含桌面应用工程与 xUnit 测试工程。测试工程通过 `InternalsVisibleTo` 访问需要覆盖的内部纯逻辑，不直接驱动全局鼠标 hook、窗口前置或 WebView2。

当前单元测试覆盖：

- `GestureRecognizer`：有效移动距离、单笔 8 方向、多笔首段归一化和灵敏度。
- `GestureMatcher`：`app > category > global` 作用域优先级、分类关联顺序、鼠标按键和完整方向模式匹配。
- `EdgeHitTester`：多屏坐标、边缘优先级、摩擦边角落排除和到边距离计算。

## 构建与发布

构建：

```powershell
dotnet build WuGesture.slnx
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
dotnet test WuGesture.slnx
```

`dotnet test WuGesture.slnx` 会构建桌面应用和测试工程，并执行上述单元测试。

发布：

```powershell
.\scripts\publish-app.ps1
```

发布脚本会在执行 `dotnet publish` 前自动关闭正在运行的 `WuGesture`、清空输出目录，再重新生成发布产物，避免发布目录里残留旧的 `Wu Gesture.*` 文件。它将依赖框架的桌面应用直接发布为用户入口 `WuGesture.exe`，并将对应的 `WebView2Loader.dll` 放到输出根目录。发行包不包含 .NET；缺少 .NET 10 Desktop Runtime x64 时由 .NET Host 显示系统安装提示，应用启动后会检测 WebView2 Runtime 并提供官方下载引导。可通过 `-Version` 设置程序集版本，通过 `-RuntimeIdentifier` 和 `-SelfContained` 生成指定运行时的自包含产物。

自动发行：

- `scripts\publish-app.ps1`：发布应用目录的通用步骤，负责关闭运行实例、执行 `dotnet publish`、复制 Web 前端产物与 `WebView2Loader.dll`。
- `scripts\package-debug.ps1`：无需参数，调用 `publish-app.ps1` 生成 Debug `win-x64` 发布目录和 `WuGesture-debug-windows-x64.zip`。
- `scripts\package-release.ps1`：基于已创建的 `v*` Git 标签生成 framework-dependent `win-x64` 发布目录、版本化 ZIP 包和从相邻标签之间提交整理的 `RELEASE_NOTES.md`；ZIP 内的根目录固定为 `WuGesture`，而 ZIP 文件名保留版本号。发行包不包含 .NET，缺少 .NET 10 Desktop Runtime 时由 .NET Host 显示系统安装提示。
- `.github\workflows\release.yml`：GitHub 收到 `v*` 标签后构建发行包、创建 GitHub Release；配置 `GITEE_REPOSITORY` 和 `GITEE_TOKEN` secrets 后，会镜像 `main` 和标签到 Gitee，并将相同的附件和更新说明发布至 Gitee。
- `scripts\upload-gitee-release.ps1`：由 GitHub Actions 调用 Gitee API 创建发行版和上传附件；令牌只通过工作流 secret 传入，不存入仓库。

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
