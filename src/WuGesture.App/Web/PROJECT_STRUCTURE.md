# Web 项目结构

这是 `src\WuGesture.App\Web` 的项目地图。修改 Web 前端页面、路由、组件、样式、构建配置或 WebView 消息时，先阅读并按需同步更新这里。

## 概览

这是 WuGesture 的 Vite + Vue 3 前端工程，由桌面宿主通过 WebView2 加载。它负责规则编辑、分类和程序管理、边缘操作配置，以及轨迹线和提示窗外观设置。

## 根目录

```text
src\WuGesture.App\Web
├─ agents.md
├─ index.html
├─ package.json
├─ pnpm-lock.yaml
├─ pnpm-workspace.yaml
├─ jsconfig.json
├─ uno.config.js
├─ vite.config.js
├─ PROJECT_STRUCTURE.md
├─ node_modules
└─ src
```

根目录重要文件：

- `agents.md`：Web 子项目的代理指令。
- `index.html`：Vite 入口 HTML，页面标题为 `WuGesture`。
- `package.json`：前端工程依赖与脚本。
- `pnpm-lock.yaml`：锁定依赖树。
- `pnpm-workspace.yaml`：工作区配置，并放行 `@parcel/watcher` 的本地构建脚本，避免非交互环境下依赖安装中断。
- `jsconfig.json`：配置编辑器路径提示，`@/*` 指向 `src/*`。
- `uno.config.js`：UnoCSS 配置，集中定义页面区块、表单、列表等常用 shortcuts。
- `vite.config.js`：Vite 构建配置，包含 `vite-svg-loader`、UnoCSS Vite 插件和 `@` 到 `src` 的路径别名。
- `PROJECT_STRUCTURE.md`：当前 Web 子项目地图。
- `node_modules`：本地依赖目录，不纳入源码维护。

## Source Structure

```text
src
├─ main.js
├─ App.vue
├─ router
├─ styles.scss
├─ assets
├─ components
├─ composables
├─ constants
├─ gestureEditor
├─ utils
└─ pages
```

- `src\main.js`：Vue 应用入口，加载全局样式与插件并挂载应用；路由定义集中在 `router` 目录。
- `src\router\index.js`：创建 hash 路由实例。
- `src\router\routes\index.js`：组合基础页面、作用域、边缘操作和设置路由模块；页面组件和嵌套路由承载组件均通过动态 `import()` 懒加载。
- `src\router\routes\scopeRoutes.js`：分类和程序动态子路由。
- `src\router\routes\edgeRoutes.js`：触发角、摩擦边和边缘滚动子路由。
- `src\router\routes\settingsRoutes.js`：各设置项子路由。
- `src\App.vue`：固定自定义顶部栏、侧栏/页面工作区、路由切换过渡和全局弹窗挂载；顶部栏及右下角缩放把手通过生命周期 store 控制宿主窗口。
- `src\styles.scss`：编辑器全局设计 token、reset、通用按钮、输入框、弹窗和图标样式；常用布局/区块/表单/列表样式优先用 UnoCSS shortcuts，复杂动态样式和组件专属样式放在对应 `.vue` 文件的 scoped SCSS 中。

## Assets

路径：

```text
src\assets
```

SVG 按使用位置分目录：

- `actions\`：通用操作，包括新增、删除、导入导出、重置、测试和 WebDAV 上传下载。
- `category\`：分类显示图标，包括浏览器、代码、媒体和星光。
- `gesture\`：手势录制与选择图标，包括准星、键盘和录制。
- `navigation\`：侧栏、搜索和应用状态图标。
- `window\`：窗口标题栏与对话框的关闭、最小化、最大化、还原和缩放把手图标。

这些 SVG 通过 `vite-svg-loader` 作为 Vue 组件导入。图标文件名统一使用小写 kebab-case，路径颜色应使用 `currentColor`，便于按钮和状态样式控制。

## Components

路径：

```text
src\components
```

- `AppTitleBar.vue`：固定自定义窗口顶部栏；左侧承载应用图标、构建版本和暂停/恢复入口，右侧承载最小化、最大化/还原和关闭，空白区负责窗口拖动与双击最大化。
- `AppSidebar.vue`：顶部栏下方的贴边满高多级左侧导航，支持分类/程序动态子项、子菜单搜索、分组折叠、项目重命名/删除以及设置配置导入导出/恢复默认操作。底部左侧按钮可折叠为仅图标导航，搜索和规则优先级帮助入口位于底部右侧。
- `NestedRouteView.vue`：嵌套路由的轻量承载组件，使分类、程序、边缘操作和设置的子路由在同一工作区内独立渲染。
- `AppShell.vue`：无外层卡片样式的页面布局壳，提供主体区域和插槽，由应用壳层负责右侧工作区滚动。
- `BaseDialog.vue`：共享对话框外壳，统一遮罩关闭、可选关闭按钮、默认取消/确认操作区及上移淡出关闭动画；搜索和自动保存编辑器可关闭默认操作区。
- `BaseInput.vue`：共享原生文本、数字、密码、URL 和颜色输入控件的 `v-model` 事件与基础宽度约束。
- `BaseRange.vue`：共享范围滑块，集中维护进度填充样式、`v-model` 和原生输入/变更事件。
- `GestureRuleDialog.vue`：添加和编辑手势规则的弹窗。
- `GestureRuleList.vue`：规则表、规则展示和规则操作入口。
- `HoverBubble.vue`：悬浮提示气泡。
- `IconActionButton.vue`：共享图标按钮，集中导入 `src\assets` 下的 SVG，并通过 `icon` key 映射到按钮图标。
- `WindowResizeGrip.vue`：右下角窗口缩放把手，使用旋转后的三角点阵图标，通过生命周期 store 发起宿主系统缩放，并在缩放周期内抑制标题栏提示。
- `QuickSearchDialog.vue`：全局快速搜索弹层，按配置类型显示匹配结果，包含带搜索图标和焦点反馈的输入字段。
- `ConfirmDialog.vue`：重大操作确认弹窗，带缩放进入/退出动画。
- `CustomSelect.vue`：共享弹层式自定义单选下拉控件，不复用浏览器默认 select。
- `ToggleCheckbox.vue`：共享自定义复选控件，用于替代浏览器默认 checkbox。
- `ScopeCreateDialog.vue`：分类或作用域名称创建/编辑弹窗。
- `ScopeSidebar.vue`：旧版分类和程序作用域列表侧栏，当前分类/程序页面由 `AppSidebar.vue` 提供作用域导航。
- `applications\ApplicationListItem.vue`：分类页右侧程序关联列表项。
- `rules\RulesSection.vue`：规则页右侧复用区块，统一标题、说明、操作区和内容面板。
- `ScopePriorityNotice.vue`：全局、分类和程序规则页共享的作用域优先级与继承提示。
- `scope\ScopeListItem.vue`：旧版分类和程序页作用域列表项，当前页面的动态作用域条目由 `AppSidebar.vue` 直接渲染。

## Composables

路径：

```text
src\composables
```

- `useQuickSearch.js`：全局快速搜索的弹层开关、`Ctrl+K` 生命周期与结果直达路由，协调搜索 store 的 scope 选择和规则编辑入口。

## Gesture Editor

路径：

```text
src\gestureEditor
```

手势编辑器领域模块集中放在这里；`composables` 只保留 Vue 组合式入口或历史兼容 facade。

- `gestureEditor\context\gestureEditorContext.js`：手势编辑器共享单例上下文，集中组装状态、WebView 桥接、通知、作用域、应用、规则编辑、持久化和应用选择器能力。
- `gestureEditor\stores\useGestureEditorLifecycleStore.js`：应用根组件使用的窄 store，只暴露初始化和运行状态栏字段。
- `gestureEditor\stores\useGestureEditorOverlayStore.js`：应用根组件使用的窄 store，只暴露全局应用选择弹窗和规则编辑弹窗所需状态与动作。
- `gestureEditor\stores\useGestureRulesStore.js`：全局、分类和程序规则页使用的窄 store，只暴露规则列表、作用域管理、应用关联和规则编辑入口。
- `gestureEditor\stores\useGestureEdgeActionsStore.js`：边缘操作页使用的窄 store，只暴露边缘动作列表、边缘动作保存、快捷键录制和边缘操作选项。
- `gestureEditor\stores\useGestureSettingsStore.js`：设置页使用的窄 store，只暴露 UI 设置草稿保存、恢复默认、本地导入导出和 WebDAV 状态/操作。
- `gestureEditor\stores\useGestureExclusionsStore.js`：排除项页使用的窄 store，只暴露排除项列表、应用图标匹配和排除项增删改入口。
- `gestureEditor\stores\useGestureQuickSearchStore.js`：全局快速搜索使用的窄 store，从现有编辑器状态汇总规则、作用域、程序和排除项。
- `gestureEditor\modules\useGestureEditorApplicationPicker.js`：共享 context 内部使用的应用选择器流程，包括打开选择器、发起窗口/文件选择请求，以及处理宿主返回的程序信息。
- `gestureEditor\modules\useGestureScopes.js`：分类/程序 scope 选择、新增、重命名、删除和规则查询。
- `gestureEditor\modules\useGestureApplications.js`：应用程序视图数据读取、创建、显示名和有序分类列表更新。
- `gestureEditor\modules\useCategoryApplications.js`：分类与应用程序之间的多对多关联、重排和移除。
- `gestureEditor\modules\useGestureRuleEditor.js`：规则新增/编辑弹窗、规则草稿提交、手势录制和快捷键录制流程。
- `gestureEditor\modules\useGestureConfigPersistence.js`：规则保存节流、配置重载/重置、本地导入导出、WebDAV 备份恢复、UI 设置保存和边缘操作保存流程。
- `gestureEditor\modules\useGestureEditorNotifications.js`：配置状态消息和 toast 的统一通知入口；普通自动保存成功只更新内部状态，失败和明确操作结果才显示 toast。
- `gestureEditor\modules\useGestureEditorWebViewBridge.js`：WebView 消息发送、静默发送、可用性判断和消息监听入口。

## Constants

路径：

```text
src\constants
```

- `gestureEditorOptions.js`：手势编辑器使用的契约常量和 UI 选项，包括 scope、鼠标按键、动作类型、操作名、边缘触发、关闭行为、WebView 消息类型、方向符号和选项 label。
- `gestureEditorDefaults.js`：浏览器预览默认规则、默认应用、默认 UI 设置和默认边缘操作模板。
- `gestureEditorLimits.js`：手势编辑器输入限制常量，供表单控件和归一化逻辑共用。

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

- `global\index.vue`：全局规则页，单列表布局直接编辑全局规则表。
- `category\index.vue`：分类规则子页，通过 `/category/:name` 显示当前分类关联的应用程序和手势列表；分类列表由全局多级侧栏提供。
  - `category\composables\useCategoryPage.js`：分类页私有弹窗草稿、分类新增/重命名/删除编排和分类图标选择。
- `app\index.vue`：程序规则子页，通过 `/app/:name` 显示当前程序的分类优先级和手势列表；程序列表由全局多级侧栏提供。
  - `app\composables\useAppPage.js`：程序页私有重命名弹窗、删除编排和程序图标 fallback 文本。
- `edge\index.vue`：边缘操作子页，通过路由参数只展示触发角、摩擦边或边缘滚动其中一组配置。
  - `edge\components\EdgeActionSection.vue`：边缘操作大项区块。
  - `edge\components\EdgeActionCard.vue`：单个边缘操作卡片。
  - `edge\components\EdgeActionDialog.vue`：边缘操作编辑弹窗。
  - `edge\components\EdgeActionCommandFields.vue`：快捷键、窗口、音量、亮度命令字段。
  - `edge\composables\useEdgeActionDraft.js`：边缘操作编辑草稿、打开、关闭和保存逻辑。
- `exclusions\index.vue`：排除项页，用于维护不执行鼠标手势的程序列表，并可对单个排除项禁用边缘操作。
- `settings\index.vue`：设置子页，通过路由参数只展示轨迹线、手势提示、音量/亮度 OSD、灵敏度、应用行为或 WebDAV 中的一项；配置导入、导出和恢复默认入口由全局多级侧栏提供。
  - `settings\components\SettingsSectionCard.vue`：设置页大项标题、说明、操作区和内容布局。
  - `settings\components\SettingsFormGrid.vue`：设置项双列表单布局。
  - `settings\components\SettingsField.vue`：设置项卡片。
  - `settings\components\MouseTrailSettings.vue`：轨迹线配置，顶部内置轨迹线实时预览。
  - `settings\components\LevelOsdSettings.vue`：音量/亮度 OSD 配置，顶部内置位置、尺寸和圆角实时预览。
  - `settings\components\AppBehaviorSettings.vue`：应用行为配置，包括开机启动、管理员启动、目标窗口模式、暂停 WuGesture 和关闭按钮行为。
  - `settings\components\WebDavSettings.vue`：WebDAV 配置、测试、恢复和保存操作。
  - `settings\components\GestureHintSettings.vue`：底部提示窗配置，顶部内置提示窗实时预览。
  - `settings\components\GestureSensitivitySettings.vue`：手势灵敏度三档配置。
  - `settings\composables\useUiSettingsDraft.js`：基于 `gestureEditorNormalizers.js` 的共享设置深拷贝创建草稿，负责 debounce 保存、重置和预览样式计算。

## Routing

路由使用 hash 模式，页面路由按层级组织，当前页面包括：

- `global`
- `category/:name`
- `app/:name`
- `edge/corner`、`edge/friction`、`edge/wheel`
- `exclusions`
- `settings/mouse-trail`、`settings/gesture-hint`、`settings/level-osd`、`settings/sensitivity`、`settings/app-behavior`、`settings/webdav`

页面组件通过动态 `import()` 加载，`router/routes` 下的路由模块只在路由命中时加载对应页面 chunk。

自定义顶部栏左侧运行状态标识可点击，临时暂停或恢复 WuGesture；右侧提供宿主窗口控制。下方侧栏包含 `全局`、`分类`、`程序`、`边缘操作`、`排除项` 和 `设置`，分类/程序子项动态来自当前配置，边缘操作和设置子项对应独立嵌套路由。

## 当前 UI

- `全局` 采用单列表布局，直接编辑整张全局规则表，不显示左侧作用域区域。
- 页面切换使用方向感过渡：按顶部标签顺序向右切换时新页面从右侧滑入并渐显，向左切换时从左侧滑入并渐显，旧页面会轻微反向淡出；分类和程序子项切换与边缘操作、设置一样先完成路由切换，再由目标页面同步选中作用域，避免过渡中重复更新内容。
- `分类` 和 `程序` 使用全局多级侧栏管理动态作用域，页面主体只展示当前分类/程序内容，不再重复显示内部作用域列表。
- `分类` 页右侧包含“应用程序”和“手势列表”两个区块；程序可同时加入多个分类，分类关联顺序决定相同手势的覆盖顺序，越靠前的分类优先级越高。
- `程序` 页展示关联分类排序和手势列表；关联分类越靠上优先级越高，可通过拖拽或上移、下移调整同手势冲突时的覆盖顺序，程序规则仍按 app 作用域单独维护。
- `边缘操作` 的触发角、摩擦边、边缘滚动分别通过独立子路由展示配置；点击卡片打开独立弹窗编辑启用状态、命令和参数，名称由触发类型、位置与滚轮方向自动生成，关闭弹窗后自动保存。
- `排除项` 页维护不执行鼠标手势的程序列表；可通过拖动准星或浏览 exe 添加程序，也可为单个排除项勾选“同时禁用边缘操作”，列表会展示从 exe 路径动态提取的应用图标。
- `全局`、`分类` 和 `程序` 规则页会显示共享的作用域优先级提示，明确 `程序 > 分类 > 全局` 以及未命中当前层级时的继承关系。
- `设置` 的每个配置项通过独立子路由展示；本地导出、本地导入和恢复默认按钮位于侧栏设置分组。恢复默认会通过确认弹窗二次确认。本地导入/导出只处理主配置 JSON，不包含窗口状态。页面中的调整会在输入变化时 debounce 自动保存，并在控件变更结束或离开页面时强制提交最后一次修改；普通自动保存成功不弹出 toast，保存失败仍会提示。本地编辑期间会避免宿主回传覆盖当前滑块值。设置页也包含开机自启动、以管理员身份打开、暂停 WuGesture、关闭按钮行为，以及 WebDAV 地址、账号、密码和远程路径。开机自启动、以管理员身份打开和暂停 WuGesture 等布尔项使用共享自定义复选控件，单选下拉使用共享弹层式 `CustomSelect`。轨迹线、音量/亮度 OSD 和底部提示窗预览分别内置在对应独立设置页顶部，音量/亮度 OSD 区块还提供实际桌面测试按钮；手势灵敏度使用宽松、标准、严格三档。WebDAV 区域提供测试、恢复和保存按钮；当前 WebDAV 参数测试成功后，才允许把当前配置保存到远程或从远程恢复本地配置。
- 分类新增通过名称弹窗完成，不再使用左侧内联输入框；分类和程序名称都通过双击列表项后在弹窗里重命名。
- 分类页、程序页和排除项页添加程序时都会先显示前端弹窗，用户可按住“拖动准星选择窗口”拖到目标窗口松开，或选择“浏览 exe 文件”作为备用方式。
- 程序列表和详情会展示从 exe 路径动态提取的应用图标；图标通过 WebView 消息传递，不写入配置文件。
- 规则表列为 `名称`、`手势`、`命令`，删除按钮默认隐藏、在行悬浮时才显示；双击规则行、点击手势列或点击命令列都会打开规则编辑弹窗。
- 全局快速搜索会匹配手势规则的名称、手势、命令和作用域，以及分类、程序和排除项；点击规则会打开其编辑弹窗，点击分类、程序和排除项会定位对应项。
- 添加/编辑手势通过弹窗完成：弹窗里可选择命令类型，快捷键命令显示录制按钮，窗口控制命令显示操作下拉框。
- 添加/编辑手势弹窗打开时会通过 WebView 消息暂停全局手势；手势录制由后端接管，前端只接收最终识别结果。
- 规则编辑、删除、快捷键录制、分类/App 变更会发送 `save-rules` 写入配置文件。
- 已移除编辑器内的手势提示区，只保留配置结果提示；toast 统一显示在顶部居中。新增、删除、重置、导入导出、WebDAV 操作和配置错误会通过 toast 弹出反馈，普通自动保存成功不弹出提示。

## WebView 消息流

前端发送：

- `"get-status"`
- `{ type: "select-application", requestId: "...", category: "..." }`
- `{ type: "pick-application-window", requestId: "...", category: "..." }`
- `{ type: "start-gesture-recording", requestId: "..." }`
- `{ type: "stop-gesture-recording" }`
- `{ type: "set-gesture-paused", paused: true/false }`：仅控制规则编辑/录制期间的临时暂停。
- `{ type: "set-user-paused", paused: true/false }`：控制与托盘菜单一致的临时用户暂停，不写入配置文件。
- `{ type: "start-hotkey-recording", requestId: "..." }`
- `{ type: "stop-hotkey-recording" }`
- `{ type: "save-rules", rules: [{ scope, mouseButton, pattern, actionName, action }, ...], applications: [{ name, displayName, path, categories }, ...], edgeActions: [...], uiSettings: {...} }`
- `{ type: "webdav-test", rules: [...], applications: [...], edgeActions: [...], uiSettings: {...} }`
- `{ type: "webdav-save", rules: [...], applications: [...], edgeActions: [...], uiSettings: {...} }`
- `{ type: "webdav-restore", rules: [...], applications: [...], edgeActions: [...], uiSettings: {...} }`
- `{ type: "export-config", rules: [...], applications: [...], edgeActions: [...], uiSettings: {...} }`
- `{ type: "import-config" }`
- `{ type: "reload-rules" }`
- `{ type: "reset-rules" }`
- `{ type: "preview-level-osd", kind: "volume|brightness" }`
- `{ type: "window-minimize" }`
- `{ type: "window-toggle-maximize" }`
- `{ type: "window-close" }`
- `{ type: "window-start-drag" }`
- `{ type: "window-start-resize" }`

后端发送：

- `{ type: "status", status: "running|paused|..." }`
- `{ type: "window-state", maximized: true|false }`
- `{ type: "window-resize-state", resizing: true|false }`：宿主在原生缩放周期内通知前端抑制标题栏提示。
- `{ type: "rules", rules: [...], applications: [{ name, displayName, path, categories, icon }, ...], edgeActions: [...], uiSettings: {...}, ... }`；`categories` 的顺序决定分类规则冲突时的覆盖顺序，越靠前优先级越高，其中 `uiSettings.appBehavior.excludedApplications` 的运行态消息项会额外带 `icon`，不写入配置文件。
- `{ type: "application-selected", requestId: "...", name: "...", displayName: "...", path: "...", category: "...", icon: "..." }`
- `{ type: "gesture-recorded", requestId: "...", button: "right|middle", pattern: ["Down", "Right"] }`
- `{ type: "hotkey-recorded", requestId: "...", keys: ["Control", "W"] }`
- `{ type: "gesture", ... }`
- `{ type: "gesture-action-failed", ... }`
- `{ type: "edge-action-failed", ... }`
- `{ type: "config-result", operation: "save|export|import|reload|reset|action", success: true|false, message: "..." }`；`save` 成功只更新内部状态，其余明确操作结果显示 toast，任意失败均显示错误 toast。
- `{ type: "webdav-result", operation: "test|save|restore", success: true/false, message: "..." }`

`uiSettings.appBehavior` 当前包含：

- `launchAtStartup`
- `runAsAdministrator`
- `closeButtonBehavior`
- `targetWindowMode`：`start-window`（默认，手势按下位置所在的顶层窗口）或 `current-window`（动作执行时的活动窗口）
- `gesturePaused`
- `excludedApplications: [{ name, displayName, path, disableEdgeActions }]`

`uiSettings.mouseTrail` 当前包含：

- `enabled`
- `inactiveColor`
- `activeColor`
- `inactiveThickness`
- `activeThickness`
- `thickness`
- `inactiveOpacity`
- `activeOpacity`

`uiSettings.gestureHint` 当前包含：

- `enabled`
- `displayDurationMs`
- `fadeDurationMs`
- `fontFamily`
- `fontSize`
- `textColor`
- `backgroundColor`
- `backgroundOpacity`
- `width`
- `widthPercent`
- `autoWidth`
- `height`
- `heightPercent`
- `cornerRadius`
- `bottomOffset`
- `bottomOffsetPercent`

`uiSettings.levelOsd` 当前包含：

- `enabled`
- `displayDurationMs`
- `fadeDurationMs`
- `backgroundColor`
- `backgroundOpacity`
- `textColor`
- `trackColor`
- `volumeColor`
- `brightnessColor`
- `width`
- `height`
- `cornerRadius`
- `position`
- `offsetX`
- `offsetY`

`uiSettings.gestureSensitivity` 当前包含：

- `percent`：0-200 的连续灵敏度百分比，默认 110

## 构建

- 前端使用 `pnpm build` 生成仓库根目录下的 `dist\web`。
- `WuGesture.App.csproj` 会在 `.NET` 构建前自动执行前端构建。
- `WuGesture.App.csproj` 会在前端构建后把 `dist\web` 复制到宿主输出目录中的 `Web\dist`。
- 桌面宿主通过 WebView2 虚拟主机 `https://gesture.wu.philosophy/` 加载宿主输出目录中的 `Web\dist`。

## 何时更新此文件

在修改以下内容时同步更新：

- Web 目录结构
- 页面、路由、布局或复用组件
- 前端构建配置和输出路径
- WebView 消息契约
- 规则编辑器、边缘操作或设置页的核心职责
- 图标资产和共享 UI 组件职责
