# Wu Gesture

使用 C# WinForms、WebView2 和原生全局鼠标钩子的 Windows 鼠标手势应用。

## 当前状态

- 桌面宿主：`src/MyGesture.App`
- 前端宿主：`src/MyGesture.App/Web`
- 现有 AutoHotkey 实验：`gesture.ahk`
- 手势配置：`%AppData%\MyGesture\gestures.json`

首个原型内置了 3 个全局手势：

| 手势 | 动作 |
| --- | --- |
| Left | Alt + Left |
| Right | Alt + Right |
| Down, Right | Ctrl + W |

在手势跟踪期间，原始右键按下/抬起事件会被吞掉，这样其他应用不会收到右键拖拽。如果移动太小，不足以构成手势，应用会重放一次普通右键单击。

## 运行

```powershell
dotnet run --project src\MyGesture.App\MyGesture.App.csproj
```

## 构建

```powershell
dotnet build MyGesture.slnx
```

默认 Debug 输出会被 Git 忽略：

```text
artifacts\debug\WuGesture
```

## 发布

```powershell
.\scripts\publish.ps1
```

默认发布输出会被 Git 忽略：

```text
artifacts\publish\WuGesture
```

## 架构

```text
MouseHook -> GestureService -> GestureRecognizer -> GestureMatcher -> Action execution
                                      |
                                      v
                                  WebView2 UI
```

原生侧负责全局鼠标钩子、手势识别和动作执行。
WebView 界面当前是独立的 Vue3 + Vite 配置工程，顶部包含 `全局`、`分类`、`App`、`边缘操作`、`排除项`。
`分类` 和 `App` 采用左右分栏：左侧选择具体项，右侧编辑对应规则；`排除项` 用于维护不执行鼠标手势的程序列表。
编辑器内不再显示手势提示区，命中结果仍由独立的全局提示窗展示。

## 下一步

1. 完善 `app/category/global` 规则作用域优先级的边界验证和文档同步。
2. 将 AutoHotkey 作为可选动作类型。
3. 等规则编辑器稳定后，添加配置导入/导出。
