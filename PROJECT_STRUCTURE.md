# 项目结构

这是 `my-gesture` 的项目地图。每次修改前先阅读；只要改动影响项目布局、启动流程、配置、构建/发布行为，或核心模块职责，就要同步更新这里。

## 产品方向

`my-gesture` 是一个 Windows 鼠标手势应用。

当前目标：

- 原生后端负责全局鼠标钩子、手势识别、规则匹配和动作执行。
- WebView2 前端负责配置界面，当前是独立的 Vue3 + Vite 工程，构建产物由桌面宿主加载。
- 手势使用 8 个方向。
- 动作当前只支持通过 `SendInput` 发送热键。
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
- `MainForm.cs` 初始化 WebView2、加载配置、创建 `GestureService`，并桥接 WebView 消息。

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
- `GestureService.cs`：跟踪右键手势生命周期，调用识别器、匹配器和执行器，并向 UI 发送事件。
- `GestureRecognizer.cs`：把鼠标轨迹转换为稳定的 8 方向模式。
- `GestureMatcher.cs`：将识别出的方向模式与已加载规则进行匹配，并按作用域优先级选择命中项。
- `GestureScopeContext.cs`：当前前台窗口的 app/category 上下文模型。
- `ForegroundWindowScopeContextProvider.cs`：读取前台窗口进程名。
- `ConfiguredScopeContextProvider.cs`：用配置里的应用程序列表把前台进程名映射到分类，供作用域匹配使用。
- `ActionExecutor.cs`：通过 Win32 `SendInput` 执行热键。
- `MouseInput.cs`：当移动距离太小，不足以构成手势时，重放一次普通右键。
- `GestureDirection.cs`：8 方向枚举。
- `GestureRule.cs`：运行时规则和热键动作模型。
- `GestureHintForm.cs`：独立的全局命中提示窗，移动过程中匹配到规则时立即显示规则名。

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
- 如果移动太小，就会重放一次普通右键。
- 移动过程中会增量识别当前轨迹；一旦匹配规则，全局提示窗会立即显示规则名。
- 动作仍在右键抬起时执行。
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
- `pattern`：手势方向列表，例如 `["Down", "Right"]`。
- `action.type`：当前仅支持 `hotkey`。
- `action.keys`：按键列表，例如 `["Control", "W"]`。
- `applications`：应用程序归属列表，每项包含 `name`、`path`、`category`，运行时通过前台进程名匹配 `name` 后得到分类。

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
- `styles.css`
- `package.json`：前端工程依赖与脚本。
- `vite.config.js`：Vite 构建配置。
- `src\main.js`：Vue 入口。
- `src\App.vue`：路由壳和顶栏。
- `src\components\`：页面壳、左侧插槽和规则表等通用组件。
- `src\composables\gestureEditorStore.js`：共享编辑状态、WebView 消息和快捷键监听。
- `src\pages\`：`global`、`category`、`app` 三个路由页。
- `src\styles.css`：编辑器样式。
- `dist\web\`：Vite 构建产物目录，由桌面宿主加载。

构建方式：

- 前端使用 `pnpm build` 生成根目录下的 `dist\web`。
- `MyGesture.App.csproj` 会在 `.NET` 构建前自动执行前端构建。
- `MyGesture.App.csproj` 会在前端构建后把 `dist\web` 复制到宿主输出目录中的 `Web\dist`。
- 桌面宿主通过 WebView2 虚拟主机 `https://appassets.local/` 加载宿主输出目录中的 `Web\dist`。

当前 UI：

- 3 个规则 tab：`全局`、`分类`、`App`，顶部使用横向标签页切换。
- `全局` 直接编辑整张表。
- `分类` 和 `App` 采用三栏骨架：左侧是分类/App 列表，中间是当前项内容区，右侧是参数区占位。
- `分类` 和 `App` 页的中间内容区保留“应用程序”和“手势列表”两个区块，分类页可管理当前分类下的 App，App 页可设置所属分类。
- 规则表列为 `名称`、`手势`、`命令`，删除按钮在每行右侧。
- 已移除编辑器内的手势提示区，只保留配置结果提示。
- 热键输入在获得焦点时会监听按键：
  - 修饰键/特殊键组合会自动记录。
  - 也可以手动输入纯文本。

WebView 消息流：

- 前端发送：
  - `"get-status"`
  - `{ type: "select-application", requestId: "...", category: "..." }`
  - `{ type: "save-rules", rules: [...], applications: [...] }`
  - `{ type: "reload-rules" }`
  - `{ type: "reset-rules" }`
- 后端发送：
  - `{ type: "status", ... }`
  - `{ type: "rules", rules: [...], applications: [...], ... }`
  - `{ type: "application-selected", requestId: "...", name: "...", path: "...", category: "..." }`
  - `{ type: "gesture", ... }`
  - `{ type: "gesture-action-failed", ... }`
  - `{ type: "config-result", ... }`

## 测试

路径：

```text
tests\MyGesture.App.Tests
```

当前测试覆盖：

- `GestureRecognizerTests.cs`：抖动过滤、噪声转向、短回拉、对角线识别。
- `GestureConfigMapperTests.cs`：默认配置映射、按键别名和作用域保留。
- `GestureMatcherTests.cs`：作用域优先级。
- `ConfiguredScopeContextProviderTests.cs`：应用程序到分类的运行时映射。

运行：

```powershell
dotnet test MyGesture.slnx
```

## 构建与发布

构建：

```powershell
dotnet build MyGesture.slnx
```

测试：

```powershell
dotnet test MyGesture.slnx
```

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
