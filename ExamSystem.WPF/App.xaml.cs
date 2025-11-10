using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ExamSystem.Data;
using ExamSystem.Infrastructure.Repositories;
using ExamSystem.Services.Interfaces;
using ExamSystem.Services.Services;
using ExamSystem.WPF.Views;
using ExamSystem.WPF.ViewModels;
using Serilog;
using System.IO;
using System.Windows.Threading;

namespace ExamSystem.WPF
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        private IHost? _host;
        
        public IServiceProvider? Services { get; private set; }

        public IServiceProvider GetServices() => _host?.Services ?? throw new InvalidOperationException("Host not initialized");

        protected override async void OnStartup(StartupEventArgs e)
        {
            // 全局异常处理：避免第三方控件（如 LiveCharts）在控件卸载时抛出未处理异常导致应用崩溃
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;

            _host = CreateHostBuilder().Build();
            Services = _host.Services;
            await _host.StartAsync();

            // 初始化数据库（迁移 + 种子数据）
            try
            {
                using var scope = Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ExamDbContext>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<DatabaseSeeder>>();

                // 记录实际使用的数据库路径，避免路径不一致导致数据不显示
                var appLogger = scope.ServiceProvider.GetRequiredService<ILogger<App>>();
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var dbPath = Path.Combine(baseDir, "exam_system.db");
                appLogger.LogInformation("SQLite 数据库路径: {DbPath}", dbPath);

                // 确保数据库迁移已应用（避免缺失列导致运行时错误，如 IsPublished）
                try
                {
                    await context.Database.MigrateAsync();
                    appLogger.LogInformation("数据库迁移应用成功");
                }
                catch (Exception migrationEx)
                {
                    appLogger.LogWarning(migrationEx, "数据库迁移过程中出现警告，尝试确保数据库连接");
                    // 如果迁移失败，尝试确保数据库可以连接
                    await context.Database.EnsureCreatedAsync();
                }

                var seeder = new DatabaseSeeder(context, logger);
                await seeder.SeedAsync();
            }
            catch (Exception ex)
            {
                var logger = Services.GetRequiredService<ILogger<App>>();
                logger.LogError(ex, "数据库初始化失败");
            }

            var loginWindow = Services.GetRequiredService<LoginWindow>();
            loginWindow.Show();

            // 应用启动后异步执行客户端更新检查，并在发现新版本时提示用户
            try
            {
                var updateChecker = Services.GetService<ExamSystem.WPF.Services.IUpdateCheckService>();
                _ = updateChecker?.CheckAndPromptAsync();
            }
            catch (Exception ex)
            {
                var logger = Services.GetRequiredService<ILogger<App>>();
                logger.LogWarning(ex, "启动时更新检查初始化失败（忽略，不影响主流程）");
            }

            // 后台执行远程数据库更新（拉取服务器 SQL 补丁并应用）
            try
            {
                var dbUpdater = Services.GetService<ExamSystem.WPF.Services.IDatabaseUpdateService>();
                _ = dbUpdater?.FetchAndApplyAsync();
            }
            catch (Exception ex)
            {
                var logger = Services.GetRequiredService<ILogger<App>>();
                logger.LogWarning(ex, "启动时数据库更新初始化失败（忽略，不影响主流程）");
            }

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _host?.Dispose();
            base.OnExit(e);
        }

        /// <summary>
        /// 全局 Dispatcher 未处理异常捕获。
        /// 仅在 LiveChartsCore.SkiaSharpView.WPF 的 MotionCanvas 卸载过程中出现的 NullReferenceException 时进行忽略，
        /// 以避免应用在关闭或视图卸载时崩溃。其他异常将继续抛出，便于发现真实问题。
        /// </summary>
        private void App_DispatcherUnhandledException(object? sender, DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                var ex = e.Exception;
                var source = ex.Source ?? string.Empty;
                var stack = ex.StackTrace ?? string.Empty;

                // 仅忽略 LiveChartsCore 在控件卸载时偶发的空引用异常（已知问题，后续考虑升级包版本解决）
                if (ex is NullReferenceException && source.Contains("LiveChartsCore.SkiaSharpView.WPF")
                    && (stack.Contains("CompositionTargetTicker.DisposeTicker") || stack.Contains("MotionCanvas.OnUnloaded")))
                {
                    // 记录并标记为已处理，避免应用崩溃
                    var logger = Services?.GetService<ILogger<App>>();
                    logger?.LogWarning(ex, "忽略 LiveCharts 卸载过程中的 NullReference 异常（不影响功能）");
                    e.Handled = true;
                    return;
                }
            }
            catch
            {
                // 忽略日志记录中的非关键错误，确保不会引发新的异常
            }
            // 其他异常保持默认处理（未标记为处理），便于定位真实问题
        }

        private static IHostBuilder CreateHostBuilder()
        {
            return Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // 数据库上下文
                    var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    var dbPath = Path.Combine(baseDir, "exam_system.db");
                    services.AddDbContext<ExamDbContext>(options =>
                        options.UseSqlite($"Data Source={dbPath}", b => b.MigrationsAssembly("ExamSystem.Infrastructure")));

                    // Repository层
                    services.AddScoped<IUserRepository, UserRepository>();
                    services.AddScoped<IQuestionRepository, QuestionRepository>();
                    services.AddScoped<IQuestionBankRepository, QuestionBankRepository>();
                    services.AddScoped<IExamPaperRepository, ExamPaperRepository>();
                    services.AddScoped<IExamRecordRepository, ExamRecordRepository>();
                    // 新增：注册通用仓储（用于IRepository<T>）
                    services.AddScoped(typeof(IRepository<>), typeof(BaseRepository<>));
                    // 新增通知模块 Repository
                    services.AddScoped<INotificationRepository, NotificationRepository>();
                    services.AddScoped<INotificationRecipientRepository, NotificationRecipientRepository>();
                    // 新增：建筑数据仓储（用于地图绘制题建筑数据加载）
                    services.AddScoped<IBuildingRepository, BuildingRepository>();

                    // Service层
                    services.AddScoped<IUserService, UserService>();
                    services.AddScoped<IAuthService, AuthService>();
                    services.AddScoped<IQuestionService, QuestionService>();
                    services.AddScoped<IQuestionBankService, QuestionBankService>();
                    services.AddScoped<IExamPaperService, ExamPaperService>();
                    services.AddScoped<IExamService, ExamService>();
                    services.AddScoped<IPermissionService, PermissionService>();
                    // 新增通知服务
                    services.AddScoped<INotificationService, NotificationService>();
                    services.AddScoped<IExcelImportService, ExcelImportService>();
                    services.AddScoped<IExcelExportService, ExcelExportService>();
                    services.AddScoped<ExamSystem.WPF.Services.IStatisticsService, ExamSystem.WPF.Services.StatisticsService>();
                    // 新增：仪表板服务
                    services.AddScoped<IDashboardService, DashboardService>();
                    // 新增：地图绘制服务
                    services.AddScoped<IMapDrawingService, MapDrawingService>();
                    // 新增：建筑物管理服务
                    services.AddScoped<IBuildingService, BuildingService>();

                    // ViewModels
                    services.AddTransient<DashboardViewModel>();
                    services.AddTransient<ExamViewModel>();
                    services.AddTransient<ExamPaperViewModel>();
                    services.AddTransient<ExamPaperEditViewModel>();
                    services.AddTransient<ExamPreviewViewModel>();
                    services.AddTransient<ExamResultViewModel>();
                    services.AddTransient<LoginViewModel>();
                    services.AddTransient<PaperQuestionManageViewModel>();
                    services.AddTransient<QuestionBankEditViewModel>();
                    services.AddTransient<QuestionBankViewModel>();
                    services.AddTransient<StatisticsViewModel>();
                    services.AddTransient<UserManagementViewModel>();
                    // 新增：建筑物管理 ViewModel
                    services.AddTransient<BuildingManagementViewModel>();
                    services.AddTransient<BuildingEditViewModel>();

                    // 新增通知相关 ViewModel
                    services.AddTransient<MessageCenterViewModel>();
                    services.AddTransient<NotificationSendViewModel>();
                    // 新增学生相关 ViewModel
                    services.AddTransient<StudentExamListViewModel>();
                    services.AddTransient<StudentExamResultViewModel>();
                    services.AddTransient<FullScreenExamViewModel>();
                    services.AddTransient<ExamResultDetailViewModel>();
                    // 新增成绩管理 ViewModel
                    services.AddTransient<GradeManagementViewModel>();
                    // 新增：地图绘制题 ViewModel
                    services.AddTransient<MapDrawingAuthoringViewModel>();

                    // Views
                    services.AddTransient<LoginWindow>();
                    services.AddTransient<MainWindow>();
                    
                    // 添加DatabaseSeeder
                    services.AddTransient<DatabaseSeeder>();
                    // 使用Provider为DashboardView注入ViewModel
                    services.AddTransient<DashboardView>(provider =>
                    {
                        var vm = provider.GetRequiredService<DashboardViewModel>();
                        return new DashboardView(vm);
                    });
                    services.AddTransient<ExamView>();
                    services.AddTransient<ExamPaperView>(provider =>
                    {
                        var viewModel = provider.GetRequiredService<ExamPaperViewModel>();
                        return new ExamPaperView(viewModel);
                    });
                    services.AddTransient<ExamResultView>();
                    services.AddTransient<LoginWindow>();
                    services.AddTransient<QuestionBankView>(provider =>
                    {
                        var viewModel = provider.GetRequiredService<QuestionBankViewModel>();
                        return new QuestionBankView(viewModel);
                    });
                    services.AddTransient<StatisticsView>();
                    services.AddTransient<UserManagementView>();

                    // 新增视图
                    services.AddTransient<MessageCenterView>();
                    services.AddTransient<LearningResourcesView>();
                    // 新增学生相关视图
                    services.AddTransient<StudentExamListView>(provider =>
                    {
                        var vm = provider.GetRequiredService<StudentExamListViewModel>();
                        return new StudentExamListView(vm);
                    });
                     // 为学生考试结果视图注入对应的 ViewModel 并设置 DataContext
                     services.AddTransient<StudentExamResultView>(provider =>
                    {
                        var vm = provider.GetRequiredService<StudentExamResultViewModel>();
                        var view = new StudentExamResultView();
                        view.DataContext = vm;
                        return view;
                    });
                     services.AddTransient<ExamResultDetailView>();
                     services.AddTransient<FullScreenExamWindow>();
                     // 新增成绩管理视图
                     services.AddTransient<GradeManagementView>();
                    // 新增建筑物管理视图（注入 ViewModel）
                    services.AddTransient<BuildingManagementView>(provider =>
                    {
                        var vm = provider.GetRequiredService<BuildingManagementViewModel>();
                        return new BuildingManagementView(vm);
                    });
                    // 新增通知发送视图（注入 ViewModel）
                    services.AddTransient<NotificationSendView>(provider =>
                    {
                        var vm = provider.GetRequiredService<NotificationSendViewModel>();
                        return new NotificationSendView(vm);
                    });
                    // 新增：地图绘制题编辑窗口（注入对应 ViewModel）
                    services.AddTransient<MapDrawingAuthoring>(provider =>
                    {
                        var vm = provider.GetRequiredService<MapDrawingAuthoringViewModel>();
                        return new MapDrawingAuthoring(vm);
                    });
                    
                    // Dialogs
                    services.AddTransient<ExamPaperEditDialog>();
                    services.AddTransient<ExamPreviewDialog>();
                    services.AddTransient<PaperQuestionManageDialog>();
                    services.AddTransient<QuestionBankEditDialog>();
                    services.AddTransient<QuestionEditDialog>();

                    // 配置Serilog全局日志
                    Log.Logger = new LoggerConfiguration()
                        .MinimumLevel.Debug()
                        .WriteTo.Console()
                        .WriteTo.File(
                            path: "Logs/exam_system_.log",
                            rollingInterval: RollingInterval.Day,
                            retainedFileCountLimit: 7,
                            fileSizeLimitBytes: 10 * 1024 * 1024, // 10MB
                            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                        .CreateLogger();

                    // 日志
                    services.AddLogging(builder =>
                    {
                        builder.ClearProviders();
                        builder.AddSerilog();
                        builder.SetMinimumLevel(LogLevel.Debug);
                    });

                    // 客户端更新检查服务（启动后异步检查并提示）
                    services.AddSingleton<ExamSystem.WPF.Services.IUpdateCheckService, ExamSystem.WPF.Services.UpdateCheckService>();
                    // 远程数据库更新服务（后台拉取服务器 SQL 补丁并应用）
                    services.AddSingleton<ExamSystem.WPF.Services.IDatabaseUpdateService, ExamSystem.WPF.Services.DatabaseUpdateService>();
                });
        }
    }
}