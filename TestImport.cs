using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ExamSystem.Infrastructure.Data;
using ExamSystem.Services.Services;
using ExamSystem.Services.Interfaces;
using ExamSystem.Infrastructure.Repositories;

class Program
{
    static readonly string LogPath = System.IO.Path.Combine("Logs", "test_import_run.log");
    static void SafeLog(string message)
    {
        try
        {
            System.IO.Directory.CreateDirectory("Logs");
            System.IO.File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
        }
        catch { /* ignore logging failures */ }
    }
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== 测试Excel导入功能 ===");
        SafeLog("启动 TestImport 控制台");
        
        // 配置服务
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        
        // 配置数据库
        services.AddDbContext<ExamDbContext>(options =>
            options.UseSqlite("Data Source=ExamSystem.WPF/exam_system.db"));
        
        // 注册仓储
        services.AddScoped<IQuestionRepository, QuestionRepository>();
        
        // 注册服务
        services.AddScoped<IQuestionService, QuestionService>();
        services.AddScoped<IExcelImportService, ExcelImportService>();
        
        var serviceProvider = services.BuildServiceProvider();
        
        try
        {
            var excelImportService = serviceProvider.GetRequiredService<IExcelImportService>();
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            
            // 检查Excel文件是否存在
            string excelFilePath = "消防救援作战题库_系统格式.xlsx";
            if (!File.Exists(excelFilePath))
            {
                Console.WriteLine($"错误：找不到Excel文件 {excelFilePath}");
                SafeLog($"错误：找不到Excel文件 {excelFilePath}");
                return;
            }
            
            Console.WriteLine($"找到Excel文件：{excelFilePath}");
            Console.WriteLine($"文件大小：{new FileInfo(excelFilePath).Length} 字节");
            SafeLog($"找到Excel文件：{excelFilePath}，大小={new FileInfo(excelFilePath).Length} 字节");
            
            // 测试文件格式验证
            Console.WriteLine("\n=== 验证Excel格式 ===");
            using (var fileStream = File.OpenRead(excelFilePath))
            {
                var validationResult = await excelImportService.ValidateExcelFormatAsync(fileStream);
                Console.WriteLine($"格式验证结果：{(validationResult.IsSuccess ? "成功" : "失败")}");
                Console.WriteLine($"总行数：{validationResult.TotalCount}");
                Console.WriteLine($"成功行数：{validationResult.SuccessCount}");
                Console.WriteLine($"失败行数：{validationResult.FailureCount}");
                SafeLog($"格式验证：总={validationResult.TotalCount}, 成功={validationResult.SuccessCount}, 失败={validationResult.FailureCount}, 成功标记={validationResult.IsSuccess}");
                
                if (validationResult.ErrorMessages.Count > 0)
                {
                    Console.WriteLine("错误信息：");
                    foreach (var error in validationResult.ErrorMessages)
                    {
                        Console.WriteLine($"  - {error}");
                        SafeLog($"验证错误：{error}");
                    }
                }
                
                if (validationResult.DetailedErrors.Count > 0)
                {
                    Console.WriteLine("详细错误：");
                    foreach (var kvp in validationResult.DetailedErrors)
                    {
                        Console.WriteLine($"  行 {kvp.Key}: {string.Join(", ", kvp.Value)}");
                        SafeLog($"验证详细错误：行 {kvp.Key} - {string.Join(", ", kvp.Value)}");
                    }
                }
            }
            
            // 测试实际导入（使用题库ID=1）
            Console.WriteLine("\n=== 测试导入到题库 ===");
            using (var fileStream = File.OpenRead(excelFilePath))
            {
                var importResult = await excelImportService.ImportQuestionsFromExcelAsync(fileStream, 1);
                Console.WriteLine($"导入结果：{(importResult.IsSuccess ? "成功" : "失败")}");
                Console.WriteLine($"总行数：{importResult.TotalCount}");
                Console.WriteLine($"成功导入：{importResult.SuccessCount}");
                Console.WriteLine($"导入失败：{importResult.FailureCount}");
                SafeLog($"导入：总={importResult.TotalCount}, 成功={importResult.SuccessCount}, 失败={importResult.FailureCount}, 成功标记={importResult.IsSuccess}");
                
                if (importResult.ErrorMessages.Count > 0)
                {
                    Console.WriteLine("错误信息：");
                    foreach (var error in importResult.ErrorMessages)
                    {
                        Console.WriteLine($"  - {error}");
                        SafeLog($"导入错误：{error}");
                    }
                }
                
                if (importResult.FailedQuestions.Count > 0)
                {
                    Console.WriteLine("失败的题目：");
                    foreach (var failed in importResult.FailedQuestions)
                    {
                        Console.WriteLine($"  行 {failed.RowNumber}: {failed.ErrorMessage}");
                        Console.WriteLine($"    题目: {failed.Title}");
                        SafeLog($"失败题目：行 {failed.RowNumber} - {failed.Title} - {failed.ErrorMessage}");
                    }
                }
                
                if (importResult.SuccessfulQuestions.Count > 0)
                {
                    Console.WriteLine("成功导入的题目：");
                    foreach (var success in importResult.SuccessfulQuestions)
                    {
                        Console.WriteLine($"  行 {success.RowNumber}: {success.Title}");
                        SafeLog($"成功题目：行 {success.RowNumber} - {success.Title}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"程序异常：{ex.Message}");
            Console.WriteLine($"堆栈跟踪：{ex.StackTrace}");
            SafeLog($"程序异常：{ex.Message}\n{ex.StackTrace}");
        }
        
        Console.WriteLine("\n按任意键退出...");
        Console.ReadKey();
    }
}