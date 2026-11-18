# 全项目审查问题清单

- 审查日期：2026-11-17
- 范围：`src\WuGesture.App`（宿主 / 手势引擎 / 配置 / 覆盖层 / 更新器）、`src\WuGesture.App\Web`（Vue3 + Vite）、`tests`、`scripts`、`.github`
- 基线：`dotnet build` 0 警告 0 错误；后端 304 个测试通过；前端 `typecheck` / Prettier / 502 个测试通过；依赖无已知漏洞
- 说明：本文件是修复跟踪表。每项修复后更新“状态”，并同步 `PROJECT_STRUCTURE.md` 中受影响的行为描述。

状态取值：

- `待修`：已确认，尚未处理
- `进行中`：正在修复
- `已修复`：代码与测试已更新，并通过 Debug 构建
- `需决策`：方案涉及产品行为，需要用户确认
- `需实测`：依赖具体机器/环境，先验证再决定
- `已排除`：复核后不成立

---

## 高优先级

### R-01 手势解析线程一次异常后永久停摆

- 状态：`已修复`
- 位置：`src\WuGesture.App\GestureEngine\GestureParserWorker.cs:107-137`
- 问题：整个消费循环被一个 try/catch 包住，任何一次 `action()` 抛异常都会结束线程并把 `stopRequested` 置为 `true`，之后所有 `Enqueue*` 静默丢弃。`GestureService` 中 `StartTracking` / `ProcessMove` / `FinishTracking` 均无兜底，一次意外异常会让全部手势和配置触发的识别失效，只能重启进程。
- 处理：在循环内对单个 action 做 try/catch 记录后继续；线程意外退出后由下一次入队自动重启，不再把“线程已退出”和“正在关闭”混为同一状态。
- 验证：新增单元测试覆盖“前一个 action 抛异常，后续 action 仍执行”。

### R-02 配置加载失败会导致进程静默退出

- 状态：`已修复`
- 位置：`src\WuGesture.App\Application\Program.cs:48-127`、`src\WuGesture.App\Configuration\GestureConfigStore.cs:30-125,210-280`、`src\WuGesture.App\Application\MainForm.cs:127,151-153,206-224`
- 问题：未来版本配置会主动抛异常，备份失败（`File.Copy`）也会抛；异常发生在 `Application.Run` 之前的 `MainForm` 构造阶段，`Main` 只有 `try/finally`，用户双击后没有任何提示，只在 JSONL 日志留痕。
- 处理（按原因区分）：
  - 未来版本、配置文件不可读、配置目录无法创建：抛出 `GestureConfigLoadException`，`Program` 捕获后弹窗说明原因并退出，原文件保持不变。
  - 备份失败：不再覆盖原文件，记录 `backup-failed` 并以内存配置继续运行，窗口显示时弹出警告（后台启动只写日志）。
  - 备份成功但写盘失败：保留备份文件并提示，继续使用内存配置。

### R-03 关闭手势编辑器会把规则改成“永不匹配”

- 状态：`已修复`
- 位置：`Web\src\gestureEditor\modules\useGestureRuleEditor.js:60-65,95,232,311-319`
- 问题：编辑现有规则时点“录制”会清空 `patternText`，随后 Esc / 点遮罩关闭走 `closeGestureEditor`，在 edit 模式下带 `allowIncomplete` 强制提交，生成 `pattern=[]` 且无 `wheelModifier` 的规则；后端 `GestureConfigMapper.cs:64` 会丢弃该规则，界面仍保留，重启后手势静默失效。
- 处理：空手势在任何提交路径都拒绝（`allowIncomplete` 不再放行）；开始录制时保留原方向路径；录制未识别到有效手势时不覆盖原路径。
- 验证：前端 `typecheck` + Vitest 全量回归。

### R-04 更新包 SHA256 校验可被静默跳过

- 状态：`已修复`
- 位置：`src\WuGesture.App\Updater\UpdateService.cs:621-670`
- 问题：发行说明缺少哈希时只记一条 `checksum-missing` 就返回；哈希计算过程中的非 `InvalidDataException` 异常也被吞掉。随后包会被解压并执行。
- 处理（直接失败）：缺少哈希、哈希不匹配或校验过程异常都会删除已下载的包并抛出 `InvalidDataException`，更新流程向用户报告失败，不再继续安装。

---

## 中优先级

### R-05 更新检查提前结束，不比较两个更新源

- 状态：`已修复`
- 位置：`src\WuGesture.App\Updater\UpdateService.cs:80-100`
- 问题：首选源已经比当前版本新时直接 `break`，不会查询另一个源，与 `PROJECT_STRUCTURE.md` 的“两个源取较高版本”不符。
- 处理：删除提前 `break`，两个源都查询后取最高版本，“仅接受高于当前版本”的判断保留。

### R-06 取消更新后仍可能继续替换文件

- 状态：`已修复`
- 位置：`src\WuGesture.App\Application\UpdateCoordinator.cs:109-150`
- 问题：`StartApply` 不接收取消标记；取消发生在准备完成之后仍会退出并替换。`CancellationTokenSource.Dispose` 可能早于后台任务读取 `Token`，触发 `ObjectDisposedException` 并被当成“更新失败”。
- 处理：后台任务在启动时捕获 token，取消过滤器改用 `token.IsCancellationRequested`；准备完成后、替换安装目录前再次 `ThrowIfCancellationRequested`。

### R-07 音频设备可用性被永久熔断

- 状态：`已修复`
- 位置：`src\WuGesture.App\GestureEngine\AudioController.cs:104-160`
- 问题：蓝牙耳机断开、音频服务未就绪等瞬时失败会把静态 `available` 永久置 `false`，之后音量动作全部退化为媒体键、OSD 不再显示，只能重启恢复。
- 处理：删除永久布尔熔断，改为失败后 2 秒内不再重试（`retryAfterTick`），时间窗过后自动重新获取默认设备并激活 COM 接口。

### R-08 边缘滚轮与手势滚轮可能同时触发；无法处理时滚轮被吞掉

- 状态：`已修复`
- 位置：`src\WuGesture.App\GestureEngine\EdgeActionService.cs:242-260`、`src\WuGesture.App\GestureEngine\GestureService.cs:418-438`
- 问题：任意鼠标键按下时没有关闭边缘滚轮判定；手势被排除项/全屏拦截提前返回时没有复位 `wheelCaptureAllowed`，按住手势键滚动会丢失输入。
- 处理：鼠标键按住分支同时置 `wheelActionsBlocked = true`，边缘滚轮不再与手势滚轮同时触发；`StartTracking` 因排除项/全屏提前返回时调用 `SetWheelCaptureAllowed(false)`，把滚轮交还给目标程序。

### R-09 快捷键录制 hook 常驻 UI 线程

- 状态：`已修复`
- 位置：`src\WuGesture.App\GestureEngine\KeyboardShortcutRecorder.cs:48-70`
- 问题：低级键盘 hook 在 UI 线程安装，`Stop()` 只清状态不卸载，录制过一次后每次按键都经过 UI 线程；UI 卡顿会拖慢全局键盘，超时后 hook 被系统摘除。
- 处理：`Stop()` 立即 `UnhookWindowsHookEx` 并清空句柄；录制结束、取消、释放都会卸载，下次 `Start()` 再安装。

### R-10 运行时动作失败对用户不可见

- 状态：`已修复`
- 位置：`src\WuGesture.App\Overlay\GestureFeedbackCoordinator.cs:145-157`、`src\WuGesture.App\Application\MainForm.cs:531-553`、`Web\src\constants\gestureEditorOptions.js:132-134`、`Web\src\gestureEditor\context\gestureEditorContext.js:421-433`、`Web\src\utils\gestureEditorRuntimeMessages.js`
- 问题：宿主发送 `gesture-action-failed` / `edge-action-failed`，前端没有对应常量和处理分支，消息被静默丢弃。
- 处理：补齐 `WEBVIEW_MESSAGE_TYPES.gestureActionFailed` / `edgeActionFailed` 与 `handleMessage` 分支，用 Snackbar 显示动作名和错误；同一文案 5 秒内只提示一次（`createMessageDeduplicator`）。
- 验证：新增 `gestureEditorRuntimeMessages.test.js` 覆盖文案拼接和去重窗口。

### R-11 首帧主题与宿主默认值不一致

- 状态：`已修复`（浅色主题下的首帧表现建议人工确认）
- 位置：`Web\src\composables\useAppearanceTheme.ts:143-153`、`Web\src\constants\gestureEditorUiDefaults.js:14`、`Web\src\theme\applyScheme.ts:22-105`、`src\WuGesture.App\Configuration\GestureUiSettings.cs:30`
- 问题：前端默认 `dark`，后端默认 `system`；挂载时立即写入 `data-theme`，会覆盖宿主导航前注入的主题，浅色配置 + 深色系统可能出现黑闪。
- 处理：
  - 前端默认主题对齐为 `system`，未知值回退也改为 `system`。
  - 宿主配置到达前不按默认值写入 `data-theme`；浏览器预览在初始化时把 `hostConfigLoaded` 置为 `true`，保持原有预览行为。
  - 色板缓存旁新增明暗模式缓存，挂载前先套用上次模式，深色配置首屏不再闪白。

### R-12 WebDAV 路径语义与凭据安全

- 状态：`已修复`
- 位置：`src\WuGesture.App\Configuration\WebDavConfigSyncService.cs:75-110`、`src\WuGesture.App\Application\ConfigurationCoordinator.cs:86-113,236-266`、`Web\src\pages\settings\components\WebDavSettings.vue:19-22,100-110`
- 问题：用 `Path.HasExtension` 判断目录/文件，`backup.v1` 这类目录名会被当成文件名；允许明文 `http://` + Basic 认证；口令明文保存在配置中并随“导出配置”一起写出。
- 处理（强提示 + 可选剥离）：
  - 只有以 `.json` 结尾的远程路径才按文件名处理，其余一律按目录追加 `gestures.json`，并拒绝包含 `..` 的路径。
  - 设置页在地址为 `http://` 时显示明文传输警告，但仍允许保存（兼容内网/自建服务）。
  - “导出配置”默认写出口令为空字符串的副本，本地配置和 WebDAV 备份仍保留口令。

### R-13 前端 `typecheck` 不覆盖多数 `.js` 模块

- 状态：`进行中`（阶段一已完成）
- 位置：`Web\tsconfig.json:8-9,23`、`Web\tsconfig.checkjs.json`、`Web\package.json:11`
- 问题：`allowJs: true` + `checkJs: false`，`include` 只含 `.ts/.vue`；`gestureEditor`、`utils`、`constants` 下大量 `.js` 不受类型检查。
- 处理（分阶段开启）：
  - 新增 `tsconfig.checkjs.json`，继承主配置并开启 `checkJs`；第一阶段只包含 `src\constants` 与 `src\utils` 的 `.js`，为保证存量代码可落地先关闭 `noImplicitAny`。
  - `pnpm typecheck` 依次运行两份配置，CI 无需改动。
  - 已修掉阶段一暴露的唯一真实类型问题（导出配置时剥掉排除项 `icon` 的赋值）。
  - 后续阶段把 `src\gestureEditor`、`src\composables` 等目录加入 `include` 并逐步补类型。

### R-14 覆盖率下限偏低、只校验最后一份报告

- 状态：`已修复`
- 位置：`scripts\check-coverage.ps1`、`.github\workflows\test.yml:56-58`
- 现状：下限 21% / 19%，实测 24.1% / 22.9%；脚本只取最后一份 cobertura。
- 处理：默认下限上调为 23% / 21%，工作流显式传参；脚本改为比较最新一批（10 分钟窗口）内的全部 `coverage.cobertura.xml`，同批次每个测试工程都必须达到下限，既不会只看最后一份，也不会被历史遗留报告干扰。本地实测 24.36% / 23.16%，通过。

### R-15 更新脚本用固定 3 秒判断新版本是否启动

- 状态：`已修复`
- 位置：`src\WuGesture.App\Updater\apply-update.ps1:118-160`
- 问题：慢机器/杀软扫描下新进程可能 3 秒后才出现，会被判失败并回滚；用户手动启动的实例也会被误判为成功。
- 处理：用 `Start-Process -PassThru` 的进程对象优先确认启动的 PID 仍存活；提权重启导致该进程退出时，再回退到“本次更新之后启动的 WuGesture 实例”。轮询延长到 30 秒，每 500ms 检查一次，不再用固定 3 秒判定。

---

## 低优先级 / 健壮性

### R-16 删除最后一条规则与设置保存共用空规则守卫

- 状态：`已修复`
- 位置：`Web\src\gestureEditor\modules\useGestureRuleEditor.js:178-189`
- 问题：删除最后一条规则时本地已移除但请求未发送，宿主回显后“复活”；规则为空时所有 UI 设置都无法保存。
- 处理：`removeRule` 在只剩一条规则时直接提示并拒绝删除；规则非空因此成为不变量，`saveRules` 的空规则守卫不会再挡住设置保存。

### R-17 应用选择/窗口拾取取消后无回执

- 状态：`已修复`
- 位置：`Web\src\gestureEditor\modules\useGestureEditorApplicationPicker.js:28-42`、`src\WuGesture.App\Application\ApplicationPickerCoordinator.cs:31-100,142-160`
- 问题：取消后宿主不回消息，前端 `pendingRequestIds` 永久残留；`category` 被硬编码为空串。
- 处理：文件选择和窗口拾取被取消（含拾取失败）时回传同一 `requestId` 的空 `application-selected`，前端清理待处理集合；`category` 改为发送当前选择器的分类上下文。

### R-18 初始化与托盘恢复缺乏异常兜底

- 状态：`已修复`
- 位置：`src\WuGesture.App\Application\MainForm.cs:126-262`
- 问题：`async void OnLoad` 中任一 `Start()` / `EnsureWebViewAsync()` 抛异常会跳过后续初始化，应用处于“进程在但服务没起来”的半初始化状态。
- 处理：`OnLoad` 改为只调用 `InitializeServicesAsync()` 并捕获全部异常；失败时记录日志、停止已启动的服务，前台启动弹出“启动服务失败，请查看日志或重启”，后台启动只留日志和托盘。

### R-19 主题颜色任一字段缺失就整段不应用

- 状态：`已修复`
- 位置：`src\WuGesture.App\Application\MainForm.Windowing.cs:34-66`
- 问题：标题栏或文字色任一解析失败就整体 `return`，WebView 首帧底色也保持白色。
- 处理：标题栏底色和文字色各自回退到与前端默认值一致的浅/深色值，始终应用窗口底色、WebView 首帧底色和标题栏主题。

### R-20 `rules` payload 在 UI 线程逐条查文件/解图标

- 状态：`已修复`
- 位置：`src\WuGesture.App\Application\WebViewRulesPayloadFactory.cs:8-105`
- 问题：每次保存/重载都同步 `File.Exists` 和图标解码，程序多或网络盘时会卡顿。
- 处理：程序存在性按路径缓存 3 秒（最多 256 项），文件不存在时直接返回空图标、跳过解码；图标本身仍由 `ApplicationIconDataUrl` 按文件时间戳缓存。

### R-21 `Process` 对象未释放

- 状态：`已修复`
- 位置：`GestureEngine\ActionExecutor.cs:224`、`Application\ApplicationPickerCoordinator.cs:110`、`Application\WebViewHost.cs:263`、`Application\ElevationLauncher.cs:48`、`Application\UpdateCoordinator.cs:196`
- 处理：所有 `Process.Start` 返回的对象统一 `using` 释放（不影响被启动的进程）。

### R-22 退出时 UI 线程串行等待多个后台线程

- 状态：`已修复`
- 位置：`src\WuGesture.App\Application\MainForm.cs:283-347`
- 问题：最坏情况累计约 10 秒无响应。
- 处理：手势服务和边缘服务改为在线程池并行释放，整体预算 3 秒；超时后记录 `dispose-timeout` 并继续关闭，剩余后台线程随进程退出。

### R-23 更新目录清理在 UI 线程且先于开关判断

- 状态：`已修复`
- 位置：`src\WuGesture.App\Application\UpdateCoordinator.cs:53-80`
- 处理：清理改为后台任务（异常单独记录），并先读取 `checkForUpdates` 决定是否继续自动检查。

### R-24 构建输入排除漏掉 `.test.ts`

- 状态：`已修复`
- 位置：`src\WuGesture.App\WuGesture.App.csproj:52`、`scripts\verify-package.ps1:42`
- 问题：修改 `theme/*.test.ts` 也会触发完整 `vite build`；`scripts\verify-package.ps1` 的 `*.test.js` 过滤同样只认 `.js`。
- 处理：前端构建输入统一排除 `*.test.*` / `*.spec.*` 与任意层级的 `*.md`；发布校验同样禁止 `*.test.*` / `*.spec.*` 进入发行包。

### R-25 前端依赖版本未固定

- 状态：`已修复`
- 位置：`.github\workflows\test.yml:27-30`、`.github\workflows\release.yml:38-41`
- 问题：没有固定 pnpm 版本，CI 的 `corepack enable` 使用 Node 内置版本，可能与本机不一致。
- 处理：两个工作流改用 `pnpm/action-setup@v4` 并显式指定 `version: 12.18.0`。
  - 尝试过在 `package.json` 写 `"packageManager": "pnpm@12.18.0"`，但本机 pnpm 12.18.0 会把托管的 pnpm 依赖写进 lockfile，产生重复的 `lockfileVersion` 文档；为保持 lockfile 干净，改用工作流锁定版本。

### R-26 测试夹具与真实前端 payload 脱节

- 状态：`进行中`（强调色注入已完成）
- 位置：`tests\WuGesture.App.Tests\WebViewRulesPayloadFactoryTests.cs`
- 问题：手写 JSON，字段改名不会触发失败；`WebViewRulesPayloadFactoryTests.cs:194` 直接断言本机 Windows 强调色，结果随机器变化。
- 处理：`WebViewRulesPayloadFactory.Create` 增加可选的 `systemAccent` 参数，测试注入固定值，不再依赖本机强调色。由前端生成 payload fixture 的工作量较大（需要新增脚本与 CI 步骤），暂缓。

### R-27 本地开发版本号与最新标签漂移

- 状态：`已排除`（按用户决定暂不处理）
- 位置：`src\WuGesture.App\WuGesture.App.csproj:18`
- 现状：`1.1.11`，最新标签 `v1.7.0`。发布脚本会覆盖版本，但本地 Debug 构建的“检查更新”会持续提示 1.7.0。
- 决定：保持现状，发布版本仍由发布脚本覆盖；如后续希望在本地消除提示，可改用标签推导版本。

### R-28 文档与实现的小偏差

- 状态：`已修复`
- 位置：`src\WuGesture.App\Web\PROJECT_STRUCTURE.md`、`src\WuGesture.App\Web\agents.md`
- 问题：Web 消息清单缺 `open-application-folder`；`typecheck` 覆盖范围描述偏乐观。
- 处理：补齐 `open-application-folder` 与取消回执说明；明确 `typecheck` 由两份配置组成（`.ts`/`.vue` 与 `constants`/`utils` 的 `.js`）。

---

## 复核后降级或排除的项

### X-01 更新脚本跨卷 `Move-Item` “必然失败”

- 状态：`已排除`
- 结论：用应用实际使用的 `powershell.exe` 5.1 在 C:→D: 实测可成功移动目录，不是必然失败。
- 仍保留的风险：跨卷移动本质是复制+删除，不是原子操作；中途失败会留下不完整安装目录。已在 R-15 的回滚逻辑中一并考虑，暂不单独列修复项。

### X-02 “覆盖率门槛形同虚设”

- 状态：`已排除`
- 结论：下限与现状只差约 3 个百分点，属于偏低而非无效；并入 R-14 处理。

---

## 修复进度

- 2026-11-17（第一批）：R-01、R-02、R-03、R-04、R-12 已修复。
- 2026-11-17（第二批）：R-05、R-06、R-07、R-08、R-09、R-10、R-11 已修复。
- 2026-11-17（第三批）：R-15、R-16、R-17、R-18、R-19、R-20、R-21、R-22、R-23、R-24、R-25、R-28 已修复；R-14 已修复；R-13 阶段一完成；R-26 完成强调色注入；R-27 按决定不处理。
- 当前验证：Debug 构建 0 警告；后端 307 个测试通过；前端 `typecheck`（含新增 JS 检查）/ Prettier / 505 个测试通过（8 个跳过）；覆盖率脚本按 23% / 21% 实测通过（24.36% / 23.16%）。
- 决策记录：R-02 采用“按原因区分”；R-04 采用“直接失败”；R-12 采用“强提示 + 导出剥离口令”；R-13 采用“分阶段开启 checkJs”；R-14 上调到 23% / 21%；R-27 暂不处理。
- 待人工确认：浅色主题下的首帧是否仍有闪烁（R-11）。
- 后续可选：R-13 下一阶段（`gestureEditor` / `composables` 的 `.js`）；R-26 的前端生成 fixture。

## 建议修复顺序

1. R-01、R-03（已完成）
2. R-02、R-04、R-12（已完成）
3. R-05、R-06、R-07、R-08、R-09、R-10、R-11（已完成）
4. R-15、R-13（阶段一）、R-14（已完成）
5. R-16 ~ R-28（R-26 部分完成、R-27 不处理）
