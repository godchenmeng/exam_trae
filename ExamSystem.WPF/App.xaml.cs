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
            _host = CreateHostBuilder().Build();
            Services = _host.Services;
            await _host.StartAsync();

            // 初始化数据库种子数据
            try
            {
                using var scope = Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ExamDbContext>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<DatabaseSeeder>>();
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

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _host?.Dispose();
            base.OnExit(e);
        }

        private static IHostBuilder CreateHostBuilder()
        {
            return Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // 数据库上下文
                    services.AddDbContext<ExamDbContext>(options =>
                        options.UseSqlite("Data Source=exam_system.db"));

                    // Repository层
                    services.AddScoped<IUserRepository, UserRepository>();
                    services.AddScoped<IQuestionRepository, QuestionRepository>();
                    services.AddScoped<IQuestionBankRepository, QuestionBankRepository>();
                    services.AddScoped<IExamPaperRepository, ExamPaperRepository>();
                    services.AddScoped<IExamRecordRepository, ExamRecordRepository>();

                    // Service层
                    services.AddScoped<IUserService, UserService>();
                    services.AddScoped<IAuthService, AuthService>();
                    services.AddScoped<IQuestionService, QuestionService>();
                    services.AddScoped<IQuestionBankService, QuestionBankService>();
                    services.AddScoped<IExamPaperService, ExamPaperService>();
                    services.AddScoped<IExamService, ExamService>();
                    services.AddScoped<IPermissionService, PermissionService>();
                    services.AddScoped<IExcelImportService, ExcelImportService>();
                    services.AddScoped<IExcelExportService, ExcelExportService>();
                    services.AddScoped<ExamSystem.WPF.Services.IStatisticsService, ExamSystem.WPF.Services.StatisticsService>();

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

                    // Views
                    services.AddTransient<LoginWindow>();
                    services.AddTransient<MainWindow>();
                    
                    // 添加DatabaseSeeder
                    services.AddTransient<DatabaseSeeder>();
                    services.AddTransient<DashboardView>();
                    services.AddTransient<ExamView>();
                    services.AddTransient<ExamPaperView>();
                    services.AddTransient<ExamResultView>();
                    services.AddTransient<LoginWindow>();
                    services.AddTransient<QuestionBankView>(provider =>
                    {
                        var viewModel = provider.GetRequiredService<QuestionBankViewModel>();
                        return new QuestionBankView(viewModel);
                    });
                    services.AddTransient<StatisticsView>();
                    services.AddTransient<UserManagementView>();
                    
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
                });
        }
    }
}