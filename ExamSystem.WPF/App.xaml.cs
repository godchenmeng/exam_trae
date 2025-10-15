using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ExamSystem.Infrastructure.Data;
using ExamSystem.Infrastructure.Repositories;
using ExamSystem.Services.Interfaces;
using ExamSystem.Services.Services;
using ExamSystem.WPF.Views;
using ExamSystem.WPF.ViewModels;

namespace ExamSystem.WPF
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        private IHost? _host;

        protected override void OnStartup(StartupEventArgs e)
        {
            _host = CreateHostBuilder().Build();
            
            // 确保数据库已创建
            using (var scope = _host.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ExamDbContext>();
                context.Database.EnsureCreated();
            }

            // 显示主窗口
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

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

                    // ViewModels
                    services.AddTransient<ExamViewModel>();
                    services.AddTransient<ExamPaperViewModel>();
                    services.AddTransient<ExamPaperEditViewModel>();
                    services.AddTransient<ExamPreviewViewModel>();
                    services.AddTransient<ExamResultViewModel>();
                    services.AddTransient<QuestionBankViewModel>();
                    services.AddTransient<QuestionBankEditViewModel>();
                    services.AddTransient<QuestionEditViewModel>();
                    services.AddTransient<PaperQuestionManageViewModel>();

                    // Views
                    services.AddTransient<MainWindow>();
                    services.AddTransient<ExamView>();
                    services.AddTransient<ExamPaperView>();
                    services.AddTransient<ExamResultView>();
                    services.AddTransient<QuestionBankView>();

                    // 日志
                    services.AddLogging(builder =>
                    {
                        builder.AddConsole();
                        builder.AddDebug();
                    });
                });
        }
    }
}