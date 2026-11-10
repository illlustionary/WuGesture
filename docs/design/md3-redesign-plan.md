# WuGesture 界面改造计划书

> Material Design 3 视觉重构 + 多主题 / 动态配色

| 项 | 内容 |
|---|---|
| 文档版本 | v1.3 |
| 编写日期 | 2026-11-09 |
| 状态 | 实施中（阶段 0–1 已完成） |
| 适用范围 | `src\WuGesture.App\Web`（配置界面）、`src\WuGesture.App` 中与主题相关的宿主代码 |
| 不改动 | WebView 消息契约、手势引擎、动作执行、覆盖层渲染逻辑 |

---

## 1. 目标与非目标

### 1.1 目标

1. 配置界面整体切换到 **Material Design 3（Material You）** 视觉语言。
2. 建立统一的设计令牌层（颜色 / 字体 / 形状 / 高度 / 间距 / 状态层 / 动效），消除组件内硬编码。
3. 支持 **8 套预设配色 + 自定义取色 + 跟随 Windows 强调色**。
4. 配色作为正式配置项，随 `gestures.json` 一起导入导出、WebDAV 同步、换机恢复。
5. 自绘标题栏、托盘右键菜单与页面配色保持一致。
6. 恢复键盘焦点可见性与滚动反馈，提升可访问性。

### 1.2 非目标

- 不引入 UI 组件库（Vuetify / Element Plus / Material Web）。
- 不引入在线字体、CDN 资源、远程图标服务。
- 不改 `WebViewMessageTypes.cs` 中的消息类型与载荷结构（只做向后兼容的字段新增）。
- 不做移动端 / 小屏响应式布局（宿主最小窗口 1280×720，WebView2 禁用缩放）。
- 不做“整站皮肤”（换布局、换形状），只做配色 + MD3 视觉规范。
- 本期不做壁纸取色、纯黑模式、调色板风格选择器。

---

## 2. 现状诊断

### 2.1 技术现状

| 项 | 现状 |
|---|---|
| 框架 | Vue 3.5 + JavaScript 为主体（TypeScript 按附录 D 逐步引入）+ Vite 8 |
| 样式 | SCSS + UnoCSS（`presetUno`），BEM 命名，45 个 scoped + 2 个非 scoped |
| 组件库 | 无，全部自研 |
| 图标 | 44 个本地 SVG（`vite-svg-loader`），另有内联 SVG 与 Unicode 字形 |
| 字体 | 系统字体栈 `Segoe UI Variable` / `Microsoft YaHei UI` |
| 规模 | 约 112 个源文件、15,000 行 |
| 主题 | `data-theme` 双态，手工调色板，仅颜色/圆角/阴影是变量 |

### 2.2 主要问题

| 问题 | 具体表现 | 位置 |
|---|---|---|
| 颜色不成体系 | 单一 accent，无 primary/secondary/tertiary/error 与 surface 层级 | `Web/src/styles.scss:1-140` |
| 形状混乱 | 同一屏内 0 / 8 / 12 / 16 / 999 五种圆角 | `styles.scss:323-330` |
| 高度语义错误 | 卡片、按钮、快捷键标签都带投影 | `--shadow-*` 9 个变量 |
| 层级太弱 | 表头、行标题、值、说明权重接近；标题 20–22px 与 16px 难以区分 | 各页面硬编码 |
| 密度失衡 | 规则行约 92px 高，字号仅 13–14px | `GestureRuleList.vue` |
| 状态反馈缺失 | 全局 `outline: none !important`，无 pressed / dragged 状态层 | `styles.scss:146-149` |
| 侧栏不符合 MD3 | 激活项为“圆角块 + 左侧蓝条”，应为整颗胶囊 | `AppSidebar.vue:105+` |
| 图标不统一 | 线宽、尺寸、留白不一致，混用 Unicode 字形 | `src/icons/appIcons.js` |
| 深色模式失真 | UnoCSS shortcut 写死浅色 rgba | `uno.config.js:23-28` |
| Toast 未主题化 | `vue-toastification` 默认外观 | `main.js` |

### 2.3 宿主硬约束

1. 标题栏为 WinForms 自绘（40 逻辑像素、按钮 46px、缩放边 4px），前端无法替换为 MD3 Top App Bar。
2. WebView2 仅允许 `https://gesture.wu.philosophy`，Release 关闭 DevTools / 缩放 / 右键菜单 / 浏览器快捷键。
3. 不能依赖远程字体或 CDN。
4. 最小窗口 1280×720，内容区约 1042px。
5. 配置以 C# DTO 为准，未知字段保存时会被丢弃。

---

## 3. 设计基线

### 3.1 采用规范

- Material Design 3 官方规范：`https://m3.material.io`
- 配色算法：`@material/material-color-utilities`（MIT，离线 npm 包）
- 参考实现：PiliPlus（Flutter）的主题系统
  - `lib/models/common/theme/theme_color_type.dart`（预设种子色）
  - `lib/pages/setting/pages/color_select.dart`（模式 / 动态取色 / 自定义 / 变体）
  - 依赖：`material_color_utilities`、`flex_seed_scheme`、`dynamic_color`

### 3.2 本地化偏差（有意为之）

| 项 | 官方规范 | 本项目 | 原因 |
|---|---|---|---|
| 字号字重 | 大字号 400 | 中文用 500 | 微软雅黑 400 在 20px+ 偏细 |
| 控件密度 | 标准 56dp | 桌面紧凑 -1：输入 48 / 按钮 40 / 行 56 | 1280×720 下保证信息密度 |
| Top App Bar | 64dp 顶栏 | 沿用宿主 40px 自绘标题栏，内容区加 56px 页面头 | 标题栏由 WinForms 绘制 |
| 字体 | Roboto | 系统字体栈 | 不打包中文字体，离线环境 |
| 背景模糊 | 无 | 不使用 | 透明/模糊在大面积 WebView 中性能不稳 |

### 3.3 组件尺寸基线

| 元素 | 高度 | 圆角 |
|---|---|---|
| Button | 40 | 20（full） |
| IconButton | 40×40 | full |
| 输入框 / 下拉触发器 | 48 | 顶部 8 |
| 列表行（单行 / 双行） | 56 / 72 | 0（容器 12） |
| 侧栏项 | 48 | full |
| Chip | 32 | 8 |
| Switch 轨道 | 32 | 16 |
| 弹窗 | — | 28 |

---

## 4. 设计令牌

### 4.1 颜色角色（浅色 / 深色）

以 seed `#529EF1`、Tonal Spot 方案生成；落地值以 `src/theme/palettes.ts` 中的生成结果为准，下表为基线校验值。

| 角色 | Light | Dark |
|---|---|---|
| primary | `#0061A4` | `#9ECAFF` |
| onPrimary | `#FFFFFF` | `#003258` |
| primaryContainer | `#D1E4FF` | `#00497D` |
| onPrimaryContainer | `#001D36` | `#D1E4FF` |
| secondary | `#535F70` | `#BBC7DB` |
| onSecondary | `#FFFFFF` | `#253140` |
| secondaryContainer | `#D7E3F7` | `#3B4858` |
| onSecondaryContainer | `#101C2B` | `#D7E3F7` |
| tertiary | `#6B5778` | `#D6BEE4` |
| onTertiary | `#FFFFFF` | `#3B2948` |
| tertiaryContainer | `#F2DAFF` | `#523F5F` |
| onTertiaryContainer | `#251431` | `#F2DAFF` |
| error | `#BA1A1A` | `#FFB4AB` |
| onError | `#FFFFFF` | `#690005` |
| errorContainer | `#FFDAD6` | `#93000A` |
| onErrorContainer | `#410002` | `#FFDAD6` |
| surface | `#FDFCFF` | `#1A1C1E` |
| onSurface | `#1A1C1E` | `#E2E2E6` |
| onSurfaceVariant | `#43474E` | `#C3C7CF` |
| surfaceDim | `#DED9DF` | `#111416` |
| surfaceBright | `#FDFCFF` | `#37393C` |
| surfaceContainerLowest | `#FFFFFF` | `#0F1113` |
| surfaceContainerLow | `#F7F2FA` | `#1A1C1E` |
| surfaceContainer | `#F1ECF4` | `#1E2022` |
| surfaceContainerHigh | `#ECE6F0` | `#282A2C` |
| surfaceContainerHighest | `#E6E0E9` | `#333537` |
| outline | `#73777F` | `#8D9199` |
| outlineVariant | `#C3C7CF` | `#43474E` |
| inverseSurface | `#2F3033` | `#E2E2E6` |
| inverseOnSurface | `#F1F0F4` | `#2F3033` |
| inversePrimary | `#9ECAFF` | `#0061A4` |
| scrim | `#000000` | `#000000` |
| shadow | `#000000` | `#000000` |

命名统一为 `--md-sys-color-*`。阶段 1 先在 `src/styles/tokens/_color.scss` 写入 seed `#529EF1` 的基线值作为首屏兜底；阶段 6 起由 JS 按 seed 生成并通过 `<style id="md-scheme">` 覆盖。组件里不得直接引用具体色值。另有两组项目扩展角色 `success*` / `warning*`，不随 seed 生成，保持语义稳定。

### 4.2 旧变量别名（迁移兼容）

第 1 阶段让旧代码零改动即可变色：

```scss
:root {
  --accent: var(--md-sys-color-primary);
  --accent-soft: color-mix(in srgb, var(--md-sys-color-primary) 10%, transparent);
  --bg: var(--md-sys-color-surface-container-lowest);
  --panel: var(--md-sys-color-surface-container-low);
  --panel-solid: var(--md-sys-color-surface);
  --text: var(--md-sys-color-on-surface);
  --muted: var(--md-sys-color-on-surface-variant);
  --border: var(--md-sys-color-outline-variant);
  --danger: var(--md-sys-color-error);
  --radius-xs: 4px;
  --radius-sm: 8px;
  --radius-md: 12px;
  --radius-lg: 16px;
  --radius-pill: 999px;
}
```

### 4.3 字体（Typescale）

| Token | 字号 / 行高 | 字重 | 用途 |
|---|---|---|---|
| `--md-sys-typescale-headline-small` | 24 / 32 | 500 | 页面标题 |
| `--md-sys-typescale-title-large` | 22 / 28 | 500 | 弹窗标题 |
| `--md-sys-typescale-title-medium` | 16 / 24 | 500 | 区块标题 |
| `--md-sys-typescale-title-small` | 14 / 20 | 500 | 列表项主文本、侧栏项 |
| `--md-sys-typescale-body-large` | 16 / 24 | 400 | 正文（少用） |
| `--md-sys-typescale-body-medium` | 14 / 20 | 400 | 表单标签、说明 |
| `--md-sys-typescale-body-small` | 12 / 16 | 400 | 次要信息 |
| `--md-sys-typescale-label-large` | 14 / 20 | 500 | 按钮文字 |
| `--md-sys-typescale-label-medium` | 12 / 16 | 500 | 表头、Chip |
| `--md-sys-typescale-label-small` | 11 / 16 | 500 | 快捷键标签 |

约束：全局只使用 **400 / 500 / 600** 三档字重，禁用 700。

### 4.4 形状

| Token | 值 | 用途 |
|---|---|---|
| `--md-sys-shape-corner-extra-small` | 4 | 快捷键标签、Menu、Snackbar |
| `--md-sys-shape-corner-small` | 8 | 输入框顶角、Chip |
| `--md-sys-shape-corner-medium` | 12 | 卡片、列表容器 |
| `--md-sys-shape-corner-large` | 16 | 大容器、侧栏分组 |
| `--md-sys-shape-corner-extra-large` | 28 | Dialog |
| `--md-sys-shape-corner-full` | 999 | Button、IconButton、Switch、侧栏激活胶囊 |

### 4.5 高度

| Level | 用途 | 实现 |
|---|---|---|
| 0 | 页面背景 | `surface` |
| 1 | 卡片 / 分组 | `surfaceContainerLow` + 1px `outlineVariant`，无阴影 |
| 2 | Menu / Dropdown | `surfaceContainer` + `0 2px 6px rgba(0,0,0,.15)` |
| 3 | Dialog | `surfaceContainerHigh` + `0 8px 24px rgba(0,0,0,.18)` |
| — | Snackbar | `inverseSurface`，无阴影 |

现有 9 个 `--shadow-*` 收敛为 3 个。

### 4.6 间距与密度

- 4dp 网格：4 / 8 / 12 / 16 / 24 / 32 / 48。
- 页面左右内边距 32px；分组间距 24px；组内边距 16px。
- 桌面密度 -1：输入 48 / 按钮 40 / 列表行 56–72 / 侧栏项 48。

### 4.7 状态层

```scss
@mixin state-layer($color: var(--md-sys-color-on-surface)) {
  position: relative;
  &::after {
    content: '';
    position: absolute;
    inset: 0;
    border-radius: inherit;
    background: $color;
    opacity: 0;
    pointer-events: none;
    transition: opacity 120ms cubic-bezier(.2, 0, 0, 1);
  }
  &:hover::after { opacity: .08; }
  &:focus-visible::after { opacity: .10; }
  &:active::after { opacity: .10; }
}
```

透明度：hover 8% / focus 10% / pressed 10% / dragged 16%（内容禁用 38%，容器禁用 12%）。

### 4.8 可访问性

```scss
:focus-visible {
  outline: 2px solid var(--md-sys-color-primary);
  outline-offset: 2px;
}
```

- 移除 `outline: none !important`；改用 `:focus-visible`，避免鼠标点击出现焦点框。
- 滚动条改为 overlay 细条（6px，`surfaceContainerHighest` 滑块），不占布局。
- 正文对比度 ≥ 4.5:1，大字号 ≥ 3:1。

### 4.9 动效

| Token | 值 | 用途 |
|---|---|---|
| `--md-sys-motion-easing-standard` | `cubic-bezier(.2, 0, 0, 1)` | 通用 |
| `--md-sys-motion-duration-short` | 100ms | 状态层、悬停 |
| `--md-sys-motion-duration-medium` | 200ms | 弹窗、展开 |
| `--md-sys-motion-duration-long` | 300ms | 页面切换 |

---

## 5. 主题系统

### 5.1 三个正交维度

```
主题模式 theme:       system | light | dark
配色来源 colorSource:  preset | custom | system
种子色 seedColor:      #RRGGBB
（预留）schemeVariant: tonalSpot（UI 不暴露，字段先落地）
```

### 5.2 预设色板（精简 8 套）

| 名称 | seed |
|---|---|
| WuGesture 蓝（默认） | `#529EF1` |
| 青 | `#009688` |
| 绿 | `#43A047` |
| 紫 | `#7E57C2` |
| 粉 | `#FF7299` |
| 橙 | `#FB8C00` |
| 红 | `#E53935` |
| 灰 | `#757575` |

- 保存的是具体色值而非索引，后续增删预设不影响既有配置。
- 每个色板在设置页显示四色条预览（primary / secondary / tertiary / surfaceContainer），选中项外描边。

### 5.3 自定义取色

- 使用原生 `<input type="color">` + HEX 输入框（现已有颜色输入框样式），不引入第三方取色器。
- 输入 `#RRGGBB`，非法值回退到当前 seed。

### 5.4 动态配色（Windows 强调色）

数据源优先级：

1. WinRT `Windows.UI.ViewManagement.UISettings.GetColorValue(UIColorType.Accent)`
2. 注册表 `HKCU\Software\Microsoft\Windows\DWM\ColorizationColor`

流程：

```
C# 读取强调色 → rules 消息 appearance.systemAccent
  → 前端以它为 seed 生成色板
  → 饱和度过低（HSL S < 0.12）或过亮/过暗时回退到当前预设 seed
```

- 不做壁纸取色（Windows 无 Material You 壁纸取色 API）。
- 实时监听 `WM_SETTINGCHANGE("ImmersiveColorSet")` 作为二期增强，本期采用“下次打开配置窗口时重新读取”。

### 5.5 预留扩展位

| 扩展 | 预留方式 |
|---|---|
| 调色板风格（Vibrant / Expressive / Monochrome …） | `schemeVariant` 字段已入配置，Normalizer 接受合法值，UI 暂不暴露 |
| 纯黑模式 | 纯前端分支，将来新增 `pureBlack` 字段（可缺省），无需提前占位 |
| 更多预设色 | 追加 `palettes.ts` 数组项 |

### 5.6 首屏与切换

- 宿主已有 `data-theme` 预注入，主题模式无闪烁。
- 色板解析结果缓存到 `localStorage`（seed + variant + mode → 角色色），应用挂载前先用缓存值注入，`rules` 消息到达后校正。
- 切换配色只替换 `<style id="md-scheme">` 内容，不重建组件，耗时目标 < 100ms。

---

## 6. 配置与数据流

### 6.1 配置结构（新增字段）

```jsonc
"uiSettings": {
  "appearance": {
    "theme": "dark",                 // 已有
    "colorSource": "preset",         // 新增：preset | custom | system
    "seedColor": "#529EF1",          // 新增
    "schemeVariant": "tonalSpot",    // 新增：预留，UI 不暴露
    "lightTitleBarColor": "#FFFFFF", // 已有，由前端按色板回写
    "lightTitleBarTextColor": "#16202B",
    "darkTitleBarColor": "#1B222B",
    "darkTitleBarTextColor": "#EDF2F7"
  }
}
```

默认值：`theme` 保持现状（默认 `dark`），`colorSource = preset`，`seedColor = #529EF1`，`schemeVariant = tonalSpot`。

### 6.2 数据流

```
启动
  C# 读 gestures.json → GestureConfig（新字段缺失时用默认值）
  → rules 消息（含 appearance + systemAccent）
  → 前端生成色板 → 注入 CSS 变量 → 渲染

修改配色
  前端本地立即应用（乐观更新）
  → 250ms 防抖 save-rules
  → C# 反序列化 → GestureConfigNormalizer 校验
  → 原子写入 gestures.json
  → 回执 → 前端确认（失败时回滚并 Toast）

宿主同步
  前端把解析出的标题栏前景/背景色写入 appearance 四个颜色字段
  → 宿主 ApplyWindowTheme 立即重绘标题栏
  → 托盘菜单读取同一组颜色
```

### 6.3 C# 改动清单

| 文件 | 改动 | 说明 |
|---|---|---|
| `Configuration/GestureUiSettings.cs` | `AppearanceUiSettings` 增加 `ColorSource`、`SeedColor`、`SchemeVariant` | 带默认值，纯增量 |
| `Configuration/GestureConfigContract.cs` | 新增 `AppearanceColorSources`、`AppearanceSchemeVariants` 常量 | 白名单集中管理 |
| `Configuration/GestureConfigNormalizer.cs` | 归一化三个新字段：枚举回退、hex 校验 | 加单元测试 |
| `Application/WebViewRulesPayloadFactory.cs` | 下发 `colorSource`、`seedColor`、`schemeVariant`、`systemAccent` | 只加字段 |
| `Application/WindowsAccentColor.cs`（新增） | 读 WinRT 强调色，失败回退注册表 | 带缓存 |
| `Application/MainForm.Windowing.cs` | 保存后重新应用窗口主题（已有路径，确认覆盖） | 基本不改 |
| `Application/TrayMenuRenderer.cs` | 托盘调色板改为读取 `AppearanceUiSettings` | 去掉写死色值 |

### 6.4 前端改动清单

| 文件 | 改动 |
|---|---|
| `Web/src/theme/palettes.ts`（新增） | 8 套预设 seed + 变体枚举 |
| `Web/src/theme/scheme.ts`（新增） | 封装 `@material/material-color-utilities`，seed → 角色色 |
| `Web/src/theme/applyScheme.ts`（新增） | 注入 CSS 变量、缓存、fallback、标题栏颜色回写 |
| `Web/src/composables/useAppearanceTheme.ts` | 从双态切换升级为“模式 × 来源”（整文件重写，转 TS） |
| `Web/src/pages/settings/theme/index.vue`（新增） | 外观设置页 |
| `Web/src/pages/settings/theme/*`（新增） | 预设色板、自定义取色、实时预览组件 |
| `Web/src/router/routes/*` | 新增 `/settings/theme` |
| `Web/src/components/sidebar/SidebarFooter.vue` | 快捷切换改为三态循环 |
| `Web/src/constants/gestureEditorUiDefaults.js` | 新增字段默认值 |
| `Web/src/utils/gestureEditorNormalizers.js` | 新增字段白名单与归一化 |
| `Web/src/utils/gestureEditorPayloads.js` | appearance 新字段写入 payload |

### 6.5 兼容性

| 场景 | 行为 |
|---|---|
| 旧配置文件 | 新字段缺失 → Normalizer 填默认值，不报错 |
| 导入旧配置 | 同上，主题沿用原 `theme` |
| 降级到旧版本 | 旧版本保存时会丢弃新字段（与现有新增配置项行为一致） |
| `schemaVersion` | 不升版（只新增可缺省字段） |
| WebDAV | 自动携带 |
| 前端 localStorage 缓存 | 仅作首屏加速，最终以 `rules` 消息为准 |

---

## 7. 组件改造规范

| 组件 | 现文件 | MD3 规格 | 验收要点 |
|---|---|---|---|
| Filled / Tonal / Outlined / Text Button | `styles.scss:360-390` | 高 40、full 圆角、无阴影、label 14/20 500、状态层 | 4 变体 × 3 尺寸，图标 18 |
| IconButton | `components/ui/IconActionButton.vue` | 40×40、图标 24、full 圆角 | hover / pressed / focus 可辨 |
| 输入框 | `styles.scss:181-212` | Filled Text Field：高 48、顶角 8、`surfaceContainerHighest`、聚焦 2px primary 底边 | 浮动标签、错误态、禁用态 |
| 下拉 | `form/CustomSelect.vue` | Exposed Dropdown：触发器 48、Menu 圆角 4、项高 48、选中 `secondaryContainer` + 勾选 | 保留键盘导航与 Teleport |
| 布尔项 | `form/ToggleCheckbox.vue` | Switch：轨道 52×32、滑块 24/16 | 整行可点，Switch 在行尾 |
| 滑块 | `form/BaseRange.vue` | 轨道 4、滑块 20、拖动值提示 | 保留 `--range-progress` |
| 卡片 / 分组 | `styles.scss:323-354` | Filled Card：12 圆角、`surfaceContainerLow`、1px 描边、无阴影 | 去掉 0 圆角特例 |
| 规则列表 | `gesture/GestureRuleList.vue` | List：行高 56、表头 12/16 500 muted、行 hover 状态层 | 删除按钮改为常显 IconButton |
| 快捷键标签 | 行内样式 | 4 圆角、`surfaceContainerHighest` + `outlineVariant` | 去投影、去胶囊 |
| 侧栏 | `sidebar/AppSidebar.vue` | Navigation Drawer：宽 256、项高 48、激活 full 胶囊 `secondaryContainer` | 去掉左侧蓝条 |
| 弹窗 | `dialog/BaseDialog.vue` | 28 圆角、24 内边距、遮罩 `scrim` 32%、按钮右下 | 标题 22/28 |
| Toast | `vue-toastification` | Snackbar：`inverseSurface`、4 圆角、高 48 | 深色模式一致 |
| Chip | 各处 pill | 高 32、8 圆角、1px 描边、14/20 500 | 分类/作用域标签 |
| Tooltip | `ui/HoverBubble.vue` | `inverseSurface`、4 圆角、12/16 | 延迟 500ms |
| 分割线 | 各处 | 1px `outlineVariant`，缩进 16 | 统一 |
| 图标 | `icons/appIcons.js` + SVG | 24dp 网格、统一视觉重量 | 第 7 阶段替换 |
| 引导 / 帮助 / 更新弹窗 | `WelcomeDialog.vue` 等 | 同 Dialog 规范 | 内联 SVG 统一描边 |

---

## 8. 布局

```
┌───────────────────────────────────────────────┐
│ 原生标题栏（WinForms 40px，不改）              │
├───────────────┬───────────────────────────────┤
│ Navigation    │  Page header 56px             │
│ Drawer 256px  │  标题 24/32 + 右侧 actions     │
│               │───────────────────────────────│
│ 项高 48       │  surface 背景                 │
│ 激活胶囊      │  ┌ List section 12px ───────┐ │
│               │  │ 行高 56 / 内边距 16       │ │
│ 底部：主题/帮助│  └───────────────────────────┘ │
└───────────────┴───────────────────────────────┘
```

- 内容区从“卡片堆叠”改为“分组列表”。
- 保留侧栏收起 / 边缘唤醒逻辑（`uiSettings.sidebar`），只换视觉。
- 不引入 MD3 Tabs，避免改动路由结构。

---

## 9. 实施计划

### 阶段 0：设计定稿与工具链地基（1 人日）

- 产出令牌表、色板基线、尺寸基线（本文档即产出）。
- 建立开发用预览页 `/dev/md3`（仅 `vite dev`，不进入构建产物）。
- **L0 TypeScript 地基**（详见附录 D）：新增 `tsconfig.json`、`typescript` / `vue-tsc`、`src/env.d.ts`；`allowJs: true` + `checkJs: false`；Prettier 增加 `ts` / `tsx` 匹配；新增 `typecheck` 脚本与 CI 检查。**不迁移任何存量文件。**
- 验收：评审通过，无待定项；`pnpm typecheck` 通过；存量 `.js` / `.vue` 不受影响。

### 阶段 1：令牌层（1.5 人日）

- `styles.scss` 拆为 `styles/tokens/*`（color / typescale / shape / elevation / spacing / motion / state），保留旧变量别名。
- 主题令牌与色板类型用 TS 定义（L1 起点，见附录 D）。
- UnoCSS shortcut 中的写死 rgba 改为语义 token。
- 恢复 `:focus-visible`，滚动条改 overlay 样式。
- 验收：不改任何组件，全站颜色/圆角/字号统一；深色模式无浅色块。

### 阶段 2：基础控件（4 人日）

- Button、IconButton、TextField、Select、Switch、Slider、Checkbox、Chip、Tooltip、Divider。
- 上述控件均为新建或整体重写，**一律使用 `<script setup lang="ts">`**，`defineProps` / `defineEmits` 显式声明类型。
- 保持现有 props/事件兼容，页面基本不改。
- 验收：设置页与规则弹窗全部使用新控件；键盘可完整操作；`pnpm typecheck` 通过。

### 阶段 3：容器与导航（3 人日）

- `AppShell`、`AppSidebar → NavigationDrawer`、页面头、分组列表容器。
- `BaseDialog` 规范、Toast 主题化、`GestureRuleList` 行重排。
- 重写过程中拆出的新子组件一律 TS；整文件重写的组件允许直接转为 TS。
- 验收：1280×720 首屏密度合理；滚动条不闪、不占位。

### 阶段 4：页面落地（4 人日）

- 全局 / 分类 / 程序 / 边缘 / 排除项 / 设置 9 个子页 / 关于。
- 引导、帮助、快捷搜索、更新弹窗。
- 验收：所有页面同一套网格；无孤立硬编码色值。

### 阶段 5：后端配置扩展（1 人日）

- 6.3 节全部改动 + `systemAccent` 下发。
- 单元测试：Normalizer 新字段校验、Payload 工厂字段存在性。
- 验收：`dotnet test` 通过；手动保存后 `gestures.json` 保留新字段；旧配置文件可正常加载。

### 阶段 6：前端主题系统（2 人日）

- 配色引擎、预设 8 色、自定义取色、跟随系统（动态）。
- 外观设置页 + 侧栏快捷切换。
- 标题栏/托盘配色回写。
- 验收：切换配色后重开窗口、重启应用、导入导出后均保留；标题栏与新色板一致。

### 阶段 7：图标与打磨（2.5 人日）

- 图标替换为 24dp 网格 SVG（Apache 2.0，本地文件）。
- 125% / 150% DPI、深浅色、8 套配色全量走查。
- 对比度、焦点顺序、`prefers-reduced-motion`。
- 验收：检查清单全绿，产出前后对比截图。

**合计约 19 人日（区间 18–23）。**

> 若只想先看到效果：阶段 0 地基 + 阶段 1 + 2 + 3 约 9 人日，观感已完成约 80%；主题系统可在其后追加。

---

## 10. 测试与验收

### 10.1 自动化

- 前端：`pnpm format:check`、`pnpm typecheck`、`pnpm test`（Vitest）。
  - 新增：`scheme.ts` 的确定性快照（同 seed 同 variant 生成相同角色色）。
  - 新增：`palettes.ts` 长度与 hex 合法性。
  - 新增：`gestureEditorNormalizers` 对新字段的默认值/非法值处理。
- 后端：`dotnet build`、`dotnet test`、`dotnet format --verify-no-changes`。
  - 新增：`GestureConfigNormalizer` 新字段用例（缺省、非法枚举、非法 hex）。
  - 新增：`WebViewRulesPayloadFactory` 包含 `systemAccent`。
- CI 现有覆盖率下限不下调。

### 10.2 人工验收清单

| 类别 | 检查项 |
|---|---|
| 视觉 | 8 套配色 × 浅/深 × 1280/1920；无硬编码色残留；圆角仅 4/8/12/16/28/full |
| 层级 | 页面标题 / 区块标题 / 列表项 / 说明四级可区分 |
| 交互 | hover / focus / pressed / disabled 四态可见；键盘 Tab 顺序合理 |
| 主题 | 切换后立即生效；重开窗口保留；重启保留；导入导出保留；WebDAV 恢复保留 |
| 宿主 | 标题栏与页面同色；托盘菜单同色；深色标题栏文字对比度达标 |
| DPI | 100% / 125% / 150% 无错位、无裁切 |
| 性能 | 换色 < 100ms；首屏无闪烁；无滚动卡顿 |
| 可访问性 | 正文对比度 ≥ 4.5:1；焦点环不被裁切 |

### 10.3 回滚策略

- 每个阶段独立提交，可单独回退。
- 令牌层保留旧变量别名，组件层可逐步启用。
- 主题系统失败时回退到静态默认色板（`#529EF1`）。

---

## 11. 风险与应对

| 风险 | 影响 | 应对 |
|---|---|---|
| 令牌未覆盖全部硬编码 | 换主题后局部不变色 | 阶段 1 用脚本扫描 hex/rgba 残留，逐项清除 |
| UnoCSS shortcut 写死浅色 | 深色下出现浅块 | 阶段 1 一并改语义 token |
| 控件变高导致页面变长 | 720 高度下信息密度下降 | 桌面密度 -1，必要时对设置页使用 48px 行 |
| 标题栏配色不同步 | 视觉割裂 | 前端回写四个颜色字段；托盘菜单改为读配置 |
| 动态强调色过灰 | 界面发灰 | HSL 饱和度阈值判断 + 回退预设 |
| 首屏闪烁 | 体验差 | 宿主预注入 + localStorage 缓存先行应用 |
| 配置字段被后端丢弃 | 主题无法持久化 | 先落 C# schema，再上前端 UI |
| 焦点环引发滚动条 | 布局跳动 | `outline` + `outline-offset`；滚动条 overlay |
| 图标替换工作量大 | 拖慢进度 | 单独放阶段 7，可延后 |
| 中文大字重偏细 | 观感轻飘 | 中文大字号统一 500 |

---

## 12. 明确不做

- 壁纸取色（仅 Windows 强调色）。
- 纯黑（OLED）模式。
- 调色板风格选择器（字段预留）。
- 整站皮肤 / 布局变体。
- 第三方 UI 库、在线字体、CDN 图标。
- 消息契约变更。
- 小屏 / 移动端适配。
- 全量 TypeScript 迁移（按附录 D 分层收敛，不设全量目标）。

---

## 13. 决策记录

| 日期 | 决策 | 结论 |
|---|---|---|
| 2026-11-09 | 设计规范 | 采用 Material Design 3 |
| 2026-11-09 | 技术路线 | 纯自研 token + 组件，不引入 UI 库 |
| 2026-11-09 | 动态配色来源 | Windows 强调色，不做壁纸取色 |
| 2026-11-09 | 预设色板 | 精简 8 套 |
| 2026-11-09 | 调色板风格 | 本期不暴露，字段预留 |
| 2026-11-09 | 纯黑模式 | 本期不做 |
| 2026-11-09 | 默认配色来源 | 预设 WuGesture 蓝（`#529EF1`） |
| 2026-11-09 | C# 配置扩展 | 同意修改 schema（新增 3 个字段 + 强调色下发） |
| 2026-11-09 | TypeScript 策略 | 存量不迁移；新文件与新组件一律 TS；逻辑层重构后按层收敛，不设全量迁移目标 |

---

## 14. 实施进度

| 阶段 | 状态 | 完成内容 |
|---|---|---|
| 阶段 0：设计定稿与工具链地基 | 已完成（2026-11-09） | `tsconfig.json`、`src/env.d.ts`、`typecheck` 脚本、Prettier TS 匹配、CI 类型检查步骤、`#/dev/md3` 令牌预览页；删除 `jsconfig.json` |
| 阶段 1：令牌层 | 已完成（2026-11-10） | `styles.scss` 拆为 `styles/tokens/*` + `_base` + `_components`；旧变量全部加别名；UnoCSS 写死 rgba 改语义色；恢复 `:focus-visible`；6px 细滚动条；新增 `theme/tokens.ts`、`theme/palettes.ts` |
| 阶段 2：基础控件 | 未开始 | — |
| 阶段 3：容器与导航 | 未开始 | — |
| 阶段 4：页面落地 | 未开始 | — |
| 阶段 5：后端配置扩展 | 未开始 | — |
| 阶段 6：前端主题系统 | 未开始 | — |
| 阶段 7：图标与打磨 | 未开始 | — |

---

## 附录 A：新增依赖

| 依赖 | 版本 | 用途 | 体积 |
|---|---|---|---|
| `@material/material-color-utilities` | 最新稳定版 | seed → MD3 角色色（HCT / 动态方案） | 约 40–60KB gzip |

- MIT 许可，纯 JS，无运行时网络请求。
- 除该包外不新增任何运行时依赖；开发期新增 `typescript@~5.9` 与 `vue-tsc@^3.3` 用于类型检查（见附录 D）。

## 附录 B：关键文件索引

| 层 | 文件 |
|---|---|
| 全局样式 | `Web/src/styles.scss`、`Web/src/styles/tokens/*`、`Web/src/styles/_base.scss`、`Web/src/styles/_components.scss` |
| 令牌类型 | `Web/src/theme/tokens.ts`、`Web/src/theme/palettes.ts` |
| UnoCSS | `Web/uno.config.js` |
| 应用外壳 | `Web/src/App.vue` |
| 侧栏 | `Web/src/components/sidebar/AppSidebar.vue` |
| 规则列表 | `Web/src/components/gesture/GestureRuleList.vue` |
| 下拉 | `Web/src/components/form/CustomSelect.vue` |
| 布尔项 | `Web/src/components/form/ToggleCheckbox.vue` |
| 弹窗 | `Web/src/components/dialog/BaseDialog.vue` |
| 主题 composable | `Web/src/composables/useAppearanceTheme.ts` |
| 业务协调 | `Web/src/gestureEditor/context/gestureEditorContext.js` |
| 后端配置 | `Configuration/GestureUiSettings.cs` |
| 后端归一化 | `Configuration/GestureConfigNormalizer.cs` |
| 后端下发 | `Application/WebViewRulesPayloadFactory.cs` |
| 标题栏 | `Application/MainForm.Chrome.cs`、`MainForm.Windowing.cs` |
| 托盘菜单 | `Application/TrayMenuRenderer.cs` |
| 消息契约 | `Application/WebViewMessageTypes.cs`（不改） |

## 附录 C：参考资料

- Material Design 3：`https://m3.material.io`
- 动态配色：`https://m3.material.io/styles/color/dynamic`
- `material-color-utilities`：`https://github.com/material-foundation/material-color-utilities`
- PiliPlus 主题实现：`https://github.com/bggRGjQaUbCoE/PiliPlus`
  - `lib/models/common/theme/theme_color_type.dart`
  - `lib/pages/setting/pages/color_select.dart`
  - `lib/utils/theme_utils.dart`

---

## 附录 D：TypeScript 迁移策略

### D.1 原则

1. **存量不迁移**：现有 64 个 SFC、47 个 `.js` 模块保持原状，不专门排期改造。
2. **新增必 TS**：本计划新增的文件、新建组件一律 TypeScript。
3. **顺手迁移**：存量文件因 MD3 重写而改动超过 50% 时，直接转为 TS。
4. **按层收敛，不设全量目标**：逻辑层全 TS 即为达标，页面组件按需推进。

混用 `.js` / `.ts` / `.vue` 对 Vite 构建、MSBuild 前端增量构建、打包产物均无影响，策略可随时停在任一层。

### D.2 分层计划

| 层级 | 范围 | 做法 | 工作量 | 时机 | 完成定义 |
|---|---|---|---|---|---|
| **L0** | 工具链 | `tsconfig.json`、`typescript`、`vue-tsc`、`src/env.d.ts`、`typecheck` 脚本、CI 检查 | 0.5 人日 | 阶段 0 | `pnpm typecheck` 通过，存量文件零改动 |
| **L1** | 契约层 | `src/types/*.ts`、`theme/*.ts`；`gestureEditorPayloads` / `Normalizers` / `ViewModels` / `Collections` 转 TS | 1.5–2 人日 | 阶段 1–2 | 消息与配置载荷有类型，4 组单测转 TS 后通过 |
| **L2** | 状态层 | 5 个 store、5 个 composable、10 个 module、`gestureEditorContext` | 2–3 人日 | 阶段 4 之后 | 逻辑层无 `.js`，`strict` 下无 `@ts-expect-error` |
| **L3** | 组件层（增量） | MD3 新建与重写组件 | 0（顺手） | 阶段 2–3 | 新组件全部 `<script setup lang="ts">` |
| **L4** | 组件层（存量） | 剩余旧 SFC | 5–8 人日 | 可选，页面被动改动时顺手做 | 不再排期，自然收敛 |

**投入 L0–L2 约 4–5.5 人日，覆盖约 80% 的类型风险。**

### D.3 文件与命名约定

| 类型 | 约定 |
|---|---|
| 主题引擎 | `src/theme/palettes.ts`、`scheme.ts`、`applyScheme.ts` |
| 类型定义 | `src/types/config.ts`（对齐 C# DTO）、`src/types/webview.ts`（对齐消息契约）、`src/types/ui.ts` |
| 新组件 | `<script setup lang="ts">`，`defineProps` / `defineEmits` 使用类型声明 |
| 存量文件 | 保持 `.js`；仅在整体重写时改后缀 |
| 测试 | 与被测文件同目录，`*.test.ts` |

### D.4 工具链配置要点

**新增开发依赖**

| 依赖 | 版本 | 用途 |
|---|---|---|
| `typescript` | `~5.9`（固定） | 编译器 |
| `vue-tsc` | `^3.3` | SFC 类型检查 |

> 注意：TypeScript 7 的原生版不再导出 `typescript/lib/tsc`，与 `vue-tsc` 3.x 不兼容（实测报 `ERR_PACKAGE_PATH_NOT_EXPORTED`），因此固定在 5.9.x，待 `vue-tsc` 支持后再升级。

**`tsconfig.json` 关键项**

```jsonc
{
  "compilerOptions": {
    "allowJs": true,          // 允许混用存量 JS
    "checkJs": false,         // 不检查存量 JS（避免一次性爆错）
    "strict": true,
    "skipLibCheck": true,
    "moduleResolution": "bundler",
    "types": ["vite/client"],
    "baseUrl": ".",
    "paths": {
      "@/*": ["./src/*"],
      "@resources/*": ["./../Resources/*"]
    }
  },
  "include": ["src/**/*.ts", "src/**/*.d.ts", "src/**/*.vue"]
}
```

> `tsconfig.json` 取代了原来的 `jsconfig.json`（已删除），路径别名与原配置一致。

**`src/env.d.ts`**

```ts
/// <reference types="vite/client" />

declare module '*.svg?component' {
  import type { DefineComponent } from 'vue'

  const component: DefineComponent<{}, {}, any>
  export default component
}

declare module '*.svg' {
  const src: string
  export default src
}
```

> 不声明 `*.vue` 模块，交给 `vue-tsc` 按真实 SFC 推断 props 与 emits。

**脚本与检查**

```jsonc
"scripts": {
  "typecheck": "vue-tsc --noEmit"
}
```

- Prettier 的 glob 增加 `ts,tsx`。
- CI 的 `test.yml` 增加 `pnpm typecheck` 步骤；只检查 TS 文件，逐步收紧。

### D.5 防止类型与后端契约漂移

`src/types/config.ts` 与 `src/types/webview.ts` 是 C# DTO 的手工镜像，必须防止单边修改：

1. 建立契约测试 `src/types/contract.test.ts`：断言 `appearance`、消息类型的字段集合与固定 fixture 一致；
2. 修改 `GestureUiSettings.cs` / `WebViewMessageTypes.cs` 时，CI 会因契约测试失败而提醒；
3. 暂不做从 C# 自动生成 `.d.ts`（成本高于收益，后续有需要再评估）。

### D.6 风险

| 风险 | 应对 |
|---|---|
| 混合期边界不清 | 只允许 `src/types`、`src/theme`、新组件为 TS，其余默认 JS |
| `checkJs: true` 一次性爆错 | 保持 `false`，仅对已迁移文件生效 |
| `vue-tsc` 与 Vite 8 / Vue 3.5 版本兼容 | 使用最新稳定版；CI 中独立步骤，失败不阻塞构建 |
| CI 变慢 | `skipLibCheck: true`，只检查 TS 文件 |
| 类型与 C# 漂移 | 契约测试 + 修改清单双人核对 |
| 半途而废 | L1 / L2 有明确完成定义，做完即封版 |

