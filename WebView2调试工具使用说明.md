# WebView2 开发者工具使用说明

## 概述
已在 `GradeAnswerDialog.xaml.cs` 中添加了 WebView2 开发者工具的自动启动功能，用于调试学生答案地图展示页面。

## 功能特性

### 1. 自动启动条件
开发者工具会在以下情况下自动打开：

#### Debug 模式（推荐）
- 在 Visual Studio 中以 Debug 配置运行项目时自动启用
- 无需额外配置，开箱即用

#### 环境变量控制
- 设置环境变量 `WEBVIEW_DEVTOOLS=true` 可在任何模式下启用
- 适用于生产环境调试或特殊情况

### 2. 使用方法

#### 方法一：Debug 模式运行
```bash
# 在 Visual Studio 中选择 Debug 配置并运行
# 或使用命令行
dotnet run --configuration Debug
```

#### 方法二：环境变量设置
```powershell
# PowerShell 中设置环境变量
$env:WEBVIEW_DEVTOOLS = "true"
dotnet run

# 或者在系统环境变量中永久设置
# 变量名：WEBVIEW_DEVTOOLS
# 变量值：true
```

### 3. 调试流程

1. **启动应用**：运行 ExamSystem.WPF 项目
2. **打开评分对话框**：选择一个包含地图绘制题的考试记录进行评分
3. **自动弹出开发者工具**：当 WebView2 初始化完成后，开发者工具窗口会自动打开
4. **开始调试**：
   - 查看 Console 面板的 JavaScript 日志
   - 检查 Network 面板的资源加载情况
   - 使用 Elements 面板检查 DOM 结构
   - 在 Sources 面板设置断点调试

### 4. 调试要点

#### 地图相关调试
- 检查百度地图 API 是否正确加载
- 验证地图数据解析是否正确
- 查看覆盖物（标记、线条、多边形等）的渲染状态

#### 网络请求调试
- 监控 HTTP 服务器的资源请求
- 检查 CSS、JS 文件的加载状态
- 验证地图瓦片和 API 调用

#### JavaScript 错误排查
- 查看 Console 面板的错误信息
- 检查变量值和函数调用
- 验证事件处理和回调函数

### 5. 代码实现位置

```csharp
// 文件：GradeAnswerDialog.xaml.cs
// 方法：MapWebView_CoreWebView2InitializationCompleted

private void MapWebView_CoreWebView2InitializationCompleted(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs e)
{
    // Debug 模式自动启用
    #if DEBUG
    MapWebView.CoreWebView2.OpenDevToolsWindow();
    #endif
    
    // 环境变量控制
    var enableDevTools = Environment.GetEnvironmentVariable("WEBVIEW_DEVTOOLS");
    if (!string.IsNullOrEmpty(enableDevTools) && enableDevTools.ToLower() == "true")
    {
        MapWebView.CoreWebView2.OpenDevToolsWindow();
    }
}
```

### 6. 注意事项

- 开发者工具仅在需要时启用，避免影响正常使用
- Debug 模式下会自动启用，Release 模式下默认关闭
- 可通过环境变量在任何模式下强制启用
- 开发者工具窗口可能会在主窗口后面，注意查看任务栏

### 7. 故障排除

#### 开发者工具未打开
1. 确认是否在 Debug 模式下运行
2. 检查环境变量设置是否正确
3. 查看 Debug 输出窗口的日志信息

#### WebView2 初始化失败
1. 确认 WebView2 Runtime 已安装
2. 检查网络连接是否正常
3. 查看异常信息进行具体分析

## 相关文件
- `GradeAnswerDialog.xaml.cs` - 主要实现文件
- `review.html` - 学生答案展示页面
- `review.js` - 地图展示逻辑
- `review.css` - 页面样式

## 更新日志
- 2024-11-03：添加 WebView2 开发者工具自动启动功能
- 支持 Debug 模式自动启用和环境变量控制