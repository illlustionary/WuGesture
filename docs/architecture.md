# 架构说明

WuGesture 的桌面宿主使用 WinForms 和 WebView2；配置界面是独立的 Vue3 + Vite 工程。原生侧负责输入钩子、手势识别、规则匹配和动作执行，WebView2 负责编辑和展示配置。

## 启动与依赖

发行包直接以 framework-dependent 的 `WuGesture.exe` 作为桌面应用入口，开机自启动和管理员重启也直接调用该文件。普通启动默认显示配置窗口，可在应用行为中关闭并改为后台驻留；开机自启动始终后台运行。`.NET 10 Desktop Runtime x64` 未安装时，.NET Host 会在应用代码执行前显示系统安装提示。应用启动后会检查 WebView2 Runtime；缺少时显示下载引导并退出，避免在初始化配置界面时出现不明确的异常。

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

手势跟踪期间，原始中键和右键事件会被吞掉；移动距离不足以构成手势时，会重放对应的普通鼠标单击。动作在按钮抬起时执行，匹配结果可在移动过程中由全虚拟桌面轨迹覆盖层显示。最终提示会无缝接续预览，按 `gestureHint` 独立的显示和淡出时长消失；覆盖层显式鼠标穿透，不阻挡底层应用操作。

该覆盖层由 `MouseTrailForm` 承担分层窗口、原生缓冲区和画面提交，`MouseTrailRenderer` 负责路径与轨迹绘制，`GestureHintRenderer` 负责提示样式、布局和淡出状态，`LevelOsdRenderer` 负责音量/亮度 OSD 的图标、轨道、数值、布局和淡出状态。`LevelOsdOverlay` 会把来自手势、边缘操作和后台亮度队列的 OSD 请求投递回主 UI 线程。各渲染器使用独立 alpha 和生命周期，避免一个提示淡出或隐藏时影响其他内容；它们复用同一覆盖窗口，避免独立提示窗的定位和显示时序问题。

## 规则作用域

规则可使用以下作用域：

- `global`：所有应用可用的默认规则。
- `category:<分类名>`：前台应用被归属到该分类时可用。
- `app:<进程名>`：仅当前台进程名匹配时可用。

同一鼠标键和手势模式存在多个匹配项时，程序规则优先于分类和全局规则。应用程序可按关联顺序归属多个分类，分类规则冲突时后关联的分类覆盖前者；应用程序归属由配置中的 `applications` 列表维护，使用前台进程名进行匹配。

## 动作与运行时界面

当前动作类型包括快捷键、窗口控制、音量控制和亮度控制。边缘操作独立于手势规则，可配置触发角、摩擦边和边缘滚轮。

原生侧还提供轨迹线、手势提示、音量/亮度 OSD，以及全屏禁用、应用排除、暂停和托盘驻留。各项显示行为由 `uiSettings` 配置控制。

## WebView2 集成

前端工程位于 `src/WuGesture.App/Web`。`pnpm build` 会生成 `dist/web`，桌面工程会在构建时将其复制到宿主输出目录中的 `Web/dist`，再经由 WebView2 虚拟主机 `https://gesture.wu.philosophy/` 加载。

前端页面、组件、路由、状态模块与 WebView 消息细节见 [前端项目地图](../src/WuGesture.App/Web/PROJECT_STRUCTURE.md)。桌面端模块和配置契约见 [项目结构](../PROJECT_STRUCTURE.md)。
