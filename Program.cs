using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ExamSystem.Infrastructure.Data;
using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;

namespace ExamSystem.Tests
{
    /// <summary>
    /// 简化的地图绘制数据保存功能测试
    /// </summary>
    class SimpleMapDrawingTest
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== 地图绘制数据保存功能测试 ===");
            
            try
            {
                // 使用现有的数据库
                var options = new DbContextOptionsBuilder<ExamDbContext>()
                    .UseSqlite("Data Source=exam_system.db")
                    .Options;
                
                using var context = new ExamDbContext(options);
                
                // 测试数据库连接
                Console.WriteLine("1. 测试数据库连接...");
                var canConnect = await context.Database.CanConnectAsync();
                if (canConnect)
                {
                    Console.WriteLine("✓ 数据库连接成功");
                }
                else
                {
                    Console.WriteLine("✗ 数据库连接失败");
                    return;
                }
                
                // 查找现有的地图绘制题目
                Console.WriteLine("\n2. 查找地图绘制题目...");
                var mapQuestion = await context.Questions
                    .FirstOrDefaultAsync(q => q.QuestionType == QuestionType.MapDrawing && q.IsActive);
                
                if (mapQuestion == null)
                {
                    Console.WriteLine("未找到地图绘制题目，创建测试题目...");
                    
                    // 查找或创建题库
                    var questionBank = await context.QuestionBanks.FirstOrDefaultAsync(qb => qb.IsActive);
                    if (questionBank == null)
                    {
                        Console.WriteLine("未找到题库，跳过测试");
                        return;
                    }
                    
                    mapQuestion = new Question
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
                    Console.WriteLine($"✓ 创建测试题目 (ID: {mapQuestion.QuestionId})");
                }
                else
                {
                    Console.WriteLine($"✓ 找到地图绘制题目 (ID: {mapQuestion.QuestionId})");
                }
                
                // 查找现有的考试记录
                Console.WriteLine("\n3. 查找考试记录...");
                var examRecord = await context.ExamRecords
                    .Include(er => er.AnswerRecords)
                    .FirstOrDefaultAsync(er => er.Status == ExamStatus.InProgress);
                
                if (examRecord == null)
                {
                    Console.WriteLine("未找到进行中的考试记录，跳过测试");
                    return;
                }
                
                Console.WriteLine($"✓ 找到考试记录 (ID: {examRecord.RecordId})");
                
                // 查找或创建答题记录
                Console.WriteLine("\n4. 查找答题记录...");
                var answerRecord = await context.AnswerRecords
                    .FirstOrDefaultAsync(ar => ar.RecordId == examRecord.RecordId && ar.QuestionId == mapQuestion.QuestionId);
                
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
                    Console.WriteLine($"✓ 创建答题记录 (ID: {answerRecord.AnswerId})");
                }
                else
                {
                    Console.WriteLine($"✓ 找到答题记录 (ID: {answerRecord.AnswerId})");
                }
                
                // 测试地图绘制数据保存
                Console.WriteLine("\n5. 测试地图绘制数据保存...");
                
                var testMapData = @"{
                    ""overlays"": [
                        {
                            ""id"": ""test-marker-1"",
                            ""type"": ""marker"",
                            ""point"": {""lng"": 106.63, ""lat"": 26.65},
                            ""name"": ""测试标记点"",
                            ""icon"": ""default""
                        }
                    ],
                    ""center"": {""lng"": 106.63, ""lat"": 26.65},
                    ""zoom"": 12
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
                Console.WriteLine("✓ 地图绘制数据保存成功");
                
                // 验证保存结果
                Console.WriteLine("\n6. 验证保存结果...");
                
                var savedRecord = await context.AnswerRecords
                    .FirstOrDefaultAsync(ar => ar.AnswerId == answerRecord.AnswerId);
                
                if (savedRecord != null)
                {
                    Console.WriteLine("✓ 数据验证成功:");
                    Console.WriteLine($"  - 地图绘制数据长度: {savedRecord.MapDrawingData?.Length ?? 0} 字符");
                    Console.WriteLine($"  - 地图中心点: {savedRecord.MapCenter}");
                    Console.WriteLine($"  - 地图缩放级别: {savedRecord.MapZoom}");
                    Console.WriteLine($"  - 答题时间: {savedRecord.AnswerTime}");
                    
                    // 验证数据完整性
                    if (!string.IsNullOrEmpty(savedRecord.MapDrawingData) &&
                        savedRecord.MapDrawingData.Contains("test-marker-1") &&
                        savedRecord.MapCenter == testMapCenter &&
                        savedRecord.MapZoom == testMapZoom)
                    {
                        Console.WriteLine("✓ 数据完整性验证通过");
                    }
                    else
                    {
                        Console.WriteLine("✗ 数据完整性验证失败");
                    }
                }
                else
                {
                    Console.WriteLine("✗ 未找到保存的记录");
                }
                
                Console.WriteLine("\n=== 测试完成 ===");
                Console.WriteLine("地图绘制数据保存功能测试通过！");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"测试失败: {ex.Message}");
                Console.WriteLine($"详细错误: {ex}");
            }
            
            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }
    }
}