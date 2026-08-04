# 发行维护

本文件面向仓库维护者。普通使用者只需安装 [.NET 10 Desktop Runtime x64](https://dotnet.microsoft.com/download/dotnet/10.0/runtime)，完整解压发行包并运行根目录的 `WuGesture.exe`。发行包保持 framework-dependent，不内置 .NET；缺少 .NET 时由 .NET Host 显示系统安装提示。应用在 .NET 已可启动后会检测 [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/)，缺少时显示下载引导。

## 本地发布

```powershell
.\scripts\publish-app.ps1
```

通用发布脚本会关闭正在运行的 `WuGesture`、清理发布输出，再将依赖框架的桌面应用直接发布为用户入口 `WuGesture.exe`。默认输出目录为 `artifacts\publish\WuGesture`。可使用 `-Version`、`-RuntimeIdentifier` 和 `-SelfContained` 指定版本与目标运行时；自动发行始终生成 framework-dependent `win-x64` 包。

## Debug 打包

直接执行：

```powershell
.\scripts\package-debug.ps1
```

脚本会生成 `artifacts\debug\WuGesture\` 和 `artifacts\debug\WuGesture-debug-windows-x64.zip`，不需要版本号或 Git 标签。构建和发布阶段共用该应用目录，不会额外留下中间输出目录。

## 自动发行

将 `v*` 标签上传到 GitHub 后，GitHub Actions 会自动发行：

```powershell
git push github v0.97
```

GitHub Actions 的 `.github/workflows/release.yml` 在收到标签后生成 framework-dependent `win-x64` 发行包、ZIP 文件和基于相邻标签提交整理的更新说明，并创建 GitHub Release。ZIP 内的根目录固定为 `WuGesture`，而 ZIP 文件名保留版本号，例如 `WuGesture-v0.97-windows-x64.zip`。仅需在本地生成待检查发行包时，先创建对应标签，再执行：

```powershell
.\scripts\package-release.ps1 -Version v0.97
```

该命令使用 `artifacts\release\` 作为唯一输出根目录，其中包含 `WuGesture\`、版本化 ZIP 和 `RELEASE_NOTES.md`。通用发布命令单独使用 `artifacts\publish\WuGesture\`。

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
