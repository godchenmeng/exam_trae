using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ExamSystem.Data;
using ExamSystem.Services.Services;
using ExamSystem.Services.Interfaces;
using ExamSystem.Infrastructure.Repositories;

namespace ExamSystem.ImportTool
{
    /// <summary>
    /// 消防救援题库导入工具
    /// </summary>
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== 消防救援作战题库导入工具 ===");
            
            // 配置服务
            var host = CreateHostBuilder(args).Build();
            
            try
            {
                using var scope = host.Services.CreateScope();
                var services = scope.ServiceProvider;
                
                var excelImportService = services.GetRequiredService<IExcelImportService>();
                var questionBankService = services.GetRequiredService<IQuestionBankService>();
                var logger = services.GetRequiredService<ILogger<Program>>();
                
                // 检查默认题库是否存在
                var defaultQuestionBank = await questionBankService.GetQuestionBankByIdAsync(1);
                if (defaultQuestionBank == null)
                {
                    Console.WriteLine("错误：默认题库不存在！");
                    return;
                }
                
                Console.WriteLine($"目标题库：{defaultQuestionBank.Name} (ID: {defaultQuestionBank.BankId})");
                
                // 检查Excel文件是否存在
                string excelFilePath = @"E:\Project\exam_trae\消防救援作战题库_系统格式.xlsx";
                if (!File.Exists(excelFilePath))
                {
                    Console.WriteLine($"错误：Excel文件不存在：{excelFilePath}");
                    return;
                }
                
                Console.WriteLine($"Excel文件：{excelFilePath}");
                Console.WriteLine("开始导入题目...");
                
                // 执行导入
                using var fileStream = File.OpenRead(excelFilePath);
                var importResult = await excelImportService.ImportQuestionsFromExcelAsync(fileStream, 1);
                
                // 显示导入结果
                Console.WriteLine("\n=== 导入结果 ===");
                Console.WriteLine($"总题目数：{importResult.TotalCount}");
                Console.WriteLine($"成功导入：{importResult.SuccessCount}");
                Console.WriteLine($"导入失败：{importResult.FailureCount}");
                
                if (importResult.SuccessCount > 0)
                {
                    Console.WriteLine("\n成功导入的题目：");
                    foreach (var success in importResult.SuccessfulQuestions)
                    {
                        Console.WriteLine($"  行 {success.RowNumber}: {success.Title} ({success.QuestionType})");
                    }
                }
                
                if (importResult.FailureCount > 0)
                {
                    Console.WriteLine("\n导入失败的题目：");
                    foreach (var failure in importResult.FailedQuestions)
                    {
                        Console.WriteLine($"  行 {failure.RowNumber}: {failure.Title}");
                        Console.WriteLine($"    错误：{failure.ErrorMessage}");
                    }
                }
                
                if (importResult.ErrorMessages.Count > 0)
                {
                    Console.WriteLine("\n其他错误信息：");
                    foreach (var error in importResult.ErrorMessages)
                    {
                        Console.WriteLine($"  {error}");
                    }
                }
                
                Console.WriteLine("\n导入完成！");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"导入过程中发生异常：{ex.Message}");
                Console.WriteLine($"详细信息：{ex}");
            }
            
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }
        
        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    // 配置数据库连接
                    services.AddDbContext<ExamDbContext>(options =>
                        options.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=ExamSystemDb;Trusted_Connection=true;MultipleActiveResultSets=true"));
                    
                    // 注册仓储
                    services.AddScoped<IQuestionRepository, QuestionRepository>();
                    services.AddScoped<IQuestionBankRepository, QuestionBankRepository>();
                    services.AddScoped<IRepository<ExamSystem.Domain.Entities.Question>, BaseRepository<ExamSystem.Domain.Entities.Question>>();
                    services.AddScoped<IRepository<ExamSystem.Domain.Entities.QuestionBank>, BaseRepository<ExamSystem.Domain.Entities.QuestionBank>>();
                    
                    // 注册服务
                    services.AddScoped<IQuestionService, QuestionService>();
                    services.AddScoped<IQuestionBankService, QuestionBankService>();
                    services.AddScoped<IExcelImportService, ExcelImportService>();
                    
                    // 配置日志
                    services.AddLogging(builder =>
                    {
                        builder.AddConsole();
                        builder.SetMinimumLevel(LogLevel.Information);
                    });
                });
    }
}