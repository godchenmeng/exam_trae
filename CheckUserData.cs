using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ExamSystem.Data;
using ExamSystem.Domain.Entities;

namespace ExamSystem.WPF
{
    class CheckUserData
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("检查用户数据程序");
            Console.WriteLine("================");
            
            var connectionString = "Data Source=exam_system.db";
            var options = new DbContextOptionsBuilder<ExamDbContext>()
                .UseSqlite(connectionString)
                .Options;
                
            using var context = new ExamDbContext(options);
            
            try
            {
                // 检查数据库连接
                var canConnect = await context.Database.CanConnectAsync();
                Console.WriteLine($"数据库连接状态: {canConnect}");
                
                if (!canConnect)
                {
                    Console.WriteLine("无法连接到数据库");
                    return;
                }
                
                // 获取所有用户
                var users = await context.Users.ToListAsync();
                Console.WriteLine($"数据库中用户总数: {users.Count}");
                
                foreach (var user in users)
                {
                    Console.WriteLine($"\n用户信息:");
                    Console.WriteLine($"  ID: {user.UserId}");
                    Console.WriteLine($"  用户名: {user.Username}");
                    Console.WriteLine($"  真实姓名: {user.RealName}");
                    Console.WriteLine($"  角色: {user.Role}");
                    Console.WriteLine($"  是否激活: {user.IsActive}");
                    Console.WriteLine($"  登录失败次数: {user.LoginFailCount}");
                    Console.WriteLine($"  密码哈希是否为空: {string.IsNullOrEmpty(user.PasswordHash)}");
                    Console.WriteLine($"  密码哈希长度: {user.PasswordHash?.Length ?? 0}");
                    
                    if (!string.IsNullOrEmpty(user.PasswordHash))
                    {
                        Console.WriteLine($"  密码哈希前20字符: {user.PasswordHash.Substring(0, Math.Min(20, user.PasswordHash.Length))}...");
                        
                        // 测试密码验证
                        if (user.Username == "admin")
                        {
                            try
                            {
                                bool isValid = BCrypt.Net.BCrypt.Verify("admin123", user.PasswordHash);
                                Console.WriteLine($"  admin123 密码验证结果: {isValid}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"  密码验证异常: {ex.Message}");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("  警告: 密码哈希为空!");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发生异常: {ex.Message}");
                Console.WriteLine($"异常详情: {ex}");
            }
            
            Console.WriteLine("\n检查完成，按任意键退出...");
            Console.ReadKey();
        }
    }
}