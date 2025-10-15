using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExamSystem.Services.Interfaces;

namespace ExamSystem.UI.ViewModels
{
    public partial class DashboardViewModel : BaseViewModel
    {
        private readonly IUserService _userService;

        [ObservableProperty]
        private int totalUsers;

        [ObservableProperty]
        private int totalQuestions;

        [ObservableProperty]
        private int totalExamPapers;

        [ObservableProperty]
        private int totalExams;

        [ObservableProperty]
        private string welcomeMessage = string.Empty;

        [ObservableProperty]
        private ObservableCollection<RecentExamInfo> recentExams = new();

        [ObservableProperty]
        private ObservableCollection<SystemNotification> notifications = new();

        public DashboardViewModel(IUserService userService)
        {
            _userService = userService;
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            await LoadDashboardDataAsync();
            InitializeWelcomeMessage();
            LoadRecentExams();
            LoadNotifications();
        }

        private async Task LoadDashboardDataAsync()
        {
            try
            {
                IsBusy = true;
                BusyMessage = "正在加载仪表板数据...";

                // 获取用户统计
                var userStats = await _userService.GetUserStatisticsAsync();
                TotalUsers = userStats.TotalCount;

                // TODO: 从相应的服务获取其他统计数据
                TotalQuestions = 0; // 暂时设为0，等题库服务实现后更新
                TotalExamPapers = 0; // 暂时设为0，等试卷服务实现后更新
                TotalExams = 0; // 暂时设为0，等考试服务实现后更新
            }
            catch (Exception ex)
            {
                ShowMessage($"加载仪表板数据失败：{ex.Message}", MessageType.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void InitializeWelcomeMessage()
        {
            var currentUser = CurrentUser.Instance;
            if (currentUser != null)
            {
                var timeOfDay = DateTime.Now.Hour switch
                {
                    >= 6 and < 12 => "上午好",
                    >= 12 and < 18 => "下午好",
                    _ => "晚上好"
                };
                WelcomeMessage = $"{timeOfDay}，{currentUser.RealName}！";
            }
        }

        private void LoadRecentExams()
        {
            // TODO: 从考试服务获取最近的考试数据
            RecentExams.Clear();
            
            // 示例数据
            RecentExams.Add(new RecentExamInfo
            {
                ExamName = "数学期中考试",
                ExamDate = DateTime.Now.AddDays(-2),
                Status = "已结束",
                ParticipantCount = 45
            });
            
            RecentExams.Add(new RecentExamInfo
            {
                ExamName = "英语听力测试",
                ExamDate = DateTime.Now.AddDays(3),
                Status = "即将开始",
                ParticipantCount = 38
            });
        }

        private void LoadNotifications()
        {
            // TODO: 从通知服务获取系统通知
            Notifications.Clear();
            
            // 示例数据
            Notifications.Add(new SystemNotification
            {
                Title = "系统维护通知",
                Content = "系统将于本周日凌晨2:00-4:00进行维护，请提前安排考试时间。",
                CreateTime = DateTime.Now.AddHours(-2),
                IsRead = false
            });
            
            Notifications.Add(new SystemNotification
            {
                Title = "新功能上线",
                Content = "题库管理功能已上线，支持批量导入题目和智能组卷。",
                CreateTime = DateTime.Now.AddDays(-1),
                IsRead = true
            });
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadDashboardDataAsync();
            LoadRecentExams();
            LoadNotifications();
        }

        [RelayCommand]
        private void NavigateToUserManagement()
        {
            // 通过消息传递或事件通知主窗口导航
            // 这里可以使用 WeakReferenceMessenger 或其他方式
        }

        [RelayCommand]
        private void NavigateToQuestionBank()
        {
            // TODO: 导航到题库管理
        }

        [RelayCommand]
        private void NavigateToExamPaper()
        {
            // TODO: 导航到试卷管理
        }

        [RelayCommand]
        private void NavigateToExamManagement()
        {
            // TODO: 导航到考试管理
        }
    }

    // 辅助类
    public class RecentExamInfo
    {
        public string ExamName { get; set; } = string.Empty;
        public DateTime ExamDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int ParticipantCount { get; set; }
    }

    public class SystemNotification
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreateTime { get; set; }
        public bool IsRead { get; set; }
    }
}