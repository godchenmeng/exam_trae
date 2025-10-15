# ExamSystem.WPF - 在线考试系统 WPF 版本

## 📋 项目概述

这是在线考试系统的 WPF (Windows Presentation Foundation) 版本，提供了完整的桌面应用程序界面。

## 🏗️ 项目结构

```
ExamSystem.WPF/
├── App.xaml                    # 应用程序入口
├── App.xaml.cs                 # 应用程序逻辑和依赖注入配置
├── ExamSystem.WPF.csproj       # 项目文件
├── README.md                   # 项目说明
├── Converters/                 # 数据转换器
│   └── Converters.cs
├── Styles/                     # 样式文件
│   └── CommonStyles.xaml       # 通用样式定义
├── ViewModels/                 # 视图模型 (MVVM模式)
│   ├── ExamViewModel.cs
│   ├── ExamPaperViewModel.cs
│   ├── ExamPaperEditViewModel.cs
│   ├── ExamPreviewViewModel.cs
│   ├── ExamResultViewModel.cs
│   ├── PaperQuestionManageViewModel.cs
│   ├── QuestionBankViewModel.cs
│   ├── QuestionBankEditViewModel.cs
│   └── QuestionEditViewModel.cs
└── Views/                      # 视图文件
    ├── MainWindow.xaml         # 主窗口
    ├── MainWindow.xaml.cs
    ├── ExamView.xaml
    ├── ExamPaperView.xaml
    ├── ExamResultView.xaml
    ├── QuestionBankView.xaml
    └── [其他对话框和视图文件]
```

## 🎯 主要功能

### 1. 题库管理
- 题目的增删改查
- 题目分类和难度管理
- 题目导入导出功能

### 2. 试卷管理
- 试卷创建和编辑
- 题目组卷功能
- 试卷预览和发布

### 3. 考试管理
- 考试进行界面
- 计时功能
- 答题记录

### 4. 考试结果
- 成绩统计
- 答题分析
- 结果导出

## 🛠️ 技术栈

- **.NET 5.0** - 目标框架
- **WPF** - UI框架
- **Material Design in XAML** - UI设计库
- **Entity Framework Core** - 数据访问
- **Microsoft.Extensions.DependencyInjection** - 依赖注入
- **Microsoft.Extensions.Logging** - 日志记录

## 📦 NuGet 包依赖

```xml
<PackageReference Include="MaterialDesignThemes" Version="4.6.1" />
<PackageReference Include="MaterialDesignColors" Version="2.0.7" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="5.0.2" />
<PackageReference Include="Microsoft.Extensions.Logging" Version="5.0.0" />
<PackageReference Include="Microsoft.Extensions.Logging.Console" Version="5.0.0" />
<PackageReference Include="Microsoft.Extensions.Logging.Debug" Version="5.0.0" />
<PackageReference Include="Microsoft.Extensions.Hosting" Version="5.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="5.0.17" />
```

## 🚀 运行要求

### 系统要求
- **Windows 10** 或更高版本
- **.NET 5.0 Runtime** 或 **.NET 5.0 SDK**

### 开发环境
- **Visual Studio 2019/2022** (推荐)
- **Visual Studio Code** + C# 扩展
- **JetBrains Rider**

## 📝 编译和运行

### 在 Windows 上编译
```bash
# 还原 NuGet 包
dotnet restore ExamSystem.WPF/ExamSystem.WPF.csproj

# 编译项目
dotnet build ExamSystem.WPF/ExamSystem.WPF.csproj

# 运行应用程序
dotnet run --project ExamSystem.WPF/ExamSystem.WPF.csproj
```

### 发布应用程序
```bash
# 发布为独立应用程序
dotnet publish ExamSystem.WPF/ExamSystem.WPF.csproj -c Release -r win-x64 --self-contained true

# 发布为框架依赖应用程序
dotnet publish ExamSystem.WPF/ExamSystem.WPF.csproj -c Release -r win-x64 --self-contained false
```

## ⚠️ 重要说明

### macOS/Linux 兼容性
**WPF 是 Windows 专用框架**，无法在 macOS 或 Linux 上运行。如果需要跨平台支持，请使用：
- **ExamSystem.UI** (Avalonia UI) - 已包含在解决方案中
- **.NET MAUI** - 可考虑未来迁移
- **Blazor** - Web 应用程序版本

### 当前状态
- ✅ 项目结构已完成
- ✅ 依赖注入配置已完成
- ✅ 基础 UI 框架已搭建
- ⚠️ 需要在 Windows 环境中进行测试和调试
- ⚠️ 部分 ViewModels 和 Views 需要进一步完善

## 🔧 配置说明

### 数据库配置
应用程序使用 SQLite 数据库，连接字符串在 `App.xaml.cs` 中配置：
```csharp
services.AddDbContext<ExamDbContext>(options =>
    options.UseSqlite("Data Source=exam_system.db"));
```

### 依赖注入
所有服务和 ViewModels 都通过依赖注入容器管理，配置在 `App.xaml.cs` 的 `CreateHostBuilder` 方法中。

## 🎨 UI 设计

### Material Design
使用 Material Design in XAML 库提供现代化的 UI 设计：
- 统一的颜色主题
- 响应式动画效果
- 标准化的控件样式

### 自定义样式
`Styles/CommonStyles.xaml` 包含了应用程序的自定义样式定义，确保 UI 的一致性。

## 📚 开发指南

### MVVM 模式
项目严格遵循 MVVM (Model-View-ViewModel) 设计模式：
- **Model**: Domain 层的实体类
- **View**: XAML 文件定义的用户界面
- **ViewModel**: 业务逻辑和数据绑定

### 添加新功能
1. 在 `ViewModels` 文件夹中创建对应的 ViewModel
2. 在 `Views` 文件夹中创建对应的 View
3. 在 `App.xaml.cs` 中注册新的服务和 ViewModel
4. 更新主窗口的导航逻辑

## 🤝 贡献指南

1. Fork 项目
2. 创建功能分支
3. 提交更改
4. 推送到分支
5. 创建 Pull Request

## 📄 许可证

本项目采用 MIT 许可证 - 查看 [LICENSE](../LICENSE) 文件了解详情。