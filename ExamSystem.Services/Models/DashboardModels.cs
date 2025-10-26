using System;
using System.Collections.Generic;
using ExamSystem.Domain.Enums;

namespace ExamSystem.Services.Models
{
    /// <summary>
    /// 首页概览卡片统计
    /// </summary>
    public class DashboardSummary
    {
        public int QuestionBankCount { get; set; }
        public int QuestionCount { get; set; }
        public int ExamPaperCount { get; set; }
        public int ExamRecordCount { get; set; }
    }

    /// <summary>
    /// 管理员仪表板数据
    /// </summary>
    public class AdminDashboardData
    {
        public DashboardSummary Summary { get; set; } = new DashboardSummary();
        // 用户统计
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int LockedUsers { get; set; }
        public int AdminCount { get; set; }
        public int TeacherCount { get; set; }
        public int StudentCount { get; set; }
        // 试卷统计
        public int PublishedPaperCount { get; set; }
        public int UnpublishedPaperCount { get; set; }
        // 最近通知
        public List<DashboardNotificationItem> RecentNotifications { get; set; } = new();
    }

    /// <summary>
    /// 教师仪表板数据
    /// </summary>
    public class TeacherDashboardData
    {
        public DashboardSummary Summary { get; set; } = new DashboardSummary();
        public int MyQuestionBankCount { get; set; }
        public int MyQuestionCount { get; set; }
        public int MyExamPaperCount { get; set; }
        public int MyPublishedPaperCount { get; set; }
        public int MyUnpublishedPaperCount { get; set; }
        public decimal MyAverageScore { get; set; }
        public decimal MyPassRate { get; set; }
        public List<TopPaperStat> TopPapers { get; set; } = new();
    }

    /// <summary>
    /// 学生仪表板数据
    /// </summary>
    public class StudentDashboardData
    {
        public DashboardSummary Summary { get; set; } = new DashboardSummary();
        public int AvailableExamCount { get; set; }
        public int CompletedExamCount { get; set; }
        public decimal AverageScore { get; set; }
        public decimal PassRate { get; set; }
        public List<StudentExamInfo> UpcomingExams { get; set; } = new();
        public List<StudentExamResult> RecentResults { get; set; } = new();
    }

    /// <summary>
    /// 仪表板展示的简版通知项
    /// </summary>
    public class DashboardNotificationItem
    {
        public int NotificationId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ContentPreview { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Priority { get; set; } = "Normal";
    }

    /// <summary>
    /// 试卷排行榜项（教师）
    /// </summary>
    public class TopPaperStat
    {
        public int PaperId { get; set; }
        public string PaperName { get; set; } = string.Empty;
        public decimal AverageScore { get; set; }
        public decimal PassRate { get; set; }
    }
}