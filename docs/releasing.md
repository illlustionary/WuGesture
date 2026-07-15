# 发行维护

本文件面向仓库维护者。普通使用者只需下载发行包，并安装 [.NET 10 Desktop Runtime x64](https://dotnet.microsoft.com/download/dotnet/10.0/runtime)。

## 本地发布

```powershell
.\scripts\publish.ps1
```

发布脚本会关闭正在运行的 `WuGesture`、清理发布输出，再执行发布。默认输出目录为 `artifacts\publish\WuGesture`。可使用 `-Version`、`-RuntimeIdentifier` 和 `-SelfContained` 指定版本与目标运行时。

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
