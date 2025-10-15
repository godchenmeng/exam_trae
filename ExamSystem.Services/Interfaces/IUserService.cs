using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;

namespace ExamSystem.Services.Interfaces
{
    /// <summary>
    /// 用户服务接口
    /// </summary>
    public interface IUserService
{
    /// <summary>
    /// 获取所有用户
    /// </summary>
    Task<List<User>> GetAllUsersAsync();

    /// <summary>
    /// 根据ID获取用户
    /// </summary>
    Task<User?> GetUserByIdAsync(int userId);

    /// <summary>
    /// 根据用户名获取用户
    /// </summary>
    Task<User?> GetUserByUsernameAsync(string username);

    /// <summary>
    /// 根据角色获取用户列表
    /// </summary>
    Task<List<User>> GetUsersByRoleAsync(UserRole role);

    /// <summary>
    /// 分页获取用户列表
    /// </summary>
    Task<(List<User> Users, int TotalCount)> GetUsersPagedAsync(int pageIndex, int pageSize, string? searchKeyword = null, UserRole? role = null);

    /// <summary>
    /// 创建用户
    /// </summary>
    Task<(bool Success, string Message)> CreateUserAsync(User user);

    /// <summary>
    /// 更新用户信息
    /// </summary>
    Task<bool> UpdateUserAsync(User user);

    /// <summary>
    /// 删除用户
    /// </summary>
    Task<bool> DeleteUserAsync(int userId);

    /// <summary>
    /// 激活/禁用用户
    /// </summary>
    Task<bool> ToggleUserStatusAsync(int userId);

    /// <summary>
    /// 批量删除用户
    /// </summary>
    Task<(bool Success, string Message)> BatchDeleteUsersAsync(List<int> userIds);

    /// <summary>
    /// 获取用户统计信息
    /// </summary>
    Task<Dictionary<string, int>> GetUserStatisticsAsync();

    /// <summary>
    /// 检查用户是否有权限执行操作
    /// </summary>
    bool HasPermission(UserRole userRole, string operation);

    /// <summary>
    /// 导出用户数据
    /// </summary>
    Task<byte[]> ExportUsersAsync();

    /// <summary>
    /// 导入用户数据
    /// </summary>
    Task<(bool Success, string Message, int ImportedCount)> ImportUsersAsync(byte[] fileData);
    }
}