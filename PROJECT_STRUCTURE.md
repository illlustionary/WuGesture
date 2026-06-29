# 项目结构

这是 `my-gesture` 的项目地图。每次修改前先阅读；只要改动影响项目布局、启动流程、配置、构建/发布行为，或核心模块职责，就要同步更新这里。

## 产品方向

`my-gesture` 是一个 Windows 鼠标手势应用。

当前目标：

- 原生后端负责全局鼠标钩子、手势识别、规则匹配和动作执行。
- WebView2 前端负责配置界面。
- 手势使用 8 个方向。
- 动作当前只支持通过 `SendInput` 发送热键。
- 规则当前只支持 `global` 作用域。未来计划支持 `category` 和 `app`。

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
- `GestureMatcher.cs`：将识别出的方向模式与已加载规则进行匹配。
- `ActionExecutor.cs`：通过 Win32 `SendInput` 执行热键。
- `MouseInput.cs`：当移动距离太小，不足以构成手势时，重放一次普通右键。
- `GestureDirection.cs`：8 方向枚举。
- `GestureRule.cs`：运行时规则和热键动作模型。
- `GestureHintForm.cs`：置顶的底部手势提示覆盖层。

手势流水线：

```text
MouseHook
-> GestureService
-> GestureRecognizer
-> GestureMatcher
-> ActionExecutor
-> GestureHintForm / WebView 状态
```

重要行为：

- 右键按下/抬起在手势跟踪期间会被吞掉。
- 如果移动太小，就会重放一次普通右键。
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

- `scope`：当前仅支持 `global`。
- `pattern`：手势方向列表，例如 `["Down", "Right"]`。
- `action.type`：当前仅支持 `hotkey`。
- `action.keys`：按键列表，例如 `["Control", "W"]`。

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
- `app.js`

当前 UI：

- 显示应用状态。
- 显示配置文件路径。
- 列出当前规则。
- 允许编辑现有规则的动作名称和热键字符串。
- 允许保存、重新加载和恢复默认规则。
- 热键输入在获得焦点时会监听按键：
  - 修饰键/特殊键组合会自动记录。
  - 也可以手动输入纯文本。

WebView 消息流：

- 前端发送：
  - `"get-status"`
  - `{ type: "save-rules", rules: [...] }`
  - `{ type: "reload-rules" }`
  - `{ type: "reset-rules" }`
- 后端发送：
  - `{ type: "status", ... }`
  - `{ type: "rules", ... }`
  - `{ type: "gesture", ... }`
  - `{ type: "gesture-progress", ... }`
  - `{ type: "gesture-action-failed", ... }`
  - `{ type: "config-result", ... }`

## 测试

路径：

```text
tests\MyGesture.App.Tests
```

当前测试覆盖：

- `GestureRecognizerTests.cs`：抖动过滤、噪声转向、短回拉、对角线识别。
- `GestureConfigMapperTests.cs`：默认配置映射和按键别名。

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

已知警告：

- .NET 10 下，WebView2 包会触发 `WindowsBase` 版本冲突警告。当前构建和测试仍然通过。

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
