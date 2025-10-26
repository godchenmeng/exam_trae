using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using ExamSystem.Domain.Enums;
using ExamSystem.Services.Interfaces;
using ExamSystem.Services.Models;
using ExamSystem.Infrastructure.Repositories;

namespace ExamSystem.Services.Services
{
    /// <summary>
    /// 仪表板服务实现：聚合仓储和服务层数据
    /// </summary>
    public class DashboardService : IDashboardService
    {
        private readonly IQuestionBankRepository _questionBankRepo;
        private readonly IQuestionRepository _questionRepo;
        private readonly IExamPaperRepository _examPaperRepo;
        private readonly IExamRecordRepository _examRecordRepo;
        private readonly IUserRepository _userRepo;
        private readonly INotificationRepository _notificationRepo;
        private readonly INotificationRecipientRepository _notificationRecipientRepo;
        private readonly IExamService _examService;
        private readonly IExamPaperService _examPaperService;

        public DashboardService(
            IQuestionBankRepository questionBankRepo,
            IQuestionRepository questionRepo,
            IExamPaperRepository examPaperRepo,
            IExamRecordRepository examRecordRepo,
            IUserRepository userRepo,
            INotificationRepository notificationRepo,
            INotificationRecipientRepository notificationRecipientRepo,
            IExamService examService,
            IExamPaperService examPaperService)
        {
            _questionBankRepo = questionBankRepo;
            _questionRepo = questionRepo;
            _examPaperRepo = examPaperRepo;
            _examRecordRepo = examRecordRepo;
            _userRepo = userRepo;
            _notificationRepo = notificationRepo;
            _notificationRecipientRepo = notificationRecipientRepo;
            _examService = examService;
            _examPaperService = examPaperService;
        }

        public async Task<DashboardSummary> GetCommonSummaryAsync(int? userId = null)
        {
            // 通用概览：如果传入 userId，可按用户过滤（教师/学生个性化）；否则返回全局统计
            // 这里先取全局统计，后续可优化为按所有权/授权过滤
            var qBanks = await _questionBankRepo.GetAllAsync();
            var questions = await _questionRepo.GetAllAsync();
            var papers = await _examPaperRepo.GetAllAsync();
            var records = await _examRecordRepo.GetAllAsync();

            return new DashboardSummary
            {
                QuestionBankCount = qBanks.Count(),
                QuestionCount = questions.Count(),
                ExamPaperCount = papers.Count(),
                ExamRecordCount = records.Count()
            };
        }

        public async Task<AdminDashboardData> GetAdminDashboardAsync()
        {
            var summary = await GetCommonSummaryAsync();

            // 用户统计
            var allUsers = await _userRepo.GetAllAsync();
            var adminCount = allUsers.Count(u => u.Role == UserRole.Admin);
            var teacherCount = allUsers.Count(u => u.Role == UserRole.Teacher);
            var studentCount = allUsers.Count(u => u.Role == UserRole.Student);
            var activeUsers = allUsers.Count(u => u.IsActive);
            var lockedUsers = allUsers.Count(u => !u.IsActive);

            // 试卷发布统计
            var allPapers = await _examPaperRepo.GetAllAsync();
            var publishedCount = allPapers.Count(p => p.IsPublished);
            var unpublishedCount = allPapers.Count() - publishedCount;

            // 最近通知（取最新10条）
            var notifications = await _notificationRepo.GetAllAsync();
            var recent = notifications
                .OrderByDescending(n => n.CreatedAt)
                .Take(10)
                .Select(n => new DashboardNotificationItem
                {
                    NotificationId = n.NotificationId,
                    Title = n.Title,
                    ContentPreview = string.IsNullOrEmpty(n.Content) ? string.Empty : (n.Content.Length > 100 ? n.Content.Substring(0, 100) + "..." : n.Content),
                    CreatedAt = n.CreatedAt,
                    Priority = n.Priority.ToString()
                })
                .ToList();

            return new AdminDashboardData
            {
                Summary = summary,
                TotalUsers = allUsers.Count(),
                ActiveUsers = activeUsers,
                LockedUsers = lockedUsers,
                AdminCount = adminCount,
                TeacherCount = teacherCount,
                StudentCount = studentCount,
                PublishedPaperCount = publishedCount,
                UnpublishedPaperCount = unpublishedCount,
                RecentNotifications = recent
            };
        }

        public async Task<TeacherDashboardData> GetTeacherDashboardAsync(int teacherId)
        {
            // 原先使用全局概览，改为按教师个性化统计
            // var summary = await GetCommonSummaryAsync(teacherId);

            // 我的资源（按CreatorId过滤）
            var qBanksAll = await _questionBankRepo.GetAllAsync();
            var myBanks = qBanksAll.Where(b => b.CreatorId == teacherId).ToList();

            var questionsAll = await _questionRepo.GetAllAsync();
            var myQuestionBankIds = myBanks.Select(b => b.BankId).ToHashSet();
            var myQuestionsCount = questionsAll.Count(q => myQuestionBankIds.Contains(q.BankId));

            var papersAll = await _examPaperRepo.GetAllAsync();
            var myPapers = papersAll.Where(p => p.CreatorId == teacherId).ToList();
            var myPublished = myPapers.Count(p => p.IsPublished);
            var myUnpublished = myPapers.Count - myPublished;

            // 统计与我相关的考试记录数量（我的试卷产生的记录）
            var recordsAll = await _examRecordRepo.GetAllAsync();
            var myPaperIds = myPapers.Select(p => p.PaperId).ToHashSet();
            var myExamRecordCount = recordsAll.Count(r => myPaperIds.Contains(r.PaperId));

            // 我的试卷表现（选取平均分、通过率前5）
            var topStats = new List<TopPaperStat>();
            foreach (var p in myPapers)
            {
                var stats = await _examRecordRepo.GetExamStatisticsAsync(null, p.PaperId);
                decimal passRate = 0m;
                try
                {
                    passRate = await _examRecordRepo.GetPassRateAsync(p.PaperId, p.PassScore);
                }
                catch
                {
                    passRate = 0m;
                }
                topStats.Add(new TopPaperStat
                {
                    PaperId = p.PaperId,
                    PaperName = p.Name,
                    AverageScore = stats.AverageScore,
                    PassRate = passRate
                });
            }
            var orderedTop = topStats.OrderByDescending(t => t.AverageScore).ThenByDescending(t => t.PassRate).Take(5).ToList();

            // 汇总
            var overallStats = orderedTop.Any()
                ? new { Avg = orderedTop.Average(t => t.AverageScore), Pass = orderedTop.Average(t => t.PassRate) }
                : new { Avg = 0m, Pass = 0m };

            // 教师个性化概览
            var summary = new DashboardSummary
            {
                QuestionBankCount = myBanks.Count,
                QuestionCount = myQuestionsCount,
                ExamPaperCount = myPapers.Count,
                ExamRecordCount = myExamRecordCount
            };

            return new TeacherDashboardData
            {
                Summary = summary,
                MyQuestionBankCount = myBanks.Count,
                MyQuestionCount = myQuestionsCount,
                MyExamPaperCount = myPapers.Count,
                MyPublishedPaperCount = myPublished,
                MyUnpublishedPaperCount = myUnpublished,
                MyAverageScore = overallStats.Avg,
                MyPassRate = overallStats.Pass,
                TopPapers = orderedTop
            };
        }

        public async Task<StudentDashboardData> GetStudentDashboardAsync(int studentId)
        {
            // 原先使用全局概览，改为按学生个性化统计
            // var summary = await GetCommonSummaryAsync(studentId);

            // 待参加考试和近期成绩
            var available = await _examService.GetAvailableExamsForStudentAsync(studentId);
            var results = await _examService.GetStudentExamResultsAsync(studentId);

            // 直接使用服务返回的模型，排序并截取前5项
            var upcoming = available
                .OrderBy(e => e.StartTime)
                .Take(5)
                .ToList();

            var recentResults = results
                .OrderByDescending(r => r.ExamDate)
                .Take(5)
                .ToList();

            // 汇总统计：平均分与通过率（通过率基于 ExamRecord 的 IsPassed 字段）
            decimal avg = results.Any() ? results.Average(r => r.Score) : 0m;

            var allRecords = await _examRecordRepo.GetAllAsync();
            var studentRecords = allRecords.Where(r => r.UserId == studentId && (r.Status == ExamStatus.Submitted || r.Status == ExamStatus.Completed || r.Status == ExamStatus.Graded)).ToList();
            decimal passRate = studentRecords.Any() ? (decimal)studentRecords.Count(r => r.IsPassed) / studentRecords.Count : 0m;

            // 学生个性化概览：以“可参加考试”和“已完成考试”为主
            var summary = new DashboardSummary
            {
                QuestionBankCount = 0, // 学生不拥有题库
                QuestionCount = 0,     // 学生不拥有试题
                ExamPaperCount = available.Count,
                ExamRecordCount = studentRecords.Count
            };

            return new StudentDashboardData
            {
                Summary = summary,
                AvailableExamCount = available.Count,
                CompletedExamCount = studentRecords.Count, // 已完成/已提交/已评分数量
                AverageScore = avg,
                PassRate = passRate,
                UpcomingExams = upcoming,
                RecentResults = recentResults
            };
        }

        public async Task<object> GetDashboardDataAsync(UserRole role, int userId)
        {
            return role switch
            {
                UserRole.Admin => await GetAdminDashboardAsync(),
                UserRole.Teacher => await GetTeacherDashboardAsync(userId),
                UserRole.Student => await GetStudentDashboardAsync(userId),
                _ => await GetCommonSummaryAsync(userId)
            };
        }
    }
}