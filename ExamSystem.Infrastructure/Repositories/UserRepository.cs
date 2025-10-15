using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ExamSystem.Data;
using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;

namespace ExamSystem.Infrastructure.Repositories
{
    /// <summary>
    /// 用户Repository实现类
    /// </summary>
    public class UserRepository : BaseRepository<User>, IUserRepository
    {
        public UserRepository(ExamDbContext context) : base(context)
        {
        }

        /// <summary>
        /// 根据用户名获取用户
        /// </summary>
        public async Task<User?> GetByUsernameAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return null;

            return await _dbSet.FirstOrDefaultAsync(u => u.Username == username);
        }

        /// <summary>
        /// 根据邮箱获取用户
        /// </summary>
        public async Task<User?> GetByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            return await _dbSet.FirstOrDefaultAsync(u => u.Email == email);
        }

        /// <summary>
        /// 检查用户名是否存在
        /// </summary>
        public async Task<bool> IsUsernameExistsAsync(string username, int? excludeUserId = null)
        {
            if (string.IsNullOrWhiteSpace(username))
                return false;

            var query = _dbSet.Where(u => u.Username == username);
            
            if (excludeUserId.HasValue)
            {
                query = query.Where(u => u.UserId != excludeUserId.Value);
            }

            return await query.AnyAsync();
        }

        /// <summary>
        /// 检查邮箱是否存在
        /// </summary>
        public async Task<bool> IsEmailExistsAsync(string email, int? excludeUserId = null)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            var query = _dbSet.Where(u => u.Email == email);
            
            if (excludeUserId.HasValue)
            {
                query = query.Where(u => u.UserId != excludeUserId.Value);
            }

            return await query.AnyAsync();
        }

        /// <summary>
        /// 根据角色获取用户列表
        /// </summary>
        public async Task<IEnumerable<User>> GetUsersByRoleAsync(UserRole role)
        {
            return await _dbSet.Where(u => u.Role == role).ToListAsync();
        }

        /// <summary>
        /// 获取活跃用户列表
        /// </summary>
        public async Task<IEnumerable<User>> GetActiveUsersAsync()
        {
            return await _dbSet.Where(u => u.IsActive && !u.IsLocked).ToListAsync();
        }

        /// <summary>
        /// 搜索用户
        /// </summary>
        public async Task<IEnumerable<User>> SearchUsersAsync(string keyword, UserRole? role = null, bool? isActive = null)
        {
            var query = _dbSet.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(u => u.Username.Contains(keyword) || 
                                        u.RealName.Contains(keyword) || 
                                        u.Email.Contains(keyword));
            }

            if (role.HasValue)
            {
                query = query.Where(u => u.Role == role.Value);
            }

            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }

            return await query.OrderBy(u => u.Username).ToListAsync();
        }

        /// <summary>
        /// 更新用户最后登录信息
        /// </summary>
        public async Task UpdateLastLoginAsync(int userId, DateTime loginTime, string ipAddress)
        {
            var user = await GetByIdAsync(userId);
            if (user != null)
            {
                user.LastLoginAt = loginTime;
                // Note: LastLoginIp property doesn't exist in User entity, skipping
                await UpdateAsync(user);
            }
        }

        /// <summary>
        /// 锁定/解锁用户
        /// </summary>
        public async Task SetUserLockStatusAsync(int userId, bool isLocked, string? lockReason = null, DateTime? lockUntil = null)
        {
            var user = await GetByIdAsync(userId);
            if (user != null)
            {
                user.LockoutEnd = isLocked ? lockUntil : null;
                // Note: LockReason property doesn't exist in User entity, skipping
                await UpdateAsync(user);
            }
        }

        /// <summary>
        /// 激活/停用用户
        /// </summary>
        public async Task SetUserActiveStatusAsync(int userId, bool isActive)
        {
            var user = await GetByIdAsync(userId);
            if (user != null)
            {
                user.IsActive = isActive;
                await UpdateAsync(user);
            }
        }

        /// <summary>
        /// 更新用户密码
        /// </summary>
        public async Task UpdatePasswordAsync(int userId, string passwordHash)
        {
            var user = await GetByIdAsync(userId);
            if (user != null)
            {
                user.PasswordHash = passwordHash;
                await UpdateAsync(user);
            }
        }

        /// <summary>
        /// 获取用户统计信息
        /// </summary>
        public async Task<(int TotalUsers, int ActiveUsers, int LockedUsers, int AdminUsers, int TeacherUsers, int StudentUsers)> GetUserStatisticsAsync()
        {
            var totalUsers = await _dbSet.CountAsync();
            var activeUsers = await _dbSet.CountAsync(u => u.IsActive);
            var lockedUsers = await _dbSet.CountAsync(u => u.IsLocked);
            var adminUsers = await _dbSet.CountAsync(u => u.Role == UserRole.Admin);
            var teacherUsers = await _dbSet.CountAsync(u => u.Role == UserRole.Teacher);
            var studentUsers = await _dbSet.CountAsync(u => u.Role == UserRole.Student);

            return (totalUsers, activeUsers, lockedUsers, adminUsers, teacherUsers, studentUsers);
        }

        /// <summary>
        /// 根据ID获取用户（重写以使用UserId）
        /// </summary>
        public override async Task<User?> GetByIdAsync(int id)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.UserId == id);
        }

        /// <summary>
        /// 根据ID删除用户（重写以使用UserId）
        /// </summary>
        public override async Task<bool> RemoveByIdAsync(int id)
        {
            var user = await GetByIdAsync(id);
            if (user == null)
                return false;

            await RemoveAsync(user);
            return true;
        }
    }
}