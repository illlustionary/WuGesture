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
├─ tsconfig.json
├─ uno.config.js
├─ vite.config.js
├─ PROJECT_STRUCTURE.md
├─ node_modules
└─ src
```

根目录重要文件：

- `agents.md`：Web 子项目的代理指令。
- `index.html`：Vite 入口 HTML，页面标题为 `WuGesture`。
- `package.json`：前端工程依赖与脚本；`format` 和 `format:check` 使用本地 Prettier 按仓库根目录的 `.prettierrc.json` 格式化或校验前端源码，`test` 使用 Vitest 运行前端单元测试，`typecheck` 使用 `vue-tsc --noEmit` 做类型检查。
- `pnpm-lock.yaml`：锁定依赖树。
- `pnpm-workspace.yaml`：工作区配置，并放行 `@parcel/watcher` 的本地构建脚本，避免非交互环境下依赖安装中断。
- `tsconfig.json`：TypeScript / Vue 类型检查配置；`allowJs: true` 且 `checkJs: false`，只检查 `src` 下的 `.ts`、`.d.ts` 和 `.vue`，存量 `.js` 与 SFC 不受影响。`@/*` 指向 `src/*`，`@resources/*` 指向桌面宿主的 `Resources/*`。原 `jsconfig.json` 已被该文件取代。
- `uno.config.js`：UnoCSS 配置，集中定义页面区块、表单、列表等常用 shortcuts；颜色统一映射到 `--md-sys-color-*` 语义令牌，shortcut 里不再出现写死的 rgba 色值。
- `vite.config.js`：Vite 构建配置，包含 `vite-svg-loader`、UnoCSS Vite 插件，以及 `@` 到 `src`、`@resources` 到桌面宿主 `Resources` 目录的路径别名。`vite-svg-loader` 的 SVGO 配置覆盖了 `removeViewBox`，保证 24×24 描边图标在按 CSS 缩放到 18px 等尺寸时仍能等比缩放。
- `PROJECT_STRUCTURE.md`：当前 Web 子项目地图。
- `node_modules`：本地依赖目录，不纳入源码维护。

## Source Structure

```text
src
├─ main.js
├─ App.vue
├─ env.d.ts
├─ router
├─ styles.scss
├─ styles
├─ theme
├─ types
├─ assets
├─ components
├─ composables
├─ constants
├─ gestureEditor
├─ icons
├─ utils
└─ pages
```

- `src\main.js`：Vue 应用入口，加载全局样式与插件并挂载应用；路由定义集中在 `router` 目录。
- `src\router\index.js`：创建 hash 路由实例。
- `src\router\routes\index.js`：组合基础页面、作用域、边缘操作和设置路由模块；页面组件和嵌套路由承载组件均通过动态 `import()` 懒加载。
- `src\router\routes\scopeRoutes.js`：分类和程序动态子路由。
- `src\router\routes\edgeRoutes.js`：触发角、摩擦边和边缘滚动子路由。
- `src\router\routes\settingsRoutes.js`：各设置项子路由。
- `src\env.d.ts`：Vite 客户端类型引用与 `*.svg`、`*.svg?component` 模块声明；后续 TypeScript 文件可直接导入 SVG，无需重复声明。
- `src\App.vue`：侧栏/页面工作区、路由切换过渡和全局弹窗挂载；窗口标题栏由桌面宿主在 WebView 上方自绘（不进入前端页面），分类/程序侧栏操作和主题状态由组合式函数协调。首次启动引导只在 `state.hostConfigLoaded` 为 true 且 `uiSettings.onboarding.welcomeCompleted` 为 false 时自动弹出，每个会话最多一次，浏览器预览不触发；帮助弹窗由侧栏底部「?」打开。全局弹窗不会随页面卸载，因此 `App.vue` 在路由路径变化时统一关闭应用选择器、手势规则编辑器、快速搜索、帮助弹窗和分类/程序新增重命名弹窗，避免切换页面后残留；欢迎引导和更新弹窗属于全局流程，不受路由切换影响；恢复默认设置的确认弹窗属于设置页，随页面卸载自动关闭。
- `src\icons\appIcons.js`：本地 SVG 图标的唯一显式注册表；使用文件名作为图标名称，供 `AppIcon` 和业务组件按名称查找。
- `src\styles.scss`：全局样式入口，只负责按顺序 `@use` 令牌层、基础样式和共用组件类；新增样式不要直接追加到这个文件。
- `src\styles\tokens\`：MD3 设计令牌层，按 `_color`、`_typescale`、`_shape`、`_spacing`、`_elevation`、`_motion`、`_state`、`_aliases` 拆分。颜色以 seed `#529EF1` 的静态基线兜底，阶段 6 由主题引擎在运行时覆盖；`_aliases.scss` 把 `--bg`、`--panel`、`--accent` 等旧变量映射到新令牌，保证存量组件零改动。
- `src\styles\_base.scss`：reset、`:focus-visible` 焦点环、6px 细滚动条、原生输入框/滑块/取色器样式。
- `src\styles\_components.scss`：存量页面共用的 `.section-card`、`.list-stack`、`.primary-button` / `.secondary-button` / `.ghost-button` / `.danger-button`、`.keycap`、`.icon-button`、弹窗外壳和空状态样式；按钮类已按 MD3 统一为高 40、full 圆角、状态层。
- `src\theme\tokens.ts`：颜色角色、Typescale、形状、间距和动效的类型契约，以及 `colorVar()` 等引用工具；新增令牌时必须与 SCSS 同步。
- `src\theme\palettes.ts`：8 套预设 seed 色板（蓝/青/绿/紫/粉/橙/红/灰）与默认色 `#529EF1`；保存色值而非索引。
- `src\types\ui.ts`：基础控件的共享类型契约（`SelectOption`、`SelectOptionValue`、`ButtonVariant`、`ButtonSize`、`ChipVariant`）。

## MD3 Base Controls

阶段 2 起基础控件独立成套，新代码优先使用组件而不是全局 class：

- `src\components\ui\AppButton.vue`：MD3 按钮，`variant` 支持 `filled` / `tonal` / `outlined` / `text` / `danger`，`size` 支持 `md`（40）/ `sm`（32），`leading` 插槽放 18px 图标。
- `src\components\ui\AppChip.vue`：高 32、8 圆角的 Chip，支持 `selected` / `disabled` / `removable`（`remove` 事件）。
- `src\components\ui\AppDivider.vue`：1px `outlineVariant` 分割线，支持 `inset` / `vertical`。
- `src\components\ui\IconActionButton.vue`：36×36 图标按钮，`icon` / `label` / `tone` / `color` / `showTooltip`；`inheritAttrs: false`，class、`disabled`、`@click` 直接落到内部 `<button>`。
- `src\components\ui\HoverBubble.vue`：MD3 plain tooltip，`inverseSurface` + 4 圆角、无箭头，延迟 500ms，`pointerenter` 与键盘 `focus-visible` 都会显示；根节点固定为 `.hover-bubble-trigger`。
- `src\components\form\BaseInput.vue`：Filled Text Field（高 48、顶角 8、聚焦 2px primary 底边），仍是单根 `<input>`；`type=color/range` 交回全局原生样式。
- `src\components\form\CustomSelect.vue`：Exposed Dropdown，触发器 48、菜单圆角 4、项高 48、选中项 `secondaryContainer` + 勾选；保留 `modelValue` / `value` 双入口、键盘导航和 Teleport。
- `src\components\form\ToggleCheckbox.vue`：MD3 Switch（轨道 52×32、滑块 24/16），整行可点、Switch 在行尾，保留 `label` / `note` 与 `change` 事件。
- `src\components\form\BaseRange.vue`：原生 range 包装，保留 `--range-progress` 契约与 `$attrs` 透传（`disabled` 由此生效）。
- `src\components\form\AppCheckbox.vue`：18×18 MD3 复选框，支持 `indeterminate`。
- 阶段 2 已完成的重写组件统一为 `<script setup lang="ts">`；存量 `.js` 逻辑层未迁移。

## Assets

路径：

```text
src\assets
```

SVG 按使用位置分目录：

- `actions\`：通用操作，包括新增、删除、导入导出、重置、测试、WebDAV 上传下载，以及密码显示/隐藏用的 `eye`、`eye-off`。
- `category\`：分类显示图标，包括浏览器、代码、媒体和星光。
- `gesture\`：手势录制与选择图标，包括准星、键盘和录制。
- `navigation\`：侧栏、搜索和应用状态图标。侧栏导航图标统一为 Lucide 风格（ISC 许可，24×24、`stroke-width` 1.8、路径 `currentColor`），包括 `globe`、`ban`、`folder`、`app-window`、`frame`、`corner-down-right`、`move-horizontal`、`mouse`、`setting`、`database`、`spline`、`message-square`、`monitor`、`gauge`、`toggle-left`、`refresh-cw`、`cloud`、`info`、`sun`、`moon`、`panel-left` 和 `search`；旧版实心图标仅 `briefcase`、`circle-dashed`、`close-alt` 保留给分类默认图标或无图标兜底。
- `window\`：对话框关闭图标。

这些 SVG 只由 `icons\appIcons.js` 通过 `vite-svg-loader` 作为 Vue 组件导入。图标文件名就是对外的图标名称，统一使用小写 kebab-case；路径颜色应使用 `currentColor`，便于按钮和状态样式控制。描边图标依赖根节点 `fill="none"` + `stroke="currentColor"`，侧栏对应容器不能再强制 `fill: currentColor`；分类/程序子项仍在用的实心图标靠路径自身的 `fill="currentColor"` 生效。侧栏每个入口使用语义独立的图标：全局 `globe`、排除项 `ban`、分类 `folder`、程序 `app-window`、边缘操作 `frame`（触发角 `corner-down-right`、摩擦边 `move-horizontal`、边缘滚动 `mouse`）、设置 `setting`（配置 `database`、轨迹线 `spline`、手势提示 `message-square`、音量/亮度提示 `monitor`、手势灵敏度 `gauge`、应用行为 `toggle-left`、更新 `refresh-cw`、WebDAV `cloud`）、关于&更新 `info`，页脚使用 `sun`/`moon`、`search` 和 `panel-left`。

## Components

路径：

```text
src\components
```

- `sidebar\`：侧栏壳和固定导航、分类、程序、边缘操作、设置、关于&更新及底部区域组件。
- `layout\`：`AppShell.vue` 页面布局壳和 `NestedRouteView.vue` 嵌套路由承载组件。
- `dialog\`：`BaseDialog.vue`、`ConfirmDialog.vue`、`QuickSearchDialog.vue`、`UpdateDialog.vue`、`WelcomeDialog.vue` 和 `HelpDialog.vue` 等共享对话框；`BaseDialog` 统一遮罩关闭、可选右上关闭按钮、默认操作区和关闭动画，自动保存或即时选择类弹层可复用外壳并关闭默认操作区；`UpdateDialog` 展示版本差异、发行说明、下载进度和失败后的重试/发布页入口，下载中禁止遮罩关闭；`WelcomeDialog` 是首次启动的三屏欢迎引导，`HelpDialog` 收拢快速上手、规则生效顺序、边缘操作和常见问题，并提供「重新观看引导」和文档外链。
- `form\`：`BaseInput.vue`、`BaseRange.vue`、`CustomSelect.vue` 和 `ToggleCheckbox.vue` 等共享表单控件；`BaseInput` / `BaseRange` 统一原生输入控件的 `v-model` 事件、宽度约束和滑块进度填充，页面只保留各自的配置约束与保存时机。
- `ui\`：`AppIcon.vue`、`HoverBubble.vue` 和 `IconActionButton.vue` 等基础 UI 控件。
- `gesture\`：`GestureRuleDialog.vue`、`GestureRuleList.vue` 以及 `gesture\rule-dialog\fields\` 下的规则字段组件；`GestureRuleGestureField.vue` 负责方向手势录制、按住按键、滚轮修饰和触发方式控件，`GestureRuleList.vue` 依次渲染方向箭头与「滚轮」上下修饰。
- `scope\`：`ScopeCreateDialog.vue`、`ScopePriorityNotice.vue` 和 `ApplicationListItem.vue` 等作用域配置共享组件；程序项仅路径文本可点击以打开所在文件夹，并以可选操作区支持分类页的移除按钮。
- `rules\RulesSection.vue`：规则页右侧复用区块，统一标题、说明、操作区和内容面板。

页面特有组件不放在这里：边缘页的 `pages\edge\components\` 组件和设置页的 `pages\settings\components\` 组件均只由对应页面使用，保持在页面目录内。

## Composables

路径：

```text
src\composables
```

- `useAppearanceTheme.js`：配置界面主题解析、系统主题监听、根元素主题属性写入和主题偏好持久化。
- `useGestureEditorEventListeners.js`：应用壳注册的事件监听层，订阅手势编辑器通知和导航意图，并分别调用 Toast 与 Vue Router。
- `useQuickSearch.js`：全局快速搜索的弹层开关、`Ctrl+K` 生命周期与结果直达，协调搜索 store 的 scope 选择、导航层和规则编辑入口。
- `useSidebarScopeActions.js`：侧栏分类/程序的新建、重命名、删除、应用选择完成后的 scope 选择和导航协调。

## Gesture Editor

路径：

```text
src\gestureEditor
```

手势编辑器领域模块集中放在这里；`composables` 只保留 Vue 组合式入口或历史兼容 facade。

- `gestureEditor\context\gestureEditorContext.js`：手势编辑器共享单例上下文，集中组装状态、WebView 桥接、作用域、应用、规则编辑、持久化和应用选择器能力；不直接依赖 Toast 或 Vue Router。
- `gestureEditor\events\gestureEditorEventBus.js`：应用内跨层事件总线，当前定义通知和路由跳转意图；发送方不依赖 Toast 或 Vue Router。
- `gestureEditor\stores\useGestureEditorLifecycleStore.js`：应用根组件使用的窄 store，只暴露初始化和运行状态栏字段。
- `gestureEditor\stores\useGestureEditorOverlayStore.js`：应用根组件使用的窄 store，只暴露全局应用选择弹窗和规则编辑弹窗所需状态与动作。
- `gestureEditor\stores\useGestureEdgeActionsStore.js`：边缘操作页使用的窄 store，只暴露边缘动作列表、边缘动作保存、快捷键录制和边缘操作选项。
- `gestureEditor\stores\useGestureExclusionsStore.js`：排除项页使用的窄 store，只暴露排除项列表、应用图标匹配和排除项增删改入口。
- `gestureEditor\stores\useGestureQuickSearchStore.js`：全局快速搜索使用的窄 store，从现有编辑器状态汇总规则、作用域、程序和排除项。
- `gestureEditor\modules\useGestureEditorApplicationPicker.js`：无业务状态的应用选择器流程，只负责打开选择器、发起窗口/文件选择请求并返回宿主选中的程序信息；分类、排除项和运行程序草稿的回填由 context 分派。
- `gestureEditor\modules\useGestureScopes.js`：分类/程序 scope 选择、新增、重命名、删除和规则查询。
- `gestureEditor\modules\useGestureApplications.js`：应用程序视图数据读取、创建、显示名和有序分类列表更新。
- `gestureEditor\modules\useCategoryApplications.js`：分类与应用程序之间的多对多关联、重排和移除。
- `gestureEditor\modules\useGestureRuleEditor.js`：规则新增/编辑弹窗、规则草稿提交、手势录制和快捷键录制流程；规则身份由方向文本 + 滚轮修饰共同判定，参与同作用域去重校验，滚轮修饰可选择每格连续执行或松键执行一次。
- `gestureEditor\modules\useGestureConfigPersistence.js`：规则保存节流、配置重载/重置、本地导入导出、WebDAV 备份恢复、UI 设置保存和边缘操作保存流程。
- `gestureEditor\modules\useGestureEditorNotifications.js`：配置状态消息和通知事件的统一入口；普通自动保存成功只更新内部状态，失败和明确操作结果才发布通知事件。
- `gestureEditor\modules\useGestureEditorNavigation.js`：路由名称和 scope/exclusion 跳转的统一入口，页面与组合式函数只发布路由意图而不直接执行 router 跳转。
- `gestureEditor\modules\useGestureEditorWebViewBridge.js`：WebView 消息发送、静默发送、可用性判断和消息监听入口。
- `gestureEditor\modules\useGestureUpdater.js`：更新检查/下载/取消/重试的前端状态机，处理宿主回传的 `update-*` 消息并驱动全局更新弹窗；收到 `update-completed`（更新后首次启动）时提示更新成功并切换到“关于&更新”页；浏览器预览中会拒绝检查更新。

## Constants

路径：

```text
src\constants
```

- `gestureEditorOptions.js`：手势编辑器使用的契约常量和 UI 选项，包括 scope、鼠标按键、滚轮修饰（无/上/下）、触发方式（连续/单次）、动作类型、操作名、边缘触发、关闭行为、更新源、WebView 消息类型、方向符号和选项 label。
- `gestureEditorUiDefaults.js`：UI 设置和边缘操作的前端默认模板；不包含手势、分类或具体程序示例。
- `gestureEditorBrowserPreviewData.js`：仅供未连接桌面宿主时的浏览器预览使用，提供示例全局/分类规则和程序。
- `gestureEditorLimits.js`：手势编辑器输入限制常量，供表单控件和归一化逻辑共用。

## Utils

路径：

```text
src\utils
```

- `gestureEditorNormalizers.js`：规则 scope、手势文本、快捷键、动作类型、边缘操作和 UI 设置的解析与归一化工具；同目录的 `gestureEditorNormalizers.test.js` 用 Vitest 覆盖这些纯函数。
- `gestureEditorFormatters.js`：手势助记符（方向路径 + 滚轮上/下修饰）、动作展示文案和边缘操作展示文案。
- `gestureEditorPayloads.js`：把编辑器内存模型转换成 WebView 保存、WebDAV 测试/备份使用的配置 payload；规则同时输出 `pattern`、`wheelModifier` 和 `executeOnWheel`，`gestureEditorPayloads.test.js` 覆盖各段映射和配置组装。
- `gestureEditorViewModels.js`：把宿主配置消息转换成编辑器视图模型（含 `patternText`/`wheelModifier`/`executeOnWheel`），并提供规则和手势草稿模板；`gestureEditorViewModels.test.js` 覆盖字段映射和默认值。
- `gestureEditorCollections.js`：按规则和应用程序列表聚合分类/程序侧栏条目；`gestureEditorCollections.test.js` 覆盖计数合并、空名称过滤和排序。

## Pages

路径：

```text
src\pages
```

页面按目录组织，目录名就是页面名，入口统一为 `index.vue`。页面私有组件放在同级 `components`，页面私有逻辑放在同级 `composables`。

- `global\index.vue`：全局规则页，单列表布局直接编辑全局规则表。
- `category\index.vue`：分类规则子页，通过 `/category/:name` 显示当前分类关联的应用程序和手势列表；分类列表由全局多级侧栏提供。
  - `category\composables\useCategoryPage.js`：分类页私有弹窗草稿、分类新增/重命名/删除编排和分类图标选择。
- `app\index.vue`：程序规则子页，通过 `/app/:name` 显示当前程序信息、分类优先级和手势列表；程序列表由全局多级侧栏提供。
  - `app\composables\useAppPage.js`：程序页私有重命名弹窗、删除编排和程序图标 fallback 文本。
- `edge\index.vue`：边缘操作子页，通过路由参数只展示触发角、摩擦边或边缘滚动其中一组配置。
  - `edge\components\EdgeActionSection.vue`：边缘操作大项区块。
  - `edge\components\EdgeActionCard.vue`：单个边缘操作卡片。
  - `edge\components\EdgeActionDialog.vue`：边缘操作编辑弹窗。
  - `edge\components\EdgeActionCommandFields.vue`：快捷键、窗口、音量、亮度命令字段。
  - `edge\composables\useEdgeActionDraft.js`：边缘操作编辑草稿、打开、关闭和保存逻辑。
- `exclusions\index.vue`：排除项页，用于维护不执行鼠标手势的程序列表，并可对单个排除项禁用边缘操作。
- `settings\index.vue`：设置子页，通过路由参数只展示配置管理、轨迹线、手势提示、音量/亮度 OSD、灵敏度、应用行为、更新源或 WebDAV 中的一项；本地配置导入、导出和恢复默认统一放在「设置 > 配置」页。
  - `settings\components\SettingsSectionCard.vue`：设置页大项标题、说明、操作区和内容布局。
  - `settings\components\ConfigManagement.vue`：配置管理页，分「备份与迁移」（导出、导入）和「恢复默认」两组，说明导入导出的范围并强调不可撤销。
  - `settings\components\SettingsFormGrid.vue`：设置项双列表单布局。
  - `settings\components\SettingsField.vue`：设置项卡片。
  - `settings\components\MouseTrailSettings.vue`：轨迹线配置，顶部内置轨迹线实时预览。
  - `settings\components\LevelOsdSettings.vue`：音量/亮度 OSD 配置，顶部内置位置、尺寸和圆角实时预览。
  - `settings\components\AppBehaviorSettings.vue`：应用行为配置，包括开机启动、管理员启动、目标窗口模式、全屏禁用开关和关闭按钮行为。
  - `settings\components\WebDavSettings.vue`：WebDAV 配置、测试、恢复和保存操作。
  - `settings\components\UpdateSettings.vue`：更新源下拉框和启动时自动检查开关，是更新相关设置的唯一编辑入口。
  - `settings\components\GestureHintSettings.vue`：底部提示窗配置，顶部内置提示窗实时预览。
  - `settings\components\GestureSensitivitySettings.vue`：手势灵敏度三档配置。
  - `settings\composables\useUiSettingsDraft.js`：基于 `gestureEditorNormalizers.js` 的共享设置深拷贝创建草稿，负责 debounce 保存、重置和预览样式计算；导入配置前会丢弃未提交的草稿，避免旧值在导入完成后覆盖新配置。
- `about\index.vue`：侧栏最外层的“关于&更新”页，分「关于」和「更新」两个区块；「关于」展示软件简介、当前版本、更新源和自动检查状态（只读），「更新」提供检查更新按钮、状态提示和“查看更新”入口，修改更新设置需要前往“设置 > 更新”。更新后首次启动会自动切到本页并提示已完成更新及当前版本。
  - `about\components\AboutUpdatePanel.vue`：两个区块的具体布局、更新状态文案和来源标注。
- `dev\md3\index.vue`：仅开发环境注册的 MD3 设计令牌预览页（`#/dev/md3`），展示颜色角色、字体、形状和间距令牌；阶段 1 注入 `--md-sys-*` 变量后自动显示真实取值。生产构建会移除该路由和对应 chunk，不进入发布产物。

## Routing

路由使用 hash 模式，页面路由按层级组织，当前页面包括：

- `global`
- `category/:name`
- `app/:name`
- `edge/corner`、`edge/friction`、`edge/wheel`
- `exclusions`
- `about`（侧栏最外层的“关于&更新”独立页面）
- `settings/config`、`settings/mouse-trail`、`settings/gesture-hint`、`settings/level-osd`、`settings/sensitivity`、`settings/app-behavior`、`settings/sidebar`、`settings/update`、`settings/webdav`
- `dev/md3`（仅开发环境；`import.meta.env.DEV` 为 false 时路由和页面 chunk 都不会生成）

页面组件通过动态 `import()` 加载，`router/routes` 下的路由模块只在路由命中时加载对应页面 chunk。

未连接 WebView 宿主时，`gestureEditorContext.js` 会加载 `gestureEditorBrowserPreviewData.js` 的示例数据，便于浏览器中预览分类和程序界面；桌面宿主始终以 `rules` 消息下发的真实配置为准。

## 当前 UI

界面外壳：窗口控制由桌面宿主自绘的标题栏负责（标题条位于 WebView 上方，属于宿主绘制区域，不进入前端页面）；其下方侧栏顶部的 `SidebarAppStatus` 组件可点击，临时暂停或恢复 WuGesture，并在图标右侧显示名称与程序集版本。侧栏的导航、分类、程序、边缘操作、设置和关于&更新各自使用独立区域组件，页脚控制也独立封装；分类/程序子项动态来自当前配置，边缘操作和设置子项对应独立嵌套路由，关于&更新是与导航同级的最外层入口（`/about`）。侧栏收起状态保存在 `uiSettings.sidebar.collapsed`，重建配置页面后保持不变。收起时热区通过 `v-show` 控制，侧栏自身以固定定位、`visibility`、透明度和鼠标事件状态隐藏；导航分组内容继续参与布局，因此不会压缩滚动高度或重置滚动位置。进入热区后淡入，离开侧栏后淡出。侧栏只有一个滚动区域：导航列（`.app-sidebar__nav`）整体滚动，展开后的分类/程序子项按内容自然撑开，分组内部不再单独滚动。

- `全局` 采用单列表布局，直接编辑整张全局规则表，不显示左侧作用域区域。
- 页面切换使用方向感过渡：按顶部标签顺序向右切换时新页面从右侧滑入并渐显，向左切换时从左侧滑入并渐显，旧页面会轻微反向淡出；分类和程序子项切换与边缘操作、设置一样先完成路由切换，再由目标页面同步选中作用域，避免过渡中重复更新内容。
- `分类` 和 `程序` 使用全局多级侧栏管理动态作用域，页面主体只展示当前分类/程序内容，不再重复显示内部作用域列表。
- `分类` 页右侧包含“应用程序”和“手势列表”两个区块；程序可同时加入多个分类，分类关联顺序决定相同手势的覆盖顺序，越靠前的分类优先级越高。
- `程序` 页顶部展示共享程序项，随后展示关联分类排序和手势列表；关联分类越靠上优先级越高，可通过拖拽或上移、下移调整同手势冲突时的覆盖顺序，程序规则仍按 app 作用域单独维护。
- `边缘操作` 的触发角、摩擦边、边缘滚动分别通过独立子路由展示配置；点击卡片打开独立弹窗编辑启用状态、命令和参数，名称由触发类型、位置与滚轮方向自动生成，关闭弹窗后自动保存。
- `排除项` 页维护不执行鼠标手势的程序列表；可通过拖动鼠标或浏览 exe 添加程序，也可为单个排除项勾选“同时禁用边缘操作”，列表会展示从 exe 路径动态提取的应用图标。
- `全局`、`分类` 和 `程序` 规则页会显示共享的作用域优先级提示，明确 `程序 > 分类 > 全局` 以及未命中当前层级时的继承关系。
- `设置` 的每个配置项通过独立子路由展示；本地导出、本地导入和恢复默认集中到「设置 > 配置」页，侧栏设置分组只保留导航入口。恢复默认会通过确认弹窗二次确认；本地导入在用户选好 JSON 文件后由桌面宿主再次确认覆盖，避免误选文件直接丢失配置。本地导入/导出只处理主配置 JSON，不包含窗口状态。页面中的调整会在输入变化时 debounce 自动保存，并在控件变更结束或离开页面时强制提交最后一次修改；普通自动保存成功不弹出 toast，保存失败仍会提示。本地编辑期间会避免宿主回传覆盖当前滑块值。设置页也包含开机自启动（默认关闭）、以管理员身份打开、关闭按钮行为，以及 WebDAV 地址、账号、密码和远程路径。开机自启动、以管理员身份打开和全屏禁用等布尔项使用共享自定义复选控件，单选下拉使用共享弹层式 `CustomSelect`。临时暂停只由侧栏状态标识和托盘菜单提供，不写入配置文件。轨迹线、音量/亮度 OSD 和底部提示窗预览分别内置在对应独立设置页顶部，音量/亮度 OSD 区块还提供实际桌面测试按钮；手势灵敏度使用宽松、标准、严格三档。WebDAV 区域提供测试、恢复和保存按钮；当前 WebDAV 参数测试成功后，才允许把当前配置保存到远程或从远程恢复本地配置。密码输入框右侧有显示/隐藏按钮，切换时在 `password` 与 `text` 之间切换输入类型，并用 `eye`/`eye-off` 图标和 `aria-label` 同步状态；“更新”页的“启动时自动检查更新”复选卡片与“应用行为”页一致，最多占一行的一半宽度，窄屏下改为整行。
- `关于&更新` 是侧栏最外层的独立页面，分「关于」和「更新」两个区块：「关于」介绍软件用途，并只读展示当前版本、更新源和是否在启动时自动检查；「更新」提供检查更新按钮、状态提示和“查看更新”入口，确认后才由全局更新弹窗下载、校验并重启安装，弹窗会注明实际使用的更新源以及回退原因。更新源和自动检查开关统一放在“设置 > 更新”中修改。
- 分类新增通过名称弹窗完成，不再使用左侧内联输入框；分类和程序名称都通过双击列表项后在弹窗里重命名。
- 分类页、程序页和排除项页添加程序时都会先显示前端弹窗，用户可通过“拖动选择窗口”将鼠标移到目标窗口后松开，或选择“浏览 exe 文件”作为备用方式。
- 程序列表和详情会展示从 exe 路径动态提取的应用图标；图标通过 WebView 消息传递，不写入配置文件。
- 规则表列为 `名称`、`手势`、`命令`，删除按钮默认隐藏、在行悬浮时才显示；双击规则行、点击手势列或点击命令列都会打开规则编辑弹窗。
- 全局快速搜索会匹配手势规则的名称、手势、命令和作用域，以及分类、程序和排除项；点击规则会打开其编辑弹窗，点击分类、程序和排除项会定位对应项。
- 添加/编辑手势通过弹窗完成：手势区域始终可以录制方向手势，并可选择按住按键、滚轮修饰（无/上/下）和触发方式（每格连续执行/松开时执行一次）。选择滚轮修饰后，方向手势可以留空表示按住直接滚动；右侧仍可选择命令类型，快捷键命令显示录制按钮，窗口控制命令显示操作下拉框。
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
- `{ type: "import-config" }`：宿主打开文件选择框，用户选好文件后弹出覆盖确认；取消选择或选择“否”都不会改动配置。
- `{ type: "reload-rules" }`
- `{ type: "reset-rules" }`
- `{ type: "preview-level-osd", kind: "volume|brightness" }`
- `{ type: "check-update" }`：手动检查更新。
- `{ type: "start-update" }`：下载并安装当前提示的新版本。
- `{ type: "cancel-update" }`：取消正在进行的下载。
- `{ type: "open-update-page" }`：在浏览器中打开发布页。

后端发送：

- `{ type: "status", status: "running|paused|..." }`
- `{ type: "rules", appVersion: "v...", rules: [{ scope, mouseButton, pattern, wheelModifier, executeOnWheel, actionName, actionType, ... }], applications: [{ name, displayName, path, categories, icon }, ...], edgeActions: [...], uiSettings: { sidebar: { collapsed }, update: { checkForUpdates, source }, onboarding: { welcomeCompleted }, ... }, ... }`；`wheelModifier` 为 `up`/`down` 时表示方向手势叠加了滚轮修饰，`pattern` 可以为空表示按住直接滚动；`executeOnWheel` 决定每格连续执行还是松键执行一次。`appVersion` 来自程序集版本，仅用于配置界面展示，不写入配置文件；`uiSettings.sidebar.collapsed` 保存侧栏收起状态；`uiSettings.onboarding.welcomeCompleted` 决定是否自动弹出首次引导；`categories` 的顺序决定分类规则冲突时的覆盖顺序，越靠前优先级越高，其中 `uiSettings.appBehavior.excludedApplications` 的运行态消息项会额外带 `icon`，不写入配置文件。
- `{ type: "application-selected", requestId: "...", name: "...", displayName: "...", path: "...", category: "...", icon: "..." }`
- `{ type: "gesture-recorded", requestId: "...", button: "right|middle", pattern: ["Down", "Right"] }`
- `{ type: "hotkey-recorded", requestId: "...", keys: ["Control", "W"] }`
- `{ type: "gesture-action-failed", ... }`
- `{ type: "edge-action-failed", ... }`
- `{ type: "config-result", operation: "save|export|import|reload|reset|action", success: true|false, message: "..." }`；`save` 成功只更新内部状态，其余明确操作结果显示 toast，任意失败均显示错误 toast。
- `{ type: "webdav-result", operation: "test|save|restore", success: true/false, message: "..." }`
- `{ type: "update-available", currentVersion, latestVersion, source, preferredSource, notes, publishedAt, htmlUrl, assetName, assetSize, manual }`：发现新版本；`source` 是实际提供发行版的源，`preferredSource` 是设置里选择的源，两者不同表示已自动回退；`manual` 为 `false` 时表示启动自动检查。
- `{ type: "update-none", currentVersion }`：手动检查后确认已是最新版本。
- `{ type: "update-error", message }`：检查更新失败。
- `{ type: "update-progress", phase: "downloading|verifying|extracting|elevating", percent, receivedBytes, totalBytes }`：下载和准备进度。
- `{ type: "update-failed", message }`：下载、校验或启动更新程序失败。
- `{ type: "update-restarting", message }`：更新程序已启动，应用即将退出重启。
- `{ type: "update-completed", version, message }`：更新成功后首次启动、Web 界面完成 `get-status` 后由宿主发送一次；前端提示更新成功、关闭更新弹窗并切换到“关于&更新”页。

`uiSettings.appBehavior` 当前包含：

- `launchAtStartup`
- `runAsAdministrator`
- `closeButtonBehavior`
- `targetWindowMode`：`start-window`（默认，手势按下位置所在的顶层窗口）或 `current-window`（动作执行时的活动窗口）
- `excludedApplications: [{ name, displayName, path, disableEdgeActions }]`

`uiSettings.mouseTrail` 当前包含：

- `enabled`
- `inactiveColor`
- `activeColor`
- `inactiveThickness`
- `activeThickness`
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
- `widthPercent`
- `autoWidth`
- `heightPercent`
- `cornerRadius`
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

`uiSettings.update` 当前包含：

- `checkForUpdates`：进程启动时是否自动检查一次更新，默认 `true`
- `source`：`gitee`（默认）或 `github`，作为检查更新时的首选源；另一个源只在首选源没有更高版本时才参与比较，两个源都会按版本号取较高者，且不会安装不高于当前版本的包

`uiSettings.onboarding` 当前包含：

- `welcomeCompleted`：首次启动的三屏欢迎引导是否已完成，默认 `false`。完成、跳过关窗（含 Esc 和点击遮罩）都会写回 `true`；标记随主配置导出/导入和 WebDAV 同步，因此恢复配置后不会重复弹出。想重看时从侧栏「?」打开的帮助弹窗点「重新观看引导」，该操作不会清除标记。

## 测试

前端单元测试使用 Vitest，测试文件与被测模块同目录，命名为 `*.test.js`（已迁移到 TypeScript 的模块使用 `*.test.ts`）：

```powershell
pnpm test
```

类型检查单独执行，只检查 `src` 下的 `.ts`、`.d.ts` 和 `.vue`，存量 `.js` 不纳入检查：

```powershell
pnpm typecheck
```

Vitest 直接复用 `vite.config.js` 的路径别名，不需要额外的测试配置。测试文件与被测模块同目录，当前覆盖 `utils` 下的纯函数：`gestureEditorNormalizers`（枚举回退、数值收敛、作用域解析、排除项进程名归一化和去重、边缘操作合并）、`gestureEditorPayloads`（配置 payload 映射）、`gestureEditorViewModels`（宿主消息到编辑器模型）和 `gestureEditorCollections`（侧栏条目聚合）。`*.test.js` / `*.test.ts` 不参与 `vite build`，也被桌面工程的增量前端构建排除。

## 构建

前端构建由桌面工程驱动，完整命令、输出路径和增量规则见仓库根目录 [项目结构](../../../PROJECT_STRUCTURE.md) 的“WebView 前端”和“构建与发布”两节，这里不重复。前端侧只需注意：

- 正常构建/发布不需要手动执行 `pnpm build`；`WuGesture.App.csproj` 会在 `.NET` 构建前自动调用 `vite build`，源文件未变化时会跳过。
- 手动 `pnpm build` 只用于单独验证前端，产物落在 `Web\dist`（已被 Git 忽略），不会进入发行包。
- `pnpm typecheck` 只做类型检查，不产出文件；CI 在格式检查和单元测试之间执行该步骤。

## 何时更新此文件

在修改以下内容时同步更新：

- Web 目录结构
- 页面、路由、布局或复用组件
- 前端构建配置和输出路径（仅前端侧约定，宿主侧构建见根文档）
- 前端收发的 WebView 消息（宿主侧处理逻辑见根文档）
- 前端测试结构、测试命令或测试框架
- TypeScript 配置、类型检查脚本或 CI 检查步骤
- 规则编辑器、边缘操作或设置页的核心职责
- 设计令牌、颜色角色、Typescale、形状/间距/动效令牌或主题类型
- 图标资产和共享 UI 组件职责
