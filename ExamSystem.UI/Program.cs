using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ExamSystem.Services.Interfaces;
using ExamSystem.Services.Services;
using ExamSystem.Infrastructure.Data;
using ExamSystem.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using ExamSystem.UI.ViewModels;
using ExamSystem.UI.Views;
using ExamSystem.WPF.ViewModels;

namespace ExamSystem.UI
{
    class Program
    {
        public static ServiceProvider ServiceProvider { get; private set; }

        [STAThread]
        public static void Main(string[] args)
        {
            ConfigureServices();
            
            var app = new App();
            app.InitializeComponent();
            app.Run();
        }

        private static void ConfigureServices()
        {
            var services = new ServiceCollection();

            // 配置日志
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
                builder.SetMinimumLevel(LogLevel.Information);
            });

            // 配置数据库
            services.AddDbContext<ExamDbContext>(options =>
                options.UseSqlite("Data Source=exam_system.db"));

            // 注册Repository接口和实现
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IQuestionRepository, QuestionRepository>();
            services.AddScoped<IQuestionBankRepository, QuestionBankRepository>();
            services.AddScoped<IExamPaperRepository, ExamPaperRepository>();
            services.AddScoped<IExamRecordRepository, ExamRecordRepository>();

            // 注册服务
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IPermissionService, PermissionService>();
            services.AddScoped<IQuestionBankService, QuestionBankService>();
            services.AddScoped<IQuestionService, QuestionService>();
            services.AddScoped<IExamPaperService, ExamPaperService>();
            services.AddScoped<IExamService, ExamService>();

            // 注册ViewModels
            services.AddTransient<LoginViewModel>();
            services.AddTransient<MainViewModel>();
            services.AddTransient<UserManagementViewModel>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<QuestionBankViewModel>();
            services.AddTransient<QuestionBankEditViewModel>();
            services.AddTransient<QuestionEditViewModel>();
            services.AddTransient<ExamPaperViewModel>();
            services.AddTransient<ExamPaperEditViewModel>();
            services.AddTransient<PaperQuestionManageViewModel>();
            services.AddTransient<ExamViewModel>();
            services.AddTransient<ExamPreviewViewModel>();
            services.AddTransient<ExamResultViewModel>();

            ServiceProvider = services.BuildServiceProvider();
        }
    }
}
