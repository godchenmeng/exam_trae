using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using ExamSystem.Domain.Enums;
using ExamSystem.Services.Interfaces;
using ExamSystem.Services.Models;

namespace ExamSystem.Services.Services
{

/// <summary>
/// 权限验证服务
/// </summary>
public class PermissionService : IPermissionService
{
    private readonly ILogger<PermissionService> _logger;

    public PermissionService(ILogger<PermissionService> logger)
    {
        _logger = logger;
    }
    /// <summary>
    /// 检查用户是否有权限执行指定操作
    /// </summary>
    /// <param name="userRole">用户角色</param>
    /// <param name="operation">操作名称</param>
    /// <returns>是否有权限</returns>
    public bool HasPermission(UserRole userRole, string operation)
    {
        var result = operation switch
        {
            // 用户管理权限
            "CreateUser" => userRole == UserRole.Admin,
            "UpdateUser" => userRole == UserRole.Admin,
            "DeleteUser" => userRole == UserRole.Admin,
            "ViewAllUsers" => userRole == UserRole.Admin,
            "ResetPassword" => userRole == UserRole.Admin,
            "LockUser" => userRole == UserRole.Admin,
            "UnlockUser" => userRole == UserRole.Admin,
            "ImportUsers" => userRole == UserRole.Admin,
            "ExportUsers" => userRole == UserRole.Admin,

            // 题库管理权限
            "CreateQuestionBank" => userRole == UserRole.Admin || userRole == UserRole.Teacher,
            "UpdateQuestionBank" => userRole == UserRole.Admin || userRole == UserRole.Teacher,
            "DeleteQuestionBank" => userRole == UserRole.Admin || userRole == UserRole.Teacher,
            "ViewQuestionBank" => true, // 所有用户都可以查看题库

            // 题目管理权限
            "CreateQuestion" => userRole == UserRole.Admin || userRole == UserRole.Teacher,
            "UpdateQuestion" => userRole == UserRole.Admin || userRole == UserRole.Teacher,
            "DeleteQuestion" => userRole == UserRole.Admin || userRole == UserRole.Teacher,
            "ViewQuestion" => userRole == UserRole.Admin || userRole == UserRole.Teacher,

            // 试卷管理权限
            "CreateExamPaper" => userRole == UserRole.Admin || userRole == UserRole.Teacher,
            "UpdateExamPaper" => userRole == UserRole.Admin || userRole == UserRole.Teacher,
            "DeleteExamPaper" => userRole == UserRole.Admin || userRole == UserRole.Teacher,
            "ViewExamPaper" => userRole == UserRole.Admin || userRole == UserRole.Teacher,
            "PublishExamPaper" => userRole == UserRole.Admin || userRole == UserRole.Teacher,

            // 考试管理权限
            "StartExam" => userRole == UserRole.Admin || userRole == UserRole.Teacher,
            "StopExam" => userRole == UserRole.Admin || userRole == UserRole.Teacher,
            "MonitorExam" => userRole == UserRole.Admin || userRole == UserRole.Teacher,
            "TakeExam" => userRole == UserRole.Student,

            // 评分管理权限
            "GradeExam" => userRole == UserRole.Admin || userRole == UserRole.Teacher,
            "ViewAllGrades" => userRole == UserRole.Admin || userRole == UserRole.Teacher,
            PermissionKeys.ViewOwnGrades => true, // 所有用户都可以查看自己的成绩

            // 统计报表权限
            "ViewStatistics" => userRole == UserRole.Admin || userRole == UserRole.Teacher,
            "ExportStatistics" => userRole == UserRole.Admin || userRole == UserRole.Teacher,

            // 系统设置权限
            "SystemSettings" => userRole == UserRole.Admin,
            "ViewLogs" => userRole == UserRole.Admin,

            // 消息通知与学习资源（新增权限点）
            PermissionKeys.SendNotification => userRole == UserRole.Admin || userRole == UserRole.Teacher,
            PermissionKeys.ViewNotification => true, // 所有角色可查看通知
            PermissionKeys.ReceiveNotification => userRole == UserRole.Student,
            PermissionKeys.UploadResource => userRole == UserRole.Admin || userRole == UserRole.Teacher,
            PermissionKeys.ManageResource => userRole == UserRole.Admin || userRole == UserRole.Teacher,
            PermissionKeys.ViewResource => true, // 所有角色可浏览资源
            PermissionKeys.DownloadResource => userRole == UserRole.Student || userRole == UserRole.Teacher || userRole == UserRole.Admin,

            // 默认无权限
            _ => false
        };
        
        // 添加调试日志
        _logger.LogInformation($"PermissionService.HasPermission: Role={userRole}, Operation={operation}, Result={result}");
        
        return result;
    }

    /// <summary>
    /// 检查用户是否有权限访问指定模块
    /// </summary>
    /// <param name="userRole">用户角色</param>
    /// <param name="module">模块名称</param>
    /// <returns>是否有权限</returns>
    public bool HasModuleAccess(UserRole userRole, string module)
    {
        return module switch
        {
            "UserManagement" => userRole == UserRole.Admin,
            "QuestionBankManagement" => userRole == UserRole.Admin || userRole == UserRole.Teacher,
            "ExamPaperManagement" => userRole == UserRole.Admin || userRole == UserRole.Teacher,
            ModuleKeys.ExamManagement => true, // 所有用户都可以访问考试模块
            "GradeManagement" => userRole == UserRole.Admin || userRole == UserRole.Teacher,
            "StatisticsReports" => userRole == UserRole.Admin || userRole == UserRole.Teacher,
            "SystemSettings" => userRole == UserRole.Admin,
            // 新模块：消息中心与学习资源（所有角色可访问）
            ModuleKeys.MessageCenter => true,
            ModuleKeys.LearningResources => true,
            _ => false
        };
    }

    /// <summary>
    /// 获取用户角色的所有权限列表
    /// </summary>
    /// <param name="userRole">用户角色</param>
    /// <returns>权限列表</returns>
    public List<string> GetUserPermissions(UserRole userRole)
    {
        var permissions = new List<string>();

        // 所有用户的基本权限（新增：消息通知与学习资源的浏览权限）
        permissions.AddRange(new[]
        {
            "ViewQuestionBank",
            PermissionKeys.ViewOwnGrades,
            PermissionKeys.ViewNotification,
            PermissionKeys.ViewResource
        });

        // 学生权限（新增：接收通知、下载资源）
        if (userRole == UserRole.Student)
        {
            permissions.AddRange(new[]
            {
                "TakeExam",
                PermissionKeys.ReceiveNotification,
                PermissionKeys.DownloadResource
            });
        }

        // 教师权限（新增：发送通知、资源上传/管理、下载资源）
        if (userRole == UserRole.Teacher)
        {
            permissions.AddRange(new[]
            {
                "CreateQuestionBank", "UpdateQuestionBank", "DeleteQuestionBank",
                "CreateQuestion", "UpdateQuestion", "DeleteQuestion", "ViewQuestion",
                "CreateExamPaper", "UpdateExamPaper", "DeleteExamPaper", "ViewExamPaper", "PublishExamPaper",
                "StartExam", "StopExam", "MonitorExam",
                "GradeExam", "ViewAllGrades",
                "ViewStatistics", "ExportStatistics",
                PermissionKeys.SendNotification,
                PermissionKeys.UploadResource,
                PermissionKeys.ManageResource,
                PermissionKeys.DownloadResource
            });
        }

        // 管理员权限（包含所有权限，新增：消息通知与学习资源相关）
        if (userRole == UserRole.Admin)
        {
            permissions.AddRange(new[]
            {
                "CreateUser", "UpdateUser", "DeleteUser", "ViewAllUsers", "ResetPassword", "LockUser", "UnlockUser", "ImportUsers", "ExportUsers",
                "CreateQuestionBank", "UpdateQuestionBank", "DeleteQuestionBank",
                "CreateQuestion", "UpdateQuestion", "DeleteQuestion", "ViewQuestion",
                "CreateExamPaper", "UpdateExamPaper", "DeleteExamPaper", "ViewExamPaper", "PublishExamPaper",
                "StartExam", "StopExam", "MonitorExam", "TakeExam",
                "GradeExam", "ViewAllGrades",
                "ViewStatistics", "ExportStatistics",
                "SystemSettings", "ViewLogs",
                PermissionKeys.SendNotification,
                PermissionKeys.ViewNotification,
                PermissionKeys.ReceiveNotification,
                PermissionKeys.UploadResource,
                PermissionKeys.ManageResource,
                PermissionKeys.ViewResource,
                PermissionKeys.DownloadResource
            });
        }

        return permissions.Distinct().ToList();
    }

    /// <summary>
    /// 获取用户可访问的模块列表
    /// </summary>
    /// <param name="userRole">用户角色</param>
    /// <returns>模块列表</returns>
    public List<string> GetUserModules(UserRole userRole)
    {
        var modules = new List<string>
        {
            ModuleKeys.ExamManagement, // 所有用户都可以访问考试模块
            ModuleKeys.MessageCenter,
            ModuleKeys.LearningResources
        };

        if (userRole == UserRole.Teacher)
        {
            modules.AddRange(new[]
            {
                "QuestionBankManagement",
                "ExamPaperManagement",
                "GradeManagement",
                "StatisticsReports"
            });
        }

        if (userRole == UserRole.Admin)
        {
            modules.AddRange(new[]
            {
                "UserManagement",
                "QuestionBankManagement",
                "ExamPaperManagement",
                "GradeManagement",
                "StatisticsReports",
                "SystemSettings"
            });
        }

        return modules.Distinct().ToList();
    }
}}
