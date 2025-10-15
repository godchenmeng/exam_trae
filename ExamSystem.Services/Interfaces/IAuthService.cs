using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using ExamSystem.Domain.Entities;

namespace ExamSystem.Services.Interfaces
{
    /// <summary>
    /// 认证服务接口
    /// </summary>
    public interface IAuthService
{
    /// <summary>
    /// 用户登录
    /// </summary>
    /// <param name="username">用户名</param>
    /// <param name="password">密码</param>
    /// <returns>登录结果</returns>
    Task<(bool Success, User? User, string Message)> LoginAsync(string username, string password);

    /// <summary>
    /// 用户注册
    /// </summary>
    /// <param name="user">用户信息</param>
    /// <param name="password">密码</param>
    /// <returns>注册结果</returns>
    Task<(bool Success, string Message)> RegisterAsync(User user, string password);

    /// <summary>
    /// 修改密码
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="oldPassword">旧密码</param>
    /// <param name="newPassword">新密码</param>
    /// <returns>修改结果</returns>
    Task<(bool Success, string Message)> ChangePasswordAsync(int userId, string oldPassword, string newPassword);

    /// <summary>
    /// 重置密码
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="newPassword">新密码</param>
    /// <returns>重置结果</returns>
    Task<(bool Success, string Message)> ResetPasswordAsync(int userId, string newPassword);

    /// <summary>
    /// 验证密码强度
    /// </summary>
    /// <param name="password">密码</param>
    /// <returns>验证结果</returns>
    (bool IsValid, string Message) ValidatePassword(string password);

    /// <summary>
    /// 检查用户名是否存在
    /// </summary>
    /// <param name="username">用户名</param>
    /// <returns>是否存在</returns>
    Task<bool> IsUsernameExistsAsync(string username);

    /// <summary>
    /// 锁定用户账户
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="lockUntil">锁定截止时间</param>
    /// <returns>锁定结果</returns>
    Task<bool> LockUserAsync(int userId, DateTime lockUntil);

    /// <summary>
    /// 解锁用户账户
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>解锁结果</returns>
    Task<bool> UnlockUserAsync(int userId);

    /// <summary>
    /// 检查用户是否被锁定
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>是否被锁定</returns>
    Task<bool> IsUserLockedAsync(int userId);

    /// <summary>
    /// 获取当前登录用户
    /// </summary>
    /// <returns>当前用户</returns>
    User? GetCurrentUser();

    /// <summary>
    /// 用户退出登录
    /// </summary>
    void Logout();
    }
}