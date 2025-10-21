using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ExamSystem.Services.Services;
using ExamSystem.Services.Interfaces;
using ExamSystem.Infrastructure.Repositories;
using ExamSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ExamSystem.Services.Models;

namespace TestValidation
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== 考试验证逻辑测试 ===");
            
            // 配置服务
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole());
            services.AddDbContext<ExamDbContext>(options =>
                options.UseSqlite("Data Source=exam_system.db"));
            services.AddScoped<IExamPaperRepository, ExamPaperRepository>();
            services.AddScoped<IExamRecordRepository, ExamRecordRepository>();
            services.AddScoped<IExamService, ExamService>();
            
            var serviceProvider = services.BuildServiceProvider();
            var examService = serviceProvider.GetRequiredService<IExamService>();
            
            try
            {
                // 测试用户ID和试卷ID（这些需要根据实际数据库中的数据调整）
                int testUserId = 2; // 假设学生用户ID为2
                int testPaperId = 1; // 假设试卷ID为1
                
                Console.WriteLine($"测试用户ID: {testUserId}");
                Console.WriteLine($"测试试卷ID: {testPaperId}");
                Console.WriteLine();
                
                // 测试验证方法
                Console.WriteLine("=== 测试 ValidateUserExamEligibilityAsync 方法 ===");
                var validationResult = await examService.ValidateUserExamEligibilityAsync(testUserId, testPaperId);
                
                Console.WriteLine($"验证结果: {(validationResult.IsValid ? "通过" : "失败")}");
                if (!validationResult.IsValid)
                {
                    Console.WriteLine($"错误信息: {validationResult.ErrorMessage}");
                    Console.WriteLine($"错误类型: {validationResult.ErrorType}");
                }
                else
                {
                    Console.WriteLine("用户可以参加考试");
                }
                
                Console.WriteLine();
                
                // 测试原有的布尔方法
                Console.WriteLine("=== 测试 CanUserTakeExamAsync 方法 ===");
                var canTake = await examService.CanUserTakeExamAsync(testUserId, testPaperId);
                Console.WriteLine($"CanUserTakeExamAsync 结果: {canTake}");
                
                Console.WriteLine();
                Console.WriteLine("=== 测试不同的错误场景 ===");
                
                // 测试不存在的试卷
                Console.WriteLine("1. 测试不存在的试卷 (ID: 999)");
                var result1 = await examService.ValidateUserExamEligibilityAsync(testUserId, 999);
                Console.WriteLine($"   结果: {(result1.IsValid ? "通过" : "失败")}");
                if (!result1.IsValid)
                {
                    Console.WriteLine($"   错误: {result1.ErrorMessage}");
                }
                
                // 测试不存在的用户
                Console.WriteLine("2. 测试不存在的用户 (ID: 999)");
                var result2 = await examService.ValidateUserExamEligibilityAsync(999, testPaperId);
                Console.WriteLine($"   结果: {(result2.IsValid ? "通过" : "失败")}");
                if (!result2.IsValid)
                {
                    Console.WriteLine($"   错误: {result2.ErrorMessage}");
                }
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"测试过程中发生错误: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            }
            
            Console.WriteLine();
            Console.WriteLine("测试完成，按任意键退出...");
            Console.ReadKey();
        }
    }
}