# 发行维护

本文件面向仓库维护者。普通使用者只需完整解压发行包并运行根目录的 `WuGesture.exe`。该原生启动器会检查 [.NET 10 Desktop Runtime x64](https://dotnet.microsoft.com/download/dotnet/10.0/runtime) 和 [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/)；缺少依赖时会提示用户并可打开官方下载页面。

## 本地发布

```powershell
.\scripts\publish.ps1
```

发布脚本会关闭正在运行的 `WuGesture`、清理发布输出，再执行发布。它会生成依赖框架的主程序 `WuGesture.App.exe`，并将 NativeAOT 启动器作为用户入口 `WuGesture.exe` 写入同一目录。默认输出目录为 `artifacts\publish\WuGesture`。可使用 `-Version`、`-RuntimeIdentifier` 和 `-SelfContained` 指定版本与目标运行时。

本地执行发布脚本还需要安装 Visual Studio 或 Build Tools 的“使用 C++ 的桌面开发”工作负载（MSVC 链接器和 Windows SDK）；NativeAOT 启动器在 Windows 上需要该工具链。GitHub Actions 的 `windows-latest` 镜像已具备所需工具。

## 启动器验证

维护时可在启动 `WuGesture.exe` 前设置 `WUGESTURE_DOTNET_ROOT_OVERRIDE` 为一个空目录，用于安全验证缺少 .NET Desktop Runtime 时的提示和官方下载跳转。未设置该环境变量时，启动器始终检查系统默认的 `C:\Program Files\dotnet`。

完成测试后请在当前 PowerShell 会话执行 `$env:WUGESTURE_DOTNET_ROOT_OVERRIDE = $null`，避免后续启动继续使用测试目录。

可使用以下脚本完成一次可逆测试。它会创建空的临时 .NET 根目录、启动最新的本地发行包，并在结束时自动清理：

```powershell
.\scripts\test-missing-dotnet-runtime.ps1
```

默认运行时，脚本会自动点击缺少运行环境提示中的“是”，用于检查默认浏览器是否打开微软下载页：

```powershell
.\scripts\test-missing-dotnet-runtime.ps1
```

传入 `-Manual` 可保留提示框，手动确认浏览器跳转：

```powershell
.\scripts\test-missing-dotnet-runtime.ps1 -Manual
```

## 自动发行

从干净的 `main` 工作区执行：

```powershell
.\scripts\start-release.ps1 -Version v0.97
```

该脚本会创建并推送 `v*` 注释标签，默认推送至 `github` 远程。GitHub Actions 的 `.github/workflows/release.yml` 在收到标签后生成 framework-dependent `win-x64` 发行包、ZIP 文件和基于相邻标签提交整理的更新说明，并创建 GitHub Release。

不传 `-Version` 时，脚本会交互式提示输入版本。仅需在本地生成待检查发行包时，先创建对应标签，再执行：

```powershell
.\scripts\new-release-package.ps1 -Version v0.97
```

## Gitee 同步

若需在 GitHub Actions 同步发布至 Gitee，请在 GitHub 仓库 Actions secrets 中配置：

- `GITEE_REPOSITORY`：Gitee 仓库路径，例如 `neko_nya/WuGesture`。
- `GITEE_TOKEN`：拥有仓库写入权限的 Gitee 私人令牌。

两个 secret 都已配置时，工作流会镜像 `main` 和发行标签到 Gitee，并发布相同的附件和更新说明。未配置时，Gitee 同步会被跳过，GitHub Release 不受影响。

首次使用双远程时，可保留 Gitee 为 `origin`，并添加 GitHub 远程：

```powershell
git remote set-url origin git@gitee.com:neko_nya/WuGesture.git
git remote add github git@github.com:illlustionary/WuGesture.git
git push -u github main
git push github --tags
```

更多构建与发行脚本说明见 [项目结构](../PROJECT_STRUCTURE.md)。
