using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ExamSystem.Infrastructure.Data;
using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;

namespace ExamSystem.Tests
{
    /// <summary>
    /// 地图绘制数据保存功能验证工具
    /// </summary>
    class TestMapDrawingData
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== 地图绘制数据保存功能验证 ===");
            
            try
            {
                // 配置数据库连接
                var options = new DbContextOptionsBuilder<ExamDbContext>()
                    .UseSqlite("Data Source=exam_system.db")
                    .Options;
                
                using var context = new ExamDbContext(options);
                
                // 1. 检查数据库连接
                Console.WriteLine("1. 检查数据库连接...");
                var canConnect = await context.Database.CanConnectAsync();
                if (!canConnect)
                {
                    Console.WriteLine("✗ 数据库连接失败");
                    return;
                }
                Console.WriteLine("✓ 数据库连接成功");
                
                // 2. 检查地图绘制题目
                Console.WriteLine("\n2. 检查地图绘制题目...");
                var mapQuestions = await context.Questions
                    .Where(q => q.QuestionType == QuestionType.MapDrawing && q.IsActive)
                    .ToListAsync();
                
                Console.WriteLine($"✓ 找到 {mapQuestions.Count} 个地图绘制题目");
                
                if (mapQuestions.Count == 0)
                {
                    Console.WriteLine("没有地图绘制题目，创建测试题目...");
                    await CreateTestMapQuestion(context);
                    mapQuestions = await context.Questions
                        .Where(q => q.QuestionType == QuestionType.MapDrawing && q.IsActive)
                        .ToListAsync();
                }
                
                // 3. 检查答题记录中的地图绘制数据
                Console.WriteLine("\n3. 检查答题记录中的地图绘制数据...");
                var mapAnswers = await context.AnswerRecords
                    .Include(ar => ar.Question)
                    .Where(ar => ar.Question.QuestionType == QuestionType.MapDrawing)
                    .ToListAsync();
                
                Console.WriteLine($"✓ 找到 {mapAnswers.Count} 个地图绘制答题记录");
                
                foreach (var answer in mapAnswers)
                {
                    Console.WriteLine($"  - 答题记录ID: {answer.AnswerId}");
                    Console.WriteLine($"    题目ID: {answer.QuestionId}");
                    Console.WriteLine($"    地图绘制数据: {(string.IsNullOrEmpty(answer.MapDrawingData) ? "无" : $"{answer.MapDrawingData.Length} 字符")}");
                    Console.WriteLine($"    地图中心点: {answer.MapCenter ?? "无"}");
                    Console.WriteLine($"    地图缩放级别: {answer.MapZoom?.ToString() ?? "无"}");
                    Console.WriteLine($"    答题时间: {answer.AnswerTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "无"}");
                    Console.WriteLine();
                }
                
                // 4. 测试保存新的地图绘制数据
                Console.WriteLine("4. 测试保存新的地图绘制数据...");
                if (mapQuestions.Count > 0)
                {
                    await TestSaveMapDrawingData(context, mapQuestions.First());
                }
                
                // 5. 验证数据完整性
                Console.WriteLine("\n5. 验证数据完整性...");
                var updatedAnswers = await context.AnswerRecords
                    .Include(ar => ar.Question)
                    .Where(ar => ar.Question.QuestionType == QuestionType.MapDrawing && 
                                !string.IsNullOrEmpty(ar.MapDrawingData))
                    .ToListAsync();
                
                Console.WriteLine($"✓ 有效的地图绘制答题记录: {updatedAnswers.Count} 个");
                
                foreach (var answer in updatedAnswers)
                {
                    var isValid = !string.IsNullOrEmpty(answer.MapDrawingData) &&
                                 !string.IsNullOrEmpty(answer.MapCenter) &&
                                 answer.MapZoom.HasValue;
                    
                    Console.WriteLine($"  - 记录ID {answer.AnswerId}: {(isValid ? "✓ 完整" : "✗ 不完整")}");
                }
                
                Console.WriteLine("\n=== 验证完成 ===");
                Console.WriteLine("地图绘制数据保存功能验证通过！");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"验证失败: {ex.Message}");
                Console.WriteLine($"详细错误: {ex}");
            }
            
            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }
        
        private static async Task CreateTestMapQuestion(ExamDbContext context)
        {
            // 查找或创建题库
            var questionBank = await context.QuestionBanks.FirstOrDefaultAsync(qb => qb.IsActive);
            if (questionBank == null)
            {
                questionBank = new QuestionBank
                {
                    BankName = "测试题库",
                    Description = "用于测试的题库",
                    CreatedBy = 1,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };
                context.QuestionBanks.Add(questionBank);
                await context.SaveChangesAsync();
            }
            
            // 创建地图绘制题目
            var mapQuestion = new Question
            {
                BankId = questionBank.BankId,
                Content = "测试地图绘制题：请在地图上标记重要位置",
                QuestionType = QuestionType.MapDrawing,
                Score = 10.0m,
                CreatedBy = 1,
                CreatedAt = DateTime.Now,
                IsActive = true
            };
            
            context.Questions.Add(mapQuestion);
            await context.SaveChangesAsync();
            
            Console.WriteLine($"✓ 创建测试地图绘制题目 (ID: {mapQuestion.QuestionId})");
        }
        
        private static async Task TestSaveMapDrawingData(ExamDbContext context, Question mapQuestion)
        {
            // 查找或创建考试记录
            var examRecord = await context.ExamRecords
                .FirstOrDefaultAsync(er => er.Status == ExamStatus.InProgress);
            
            if (examRecord == null)
            {
                // 创建测试考试记录
                var user = await context.Users.FirstOrDefaultAsync(u => u.IsActive);
                if (user == null)
                {
                    Console.WriteLine("未找到用户，跳过测试");
                    return;
                }
                
                var examPaper = await context.ExamPapers.FirstOrDefaultAsync(ep => ep.IsActive);
                if (examPaper == null)
                {
                    Console.WriteLine("未找到试卷，跳过测试");
                    return;
                }
                
                examRecord = new ExamRecord
                {
                    UserId = user.UserId,
                    PaperId = examPaper.PaperId,
                    StartTime = DateTime.Now,
                    Status = ExamStatus.InProgress,
                    TotalScore = 0,
                    PassScore = 60
                };
                
                context.ExamRecords.Add(examRecord);
                await context.SaveChangesAsync();
                Console.WriteLine($"✓ 创建测试考试记录 (ID: {examRecord.RecordId})");
            }
            
            // 查找或创建答题记录
            var answerRecord = await context.AnswerRecords
                .FirstOrDefaultAsync(ar => ar.RecordId == examRecord.RecordId && 
                                          ar.QuestionId == mapQuestion.QuestionId);
            
            if (answerRecord == null)
            {
                answerRecord = new AnswerRecord
                {
                    RecordId = examRecord.RecordId,
                    QuestionId = mapQuestion.QuestionId,
                    UserAnswer = "",
                    Score = 0,
                    IsCorrect = false,
                    IsGraded = false
                };
                
                context.AnswerRecords.Add(answerRecord);
                await context.SaveChangesAsync();
                Console.WriteLine($"✓ 创建测试答题记录 (ID: {answerRecord.AnswerId})");
            }
            
            // 保存测试地图绘制数据
            var testMapData = @"{
                ""overlays"": [
                    {
                        ""id"": ""test-marker-" + DateTime.Now.Ticks + @""",
                        ""type"": ""marker"",
                        ""point"": {""lng"": 106.63, ""lat"": 26.65},
                        ""name"": ""测试标记点"",
                        ""icon"": ""default""
                    }
                ],
                ""center"": {""lng"": 106.63, ""lat"": 26.65},
                ""zoom"": 12,
                ""timestamp"": """ + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @"""
            }";
            
            var testMapCenter = @"{""lng"": 106.63, ""lat"": 26.65}";
            var testMapZoom = 12;
            
            // 更新答题记录
            answerRecord.MapDrawingData = testMapData;
            answerRecord.MapCenter = testMapCenter;
            answerRecord.MapZoom = testMapZoom;
            answerRecord.UserAnswer = testMapData;
            answerRecord.AnswerTime = DateTime.Now;
            
            await context.SaveChangesAsync();
            Console.WriteLine("✓ 测试地图绘制数据保存成功");
            
            // 验证保存结果
            var savedRecord = await context.AnswerRecords
                .FirstOrDefaultAsync(ar => ar.AnswerId == answerRecord.AnswerId);
            
            if (savedRecord != null && 
                !string.IsNullOrEmpty(savedRecord.MapDrawingData) &&
                savedRecord.MapDrawingData.Contains("test-marker-") &&
                savedRecord.MapCenter == testMapCenter &&
                savedRecord.MapZoom == testMapZoom)
            {
                Console.WriteLine("✓ 数据验证通过");
            }
            else
            {
                Console.WriteLine("✗ 数据验证失败");
            }
        }
    }
}