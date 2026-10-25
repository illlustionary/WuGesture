# 架构说明

WuGesture 的桌面宿主使用 WinForms 和 WebView2；配置界面是独立的 Vue3 + Vite 工程。原生侧负责输入钩子、手势识别、规则匹配和动作执行，WebView2 负责编辑和展示配置。

## 启动与依赖

发行包直接以 framework-dependent 的 `WuGesture.exe` 作为桌面应用入口，开机自启动和管理员重启也直接调用该文件。普通启动默认显示配置窗口，可在应用行为中关闭并改为后台驻留；开机自启动始终后台运行。开启“以管理员身份打开”后，应用会在启动服务前用 `runas` 重启自身；用户在 UAC 提示中取消或提权失败时不会以普通权限继续运行，而是退出（普通启动先提示原因，后台启动只记录日志），避免出现权限不完整的手势服务。`.NET 10 Desktop Runtime x64` 未安装时，.NET Host 会在应用代码执行前显示系统安装提示。应用启动后会检查 WebView2 Runtime；缺少时显示下载引导并退出，避免在初始化配置界面时出现不明确的异常。本地配置保存采用原子替换：先写同目录临时文件并刷盘，再用 `File.Replace` 一步替换，断电或崩溃不会留下被截断的 `gestures.json`。

## 手势流水线

```text
MouseHook
-> GestureService
-> GestureRecognizer
-> GestureMatcher
-> ActionExecutor
-> MouseTrailForm / WebView 状态
```

`GestureService` 跟踪右键或中键轨迹，`GestureRecognizer` 将轨迹转换为 8 方向模式，`GestureMatcher` 按当前前台窗口的作用域选择规则，`ActionExecutor` 执行命中的动作。

性能约束：钩子回调、手势解析线程和 16ms 边缘轮询都属于延迟敏感路径，任何对"外部进程"的查询都可能被安全软件的自防卫机制拖慢。窗口所属进程的身份统一由 `WindowProcessCache` 提供（1 秒短期缓存），底层用 `OpenProcess` + `QueryFullProcessImageNameW` 读取，不枚举模块也不访问 `Process.MainModule`。实测在卡巴斯基 UI 进程上，`MainModule` 单次约 700ms，而 `QueryFullProcessImageNameW` 约 0.15ms；钩子回调超过 10ms 或进程查询超过 50ms 都会写入应用日志以便发现回归。为避免同类问题影响输入链，手势捕获门控和边缘操作轮询都不在各自的触发线程上做查询：`GestureCaptureGate` 在后台线程按 32ms 发布"当前窗口是否禁止手势"的快照，hook 只读快照；`EdgeActionService` 的 16ms 轮询也运行在独立后台线程，UI 线程只接收已命中的动作。

手势跟踪期间，原始中键和右键事件会被吞掉；移动距离不足以构成手势时，会重放对应的普通鼠标单击。动作在按钮抬起时执行，匹配结果可在移动过程中由全虚拟桌面轨迹覆盖层显示。最终提示会无缝接续预览，按 `gestureHint` 独立的显示和淡出时长消失；覆盖层显式鼠标穿透，不阻挡底层应用操作。

该覆盖层由 `MouseTrailForm` 承担分层窗口、原生缓冲区和画面提交，`MouseTrailRenderer` 负责路径与轨迹绘制，`GestureHintRenderer` 负责提示样式、布局和淡出状态，`LevelOsdRenderer` 负责音量/亮度 OSD 的图标、轨道、数值、布局和淡出状态。`LevelOsdOverlay` 会把来自手势、边缘操作和后台亮度队列的 OSD 请求投递回主 UI 线程。各渲染器使用独立 alpha 和生命周期，避免一个提示淡出或隐藏时影响其他内容；它们复用同一覆盖窗口，避免独立提示窗的定位和显示时序问题。

## 规则作用域

规则可使用以下作用域：

- `global`：所有应用可用的默认规则。
- `category:<分类名>`：前台应用被归属到该分类时可用。
- `app:<进程名>`：仅当前台进程名匹配时可用。

同一鼠标键和手势模式存在多个匹配项时，程序规则优先于分类和全局规则。应用程序可按关联顺序归属多个分类，分类规则冲突时越靠前的分类优先级越高；应用程序归属由配置中的 `applications` 列表维护，使用前台进程名进行匹配。

## 动作与运行时界面

当前动作类型包括快捷键、窗口控制、音量控制和亮度控制。边缘操作独立于手势规则，可配置触发角、摩擦边和边缘滚轮。快捷键通过 `SendInput` 注入，注入失败时会先补发全量抬键避免修饰键卡住，再短暂重试；仍失败则记录按键、成功事件数和 Win32 错误码。边缘轮询只接受 `GetCursorPos` 成功返回的光标位置，并在系统唤醒后短暂忽略采样——唤醒过渡期间该调用会失败，而 WinForms 的 `Cursor.Position` 会静默返回 `(0, 0)`，正好命中左上角触发角。

原生侧还提供轨迹线、手势提示、音量/亮度 OSD，以及全屏禁用、应用排除、暂停和托盘驻留。各项显示行为由 `uiSettings` 配置控制。

## 自动更新

`UpdateCoordinator` 在进程启动后延迟检查一次发行版（`uiSettings.update` 可关闭）。检查会查询 Gitee 和 GitHub，按版本号取较高者：首选源已经提供更高版本时不再等待另一个源，首选源没有更高版本时才继续比较另一个源，因此镜像落后或缺少发行版都不会导致回退安装；开始下载前还会再校验一次更新包版本。`UpdateService` 负责解析 Release API、挑选 `WuGesture-v<版本>-windows-x64.zip`、下载到 `%LocalAppData%\WuGesture\updates`，并从发行说明里的 `SHA256 (<ZIP 文件名>): <哈希>` 行读取期望哈希校验完整性（不需要额外下载校验文件），然后解压并检查 `WuGesture.exe` 与 `Web\index.html`。真正的替换发生在应用退出之后：宿主把内置的 `apply-update.ps1` 和路径参数交给独立的 PowerShell 进程，由它等待当前进程结束、备份安装目录、移入新版本并重启；安装目录不可写时通过 UAC 启动该脚本。应用内更新只替换完整发行包，不做逐文件差异更新，避免 DLL 与前端资源版本混用。

## WebView2 集成

前端工程位于 `src/WuGesture.App/Web`。桌面工程会在构建时调用 `pnpm exec vite build --outDir <宿主输出目录>/Web`；Vite 先清空最终输出目录，因此构建结果直接位于宿主的 `Web`，不会覆盖前端源码。普通构建使用 `OutputPath/Web`，`dotnet publish -o` 使用实际 `PublishDir/Web`，再经由 WebView2 虚拟主机 `https://gesture.wu.philosophy/` 加载。该步骤是增量的：MSBuild 以 `Web` 源码为 `Inputs`、宿主 `Web\index.html` 为 `Outputs`，源文件都不比输出新时整段 `vite build` 会被跳过；Vite 对资源名做哈希，任何真实前端改动都会重写 `index.html`。

WebView2 的浏览器 profile 固定在 `%LocalAppData%\WuGesture\WebView2`，不放在安装目录旁边，因此安装到只读目录或清空输出目录都不会影响配置界面。宿主使用显式 `CoreWebView2Environment`，配置窗口隐藏到托盘时释放控件，并在浏览器进程没有自行退出时将其回收；`ready` / `disposed` 日志会带上浏览器进程号，便于确认后台是否真的不再持有 Chromium 进程。

前端页面、组件、路由、状态模块与 WebView 消息细节见 [前端项目地图](../src/WuGesture.App/Web/PROJECT_STRUCTURE.md)。桌面端模块和配置契约见 [项目结构](../PROJECT_STRUCTURE.md)。
