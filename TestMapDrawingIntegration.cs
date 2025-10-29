using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ExamSystem.Data;
using ExamSystem.Services.Interfaces;
using ExamSystem.Services.Services;
using ExamSystem.Infrastructure.Repositories;
using ExamSystem.Domain.DTOs;
using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;

namespace ExamSystem.Tests
{
    /// <summary>
    /// 地图绘制功能集成测试
    /// </summary>
    class TestMapDrawingIntegration
    {
        private static IServiceProvider? _serviceProvider;

        static async Task Main(string[] args)
        {
            Console.WriteLine("=== 地图绘制功能集成测试 ===");
            
            // 配置服务
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            try
            {
                // 初始化数据库
                await InitializeDatabaseAsync();
                
                // 测试地图绘制服务
                await TestMapDrawingServiceAsync();
                
                // 测试考试流程集成
                await TestExamIntegrationAsync();
                
                Console.WriteLine("\n✅ 所有测试通过！地图绘制功能集成正常。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ 测试失败: {ex.Message}");
                Console.WriteLine($"详细错误: {ex}");
            }
            finally
            {
                _serviceProvider?.Dispose();
            }

            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            // 配置日志
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });

            // 配置数据库
            var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test_exam_system.db");
            services.AddDbContext<ExamDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));

            // 注册仓储
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IQuestionRepository, QuestionRepository>();
            services.AddScoped<IExamPaperRepository, ExamPaperRepository>();
            services.AddScoped<IQuestionBankRepository, QuestionBankRepository>();

            // 注册服务
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IExamService, ExamService>();
            services.AddScoped<IMapDrawingService, MapDrawingService>();
        }

        private static async Task InitializeDatabaseAsync()
        {
            Console.WriteLine("1. 初始化测试数据库...");
            
            using var scope = _serviceProvider!.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ExamDbContext>();
            
            // 确保数据库存在
            await context.Database.EnsureCreatedAsync();
            
            // 创建测试用户
            if (!await context.Users.AnyAsync())
            {
                var testUser = new User
                {
                    Username = "testuser",
                    PasswordHash = "test123",
                    Email = "test@example.com",
                    Role = UserRole.Student,
                    CreatedAt = DateTime.Now
                };
                context.Users.Add(testUser);
                await context.SaveChangesAsync();
                Console.WriteLine("   ✓ 创建测试用户");
            }

            // 创建测试题库
            if (!await context.QuestionBanks.AnyAsync())
            {
                var questionBank = new QuestionBank
                {
                    Name = "地图绘制测试题库",
                    Description = "用于测试地图绘制功能的题库",
                    CreatedAt = DateTime.Now
                };
                context.QuestionBanks.Add(questionBank);
                await context.SaveChangesAsync();

                // 创建地图绘制题
                var mapQuestion = new Question
                {
                    QuestionBankId = questionBank.Id,
                    Type = QuestionType.MapDrawing,
                    Content = "请在地图上标注消防站位置",
                    Difficulty = Difficulty.Medium,
                    Score = 10,
                    CreatedAt = DateTime.Now
                };
                context.Questions.Add(mapQuestion);
                await context.SaveChangesAsync();
                Console.WriteLine("   ✓ 创建测试题库和地图绘制题");
            }

            Console.WriteLine("   数据库初始化完成");
        }

        private static async Task TestMapDrawingServiceAsync()
        {
            Console.WriteLine("\n2. 测试地图绘制服务...");
            
            using var scope = _serviceProvider!.CreateScope();
            var mapDrawingService = scope.ServiceProvider.GetRequiredService<IMapDrawingService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<TestMapDrawingIntegration>>();

            try
            {
                // 测试保存地图绘制数据
                var saveRequest = new SaveMapDrawingRequest
                {
                    QuestionId = 1,
                    AnswerId = 1,
                    DrawingData = "[{\"type\":\"marker\",\"lat\":39.9042,\"lng\":116.4074,\"title\":\"消防站1\"}]"
                };

                var saveResponse = await mapDrawingService.SaveMapDrawingAsync(saveRequest);
                Console.WriteLine($"   ✓ 保存地图绘制数据: {saveResponse.Message}");

                // 测试加载地图绘制数据
                var loadRequest = new LoadMapDrawingRequest
                {
                    QuestionId = 1,
                    AnswerId = 1
                };

                var loadResponse = await mapDrawingService.LoadMapDrawingAsync(loadRequest);
                Console.WriteLine($"   ✓ 加载地图绘制数据: {loadResponse.Message}");
                Console.WriteLine($"   ✓ 绘制数据: {loadResponse.DrawingData}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "地图绘制服务测试失败");
                throw;
            }
        }

        private static async Task TestExamIntegrationAsync()
        {
            Console.WriteLine("\n3. 测试考试流程集成...");
            
            using var scope = _serviceProvider!.CreateScope();
            var examService = scope.ServiceProvider.GetRequiredService<IExamService>();
            var context = scope.ServiceProvider.GetRequiredService<ExamDbContext>();

            try
            {
                // 创建测试试卷
                var examPaper = new ExamPaper
                {
                    Title = "地图绘制功能测试试卷",
                    Description = "测试地图绘制功能的集成效果",
                    Duration = 60,
                    TotalScore = 100,
                    IsPublished = true,
                    CreatedAt = DateTime.Now
                };
                context.ExamPapers.Add(examPaper);
                await context.SaveChangesAsync();

                Console.WriteLine("   ✓ 创建测试试卷");

                // 测试考试开始流程
                var user = await context.Users.FirstAsync();
                Console.WriteLine($"   ✓ 获取测试用户: {user.Username}");

                // 验证地图绘制题类型处理
                var questions = await context.Questions
                    .Where(q => q.Type == QuestionType.MapDrawing)
                    .ToListAsync();
                
                Console.WriteLine($"   ✓ 找到 {questions.Count} 道地图绘制题");

                foreach (var question in questions)
                {
                    Console.WriteLine($"     - 题目ID: {question.Id}, 内容: {question.Content}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ 考试流程集成测试失败: {ex.Message}");
                throw;
            }
        }
    }
}