# Web 项目结构

这是 `src\MyGesture.App\Web` 的项目地图。修改 Web 前端页面、路由、组件、样式、构建配置或 WebView 消息时，先阅读并按需同步更新这里。

## 概览

这是 Wu Gesture 的 Vite + Vue 3 前端工程，由桌面宿主通过 WebView2 加载。它负责规则编辑、分类和程序管理、边缘操作配置，以及轨迹线和提示窗外观设置。

## 根目录

```text
src\MyGesture.App\Web
├─ agents.md
├─ index.html
├─ package.json
├─ pnpm-lock.yaml
├─ pnpm-workspace.yaml
├─ uno.config.js
├─ vite.config.js
├─ PROJECT_STRUCTURE.md
├─ node_modules
└─ src
```

根目录重要文件：

- `agents.md`：Web 子项目的代理指令。
- `index.html`：Vite 入口 HTML，页面标题为 `Wu Gesture`。
- `package.json`：前端工程依赖与脚本。
- `pnpm-lock.yaml`：锁定依赖树。
- `pnpm-workspace.yaml`：工作区配置，并放行 `@parcel/watcher` 的本地构建脚本，避免非交互环境下依赖安装中断。
- `uno.config.js`：UnoCSS 配置，集中定义页面区块、表单、列表等常用 shortcuts。
- `vite.config.js`：Vite 构建配置，包含 `vite-svg-loader` 和 UnoCSS Vite 插件。
- `PROJECT_STRUCTURE.md`：当前 Web 子项目地图。
- `node_modules`：本地依赖目录，不纳入源码维护。

## Source Structure

```text
src
├─ main.js
├─ App.vue
├─ styles.scss
├─ assets
├─ components
├─ composables
├─ constants
├─ utils
└─ pages
```

- `src\main.js`：Vue 应用入口，设置 hash 路由、加载 `virtual:uno.css` 并挂载应用。
- `src\App.vue`：路由壳、全局弹窗挂载和规则编辑弹窗挂载，顶部 `...` 入口会跳转到设置页。
- `src\styles.scss`：编辑器全局设计 token、reset、通用按钮、输入框、弹窗和图标样式；常用布局/区块/表单/列表样式优先用 UnoCSS shortcuts，复杂动态样式和组件专属样式放在对应 `.vue` 文件的 scoped SCSS 中。

## Assets

路径：

```text
src\assets
```

当前 SVG 图标：

- `add.svg`
- `briefcase.svg`
- `browser.svg`
- `circle-dashed.svg`
- `close.svg`
- `close-alt.svg`
- `code.svg`
- `crosshair.svg`
- `delete.svg`
- `download.svg`
- `folder.svg`
- `keyboard.svg`
- `media.svg`
- `mouse.svg`
- `record.svg`
- `reset.svg`
- `setting.svg`
- `sparkle.svg`
- `test.svg`
- `upload.svg`

这些 SVG 通过 `vite-svg-loader` 作为 Vue 组件导入。图标文件名统一使用小写 kebab-case，路径颜色应使用 `currentColor`，便于按钮和状态样式控制。

## Components

路径：

```text
src\components
```

- `AppHeader.vue`：顶部栏和规则 tab / 设置入口。
- `AppShell.vue`：页面布局壳，提供主体区域和插槽。
- `GestureRuleDialog.vue`：添加和编辑手势规则的弹窗。
- `GestureRuleList.vue`：规则表、规则展示和规则操作入口。
- `HoverBubble.vue`：悬浮提示气泡。
- `IconActionButton.vue`：共享图标按钮，集中导入 `src\assets` 下的 SVG，并通过 `icon` key 映射到按钮图标。
- `ConfirmDialog.vue`：重大操作确认弹窗，带缩放进入/退出动画。
- `CustomSelect.vue`：共享弹层式自定义单选下拉控件，不复用浏览器默认 select。
- `ToggleCheckbox.vue`：共享自定义复选控件，用于替代浏览器默认 checkbox。
- `ScopeCreateDialog.vue`：分类或作用域名称创建/编辑弹窗。
- `ScopeSidebar.vue`：分类和程序等作用域列表侧栏。
- `applications\ApplicationListItem.vue`：分类页右侧程序关联列表项。
- `rules\RulesSection.vue`：规则页右侧复用区块，统一标题、说明、操作区和内容面板。
- `scope\ScopeListItem.vue`：分类页和程序页左侧作用域列表项，统一选中态、键盘选择、双击重命名和删除操作。

## Composables

路径：

```text
src\composables
```

- `gestureEditorStore.js`：共享编辑状态、规则加载保存、作用域选择、应用/分类状态、WebView 消息、快捷键监听、手势录制状态和 UI 设置同步；静态选项、默认值和纯格式化/归一化逻辑放在 `constants` / `utils`。
- `useGestureEditorApplicationPicker.js`：共享 store 内部使用的应用选择器流程，包括打开选择器、发起窗口/文件选择请求，以及处理宿主返回的程序信息。
- `gestureEditor\useGestureScopes.js`：分类/程序 scope 选择、新增、重命名、删除和规则查询。
- `gestureEditor\useGestureApplications.js`：应用程序视图数据读取、创建、显示名和分类字段更新。
- `gestureEditor\useCategoryApplications.js`：分类与应用程序之间的关联和移除。
- `gestureEditor\useGestureEditorNotifications.js`：配置状态消息和 toast 的统一通知入口。
- `gestureEditor\useGestureEditorWebViewBridge.js`：WebView 消息发送、静默发送、可用性判断和消息监听入口。

## Constants

路径：

```text
src\constants
```

- `gestureEditorOptions.js`：手势编辑器使用的鼠标按键符号、方向符号、动作操作选项和边缘位置选项。
- `gestureEditorDefaults.js`：浏览器预览默认规则、默认应用、默认 UI 设置和默认边缘操作模板。

## Utils

路径：

```text
src\utils
```

- `gestureEditorNormalizers.js`：规则 scope、手势文本、快捷键、动作类型、边缘操作和 UI 设置的解析与归一化工具。
- `gestureEditorFormatters.js`：手势助记符、动作展示文案和边缘操作展示文案。
- `gestureEditorPayloads.js`：把编辑器内存模型转换成 WebView 保存、WebDAV 测试/备份使用的配置 payload。
- `gestureEditorViewModels.js`：把宿主配置消息转换成编辑器视图模型，并提供规则和手势草稿模板。
- `gestureEditorCollections.js`：按规则和应用程序列表聚合分类/程序侧栏条目。

## Pages

路径：

```text
src\pages
```

页面按目录组织，目录名就是页面名，入口统一为 `index.vue`。页面私有组件放在同级 `components`，页面私有逻辑放在同级 `composables`。

- `global\index.vue`：全局规则页，直接编辑全局规则表。
- `category\index.vue`：分类规则页，左侧分类列表，右侧包含分类下应用程序和手势列表。
  - `category\composables\useCategoryPage.js`：分类页私有弹窗草稿、分类新增/重命名/删除编排和分类图标选择。
- `app\index.vue`：程序规则页，左侧程序列表，右侧展示当前程序的手势列表。
  - `app\composables\useAppPage.js`：程序页私有重命名弹窗、删除编排和程序图标 fallback 文本。
- `edge\index.vue`：边缘操作页，按触发角、摩擦边、边缘滚动三组展示配置。
  - `edge\components\EdgeActionSection.vue`：边缘操作大项区块。
  - `edge\components\EdgeActionCard.vue`：单个边缘操作卡片。
  - `edge\components\EdgeActionDialog.vue`：边缘操作编辑弹窗。
  - `edge\components\EdgeActionCommandFields.vue`：快捷键、窗口、音量、亮度命令字段。
  - `edge\composables\useEdgeActionDraft.js`：边缘操作编辑草稿、打开、关闭和保存逻辑。
- `exclusions\index.vue`：排除项页，用于维护不执行鼠标手势的程序列表，并可对单个排除项禁用边缘操作。
- `settings\index.vue`：设置页，用于组合轨迹线、底部提示窗、应用行为和 WebDAV 配置区块。
  - `settings\components\SettingsSectionCard.vue`：设置页大项标题、说明、操作区和内容布局。
  - `settings\components\SettingsFormGrid.vue`：设置项双列表单布局。
  - `settings\components\SettingsField.vue`：设置项卡片。
  - `settings\components\MouseTrailSettings.vue`：轨迹线配置，顶部内置轨迹线实时预览。
  - `settings\components\AppBehaviorSettings.vue`：应用行为配置，包括开机启动、管理员启动、暂停 Wu Gesture 和关闭按钮行为。
  - `settings\components\WebDavSettings.vue`：WebDAV 配置、测试、恢复和保存操作。
  - `settings\components\GestureHintSettings.vue`：底部提示窗配置，顶部内置提示窗实时预览。
  - `settings\composables\useUiSettingsDraft.js`：设置草稿归一化、debounce 保存、重置和预览样式计算。

## Routing

路由使用 hash 模式，当前页面包括：

- `global`
- `category`
- `app`
- `edge`
- `exclusions`
- `settings`

顶部规则 tab 包含 `全局`、`分类`、`程序`、`边缘操作`、`排除项`。顶部 `...` 按钮打开独立的 `settings` 页面。

## 当前 UI

- `全局` 直接编辑整张表。
- `分类` 和 `程序` 采用左右布局：左侧是分类/程序列表和底部新增按钮，右侧是对应内容区。
- `分类` 页右侧包含“应用程序”和“手势列表”两个区块，分类页可管理当前分类下的 App。
- `程序` 页右侧只展示手势列表；程序页左侧会列出已保存的全部程序，程序规则仍按 app 作用域单独维护。
- `边缘操作` 页按触发角、摩擦边、边缘滚动三组展示配置；点击卡片打开独立弹窗编辑启用状态、命令和参数，名称由触发类型、位置与滚轮方向自动生成，关闭弹窗后自动保存。
- `排除项` 页维护不执行鼠标手势的程序列表；可通过拖动准星或浏览 exe 添加程序，也可为单个排除项勾选“同时禁用边缘操作”，列表会展示从 exe 路径动态提取的应用图标。
- `设置` 页右上角提供恢复默认按钮，点击后通过确认弹窗二次确认；页面中的调整会在输入变化时 debounce 自动保存，并在控件变更结束或离开页面时强制提交最后一次修改；本地编辑期间会避免宿主回传覆盖当前滑块值。设置页也包含开机自启动、以管理员身份打开、暂停 Wu Gesture、关闭按钮行为，以及 WebDAV 地址、账号、密码和远程路径。开机自启动、以管理员身份打开和暂停 Wu Gesture 等布尔项使用共享自定义复选控件，单选下拉使用共享弹层式 `CustomSelect`。轨迹线和底部提示窗预览分别内置在对应设置区块顶部。WebDAV 区域提供测试、恢复和保存按钮；当前 WebDAV 参数测试成功后，才允许把当前配置保存到远程或从远程恢复本地配置。
- 分类新增通过名称弹窗完成，不再使用左侧内联输入框；分类和程序名称都通过双击列表项后在弹窗里重命名。
- 分类页、程序页和排除项页添加程序时都会先显示前端弹窗，用户可按住“拖动准星选择窗口”拖到目标窗口松开，或选择“浏览 exe 文件”作为备用方式。
- 程序列表和详情会展示从 exe 路径动态提取的应用图标；图标通过 WebView 消息传递，不写入配置文件。
- 规则表列为 `名称`、`手势`、`命令`，删除按钮默认隐藏、在行悬浮时才显示；双击规则行、点击手势列或点击命令列都会打开规则编辑弹窗。
- 添加/编辑手势通过弹窗完成：弹窗里可选择命令类型，快捷键命令显示录制按钮，窗口控制命令显示操作下拉框。
- 添加/编辑手势弹窗打开时会通过 WebView 消息暂停全局手势；手势录制由后端接管，前端只接收最终识别结果。
- 规则编辑、删除、快捷键录制、分类/App 变更会发送 `save-rules` 写入配置文件。
- 已移除编辑器内的手势提示区，只保留配置结果提示；新增、删除、重置和配置错误等操作会通过 toast 弹出反馈。

## WebView 消息流

前端发送：

- `"get-status"`
- `{ type: "select-application", requestId: "...", category: "..." }`
- `{ type: "pick-application-window", requestId: "...", category: "..." }`
- `{ type: "start-gesture-recording", requestId: "..." }`
- `{ type: "stop-gesture-recording" }`
- `{ type: "set-gesture-paused", paused: true/false }`
- `{ type: "start-hotkey-recording", requestId: "..." }`
- `{ type: "stop-hotkey-recording" }`
- `{ type: "save-rules", rules: [{ scope, mouseButton, pattern, actionName, action }, ...], applications: [...], edgeActions: [...], uiSettings: {...} }`
- `{ type: "webdav-test", rules: [...], applications: [...], edgeActions: [...], uiSettings: {...} }`
- `{ type: "webdav-save", rules: [...], applications: [...], edgeActions: [...], uiSettings: {...} }`
- `{ type: "webdav-restore", rules: [...], applications: [...], edgeActions: [...], uiSettings: {...} }`
- `{ type: "reload-rules" }`
- `{ type: "reset-rules" }`

后端发送：

- `{ type: "status", ... }`
- `{ type: "rules", rules: [...], applications: [{ name, displayName, path, category, icon }, ...], edgeActions: [...], uiSettings: {...}, ... }`；其中 `uiSettings.appBehavior.excludedApplications` 的运行态消息项会额外带 `icon`，不写入配置文件。
- `{ type: "application-selected", requestId: "...", name: "...", displayName: "...", path: "...", category: "...", icon: "..." }`
- `{ type: "gesture-recorded", requestId: "...", button: "right|middle", pattern: ["Down", "Right"] }`
- `{ type: "hotkey-recorded", requestId: "...", keys: ["Control", "W"] }`
- `{ type: "gesture", ... }`
- `{ type: "gesture-action-failed", ... }`
- `{ type: "edge-action-failed", ... }`
- `{ type: "config-result", ... }`
- `{ type: "webdav-result", operation: "test|save|restore", success: true/false, message: "..." }`

`uiSettings.appBehavior` 当前包含：

- `launchAtStartup`
- `runAsAdministrator`
- `closeButtonBehavior`
- `gesturePaused`
- `excludedApplications: [{ name, displayName, path, disableEdgeActions }]`

## 构建

- 前端使用 `pnpm build` 生成仓库根目录下的 `dist\web`。
- `MyGesture.App.csproj` 会在 `.NET` 构建前自动执行前端构建。
- `MyGesture.App.csproj` 会在前端构建后把 `dist\web` 复制到宿主输出目录中的 `Web\dist`。
- 桌面宿主通过 WebView2 虚拟主机 `https://appassets.local/` 加载宿主输出目录中的 `Web\dist`。

## 何时更新此文件

在修改以下内容时同步更新：

- Web 目录结构
- 页面、路由、布局或复用组件
- 前端构建配置和输出路径
- WebView 消息契约
- 规则编辑器、边缘操作或设置页的核心职责
- 图标资产和共享 UI 组件职责
