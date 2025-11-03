using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ExamSystem.Data;
using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;
using ExamSystem.Services.Services;
using ExamSystem.Infrastructure.Repositories;

namespace ExamSystem.Tests
{
    /// <summary>
    /// 地图绘制数据保存功能测试
    /// </summary>
    class TestMapDrawingSave
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== 地图绘制数据保存功能测试 ===");
            
            try
            {
                // 配置服务
                var services = new ServiceCollection();
                ConfigureServices(services);
                var serviceProvider = services.BuildServiceProvider();
                
                // 获取服务
                var context = serviceProvider.GetRequiredService<ExamDbContext>();
                var examService = serviceProvider.GetRequiredService<ExamService>();
                
                // 确保数据库存在
                await context.Database.EnsureCreatedAsync();
                
                // 测试地图绘制数据保存
                await TestMapDrawingDataSave(context, examService);
                
                Console.WriteLine("\n=== 测试完成 ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"测试失败: {ex.Message}");
                Console.WriteLine($"详细错误: {ex}");
            }
            
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }
        
        static void ConfigureServices(IServiceCollection services)
        {
            // 配置数据库上下文
            services.AddDbContext<ExamDbContext>(options =>
                options.UseSqlite("Data Source=exam_system.db"));
            
            // 配置日志
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });
            
            // 配置仓储
            services.AddScoped<IExamRecordRepository, ExamRecordRepository>();
            services.AddScoped<IExamPaperRepository, ExamPaperRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            
            // 配置服务
            services.AddScoped<ExamService>();
        }
        
        static async Task TestMapDrawingDataSave(ExamDbContext context, ExamService examService)
        {
            Console.WriteLine("\n1. 创建测试数据...");
            
            // 创建测试用户
            var testUser = new User
            {
                Username = "test_user_map",
                Password = "test123",
                Name = "测试用户",
                Role = UserRole.Student,
                IsActive = true,
                CreatedAt = DateTime.Now
            };
            
            context.Users.Add(testUser);
            await context.SaveChangesAsync();
            Console.WriteLine($"创建测试用户: {testUser.Name} (ID: {testUser.UserId})");
            
            // 创建测试题库
            var testBank = new QuestionBank
            {
                Name = "地图绘制测试题库",
                Description = "用于测试地图绘制功能",
                CreatedBy = testUser.UserId,
                CreatedAt = DateTime.Now,
                IsActive = true
            };
            
            context.QuestionBanks.Add(testBank);
            await context.SaveChangesAsync();
            Console.WriteLine($"创建测试题库: {testBank.Name} (ID: {testBank.BankId})");
            
            // 创建地图绘制题目
            var mapQuestion = new Question
            {
                BankId = testBank.BankId,
                Content = "请在地图上标记消防站位置，并绘制救援路线。",
                QuestionType = QuestionType.MapDrawing,
                Score = 10.0m,
                CreatedBy = testUser.UserId,
                CreatedAt = DateTime.Now,
                IsActive = true
            };
            
            context.Questions.Add(mapQuestion);
            await context.SaveChangesAsync();
            Console.WriteLine($"创建地图绘制题目: {mapQuestion.Content} (ID: {mapQuestion.QuestionId})");
            
            // 创建测试试卷
            var testPaper = new ExamPaper
            {
                Name = "地图绘制测试试卷",
                Description = "用于测试地图绘制功能的试卷",
                Duration = 60,
                TotalScore = 10.0m,
                CreatedBy = testUser.UserId,
                CreatedAt = DateTime.Now,
                IsActive = true,
                IsPublished = true
            };
            
            context.ExamPapers.Add(testPaper);
            await context.SaveChangesAsync();
            Console.WriteLine($"创建测试试卷: {testPaper.Name} (ID: {testPaper.PaperId})");
            
            // 创建试卷题目关联
            var paperQuestion = new ExamPaperQuestion
            {
                PaperId = testPaper.PaperId,
                QuestionId = mapQuestion.QuestionId,
                QuestionOrder = 1,
                Score = 10.0m
            };
            
            context.ExamPaperQuestions.Add(paperQuestion);
            await context.SaveChangesAsync();
            Console.WriteLine("创建试卷题目关联");
            
            Console.WriteLine("\n2. 开始模拟考试...");
            
            // 开始考试
            var examRecord = await examService.StartExamAsync(testUser.UserId, testPaper.PaperId);
            Console.WriteLine($"开始考试，考试记录ID: {examRecord.RecordId}");
            
            Console.WriteLine("\n3. 测试地图绘制数据保存...");
            
            // 模拟地图绘制数据
            var mapDrawingData = @"{
                ""overlays"": [
                    {
                        ""id"": ""marker-1"",
                        ""type"": ""marker"",
                        ""point"": {""lng"": 106.63, ""lat"": 26.65},
                        ""name"": ""消防站1"",
                        ""icon"": ""fire_station""
                    },
                    {
                        ""id"": ""polyline-1"",
                        ""type"": ""polyline"",
                        ""points"": [
                            {""lng"": 106.63, ""lat"": 26.65},
                            {""lng"": 106.64, ""lat"": 26.66},
                            {""lng"": 106.65, ""lat"": 26.67}
                        ],
                        ""name"": ""救援路线1"",
                        ""strokeColor"": ""#FF0000"",
                        ""strokeWeight"": 3
                    }
                ],
                ""center"": {""lng"": 106.63, ""lat"": 26.65},
                ""zoom"": 15
            }";
            
            var mapCenter = @"{""lng"": 106.63, ""lat"": 26.65}";
            var mapZoom = 15;
            
            // 保存地图绘制答案
            var saveResult = await examService.SaveMapDrawingAnswerAsync(
                examRecord.RecordId,
                mapQuestion.QuestionId,
                mapDrawingData,
                mapCenter,
                mapZoom);
            
            if (saveResult)
            {
                Console.WriteLine("✓ 地图绘制数据保存成功");
            }
            else
            {
                Console.WriteLine("✗ 地图绘制数据保存失败");
                return;
            }
            
            Console.WriteLine("\n4. 验证数据保存结果...");
            
            // 查询保存的数据
            var answerRecord = await context.AnswerRecords
                .FirstOrDefaultAsync(ar => ar.RecordId == examRecord.RecordId && ar.QuestionId == mapQuestion.QuestionId);
            
            if (answerRecord != null)
            {
                Console.WriteLine("✓ 找到答题记录");
                Console.WriteLine($"  - 用户答案长度: {answerRecord.UserAnswer?.Length ?? 0} 字符");
                Console.WriteLine($"  - 地图绘制数据长度: {answerRecord.MapDrawingData?.Length ?? 0} 字符");
                Console.WriteLine($"  - 地图中心点: {answerRecord.MapCenter}");
                Console.WriteLine($"  - 地图缩放级别: {answerRecord.MapZoom}");
                Console.WriteLine($"  - 答题时间: {answerRecord.AnswerTime}");
                
                // 验证数据完整性
                if (!string.IsNullOrEmpty(answerRecord.MapDrawingData) &&
                    answerRecord.MapDrawingData.Contains("overlays") &&
                    answerRecord.MapDrawingData.Contains("marker-1") &&
                    answerRecord.MapDrawingData.Contains("polyline-1"))
                {
                    Console.WriteLine("✓ 地图绘制数据完整性验证通过");
                }
                else
                {
                    Console.WriteLine("✗ 地图绘制数据完整性验证失败");
                }
                
                if (answerRecord.MapCenter == mapCenter && answerRecord.MapZoom == mapZoom)
                {
                    Console.WriteLine("✓ 地图中心点和缩放级别验证通过");
                }
                else
                {
                    Console.WriteLine("✗ 地图中心点和缩放级别验证失败");
                }
            }
            else
            {
                Console.WriteLine("✗ 未找到答题记录");
            }
            
            Console.WriteLine("\n5. 测试提交考试...");
            
            // 提交考试
            var submitResult = await examService.SubmitExamAsync(examRecord.RecordId);
            if (submitResult)
            {
                Console.WriteLine("✓ 考试提交成功");
            }
            else
            {
                Console.WriteLine("✗ 考试提交失败");
            }
            
            Console.WriteLine("\n6. 清理测试数据...");
            
            // 清理测试数据
            context.AnswerRecords.RemoveRange(context.AnswerRecords.Where(ar => ar.RecordId == examRecord.RecordId));
            context.ExamRecords.Remove(examRecord);
            context.ExamPaperQuestions.Remove(paperQuestion);
            context.ExamPapers.Remove(testPaper);
            context.Questions.Remove(mapQuestion);
            context.QuestionBanks.Remove(testBank);
            context.Users.Remove(testUser);
            
            await context.SaveChangesAsync();
            Console.WriteLine("✓ 测试数据清理完成");
        }
    }
}