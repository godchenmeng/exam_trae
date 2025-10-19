using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ExamSystem.Data;
using ExamSystem.Infrastructure.Repositories;
using ExamSystem.Services.Services;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== 数据库连接测试开始 ===");
        
        try
        {
            var options = new DbContextOptionsBuilder<ExamDbContext>()
                .UseSqlite("Data Source=ExamSystem.WPF/exam_system.db")
                .Options;

            using var context = new ExamDbContext(options);
            
            // 测试数据库连接
            Console.WriteLine("测试数据库连接...");
            var canConnect = await context.Database.CanConnectAsync();
            Console.WriteLine($"数据库连接结果: {canConnect}");
            
            if (!canConnect)
            {
                Console.WriteLine("❌ 无法连接到数据库");
                return;
            }
            
            // 检查用户表数据
            Console.WriteLine("检查用户表数据...");
            var userCount = await context.Users.CountAsync();
            Console.WriteLine($"用户总数: {userCount}");
            
            if (userCount == 0)
            {
                Console.WriteLine("❌ 数据库中没有用户数据");
                return;
            }
            
            // 列出所有用户
            var users = await context.Users.ToListAsync();
            Console.WriteLine("用户列表:");
            foreach (var user in users)
            {
                Console.WriteLine($"- ID: {user.UserId}, 用户名: {user.Username}, 角色: {user.Role}, 激活: {user.IsActive}");
                Console.WriteLine($"  密码哈希长度: {user.PasswordHash?.Length ?? 0}");
            }
            
            // 测试登录服务
            Console.WriteLine("\n测试登录服务...");
            var userRepository = new UserRepository(context);
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
            var logger = loggerFactory.CreateLogger<AuthService>();
            var authService = new AuthService(userRepository, context, logger);
            
            // 测试管理员登录
            Console.WriteLine("测试管理员登录 (admin/admin123)...");
            var result = await authService.LoginAsync("admin", "admin123");
            Console.WriteLine($"登录结果: Success={result.Success}, Message={result.Message}");
            if (result.User != null)
            {
                Console.WriteLine($"用户信息: {result.User.Username}, {result.User.Role}");
            }
            
            // 测试学生登录
            Console.WriteLine("\n测试学生登录 (student1/student123)...");
            var studentResult = await authService.LoginAsync("student1", "student123");
            Console.WriteLine($"登录结果: Success={studentResult.Success}, Message={studentResult.Message}");
            if (studentResult.User != null)
            {
                Console.WriteLine($"用户信息: {studentResult.User.Username}, {studentResult.User.Role}");
            }
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 测试过程中发生异常:");
            Console.WriteLine($"异常类型: {ex.GetType().Name}");
            Console.WriteLine($"异常消息: {ex.Message}");
            Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"内部异常: {ex.InnerException.Message}");
            }
        }
        
        Console.WriteLine("=== 数据库连接测试结束 ===");
        Console.WriteLine("按任意键退出...");
        Console.ReadKey();
    }
}