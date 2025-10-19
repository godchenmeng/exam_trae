using System;
using System.Linq;
using System.Threading.Tasks;
using ExamSystem.Data;
using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExamSystem.WPF
{
    public class DatabaseSeeder
    {
        private readonly ExamDbContext _context;
        private readonly ILogger<DatabaseSeeder> _logger;

        public DatabaseSeeder(ExamDbContext context, ILogger<DatabaseSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            try
            {
                // 确保数据库已创建
                await _context.Database.EnsureCreatedAsync();

                // 检查是否已有管理员用户
                if (!await _context.Users.AnyAsync(u => u.Role == UserRole.Admin))
                {
                    _logger.LogInformation("创建默认管理员用户");

                    var adminUser = new User
                    {
                        Username = "admin",
                        RealName = "系统管理员",
                        Email = "admin@exam.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123", 12),
                        Role = UserRole.Admin,
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        LoginFailCount = 0
                    };

                    _context.Users.Add(adminUser);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("默认管理员用户创建成功 - 用户名: admin, 密码: admin123");
                }

                // 创建测试学生用户
                if (!await _context.Users.AnyAsync(u => u.Role == UserRole.Student))
                {
                    _logger.LogInformation("创建测试学生用户");

                    var studentUser = new User
                    {
                        Username = "student",
                        RealName = "测试学生",
                        Email = "student@exam.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("student123", 12),
                        Role = UserRole.Student,
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        LoginFailCount = 0
                    };

                    _context.Users.Add(studentUser);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("测试学生用户创建成功 - 用户名: student, 密码: student123");
                }

                // 创建测试教师用户
                if (!await _context.Users.AnyAsync(u => u.Role == UserRole.Teacher))
                {
                    _logger.LogInformation("创建测试教师用户");

                    var teacherUser = new User
                    {
                        Username = "teacher",
                        RealName = "测试教师",
                        Email = "teacher@exam.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("teacher123", 12),
                        Role = UserRole.Teacher,
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        LoginFailCount = 0
                    };

                    _context.Users.Add(teacherUser);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("测试教师用户创建成功 - 用户名: teacher, 密码: teacher123");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "数据库种子数据初始化失败");
                throw;
            }
        }
    }
}