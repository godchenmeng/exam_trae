using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;

namespace ExamSystem.Infrastructure.Repositories
{
    /// <summary>
    /// 用户Repository接口
    /// </summary>
    public interface IUserRepository : IRepository<User>
    {
        /// <summary>
        /// 根据用户名获取用户
        /// </summary>
        /// <param name="username">用户名</param>
        /// <returns>用户实体</returns>
        Task<User?> GetByUsernameAsync(string username);

        /// <summary>
        /// 根据邮箱获取用户
        /// </summary>
        /// <param name="email">邮箱</param>
        /// <returns>用户实体</returns>
        Task<User?> GetByEmailAsync(string email);

        /// <summary>
        /// 检查用户名是否存在
        /// </summary>
        /// <param name="username">用户名</param>
        /// <param name="excludeUserId">排除的用户ID（用于更新时检查）</param>
        /// <returns>是否存在</returns>
        Task<bool> IsUsernameExistsAsync(string username, int? excludeUserId = null);

        /// <summary>
        /// 检查邮箱是否存在
        /// </summary>
        /// <param name="email">邮箱</param>
        /// <param name="excludeUserId">排除的用户ID（用于更新时检查）</param>
        /// <returns>是否存在</returns>
        Task<bool> IsEmailExistsAsync(string email, int? excludeUserId = null);

        /// <summary>
        /// 根据角色获取用户列表
        /// </summary>
        /// <param name="role">用户角色</param>
        /// <returns>用户列表</returns>
        Task<IEnumerable<User>> GetUsersByRoleAsync(UserRole role);

        /// <summary>
        /// 获取活跃用户列表
        /// </summary>
        /// <returns>活跃用户列表</returns>
        Task<IEnumerable<User>> GetActiveUsersAsync();

        /// <summary>
        /// 搜索用户
        /// </summary>
        /// <param name="keyword">搜索关键词</param>
        /// <param name="role">角色筛选（可选）</param>
        /// <param name="isActive">状态筛选（可选）</param>
        /// <returns>符合条件的用户列表</returns>
        Task<IEnumerable<User>> SearchUsersAsync(string keyword, UserRole? role = null, bool? isActive = null);

        /// <summary>
        /// 更新用户最后登录信息
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="loginTime">登录时间</param>
        /// <param name="ipAddress">IP地址</param>
        Task UpdateLastLoginAsync(int userId, DateTime loginTime, string ipAddress);

        /// <summary>
        /// 锁定/解锁用户
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="isLocked">是否锁定</param>
        /// <param name="lockReason">锁定原因</param>
        /// <param name="lockUntil">锁定到期时间</param>
        Task SetUserLockStatusAsync(int userId, bool isLocked, string? lockReason = null, DateTime? lockUntil = null);

        /// <summary>
        /// 激活/停用用户
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="isActive">是否激活</param>
        Task SetUserActiveStatusAsync(int userId, bool isActive);

        /// <summary>
        /// 更新用户密码
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="passwordHash">新密码哈希</param>
        Task UpdatePasswordAsync(int userId, string passwordHash);

        /// <summary>
        /// 获取用户统计信息
        /// </summary>
        /// <returns>用户统计信息</returns>
        Task<(int TotalUsers, int ActiveUsers, int LockedUsers, int AdminUsers, int TeacherUsers, int StudentUsers)> GetUserStatisticsAsync();
    }
}