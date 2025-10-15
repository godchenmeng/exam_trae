using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using ExamSystem.Domain.Enums;

namespace ExamSystem.Services.Interfaces
{
    /// <summary>
    /// 权限验证服务接口
    /// </summary>
    public interface IPermissionService
{
    /// <summary>
    /// 检查用户是否有权限执行指定操作
    /// </summary>
    /// <param name="userRole">用户角色</param>
    /// <param name="operation">操作名称</param>
    /// <returns>是否有权限</returns>
    bool HasPermission(UserRole userRole, string operation);

    /// <summary>
    /// 检查用户是否有权限访问指定模块
    /// </summary>
    /// <param name="userRole">用户角色</param>
    /// <param name="module">模块名称</param>
    /// <returns>是否有权限</returns>
    bool HasModuleAccess(UserRole userRole, string module);

    /// <summary>
    /// 获取用户角色的所有权限列表
    /// </summary>
    /// <param name="userRole">用户角色</param>
    /// <returns>权限列表</returns>
    List<string> GetUserPermissions(UserRole userRole);

    /// <summary>
    /// 获取用户可访问的模块列表
    /// </summary>
    /// <param name="userRole">用户角色</param>
    /// <returns>模块列表</returns>
    List<string> GetUserModules(UserRole userRole);
    }
}