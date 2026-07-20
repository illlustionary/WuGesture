# WuGesture

基于 C# WinForms、WebView2 和原生全局鼠标钩子的 Windows 鼠标手势应用。

WuGesture 支持右键和中键的 8 方向鼠标手势，可按全局、分类和应用程序作用域配置规则；程序可关联多个分类，分类同手势按关联顺序由后者覆盖前者，程序规则优先于分类和全局规则。动作支持快捷键、窗口控制、音量和亮度调节，并提供边缘操作、应用排除、托盘驻留和 WebDAV 配置备份。

配置文件位于：`%AppData%\WuGesture\gestures.json`。

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

默认发布入口：

```text
artifacts\publish\WuGesture\WuGesture.exe
```

发布包为 framework-dependent `win-x64` 版本。请先安装 [.NET 10 Desktop Runtime x64](https://dotnet.microsoft.com/download/dotnet/10.0/runtime)，再完整解压 ZIP 并运行 `WuGesture.exe`。缺少 .NET 时由 .NET Host 显示系统安装提示；缺少 [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/) 时，应用会显示下载引导。

## 文档

- [架构说明](docs/architecture.md)：运行时手势流水线、配置作用域与 WebView2 集成。
- [发行维护](docs/releasing.md)：本地发布、自动发行和 GitHub/Gitee 同步。
- [路线图](ROADMAP.md)：后续计划与已知工作重点。
- [项目结构](PROJECT_STRUCTURE.md)：模块职责、配置契约和完整构建发布地图。
