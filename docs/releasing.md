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

GitHub Actions 的 `.github/workflows/release.yml` 在收到标签后生成 framework-dependent `win-x64` 发行包、ZIP 文件和基于相邻标签提交整理的更新说明，并创建 GitHub Release。ZIP 内的根目录固定为 `WuGesture`，而 ZIP 文件名保留版本号，例如 `WuGesture-v0.97-windows-x64.zip`。更新说明末尾会以 `SHA256 (WuGesture-v0.97-windows-x64.zip): <哈希>` 的格式记录 ZIP 的 SHA256，应用内更新下载完成后直接读取发行说明（Release 描述）里的这行校验，不再附加或下载单独的校验文件；重新运行工作流覆盖 ZIP 时也会同步刷新更新说明。仅需在本地生成待检查发行包时，先创建对应标签，再执行：

```powershell
.\scripts\package-release.ps1 -Version v0.97
```

该命令使用 `artifacts\release\` 作为唯一输出根目录，其中包含 `WuGesture\`、版本化 ZIP 和 `RELEASE_NOTES.md`；SHA256 也会打印到控制台，方便手工核对。通用发布命令单独使用 `artifacts\publish\WuGesture\`。

打包完成后，`package-release.ps1` 会自动调用 `scripts\verify-package.ps1` 校验发行包：必需文件（`WuGesture.exe`、`WuGesture.dll`、`Web\index.html`、`WebView2Loader.dll`、WebView2 程序集等）齐全，`Web\index.html` 引用的哈希资源都存在，没有混入 `node_modules` 或 `*.test.js`，并且 `WuGesture.exe` 的文件版本与标签版本一致。任一检查失败都不会继续生成更新说明或发布；需要单独复查已有 ZIP 时，可手动执行：

```powershell
.\scripts\verify-package.ps1 -ArchivePath artifacts\release\WuGesture-v0.97-windows-x64.zip -Version v0.97
```

### 发行门禁

`release.yml` 会先运行共享测试流程 `test.yml`（前端 Prettier 格式检查、前端 Vitest 测试、`dotnet build -warnaserror`、`dotnet format --verify-no-changes`、带覆盖率的单元测试，以及 `scripts\check-coverage.ps1` 的覆盖率下限校验），只有全部通过才会打包和发布。本地提交前可用同样的命令自查：

```powershell
dotnet format WuGesture.slnx                 # 自动修复 C# 格式
dotnet format WuGesture.slnx --verify-no-changes
dotnet test WuGesture.slnx --collect:"XPlat Code Coverage" --results-directory artifacts\test-results
.\scripts\check-coverage.ps1
```

覆盖率下限写在 `scripts\check-coverage.ps1` 里（行 21%、分支 19%），补测试后应同步上调，避免门禁随代码增长而失效。标签指向的提交如果编译失败或测试不通过，GitHub 和 Gitee 都不会产生新的发行版；此时需要修复代码后删除并重建标签（远程名按你的配置替换，本仓库的 GitHub 远程是 `origin`）：

```powershell
git tag -d v0.97
git push origin :refs/tags/v0.97
```

日常提交由 `ci.yml` 在推送 `main` 和提交 PR 时运行同一套流程，因此正常情况下标签流水线的测试不会失败；它主要用于拦住“给历史提交打标签”这类操作。

## 自动更新

应用启动时默认检查一次最新发行版（开关在“设置 > 更新”，检查入口在侧栏最外层的“关于&更新”页），默认更新源为 Gitee，也可切换为 GitHub。检查会同时考虑两个源并取版本号较高者，任一源落后或暂时没有发行版都不会导致回退安装，只有更新包版本高于当前版本时才会提示。发现新版本后，用户在弹窗中确认才会下载。下载的 ZIP 在 `%LocalAppData%\WuGesture\updates\<版本>` 解压，随后启动独立的 PowerShell 辅助脚本：

1. 等待当前 `WuGesture` 进程退出。
2. 把现有安装目录移动到同级的 `WuGesture.old-<时间戳>` 备份目录。
3. 把解压后的新版本移动到原安装目录并重启应用（保留原启动参数并附加 `--updated`）；新版本会强制显示配置窗口，并自动打开“关于&更新”页提示已更新到的版本，用户不需要自己从托盘找入口。
4. 启动成功且旧版本退出后删除备份；任一替换步骤失败会回滚到备份。

安装目录不可写时会先弹出 UAC 请求管理员权限；用户取消则更新中止。替换过程写入 `%LocalAppData%\WuGesture\logs\update-*.log`，应用日志保留 7 天的同类文件可供排查。因为更新是按完整发行包替换，发布 ZIP 必须保持 `WuGesture\` 根目录和完整的 `Web\` 目录。

## Gitee 同步

若需在 GitHub Actions 同步发布至 Gitee，请在 GitHub 仓库 Actions secrets 中配置：

- `GITEE_REPOSITORY`：Gitee 仓库路径，例如 `neko_nya/WuGesture`。
- `GITEE_TOKEN`：拥有仓库写入权限的 Gitee 私人令牌。

两个 secret 都已配置时，工作流会镜像 `main` 和发行标签到 Gitee，并发布相同的 ZIP 附件和更新说明（含 SHA256 校验行）。未配置时，Gitee 同步会被跳过，GitHub Release 不受影响。

首次使用双远程时，可保留 Gitee 为 `origin`，并添加 GitHub 远程：

```powershell
git remote set-url origin git@gitee.com:neko_nya/WuGesture.git
git remote add github git@github.com:illlustionary/WuGesture.git
git push -u github main
git push github --tags
```

更多构建与发行脚本说明见 [项目结构](../PROJECT_STRUCTURE.md)。
