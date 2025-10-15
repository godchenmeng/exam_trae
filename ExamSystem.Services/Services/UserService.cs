using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;
using ExamSystem.Data;
using ExamSystem.Infrastructure.Repositories;
using ExamSystem.Services.Interfaces;
using OfficeOpenXml;
using System.Text;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using ExamSystem.Data;

namespace ExamSystem.Services.Services
{
    /// <summary>
    /// 用户服务实现
    /// </summary>
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserService> _logger;
    private readonly ExamDbContext _context;

    public UserService(IUserRepository userRepository, ILogger<UserService> logger, ExamDbContext context)
    {
        _userRepository = userRepository;
        _logger = logger;
        _context = context;
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        var users = await _userRepository.GetAllAsync();
        return users.OrderBy(u => u.CreatedAt).ToList();
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _userRepository.GetByIdAsync(userId);
    }

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        return await _userRepository.GetByUsernameAsync(username);
    }

    public async Task<List<User>> GetUsersByRoleAsync(UserRole role)
    {
        var users = await _userRepository.GetUsersByRoleAsync(role);
        return users.OrderBy(u => u.CreatedAt).ToList();
    }

    public async Task<(List<User> Users, int TotalCount)> GetUsersPagedAsync(int pageIndex, int pageSize, string? searchKeyword = null, UserRole? role = null)
    {
        var (users, totalCount) = await _userRepository.GetPagedAsync(
            pageIndex, 
            pageSize, 
            u => (string.IsNullOrWhiteSpace(searchKeyword) || 
                  u.Username.Contains(searchKeyword) ||
                  u.RealName.Contains(searchKeyword) ||
                  (u.Email != null && u.Email.Contains(searchKeyword))) &&
                 (!role.HasValue || u.Role == role.Value),
            u => u.CreatedAt);

        return (users.ToList(), totalCount);
    }

    public async Task<(bool Success, string Message)> CreateUserAsync(User user)
    {
        try
        {
            // 检查用户名是否已存在
            if (await _userRepository.IsUsernameExistsAsync(user.Username))
            {
                return (false, "用户名已存在");
            }

            // 检查邮箱是否已存在
            if (!string.IsNullOrWhiteSpace(user.Email) && 
                await _userRepository.IsEmailExistsAsync(user.Email))
            {
                return (false, "邮箱已被使用");
            }

            user.CreatedAt = DateTime.Now;
            await _userRepository.AddAsync(user);

            _logger.LogInformation("创建用户成功 - {Username}", user.Username);
            return (true, "用户创建成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建用户时发生错误 - {Username}", user.Username);
            return (false, "创建用户时发生错误");
        }
    }

    public async Task<bool> UpdateUserAsync(User user)
    {
        try
        {
            var existingUser = await _userRepository.GetByIdAsync(user.UserId);
            if (existingUser == null)
            {
                return false;
            }

            // 检查用户名是否被其他用户使用
            if (await _userRepository.IsUsernameExistsAsync(user.Username, user.UserId))
            {
                return false;
            }

            // 检查邮箱是否被其他用户使用
            if (!string.IsNullOrWhiteSpace(user.Email) && 
                await _userRepository.IsEmailExistsAsync(user.Email, user.UserId))
            {
                return false;
            }

            // 更新用户信息（保留密码和创建时间）
            existingUser.Username = user.Username;
            existingUser.RealName = user.RealName;
            existingUser.Role = user.Role;
            existingUser.Email = user.Email;
            existingUser.Phone = user.Phone;
            existingUser.IsActive = user.IsActive;

            _userRepository.Update(existingUser);
            await _context.SaveChangesAsync();

            _logger.LogInformation("更新用户信息成功 - UserId: {UserId}", user.UserId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新用户信息时发生错误 - UserId: {UserId}", user.UserId);
            return false;
        }
    }

    public async Task<bool> DeleteUserAsync(int userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            // 使用Repository检查关联数据
            // 这里需要添加检查关联数据的方法到Repository中
            // 暂时保留原有逻辑，后续可以优化
            
            await _userRepository.RemoveByIdAsync(userId);

            _logger.LogInformation("删除用户成功 - UserId: {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除用户时发生错误 - UserId: {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> ToggleUserStatusAsync(int userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            await _userRepository.SetUserActiveStatusAsync(userId, !user.IsActive);

            var status = !user.IsActive ? "激活" : "禁用";
            _logger.LogInformation("{Status}用户成功 - UserId: {UserId}", status, userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "切换用户状态时发生错误 - UserId: {UserId}", userId);
            return false;
        }
    }

    public async Task<(bool Success, string Message)> BatchDeleteUsersAsync(List<int> userIds)
    {
        try
        {
            var users = await _context.Users
                .Where(u => userIds.Contains(u.UserId))
                .ToListAsync();

            if (!users.Any())
            {
                return (false, "未找到要删除的用户");
            }

            // 检查是否有用户有关联数据
            foreach (var user in users)
            {
                var hasRelatedData = await _context.ExamRecords.AnyAsync(er => er.UserId == user.UserId) ||
                                   await _context.QuestionBanks.AnyAsync(qb => qb.CreatorId == user.UserId) ||
                                   await _context.ExamPapers.AnyAsync(ep => ep.CreatorId == user.UserId);

                if (hasRelatedData)
                {
                    return (false, $"用户 {user.Username} 有关联数据，无法删除");
                }
            }

            _context.Users.RemoveRange(users);
            await _context.SaveChangesAsync();

            _logger.LogInformation("批量删除用户成功 - Count: {Count}", users.Count);
            return (true, $"成功删除 {users.Count} 个用户");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量删除用户时发生错误");
            return (false, "批量删除用户时发生错误");
        }
    }

    public async Task<Dictionary<string, int>> GetUserStatisticsAsync()
    {
        var statistics = new Dictionary<string, int>();

        statistics["总用户数"] = await _context.Users.CountAsync();
        statistics["管理员数"] = await _context.Users.CountAsync(u => u.Role == UserRole.Admin);
        statistics["教师数"] = await _context.Users.CountAsync(u => u.Role == UserRole.Teacher);
        statistics["学生数"] = await _context.Users.CountAsync(u => u.Role == UserRole.Student);
        statistics["活跃用户数"] = await _context.Users.CountAsync(u => u.IsActive);
        statistics["禁用用户数"] = await _context.Users.CountAsync(u => !u.IsActive);

        return statistics;
    }

    public bool HasPermission(UserRole userRole, string operation)
    {
        return operation switch
        {
            "CreateUser" => userRole == UserRole.Admin,
            "UpdateUser" => userRole == UserRole.Admin,
            "DeleteUser" => userRole == UserRole.Admin,
            "ViewAllUsers" => userRole == UserRole.Admin,
            "CreateQuestionBank" => userRole == UserRole.Admin || userRole == UserRole.Teacher,
            "CreateExamPaper" => userRole == UserRole.Admin || userRole == UserRole.Teacher,
            "ViewExamResults" => userRole == UserRole.Admin || userRole == UserRole.Teacher,
            "TakeExam" => userRole == UserRole.Student,
            "ViewOwnResults" => true, // 所有用户都可以查看自己的成绩
            _ => false
        };
    }

    public async Task<byte[]> ExportUsersAsync()
    {
        try
        {
            var users = await GetAllUsersAsync();

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("用户列表");

            // 设置表头
            worksheet.Cells[1, 1].Value = "用户ID";
            worksheet.Cells[1, 2].Value = "用户名";
            worksheet.Cells[1, 3].Value = "真实姓名";
            worksheet.Cells[1, 4].Value = "角色";
            worksheet.Cells[1, 5].Value = "邮箱";
            worksheet.Cells[1, 6].Value = "电话";
            worksheet.Cells[1, 7].Value = "状态";
            worksheet.Cells[1, 8].Value = "创建时间";
            worksheet.Cells[1, 9].Value = "最后登录时间";

            // 填充数据
            for (int i = 0; i < users.Count; i++)
            {
                var user = users[i];
                var row = i + 2;

                worksheet.Cells[row, 1].Value = user.UserId;
                worksheet.Cells[row, 2].Value = user.Username;
                worksheet.Cells[row, 3].Value = user.RealName;
                worksheet.Cells[row, 4].Value = GetRoleDisplayName(user.Role);
                worksheet.Cells[row, 5].Value = user.Email ?? "";
                worksheet.Cells[row, 6].Value = user.Phone ?? "";
                worksheet.Cells[row, 7].Value = user.IsActive ? "激活" : "禁用";
                worksheet.Cells[row, 8].Value = user.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                worksheet.Cells[row, 9].Value = user.LastLoginAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
            }

            // 自动调整列宽
            worksheet.Cells.AutoFitColumns();

            return package.GetAsByteArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出用户数据时发生错误");
            throw;
        }
    }

    public async Task<(bool Success, string Message, int ImportedCount)> ImportUsersAsync(byte[] fileData)
    {
        try
        {
            using var package = new ExcelPackage(new MemoryStream(fileData));
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();

            if (worksheet == null)
            {
                return (false, "Excel文件格式错误", 0);
            }

            var importedCount = 0;
            var errors = new List<string>();

            // 从第二行开始读取数据（第一行是表头）
            for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
            {
                try
                {
                    var username = worksheet.Cells[row, 2].Value?.ToString()?.Trim();
                    var realName = worksheet.Cells[row, 3].Value?.ToString()?.Trim();
                    var roleText = worksheet.Cells[row, 4].Value?.ToString()?.Trim();
                    var email = worksheet.Cells[row, 5].Value?.ToString()?.Trim();
                    var phone = worksheet.Cells[row, 6].Value?.ToString()?.Trim();

                    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(realName))
                    {
                        errors.Add($"第{row}行：用户名和真实姓名不能为空");
                        continue;
                    }

                    // 检查用户名是否已存在
                    if (await _context.Users.AnyAsync(u => u.Username == username))
                    {
                        errors.Add($"第{row}行：用户名 {username} 已存在");
                        continue;
                    }

                    // 解析角色
                    if (!TryParseRole(roleText, out var role))
                    {
                        errors.Add($"第{row}行：角色 {roleText} 无效");
                        continue;
                    }

                    var user = new User
                    {
                        Username = username,
                        RealName = realName,
                        Role = role,
                        Email = string.IsNullOrWhiteSpace(email) ? null : email,
                        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"), // 默认密码
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };

                    _context.Users.Add(user);
                    importedCount++;
                }
                catch (Exception ex)
                {
                    errors.Add($"第{row}行：{ex.Message}");
                }
            }

            if (importedCount > 0)
            {
                await _context.SaveChangesAsync();
            }

            var message = $"成功导入 {importedCount} 个用户";
            if (errors.Any())
            {
                message += $"，{errors.Count} 个错误：\n" + string.Join("\n", errors.Take(10));
                if (errors.Count > 10)
                {
                    message += $"\n...还有 {errors.Count - 10} 个错误";
                }
            }

            return (true, message, importedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导入用户数据时发生错误");
            return (false, "导入用户数据时发生错误", 0);
        }
    }

    private static string GetRoleDisplayName(UserRole role)
    {
        return role switch
        {
            UserRole.Admin => "管理员",
            UserRole.Teacher => "教师",
            UserRole.Student => "学生",
            _ => "未知"
        };
    }

    private static bool TryParseRole(string? roleText, out UserRole role)
    {
        role = UserRole.Student;

        if (string.IsNullOrWhiteSpace(roleText))
            return false;

        return roleText switch
        {
            "管理员" or "Admin" => (role = UserRole.Admin) == UserRole.Admin,
            "教师" or "Teacher" => (role = UserRole.Teacher) == UserRole.Teacher,
            "学生" or "Student" => (role = UserRole.Student) == UserRole.Student,
            _ => false
        };
    }
}}