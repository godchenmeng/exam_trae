using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using ExamSystem.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ExamSystem.Domain.Entities;
using ExamSystem.Infrastructure.Repositories;
using ExamSystem.Services.Interfaces;
using System.Text.RegularExpressions;

namespace ExamSystem.Services.Services
{
    /// <summary>
    /// 认证服务实现
    /// </summary>
    public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<AuthService> _logger;
    private readonly ExamDbContext _context;
    private const int MaxLoginAttempts = 5;
    private const int LockoutMinutes = 30;

    public AuthService(IUserRepository userRepository, ILogger<AuthService> logger, ExamDbContext context)
    {
        _userRepository = userRepository;
        _logger = logger;
        _context = context;
    }

    public async Task<(bool Success, User? User, string Message)> LoginAsync(string username, string password)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return (false, null, "用户名和密码不能为空");
            }

            var user = await _userRepository.GetByUsernameAsync(username);

            if (user == null)
            {
                _logger.LogWarning("登录失败：用户名不存在 - {Username}", username);
                return (false, null, "用户名或密码错误");
            }

            // 检查账户是否被锁定
            if (await IsUserLockedAsync(user.UserId))
            {
                return (false, null, "账户已被锁定，请稍后再试");
            }

            // 检查账户是否激活
            if (!user.IsActive)
            {
                return (false, null, "账户已被禁用，请联系管理员");
            }

            // 验证密码
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                // 增加登录失败次数
                user.LoginFailCount++;
                
                // 如果失败次数达到上限，锁定账户
                if (user.LoginFailCount >= MaxLoginAttempts)
                {
                    var lockUntil = DateTime.Now.AddMinutes(LockoutMinutes);
                    await LockUserAsync(user.UserId, lockUntil);
                    _logger.LogWarning("用户账户被锁定 - {Username}", username);
                    await _context.SaveChangesAsync();
                    return (false, null, $"登录失败次数过多，账户已被锁定{LockoutMinutes}分钟");
                }

                await _context.SaveChangesAsync();
                _logger.LogWarning("登录失败：密码错误 - {Username}", username);
                return (false, null, "用户名或密码错误");
            }

            // 登录成功，重置失败次数和更新登录时间
            user.LoginFailCount = 0;
            user.LastLoginAt = DateTime.Now;
            user.LockoutEnd = null;
            
            await _context.SaveChangesAsync();

            _logger.LogInformation("用户登录成功 - {Username}", username);
            return (true, user, "登录成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "登录过程中发生错误 - {Username}", username);
            return (false, null, "登录过程中发生错误，请稍后再试");
        }
    }

    public async Task<(bool Success, string Message)> RegisterAsync(User user, string password)
    {
        try
        {
            // 验证用户名是否已存在
            if (await IsUsernameExistsAsync(user.Username))
            {
                return (false, "用户名已存在");
            }

            // 验证密码强度
            var passwordValidation = ValidatePassword(password);
            if (!passwordValidation.IsValid)
            {
                return (false, passwordValidation.Message);
            }

            // 加密密码
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            user.CreatedAt = DateTime.Now;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("用户注册成功 - {Username}", user.Username);
            return (true, "注册成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "用户注册过程中发生错误 - {Username}", user.Username);
            return (false, "注册过程中发生错误，请稍后再试");
        }
    }

    public async Task<(bool Success, string Message)> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return (false, "用户不存在");
            }

            // 验证旧密码
            if (!BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash))
            {
                return (false, "原密码错误");
            }

            // 验证新密码强度
            var passwordValidation = ValidatePassword(newPassword);
            if (!passwordValidation.IsValid)
            {
                return (false, passwordValidation.Message);
            }

            // 更新密码
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _context.SaveChangesAsync();

            _logger.LogInformation("用户修改密码成功 - UserId: {UserId}", userId);
            return (true, "密码修改成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "修改密码过程中发生错误 - UserId: {UserId}", userId);
            return (false, "修改密码过程中发生错误，请稍后再试");
        }
    }

    public async Task<(bool Success, string Message)> ResetPasswordAsync(int userId, string newPassword)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return (false, "用户不存在");
            }

            // 验证新密码强度
            var passwordValidation = ValidatePassword(newPassword);
            if (!passwordValidation.IsValid)
            {
                return (false, passwordValidation.Message);
            }

            // 重置密码
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.LoginFailCount = 0;
            user.LockoutEnd = null;
            
            await _context.SaveChangesAsync();

            _logger.LogInformation("管理员重置用户密码 - UserId: {UserId}", userId);
            return (true, "密码重置成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重置密码过程中发生错误 - UserId: {UserId}", userId);
            return (false, "重置密码过程中发生错误，请稍后再试");
        }
    }

    public (bool IsValid, string Message) ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return (false, "密码不能为空");
        }

        if (password.Length < 6)
        {
            return (false, "密码长度至少6位");
        }

        if (password.Length > 20)
        {
            return (false, "密码长度不能超过20位");
        }

        // 检查是否包含常见弱密码
        var weakPasswords = new[] { "123456", "password", "admin", "123123", "111111", "000000" };
        if (weakPasswords.Contains(password.ToLower()))
        {
            return (false, "密码过于简单，请使用更复杂的密码");
        }

        // 建议包含数字和字母
        bool hasDigit = password.Any(char.IsDigit);
        bool hasLetter = password.Any(char.IsLetter);

        if (!hasDigit || !hasLetter)
        {
            return (false, "建议密码包含数字和字母");
        }

        return (true, "密码符合要求");
    }

    public async Task<bool> IsUsernameExistsAsync(string username)
    {
        return await _context.Users.AnyAsync(u => u.Username == username);
    }

    public async Task<bool> LockUserAsync(int userId, DateTime lockUntil)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            user.LockoutEnd = lockUntil;
            await _context.SaveChangesAsync();

            _logger.LogInformation("用户账户已锁定至 {LockUntil} - UserId: {UserId}", lockUntil, userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "锁定用户账户时发生错误 - UserId: {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> UnlockUserAsync(int userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            user.LockoutEnd = null;
            user.LoginFailCount = 0;
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解锁用户账户时发生错误 - UserId: {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> IsUserLockedAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        return user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.Now;
    }

    public User? GetCurrentUser()
    {
        // TODO: 实现获取当前用户的逻辑
        return null;
    }

    public void Logout()
    {
        // TODO: 实现登出逻辑
    }
    }
}