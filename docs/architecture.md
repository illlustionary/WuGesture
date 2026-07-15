# 架构说明

WuGesture 的桌面宿主使用 WinForms 和 WebView2；配置界面是独立的 Vue3 + Vite 工程。原生侧负责输入钩子、手势识别、规则匹配和动作执行，WebView2 负责编辑和展示配置。

## 手势流水线

```text
MouseHook
-> GestureService
-> GestureRecognizer
-> GestureMatcher
-> ActionExecutor
-> GestureHintForm / WebView 状态
```

`GestureService` 跟踪右键或中键轨迹，`GestureRecognizer` 将轨迹转换为 8 方向模式，`GestureMatcher` 按当前前台窗口的作用域选择规则，`ActionExecutor` 执行命中的动作。

手势跟踪期间，原始中键和右键事件会被吞掉；移动距离不足以构成手势时，会重放对应的普通鼠标单击。动作在按钮抬起时执行，匹配结果可在移动过程中显示于独立的全局提示窗。

## 规则作用域

规则可使用以下作用域：

- `global`：所有应用可用的默认规则。
- `category:<分类名>`：前台应用被归属到该分类时可用。
- `app:<进程名>`：仅当前台进程名匹配时可用。

同一鼠标键和手势模式存在多个匹配项时，优先级为 `app > category > global`。应用程序归属由配置中的 `applications` 列表维护，使用前台进程名进行匹配。

## 动作与运行时界面

当前动作类型包括快捷键、窗口控制、音量控制和亮度控制。边缘操作独立于手势规则，可配置触发角、摩擦边和边缘滚轮。

原生侧还提供轨迹线、手势提示、音量/亮度 OSD，以及全屏禁用、应用排除、暂停和托盘驻留。各项显示行为由 `uiSettings` 配置控制。

## WebView2 集成

前端工程位于 `src/WuGesture.App/Web`。`pnpm build` 会生成 `dist/web`，桌面工程会在构建时将其复制到宿主输出目录中的 `Web/dist`，再经由 WebView2 虚拟主机 `https://appassets.local/` 加载。

前端页面、组件、路由、状态模块与 WebView 消息细节见 [前端项目地图](../src/WuGesture.App/Web/PROJECT_STRUCTURE.md)。桌面端模块和配置契约见 [项目结构](../PROJECT_STRUCTURE.md)。
