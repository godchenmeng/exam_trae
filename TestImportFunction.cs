using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ExamSystem.Infrastructure.Data;
using ExamSystem.Services.Interfaces;
using ExamSystem.Services.Services;
using ExamSystem.Infrastructure.Repositories;

namespace TestImportFunction
{
    class Program
    {
        static readonly string LogPath = System.IO.Path.Combine("Logs", "test_import_function_run.log");
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
            SafeLog("启动 TestImportFunction");
            
            // 配置服务
            var services = new ServiceCollection();
            
            // 添加日志
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });
            
            // 添加数据库上下文
            services.AddDbContext<ExamDbContext>(options =>
                options.UseSqlite("Data Source=exam_system.db"));
            
            // 注册仓储
            services.AddScoped<IQuestionRepository, QuestionRepository>();
            
            // 添加服务
            services.AddScoped<IQuestionService, QuestionService>();
            services.AddScoped<IExcelImportService, ExcelImportService>();
            
            var serviceProvider = services.BuildServiceProvider();
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            
            try
            {
                // 检查Excel文件是否存在
                var excelFilePath = @"E:\Project\exam_trae\test_import_data.xlsx";
                if (!File.Exists(excelFilePath))
                {
                    Console.WriteLine($"Excel文件不存在: {excelFilePath}");
                    SafeLog($"Excel文件不存在: {excelFilePath}");
                    return;
                }
                
                Console.WriteLine($"找到Excel文件: {excelFilePath}");
                SafeLog($"找到Excel文件: {excelFilePath}");
                
                // 获取导入服务
                var importService = serviceProvider.GetRequiredService<IExcelImportService>();
                
                // 如果文件不是有效的 .xlsx（Open XML Zip），则自动生成模板覆盖该文件
                bool isValidXlsx = false;
                try
                {
                    using (var fs = File.OpenRead(excelFilePath))
                    using (var zip = new System.IO.Compression.ZipArchive(fs, System.IO.Compression.ZipArchiveMode.Read, true))
                    {
                        isValidXlsx = zip.Entries.Count > 0;
                    }
                }
                catch
                {
                    isValidXlsx = false;
                }
                if (!isValidXlsx)
                {
                    Console.WriteLine($"检测到提供的文件不是有效的 .xlsx，将生成导入模板覆盖: {excelFilePath}");
                    SafeLog("检测到无效xlsx，生成模板覆盖");
                    var templateBytes = importService.GetExcelTemplate();
                    File.WriteAllBytes(excelFilePath, templateBytes);
                    Console.WriteLine("已生成有效的导入模板，路径: " + excelFilePath);
                    SafeLog("已生成有效的导入模板");
                }
                
                // 测试题库ID（假设存在ID为1的题库）
                int testBankId = 1;
                
                Console.WriteLine($"开始导入到题库ID: {testBankId}");
                SafeLog($"开始导入到题库ID: {testBankId}");
                
                // 执行导入
                using var fileStream = File.OpenRead(excelFilePath);
                var result = await importService.ImportQuestionsFromExcelAsync(fileStream, testBankId);
                
                // 显示结果
                Console.WriteLine("\n=== 导入结果 ===");
                Console.WriteLine($"总记录数: {result.TotalCount}");
                Console.WriteLine($"成功导入: {result.SuccessCount}");
                Console.WriteLine($"导入失败: {result.FailureCount}");
                Console.WriteLine($"导入成功: {result.IsSuccess}");
                SafeLog($"导入结果: 总数={result.TotalCount}, 成功={result.SuccessCount}, 失败={result.FailureCount}, 成功标记={result.IsSuccess}");
                
                if (result.ErrorMessages?.Count > 0)
                {
                    Console.WriteLine("\n错误信息:");
                    foreach (var error in result.ErrorMessages)
                    {
                        Console.WriteLine($"- {error}");
                        SafeLog($"错误信息: {error}");
                    }
                }
                
                if (result.DetailedErrors?.Count > 0)
                {
                    Console.WriteLine("\n详细错误:");
                    foreach (var error in result.DetailedErrors)
                    {
                        Console.WriteLine($"第{error.RowNumber}行: {error.ErrorMessage}");
                        SafeLog($"详细错误: 行={error.RowNumber}, {error.ErrorMessage}");
                    }
                }
                
                if (result.SuccessfulQuestions?.Count > 0)
                {
                    Console.WriteLine("\n成功导入的题目:");
                    foreach (var question in result.SuccessfulQuestions)
                    {
                        Console.WriteLine($"第{question.RowNumber}行: {question.Title} ({question.QuestionType})");
                        SafeLog($"成功: 行={question.RowNumber}, 标题={question.Title}, 类型={question.QuestionType}");
                    }
                }
                
                if (result.FailedQuestions?.Count > 0)
                {
                    Console.WriteLine("\n导入失败的题目:");
                    foreach (var question in result.FailedQuestions)
                    {
                        Console.WriteLine($"第{question.RowNumber}行: {question.Title} - {question.ErrorMessage}");
                        SafeLog($"失败: 行={question.RowNumber}, 标题={question.Title}, 错误={question.ErrorMessage}");
                    }
                }
                
                Console.WriteLine("\n=== 测试完成 ===");
                SafeLog("测试完成");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "测试导入功能时发生异常");
                Console.WriteLine($"发生异常: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                SafeLog($"异常: {ex.Message}\n{ex.StackTrace}");
            }
            
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }
    }
}