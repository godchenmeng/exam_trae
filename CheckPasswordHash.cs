using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using ExamSystem.Infrastructure.Data;
using ExamSystem.Domain.Entities;

namespace CheckPasswordHash
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("检查数据库中的密码哈希...");
            
            var options = new DbContextOptionsBuilder<ExamDbContext>()
                .UseSqlite("Data Source=ExamSystem.WPF/exam_system.db")
                .Options;

            using var context = new ExamDbContext(options);
            
            try
            {
                var users = context.Users.ToList();
                Console.WriteLine($"找到 {users.Count} 个用户");
                
                foreach (var user in users)
                {
                    Console.WriteLine($"\n用户: {user.Username}");
                    Console.WriteLine($"角色: {user.Role}");
                    Console.WriteLine($"密码哈希长度: {user.PasswordHash?.Length ?? 0}");
                    Console.WriteLine($"密码哈希前20字符: {user.PasswordHash?.Substring(0, Math.Min(20, user.PasswordHash.Length ?? 0)) ?? "NULL"}");
                    
                    // 检查哈希格式
                    if (!string.IsNullOrEmpty(user.PasswordHash))
                    {
                        if (user.PasswordHash.StartsWith("$2a$") || user.PasswordHash.StartsWith("$2b$") || user.PasswordHash.StartsWith("$2y$"))
                        {
                            Console.WriteLine("哈希格式: BCrypt 格式正确");
                            
                            // 测试验证
                            try
                            {
                                string testPassword = user.Username == "admin" ? "admin123" : 
                                                    user.Username == "student" ? "student123" : 
                                                    user.Username == "teacher" ? "teacher123" : "unknown";
                                
                                bool isValid = BCrypt.Net.BCrypt.Verify(testPassword, user.PasswordHash);
                                Console.WriteLine($"密码验证测试 ({testPassword}): {(isValid ? "成功" : "失败")}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"密码验证异常: {ex.Message}");
                            }
                        }
                        else
                        {
                            Console.WriteLine("哈希格式: 不是标准 BCrypt 格式");
                        }
                    }
                    else
                    {
                        Console.WriteLine("密码哈希: 为空或NULL");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"数据库操作异常: {ex.Message}");
            }
            
            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }
    }
}