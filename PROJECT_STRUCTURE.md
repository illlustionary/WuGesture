# 项目结构

这是 `my-gesture` 的项目地图。每次修改前先阅读；只要改动影响项目布局、启动流程、配置、构建/发布行为，或核心模块职责，就要同步更新这里。

## 产品方向

`my-gesture` 是一个 Windows 鼠标手势应用。

当前目标：

- 原生后端负责全局鼠标钩子、手势识别、规则匹配和动作执行。
- WebView2 前端负责配置界面，当前是独立的 Vue3 + Vite 工程，构建产物由桌面宿主加载。
- 手势使用 8 个方向。
- 动作当前支持快捷键和窗口控制；快捷键通过 `SendInput` 执行，窗口控制通过 Win32 窗口 API 执行。
- 规则当前支持 `global`、`category` 和 `app` 作用域，并按 `app > category > global` 优先级匹配。

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

- `Program.cs` 启动 `MainForm`。
- `MainForm.cs` 初始化 WebView2、加载配置、创建 `GestureService`，并桥接 WebView 消息；也会把配置里的 `uiSettings` 应用到轨迹窗和提示窗。
- 配置窗口首次启动时默认占据主屏工作区的一半，并居中显示；关闭时会保存窗口位置、大小和最大化状态，下次启动时恢复。

历史模板文件：

- `Form1.cs`
- `Form1.Designer.cs`

这两个文件是未使用的模板残留。

## 手势引擎

路径：

```text
src\MyGesture.App\GestureEngine
```

关键文件：

- `MouseHook.cs`：低级全局鼠标钩子。
- `KeyboardShortcutRecorder.cs`：低级键盘 hook，用于配置界面录制快捷键并吞掉录制期间的原生键盘事件。
- `GestureService.cs`：跟踪右键和中键轨迹生命周期，调用识别器、匹配器和执行器，并向 UI 发送事件；也支持录制会话，把识别结果回传给前端。
- `GestureRecognizer.cs`：把鼠标轨迹转换为稳定的 8 方向模式。
- `GestureMatcher.cs`：将识别出的鼠标键和方向模式与已加载规则进行匹配，并按作用域优先级选择命中项。
- `GestureScopeContext.cs`：当前前台窗口的 app/category 上下文模型。
- `ForegroundWindowScopeContextProvider.cs`：读取前台窗口进程名。
- `ConfiguredScopeContextProvider.cs`：用配置里的应用程序列表把前台进程名映射到分类，供作用域匹配使用。
- `ActionExecutor.cs`：按动作类型执行命令；快捷键通过 Win32 `SendInput` 执行，窗口控制通过 `ShowWindow`、`SetWindowPos` 和窗口消息执行。
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
- `GestureConfigStore.cs`：加载、保存、重置默认配置。
- `GestureConfigMapper.cs`：把配置 DTO 映射为运行时 `GestureRule`。
- `DefaultGestureRules.cs`：默认全局规则。

当前支持的配置：

- `scope`：支持 `global`、`category:<分类名>`、`app:<进程名>`；运行时按 `app > category > global` 优先级匹配。
- `mouseButton`：支持 `right`、`middle`，运行时会按当前触发的鼠标键区分规则。
- `pattern`：手势方向列表，例如 `["Down", "Right"]`。
- `action.type`：支持 `hotkey` 和 `window`。
- `action.keys`：按键列表，例如 `["Control", "W"]`；允许为空，表示先保存手势，之后再补命令，空命令规则不会参与运行时执行。
- `action.operation`：窗口控制操作，仅在 `action.type` 为 `window` 时使用；当前支持 `toggle-topmost`、`toggle-maximize`、`minimize`、`close`。
- `applications`：应用程序归属列表，每项包含 `name`、`displayName`、`path`、`category`，运行时通过前台进程名匹配 `name` 后得到分类；`displayName` 只用于 UI 展示和编辑。
- `uiSettings.mouseTrail`：轨迹窗设置，包含 `inactiveColor`、`activeColor`、`inactiveThickness`、`activeThickness`、`thickness`、`inactiveOpacity`、`activeOpacity`；`thickness` 保留用于兼容旧配置。
- `uiSettings.gestureHint`：提示泡泡设置，包含 `fontFamily`、`fontSize`、`textColor`、`backgroundColor`、`backgroundOpacity`、`width`、`autoWidth`、`height`、`bottomOffset`。

默认规则：

```text
Left        -> Alt + Left
Right       -> Alt + Right
Down,Right  -> Control + W
```

## WebView 前端

路径：

```text
src\MyGesture.App\Web
```

文件：

- `index.html`
- `styles.scss`
- `package.json`：前端工程依赖与脚本。
- `vite.config.js`：Vite 构建配置。
- `src\main.js`：Vue 入口。
- `src\App.vue`：路由壳、全局弹窗挂载和规则编辑弹窗挂载，顶部 `...` 入口会跳转到设置页。
- `src\components\`：顶部栏、页面壳、左侧插槽和规则表等通用组件。
- `src\composables\gestureEditorStore.js`：共享编辑状态、WebView 消息和快捷键监听。
- `src\pages\`：`global`、`category`、`app`、`settings` 四个路由页。
- `src\styles.scss`：编辑器全局设计 token、reset、通用按钮、输入框、弹窗和图标样式；组件和页面专属样式放在对应 `.vue` 文件的 scoped SCSS 中。
- `dist\web\`：Vite 构建产物目录，由桌面宿主加载。

构建方式：

- 前端使用 `pnpm build` 生成根目录下的 `dist\web`。
- 前端 `pnpm` 构建脚本通过 `src\MyGesture.App\Web\pnpm-workspace.yaml` 放行 `@parcel/watcher` 的本地构建脚本，避免非交互环境下的依赖安装中断。
- `MyGesture.App.csproj` 会在 `.NET` 构建前自动执行前端构建。
- `MyGesture.App.csproj` 会在前端构建后把 `dist\web` 复制到宿主输出目录中的 `Web\dist`。
- 桌面宿主通过 WebView2 虚拟主机 `https://appassets.local/` 加载宿主输出目录中的 `Web\dist`。

当前 UI：

- 3 个规则 tab：`全局`、`分类`、`程序`，顶部改为更紧凑的分段式切换，当前项使用更轻量的高亮态。
- 顶部 `...` 按钮会打开独立的 `settings` 页面，用于配置轨迹线和底部提示窗外观。
- 设置页右上角提供恢复默认按钮，页面中的调整会在输入变化时 debounce 自动保存，并在控件变更结束或离开页面时强制提交最后一次修改；本地编辑期间会避免宿主回传覆盖当前滑块值，不再依赖底部操作按钮。
- `全局` 直接编辑整张表。
- `分类` 和 `程序` 采用左右布局：左侧是分类/程序列表和底部新增按钮，右侧是对应内容区。
- `分类` 页右侧包含“应用程序”和“手势列表”两个区块，分类页可管理当前分类下的 App。
- `程序` 页右侧只展示手势列表；程序页左侧会列出已保存的全部程序，程序规则仍按 app 作用域单独维护。
- 分类新增通过名称弹窗完成，不再使用左侧内联输入框；分类和程序名称都改为双击列表项后在弹窗里重命名。
- 分类页和程序页添加程序时都会先显示前端弹窗，用户可按住“拖动准星选择窗口”拖到目标窗口松开，或选择“浏览 exe 文件”作为备用方式；分类页添加会关联到当前分类，程序页添加会创建并选中对应 app 规则作用域。
- 程序列表和详情会展示从 exe 路径动态提取的应用图标；图标通过 WebView 消息传递，不写入配置文件。
- 程序名称可通过双击弹窗重命名，进程名称保持只读并用于规则匹配；重命名后列表显示会同步更新。
- 规则表列为 `名称`、`手势`、`命令`，删除按钮默认隐藏、在行悬浮时才显示；双击规则行、点击手势列或点击命令列都会打开规则编辑弹窗，手势列改为更具图形感的鼠标示意图，并把按键拆成键帽样式。
- 添加/编辑手势通过弹窗完成：弹窗里可选择命令类型（`快捷键` 或 `窗口控制`），快捷键命令显示录制按钮，窗口控制命令显示操作下拉框；点击“录制手势”后，由后端直接复用正常鼠标轨迹采集和识别流程，识别出 8 方向手势后回传前端写入规则列表；如果名称为空，会使用手势助记符作为默认名称。
- 添加/编辑手势弹窗不再包含前端绘制 canvas，控制区只负责编辑名称、命令和触发录制，不会遮挡输入框。
- 添加/编辑手势弹窗只允许通过遮罩点击或右上角关闭按钮退出；底部确认/取消按钮已移除，名称输入和命令控件在失焦或变更时会把当前 draft 持久化到规则里。
- 规则编辑、删除、快捷键录制、分类/App 变更会发送 `save-rules` 写入配置文件；行内规则名称编辑改为失焦后提交，避免打字时触发保存打断输入，而弹窗里的名称输入仍保持即时编辑体验。
- 添加/编辑手势弹窗打开时会通过 WebView 消息暂停全局手势，避免全局鼠标钩子干扰编辑；手势录制由后端接管，轨迹会通过原生 `MouseTrailForm` 在屏幕上显示，录制期间不显示全局命中提示窗，前端只接收最终识别结果。
- 已移除编辑器内的手势提示区，只保留配置结果提示；新增、删除、重置和配置错误等操作会通过 toast 弹出反馈。
- 快捷键命令不支持手动输入；选择 `快捷键` 后点击录制按钮进入录制中，后端拦截并记录系统按键，松开所有按键后显示录制结果。

WebView 消息流：

- 前端发送：
  - `"get-status"`
  - `{ type: "select-application", requestId: "...", category: "..." }`
  - `{ type: "pick-application-window", requestId: "...", category: "..." }`
  - `{ type: "start-gesture-recording", requestId: "..." }`
  - `{ type: "stop-gesture-recording" }`
  - `{ type: "set-gesture-paused", paused: true/false }`
  - `{ type: "start-hotkey-recording", requestId: "..." }`
  - `{ type: "stop-hotkey-recording" }`
  - `{ type: "save-rules", rules: [{ scope, mouseButton, pattern, actionName, action }, ...], applications: [...], uiSettings: {...} }`
  - `{ type: "reload-rules" }`
  - `{ type: "reset-rules" }`
- 后端发送：
  - `{ type: "status", ... }`
  - `{ type: "rules", rules: [...], applications: [{ name, displayName, path, category, icon }, ...], uiSettings: {...}, ... }`
  - `{ type: "application-selected", requestId: "...", name: "...", displayName: "...", path: "...", category: "...", icon: "..." }`
  - `{ type: "gesture-recorded", requestId: "...", button: "right|middle", pattern: ["Down", "Right"] }`
  - `{ type: "hotkey-recorded", requestId: "...", keys: ["Control", "W"] }`
  - `{ type: "gesture", ... }`
  - `{ type: "gesture-action-failed", ... }`
  - `{ type: "config-result", ... }`

## 测试

当前仓库没有独立的测试项目。`MyGesture.slnx` 目前只包含桌面应用工程。

如果后续补回测试工程，可在此补充对应路径、覆盖范围和运行命令。

## 构建与发布

构建：

```powershell
dotnet build MyGesture.slnx
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
artifacts\publish\MyGesture
```

`artifacts/` 已被 Git 忽略。

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
