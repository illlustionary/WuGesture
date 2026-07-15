# WuGesture

使用 C# WinForms、WebView2 和原生全局鼠标钩子的 Windows 鼠标手势应用。

## 当前状态

- 桌面宿主：`src/WuGesture.App`
- 前端宿主：`src/WuGesture.App/Web`
- 现有 AutoHotkey 实验：`gesture.ahk`
- 手势配置：`%AppData%\WuGesture\gestures.json`

首个原型内置了 3 个全局手势：

| 手势 | 动作 |
| --- | --- |
| Left | Alt + Left |
| Right | Alt + Right |
| Down, Right | Ctrl + W |

在手势跟踪期间，原始右键按下/抬起事件会被吞掉，这样其他应用不会收到右键拖拽。如果移动太小，不足以构成手势，应用会重放一次普通右键单击。

## 运行

```powershell
dotnet run --project src\WuGesture.App\WuGesture.App.csproj
```

## 构建

```powershell
dotnet build WuGesture.slnx
```

默认 Debug 输出会被 Git 忽略：

```text
artifacts\debug\WuGesture
```

默认 Debug 可执行文件：

```text
artifacts\debug\WuGesture\WuGesture.exe
```

## 发布

```powershell
.\scripts\publish.ps1
```

默认发布输出会被 Git 忽略：

```text
artifacts\publish\WuGesture
```

默认发布可执行文件：

```text
artifacts\publish\WuGesture\WuGesture.exe
```

自动发行的 Windows x64 ZIP 是依赖框架版本，不内置 .NET。目标电脑需要安装 [.NET 10 Desktop Runtime x64](https://dotnet.microsoft.com/download/dotnet/10.0/runtime)；如果未安装，`WuGesture.exe` 的原生 .NET 启动器会显示安装提示，点击下载按钮即可打开官方安装页面。

## 自动发行

将仓库迁移到 GitHub 后，推送 `v0.97` 这类标签会触发 `.github\workflows\release.yml`：它会关闭正在运行的应用、构建并压缩发布产物、根据两个标签之间的 Git 提交生成更新说明，以及创建 GitHub Release。

为同步发布至 Gitee，在 GitHub 仓库的 Actions secrets 中配置：

- `GITEE_REPOSITORY`：Gitee 仓库路径，例如 `neko_nya/my-gesture`。
- `GITEE_TOKEN`：有仓库写入权限的 Gitee 私人令牌。

两个 secrets 都配置后，工作流会先镜像 `main` 和发行标签到 Gitee，再将同一份压缩包和更新说明发布到 Gitee。未配置时不会同步 Gitee，GitHub 发布仍会正常完成。

首次迁移时，保留 Gitee 为 `origin`，并新增 GitHub 远程。例如：

```powershell
git remote set-url origin git@gitee.com:neko_nya/WuGesture.git
git remote add github git@github.com:illlustionary/WuGesture.git
git push -u github main
git push github --tags
```

以后从干净的 `main` 工作区一键开始发布：

```powershell
.\scripts\start-release.ps1 -Version v0.97
```

该脚本默认推送到 `github`，创建并推送标签后由 GitHub Actions 接管构建、更新说明和双平台发布。本地需要只生成待检查的发行包时，可先创建标签，然后运行：

```powershell
.\scripts\new-release-package.ps1 -Version v0.97
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
