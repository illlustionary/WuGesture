# Agent Instructions

1. 每次新会话开始处理本项目之前，先阅读 `PROJECT_STRUCTURE.md`。
2. 每次修改项目后，检查是否需要同步更新 `PROJECT_STRUCTURE.md`。如果改动影响项目结构、启动流程、配置、构建发布、WebView 消息、核心模块职责或测试结构，必须更新。
3. 每次修改完成后，都要先构建一次，再给出最终结果。
4. 每次构建前，先自动查找并关闭正在运行的 `WuGesture`，避免文件锁定。
5. 日常开发与验证默认使用 Debug（`dotnet build WuGesture.slnx -c Debug` / `artifacts\debug\WuGesture`）。`Release` 仅用于发布前验证发行包（`scripts\package-release.ps1`）或排查仅在 Release 出现的问题。
6. 任务完成后不要自动执行 `git commit`。改完代码、构建并验证通过后，只在回复里写好建议的提交信息（标题 + 说明），等用户明确要求提交时再提交。
7. 创建 `v*` 发行标签（或推送标签）之前，必须先在本地跑完与 `.github/workflows/test.yml` 相同的门禁，确认全绿后再打标签：
   ```powershell
   # 在 src\WuGesture.App\Web 下执行
   cmd /c "pnpm format:check"
   cmd /c "pnpm typecheck"
   cmd /c "pnpm test"

   # 在仓库根目录执行
   dotnet build WuGesture.slnx --nologo -warnaserror
   dotnet format WuGesture.slnx --verify-no-changes
   dotnet test WuGesture.slnx --no-build --collect:"XPlat Code Coverage" --results-directory artifacts\test-results
   .\scripts\check-coverage.ps1
   ```
   本机 PowerShell 执行策略禁止运行 `pnpm.ps1`，pnpm 命令一律用 `cmd /c "..."` 调用。标签一旦推送就会触发发行流水线，门禁失败只能删除远程标签再重建，所以不要跳过这一步；只跑 `dotnet build` 不足以发现前端 Prettier、类型和测试问题。
