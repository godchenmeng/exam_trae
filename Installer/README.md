# ExamSystem 安装包构建说明（Win10/11，现有架构）

本项目采用 Inno Setup 生成 Windows 安装包，保持当前架构（.NET 8 + WPF + WebView2）。

## 目录结构

```
Installer/
├─ ExamSystem_Win10Plus_InnoSetup.iss   # Inno Setup 安装脚本
└─ Prerequisites/                        # 预安装依赖（手动放置）
   ├─ MicrosoftEdgeWebview2Setup.exe     # WebView2 Evergreen Bootstrapper（必需）
   └─ dotnet-runtime-8.0.x-win-x64.exe   # .NET 8 Desktop Runtime（可选，若非自包含）
```

## 构建流程

1. 发布应用（建议非自包含，保持较小体积）：
   - PowerShell 执行：
     ```powershell
     dotnet publish .\ExamSystem.WPF\ExamSystem.WPF.csproj -c Release -r win-x64 --self-contained false /p:PublishReadyToRun=true /p:PublishSingleFile=false
     ```
   - 生成目录：`ExamSystem.WPF\bin\Release\net8.0-windows10.0.19041\publish\`

   如需自包含（无需 .NET 运行时），改为：
   ```powershell
   dotnet publish .\ExamSystem.WPF\ExamSystem.WPF.csproj -c Release -r win-x64 --self-contained true /p:PublishReadyToRun=true /p:PublishTrimmed=false
   ```

2. 准备依赖：
   - WebView2 Evergreen Bootstrapper（必需，Win10/11）：
     - 从微软官网搜索并下载 “WebView2 Evergreen Bootstrapper” 安装包（文件名一般为 `MicrosoftEdgeWebview2Setup.exe`）。
     - 放入 `Installer\Prerequisites\MicrosoftEdgeWebview2Setup.exe`。
   - （可选）.NET 8 Windows Desktop Runtime：
     - 若第一步选择非自包含发布，建议加入运行时链式安装。
     - 从微软官网下载 `.NET 8 Desktop Runtime for Windows x64` 安装包，命名为 `dotnet-runtime-8.0.x-win-x64.exe`（与脚本一致）。
     - 放入 `Installer\Prerequisites\dotnet-runtime-8.0.x-win-x64.exe`。

3. 安装 Inno Setup：
   - 安装 Inno Setup（https://jrsoftware.org/）。
   - 用 Inno Setup 打开 `Installer\ExamSystem_Win10Plus_InnoSetup.iss`，点击 “Compile”。
   - 生成的安装包输出在默认输出目录（脚本中为 `{#AppName}_Setup.exe`）。

## 脚本行为说明

- 拷贝发布目录到安装目录：`{pf}\ExamSystem`
- 创建开始菜单和桌面快捷方式
- 检测 WebView2 是否安装（查询注册表 `SOFTWARE\Microsoft\EdgeWebView`），未安装则静默运行 `MicrosoftEdgeWebview2Setup.exe`
- （可选）检测 .NET 8 Windows Desktop Runtime 是否安装（查询注册表 `SOFTWARE\dotnet\Setup\InstalledVersions\...\Microsoft.WindowsDesktop.App\8.0`），未安装则静默运行 `dotnet-runtime-8.0.x-win-x64.exe`
- 为 `{app}\Logs` 和 `{app}\Backups` 目录授予 Users 写权限，确保应用能写日志和备份（根据现有代码路径）

## 测试建议（干净环境/虚拟机）

1. 首次安装：确认安装过程顺利，且依赖自动安装（WebView2 / .NET 8）
2. 首次运行：应用能正常启动，界面加载无报错
3. WebView2 功能测试：地图页面/内嵌网页能正常显示（验证 WebView2 可用）
4. 写权限测试：执行会生成日志和备份的操作，确认 `{app}\Logs`、`{app}\Backups` 有文件产生
5. 卸载回滚：卸载后，快捷方式、程序文件被正确移除；（如有用户数据目录需求，可在卸载前提示保留）

## 常见问题

- 安装包执行 WebView2 安装失败：
  - 确认 `Installer\Prerequisites\MicrosoftEdgeWebview2Setup.exe` 存在且可执行
  - 若目标机器网络受限，使用离线安装包替代（可在微软官网获取离线版本）

- 目标机器没有 .NET 8 运行时且选择了非自包含发布：
  - 加入 `dotnet-runtime-8.0.x-win-x64.exe` 至 `Prerequisites` 并重新编译安装包
  - 或改为自包含发布以避免运行时依赖

- 程序写日志失败（权限问题）：
  - 已为 `{app}\Logs`、`{app}\Backups` 目录设置 Users 写权限；如果还有其他需写入目录，请在脚本的 `[Dirs]` 段添加并设置权限。

## 版本号与签名（可选）

- 版本号：更新 `ExamSystem_Win10Plus_InnoSetup.iss` 中 `AppVersion`
- 代码签名：可在 Inno Setup 中配置 `SignTool`，对安装包和主程序进行签名，提升可信度与防篡改能力

---

如需适配 Windows 7，请参照此前“双栈客户端（net48 + net8）”方案评估与实施；当前安装包脚本仅面向 Win10/11。