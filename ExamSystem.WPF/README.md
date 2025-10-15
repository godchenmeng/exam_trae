# ExamSystem.WPF

## 项目概述

ExamSystem.WPF 是在线考试系统的 WPF 桌面客户端版本，提供了完整的考试管理功能，包括题库管理、试卷管理、考试管理和考试结果分析。

## 项目结构

```
ExamSystem.WPF/
├── App.xaml                    # 应用程序入口
├── App.xaml.cs                 # 应用程序代码
├── ExamSystem.WPF.csproj       # 项目文件
├── README.md                   # 项目说明
├── Views/
│   └── MainWindow.xaml         # 主窗口界面
│   └── MainWindow.xaml.cs      # 主窗口代码
├── ViewModels/                 # 视图模型
│   └── ExamViewModel.cs        # 考试视图模型
├── Converters/                 # 值转换器
└── Styles/
    └── CommonStyles.xaml       # 通用样式
```

## 主要功能

### 1. 题库管理 📖
- 题目的增删改查
- 支持多种题型（单选、多选、判断、填空、简答）
- 题目分类和标签管理
- 题目导入导出功能

### 2. 试卷管理 📄
- 试卷创建和编辑
- 自动组卷和手动组卷
- 试卷预览和打印
- 试卷模板管理

### 3. 考试管理 ✏️
- 考试安排和配置
- 实时考试监控
- 考试时间控制
- 防作弊措施

### 4. 考试结果 📊
- 成绩统计和分析
- 答题情况详细报告
- 成绩导出功能
- 图表可视化展示

## 技术栈

- **.NET 5.0** - 目标框架
- **WPF** - 用户界面框架
- **MVVM** - 架构模式
- **Entity Framework Core** - 数据访问
- **SQLite** - 数据库
- **Microsoft.Extensions.DependencyInjection** - 依赖注入
- **Microsoft.Extensions.Logging** - 日志记录
- **Microsoft.Xaml.Behaviors.Wpf** - XAML 行为

## NuGet 依赖包

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="5.0.17" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="5.0.2" />
<PackageReference Include="Microsoft.Extensions.Logging" Version="5.0.0" />
<PackageReference Include="Microsoft.Xaml.Behaviors.Wpf" Version="1.1.31" />
```

## 运行要求

- **操作系统**: Windows 10 或更高版本
- **.NET 运行时**: .NET 5.0 Desktop Runtime
- **内存**: 最少 4GB RAM
- **存储**: 至少 100MB 可用空间

## 编译和发布

### 开发环境编译
```bash
# 还原 NuGet 包
dotnet restore

# 编译项目
dotnet build

# 运行项目
dotnet run
```

### 发布应用程序
```bash
# 发布为独立应用程序
dotnet publish -c Release -r win-x64 --self-contained true

# 发布为框架依赖应用程序
dotnet publish -c Release -r win-x64 --self-contained false
```

## 重要说明

⚠️ **平台限制**: 此项目使用 WPF 框架，仅支持 Windows 平台。在 macOS 或 Linux 系统上无法编译和运行。

如果需要跨平台支持，请考虑使用：
- **Avalonia UI** - 跨平台 .NET UI 框架
- **MAUI** - Microsoft 的跨平台应用程序框架
- **Blazor** - Web 应用程序框架

## 配置说明

### 数据库配置
应用程序使用 SQLite 数据库，数据库文件将在首次运行时自动创建在应用程序目录下。

### 日志配置
应用程序使用 Microsoft.Extensions.Logging 进行日志记录，日志级别可以通过配置文件调整。

## UI 设计

### 设计原则
- **简洁明了**: 界面简洁，操作直观
- **响应式设计**: 适应不同屏幕尺寸
- **一致性**: 统一的视觉风格和交互模式
- **可访问性**: 支持键盘导航和屏幕阅读器

### 颜色方案
- **主色调**: #2196F3 (蓝色)
- **辅助色**: #FF4081 (粉色)
- **背景色**: #F5F5F5 (浅灰)
- **表面色**: #FFFFFF (白色)
- **文本色**: #212121 (深灰)

### 字体
- **主字体**: Microsoft YaHei UI
- **字体大小**: 14px (基础)

## 开发指南

### MVVM 模式
项目采用 MVVM (Model-View-ViewModel) 架构模式：
- **Model**: 数据模型和业务逻辑
- **View**: XAML 界面文件
- **ViewModel**: 视图逻辑和数据绑定

### 依赖注入
使用 Microsoft.Extensions.DependencyInjection 进行依赖注入，所有服务都在 `App.xaml.cs` 中注册。

### 样式管理
所有通用样式定义在 `Styles/CommonStyles.xaml` 中，包括：
- 按钮样式
- 文本框样式
- 数据网格样式
- 颜色资源

## 贡献指南

1. Fork 项目
2. 创建功能分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 创建 Pull Request

## 许可证

本项目采用 MIT 许可证 - 查看 [LICENSE](LICENSE) 文件了解详情。

## 联系方式

如有问题或建议，请通过以下方式联系：
- 创建 Issue
- 发送邮件到项目维护者

---

**注意**: 由于 WPF 的平台限制，此项目仅能在 Windows 环境下开发和运行。