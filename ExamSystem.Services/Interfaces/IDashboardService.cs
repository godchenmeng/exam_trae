using System.Collections.Generic;
using System.Threading.Tasks;
using ExamSystem.Domain.Enums;
using ExamSystem.Services.Models;

namespace ExamSystem.Services.Interfaces
{
    /// <summary>
    /// 仪表板服务：按角色聚合并返回首页所需的数据
    /// </summary>
    public interface IDashboardService
    {
        /// <summary>
        /// 通用概览卡片统计（题库/题目/试卷/考试记录）
        /// </summary>
        Task<DashboardSummary> GetCommonSummaryAsync(int? userId = null);

        /// <summary>
        /// 管理员仪表板数据
        /// </summary>
        Task<AdminDashboardData> GetAdminDashboardAsync();

        /// <summary>
        /// 教师仪表板数据
        /// </summary>
        Task<TeacherDashboardData> GetTeacherDashboardAsync(int teacherId);

        /// <summary>
        /// 学生仪表板数据
        /// </summary>
        Task<StudentDashboardData> GetStudentDashboardAsync(int studentId);

        /// <summary>
        /// 根据角色获取仪表板数据（统一入口）
        /// </summary>
        Task<object> GetDashboardDataAsync(UserRole role, int userId);
    }
}