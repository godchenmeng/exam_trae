using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using ExamSystem.WPF.Commands;
using ExamSystem.Services.Interfaces;
using ExamSystem.Services.Models;
using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;
using Microsoft.Extensions.Logging;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.Measure;
using System.Linq;

namespace ExamSystem.WPF.ViewModels
{
    public class DashboardViewModel : INotifyPropertyChanged
    {
        private readonly IDashboardService _dashboardService;
        private readonly ILogger<DashboardViewModel> _logger;
        private User? _currentUser;
        // 新增：当前角色，便于XAML按角色切换模块
        private UserRole? _currentRole;
        public UserRole? CurrentRole
        {
            get => _currentRole;
            set => SetProperty(ref _currentRole, value);
        }

        public DashboardViewModel(
            IDashboardService dashboardService,
            ILogger<DashboardViewModel> logger)
        {
            _dashboardService = dashboardService;
            _logger = logger;

            RecentActivities = new ObservableCollection<RecentActivityModel>();
            // 新增：角色专属集合
            TeacherTopPapers = new ObservableCollection<TopPaperStat>();
            StudentUpcomingExams = new ObservableCollection<StudentExamInfo>();
            StudentRecentResults = new ObservableCollection<StudentExamResult>();
            // 新增：教师待批阅队列集合
            TeacherPendingGradings = new ObservableCollection<PendingGradingItem>();
            
            LoadDataCommand = new RelayCommand(async () => await LoadDashboardDataAsync());
            CreateQuestionBankCommand = new RelayCommand(CreateQuestionBank);
            AddQuestionCommand = new RelayCommand(AddQuestion);
            CreateExamPaperCommand = new RelayCommand(CreateExamPaper);
            ViewStatisticsCommand = new RelayCommand(ViewStatistics);
            UserManagementCommand = new RelayCommand(UserManagement);
            SystemSettingsCommand = new RelayCommand(SystemSettings);
            DataBackupCommand = new RelayCommand(DataBackup);
            HelpDocumentCommand = new RelayCommand(HelpDocument);

            // 初始化图表绑定属性为空，避免XAML绑定Null异常
            AdminTrendSeries = Array.Empty<ISeries>();
            AdminTrendXAxes = Array.Empty<Axis>();
            TeacherPerformanceSeries = Array.Empty<ISeries>();
            TeacherPerformanceXAxes = Array.Empty<Axis>();
        }

        #region Properties

        private int _questionBankCount;
        public int QuestionBankCount
        {
            get => _questionBankCount;
            set => SetProperty(ref _questionBankCount, value);
        }

        private int _questionCount;
        public int QuestionCount
        {
            get => _questionCount;
            set => SetProperty(ref _questionCount, value);
        }

        private int _examPaperCount;
        public int ExamPaperCount
        {
            get => _examPaperCount;
            set => SetProperty(ref _examPaperCount, value);
        }

        private int _examRecordCount;
        public int ExamRecordCount
        {
            get => _examRecordCount;
            set => SetProperty(ref _examRecordCount, value);
        }

        private string _welcomeMessage = string.Empty;
        public string WelcomeMessage
        {
            get => _welcomeMessage;
            set => SetProperty(ref _welcomeMessage, value);
        }

        public ObservableCollection<RecentActivityModel> RecentActivities { get; }

        // 管理员专属统计
        private int _totalUsers;
        public int TotalUsers { get => _totalUsers; set => SetProperty(ref _totalUsers, value); }
        private int _activeUsers;
        public int ActiveUsers { get => _activeUsers; set => SetProperty(ref _activeUsers, value); }
        private int _lockedUsers;
        public int LockedUsers { get => _lockedUsers; set => SetProperty(ref _lockedUsers, value); }
        private int _adminCount;
        public int AdminCount { get => _adminCount; set => SetProperty(ref _adminCount, value); }
        private int _teacherCount;
        public int TeacherCount { get => _teacherCount; set => SetProperty(ref _teacherCount, value); }
        private int _studentCount;
        public int StudentCount { get => _studentCount; set => SetProperty(ref _studentCount, value); }
        private int _publishedPaperCount;
        public int PublishedPaperCount { get => _publishedPaperCount; set => SetProperty(ref _publishedPaperCount, value); }
        private int _unpublishedPaperCount;
        public int UnpublishedPaperCount { get => _unpublishedPaperCount; set => SetProperty(ref _unpublishedPaperCount, value); }

        // 教师专属统计与集合
        private int _myQuestionBankCount;
        public int MyQuestionBankCount { get => _myQuestionBankCount; set => SetProperty(ref _myQuestionBankCount, value); }
        private int _myQuestionCount;
        public int MyQuestionCount { get => _myQuestionCount; set => SetProperty(ref _myQuestionCount, value); }
        private int _myExamPaperCount;
        public int MyExamPaperCount { get => _myExamPaperCount; set => SetProperty(ref _myExamPaperCount, value); }
        private int _myPublishedPaperCount;
        public int MyPublishedPaperCount { get => _myPublishedPaperCount; set => SetProperty(ref _myPublishedPaperCount, value); }
        private int _myUnpublishedPaperCount;
        public int MyUnpublishedPaperCount { get => _myUnpublishedPaperCount; set => SetProperty(ref _myUnpublishedPaperCount, value); }
        private decimal _myAverageScore;
        public decimal MyAverageScore { get => _myAverageScore; set => SetProperty(ref _myAverageScore, value); }
        private decimal _myPassRate;
        public decimal MyPassRate { get => _myPassRate; set => SetProperty(ref _myPassRate, value); }
        public ObservableCollection<TopPaperStat> TeacherTopPapers { get; }

        // 学生专属统计与集合
        private int _availableExamCount;
        public int AvailableExamCount { get => _availableExamCount; set => SetProperty(ref _availableExamCount, value); }
        private int _completedExamCount;
        public int CompletedExamCount { get => _completedExamCount; set => SetProperty(ref _completedExamCount, value); }
        private decimal _averageScore;
        public decimal AverageScore { get => _averageScore; set => SetProperty(ref _averageScore, value); }
        private decimal _passRate;
        public decimal PassRate { get => _passRate; set => SetProperty(ref _passRate, value); }
        public ObservableCollection<StudentExamInfo> StudentUpcomingExams { get; }
        public ObservableCollection<StudentExamResult> StudentRecentResults { get; }
        // 新增：教师待批阅队列集合
        public ObservableCollection<PendingGradingItem> TeacherPendingGradings { get; }

        // 图表数据绑定属性
        private ISeries[] _adminTrendSeries = Array.Empty<ISeries>();
        public ISeries[] AdminTrendSeries
        {
            get => _adminTrendSeries;
            set => SetProperty(ref _adminTrendSeries, value);
        }

        private Axis[] _adminTrendXAxes = Array.Empty<Axis>();
        public Axis[] AdminTrendXAxes
        {
            get => _adminTrendXAxes;
            set => SetProperty(ref _adminTrendXAxes, value);
        }

        private ISeries[] _teacherPerformanceSeries = Array.Empty<ISeries>();
        public ISeries[] TeacherPerformanceSeries
        {
            get => _teacherPerformanceSeries;
            set => SetProperty(ref _teacherPerformanceSeries, value);
        }

        private Axis[] _teacherPerformanceXAxes = Array.Empty<Axis>();
        public Axis[] TeacherPerformanceXAxes
        {
            get => _teacherPerformanceXAxes;
            set => SetProperty(ref _teacherPerformanceXAxes, value);
        }

        #endregion

        #region Commands

        public ICommand LoadDataCommand { get; }
        public ICommand CreateQuestionBankCommand { get; }
        public ICommand AddQuestionCommand { get; }
        public ICommand CreateExamPaperCommand { get; }
        public ICommand ViewStatisticsCommand { get; }
        public ICommand UserManagementCommand { get; }
        public ICommand SystemSettingsCommand { get; }
        public ICommand DataBackupCommand { get; }
        public ICommand HelpDocumentCommand { get; }

        #endregion

        #region Methods

        public void SetCurrentUser(User user)
        {
            _currentUser = user;
            CurrentRole = user.Role;
        }

        // 新增：待批阅队列项模型（教师端）
        public class PendingGradingItem
        {
            public int PaperId { get; set; }
            public string PaperName { get; set; } = string.Empty;
            public int PendingCount { get; set; }
            public DateTime? DueDate { get; set; }
        }

        public async Task LoadDashboardDataAsync()
        {
            try
            {
                // 设置欢迎消息
                var currentHour = DateTime.Now.Hour;
                var greeting = currentHour switch
                {
                    >= 6 and < 12 => "早上好",
                    >= 12 and < 18 => "下午好",
                    _ => "晚上好"
                };
                var name = _currentUser?.Username ?? "用户";
                var roleText = _currentUser?.Role switch
                {
                    UserRole.Admin => "管理员",
                    UserRole.Teacher => "教师",
                    UserRole.Student => "学生",
                    _ => "用户"
                };
                WelcomeMessage = $"{greeting}，{name}（{roleText}），欢迎使用在线考试系统！";

                // 根据角色加载数据
                RecentActivities.Clear();
                TeacherTopPapers.Clear();
                StudentUpcomingExams.Clear();
                StudentRecentResults.Clear();

                if (_currentUser == null)
                {
                    // 未登录，加载通用概览
                    var summary = await _dashboardService.GetCommonSummaryAsync();
                    ApplySummary(summary);
                    RecentActivities.Add(new RecentActivityModel { Icon = "ℹ️", Title = "提示", Description = "请登录以查看个性化仪表板", Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm") });
                    return;
                }

                switch (_currentUser.Role)
                {
                    case UserRole.Admin:
                        var adminData = await _dashboardService.GetAdminDashboardAsync();
                        ApplySummary(adminData.Summary);
                        // 管理员统计赋值
                        TotalUsers = adminData.TotalUsers;
                        ActiveUsers = adminData.ActiveUsers;
                        LockedUsers = adminData.LockedUsers;
                        AdminCount = adminData.AdminCount;
                        TeacherCount = adminData.TeacherCount;
                        StudentCount = adminData.StudentCount;
                        PublishedPaperCount = adminData.PublishedPaperCount;
                        UnpublishedPaperCount = adminData.UnpublishedPaperCount;
                        foreach (var n in adminData.RecentNotifications)
                        {
                            RecentActivities.Add(new RecentActivityModel
                            {
                                Icon = "🔔",
                                Title = n.Title,
                                Description = n.ContentPreview,
                                Time = n.CreatedAt.ToString("yyyy-MM-dd HH:mm")
                            });
                        }

                        // 管理员趋势区图表绑定（近7天）
                        var dayLabels = adminData.NewUsers7Days.Select(d => d.Date.ToString("MM-dd")).ToArray();
                        var newUsersValues = adminData.NewUsers7Days.Select(d => (double)d.Count).ToArray();
                        var publishedValues = adminData.PublishedPapers7Days.Select(d => (double)d.Count).ToArray();
                        AdminTrendSeries = new ISeries[]
                        {
                            new LineSeries<double>
                            {
                                Name = "新增用户",
                                Values = newUsersValues,
                                GeometrySize = 6
                            },
                            new ColumnSeries<double>
                            {
                                Name = "发布试卷",
                                Values = publishedValues
                            }
                        };
                        AdminTrendXAxes = new[]
                        {
                            new Axis
                            {
                                Labels = dayLabels,
                                LabelsRotation = 0,
                                Position = AxisPosition.Start
                            }
                        };
                        break;
                    case UserRole.Teacher:
                        var teacherData = await _dashboardService.GetTeacherDashboardAsync(_currentUser.UserId);
                        ApplySummary(teacherData.Summary);
                        MyQuestionBankCount = teacherData.MyQuestionBankCount;
                        MyQuestionCount = teacherData.MyQuestionCount;
                        MyExamPaperCount = teacherData.MyExamPaperCount;
                        MyPublishedPaperCount = teacherData.MyPublishedPaperCount;
                        MyUnpublishedPaperCount = teacherData.MyUnpublishedPaperCount;
                        MyAverageScore = teacherData.MyAverageScore;
                        MyPassRate = teacherData.MyPassRate;
                        foreach (var t in teacherData.TopPapers)
                        {
                            TeacherTopPapers.Add(t);
                            RecentActivities.Add(new RecentActivityModel
                            {
                                Icon = "📊",
                                Title = $"{t.PaperName} 成绩概览",
                                Description = $"平均分 {t.AverageScore:F1}，通过率 {t.PassRate:P0}",
                                Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm")
                            });
                        }
                        // 教师试卷表现图表（Top5平均分）
                        var labels = teacherData.TopPapers.Select(p => p.PaperName).ToArray();
                        var avgValues = teacherData.TopPapers.Select(p => (double)p.AverageScore).ToArray();
                        TeacherPerformanceSeries = new ISeries[]
                        {
                            new ColumnSeries<double>
                            {
                                Name = "平均分",
                                Values = avgValues
                            }
                        };
                        TeacherPerformanceXAxes = new[]
                        {
                            new Axis
                            {
                                Labels = labels,
                                LabelsRotation = 15,
                                Position = AxisPosition.Start
                            }
                        };
                        // 暂未接入待批阅队列数据源，保留空集合以支持UI占位
                        TeacherPendingGradings.Clear();
                        break;
                    case UserRole.Student:
                        var studentData = await _dashboardService.GetStudentDashboardAsync(_currentUser.UserId);
                        ApplySummary(studentData.Summary);
                        AvailableExamCount = studentData.AvailableExamCount;
                        CompletedExamCount = studentData.CompletedExamCount;
                        AverageScore = studentData.AverageScore;
                        PassRate = studentData.PassRate;
                        foreach (var e in studentData.UpcomingExams)
                        {
                            StudentUpcomingExams.Add(e);
                            RecentActivities.Add(new RecentActivityModel
                            {
                                Icon = "🕒",
                                Title = e.Title,
                                Description = $"开始时间 {e.StartTime:yyyy-MM-dd HH:mm}",
                                Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm")
                            });
                        }
                        StudentRecentResults.Clear();
                        foreach (var r in studentData.RecentResults)
                        {
                            StudentRecentResults.Add(r);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载仪表板数据失败");
            }
        }

        private void ApplySummary(DashboardSummary summary)
        {
            QuestionBankCount = summary.QuestionBankCount;
            QuestionCount = summary.QuestionCount;
            ExamPaperCount = summary.ExamPaperCount;
            ExamRecordCount = summary.ExamRecordCount;
        }

        private void CreateQuestionBank()
        {
            _logger.LogInformation("跳转到题库管理页面");
        }

        private void AddQuestion()
        {
            _logger.LogInformation("跳转到添加试题页面");
        }

        private void CreateExamPaper()
        {
            _logger.LogInformation("跳转到试卷创建页面");
        }

        private void ViewStatistics()
        {
            _logger.LogInformation("跳转到统计分析页面");
        }

        private void UserManagement()
        {
            _logger.LogInformation("跳转到用户管理页面");
        }

        private void SystemSettings()
        {
            _logger.LogInformation("跳转到系统设置页面");
        }

        private void DataBackup()
        {
            _logger.LogInformation("执行数据备份");
        }

        private void HelpDocument()
        {
            _logger.LogInformation("打开帮助文档");
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }

    // 最近活动模型
    public class RecentActivityModel
    {
        public string Icon { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
    }

}