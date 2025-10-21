using Microsoft.EntityFrameworkCore;
using ExamSystem.Infrastructure.Data;
using ExamSystem.Domain.Entities;

namespace ExamSystem.Tools
{
    class CheckQuestionBanks
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("检查题库数据...");
            
            var connectionString = "Data Source=exam_system.db";
            var options = new DbContextOptionsBuilder<ExamDbContext>()
                .UseSqlite(connectionString)
                .Options;

            try
            {
                using var context = new ExamDbContext(options);
                
                // 检查数据库是否存在
                var canConnect = await context.Database.CanConnectAsync();
                Console.WriteLine($"数据库连接状态: {(canConnect ? "成功" : "失败")}");
                
                if (!canConnect)
                {
                    Console.WriteLine("无法连接到数据库，请检查数据库文件是否存在");
                    return;
                }
                
                // 检查题库表是否存在
                var questionBanks = await context.QuestionBanks.ToListAsync();
                Console.WriteLine($"题库总数: {questionBanks.Count}");
                
                if (questionBanks.Count == 0)
                {
                    Console.WriteLine("数据库中没有题库数据，正在创建默认题库...");
                    
                    var defaultBank = new QuestionBank
                    {
                        Name = "默认题库",
                        Description = "系统默认题库",
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };
                    
                    context.QuestionBanks.Add(defaultBank);
                    await context.SaveChangesAsync();
                    
                    Console.WriteLine("默认题库创建成功");
                }
                else
                {
                    Console.WriteLine("现有题库列表:");
                    foreach (var bank in questionBanks)
                    {
                        Console.WriteLine($"- ID: {bank.BankId}, 名称: {bank.Name}, 状态: {(bank.IsActive ? "启用" : "禁用")}, 创建时间: {bank.CreatedAt:yyyy-MM-dd}");
                    }
                }
                
                // 检查题目数量
                var questionCount = await context.Questions.CountAsync();
                Console.WriteLine($"题目总数: {questionCount}");
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"检查过程中发生错误: {ex.Message}");
                Console.WriteLine($"详细错误: {ex}");
            }
            
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }
    }
}