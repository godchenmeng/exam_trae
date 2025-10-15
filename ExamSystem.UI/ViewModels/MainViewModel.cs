using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;
using ExamSystem.Services.Interfaces;
using ExamSystem.UI.Views;
using ExamSystem.WPF.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ExamSystem.UI.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly IAuthService _authService;
        private readonly IPermissionService _permissionService;
        private User _currentUser;
        private object _currentView;
        private DateTime _currentTime;

        public MainViewModel(IAuthService authService, IPermissionService permissionService)
        {
            _authService = authService;
            _permissionService = permissionService;
            _currentUser = _authService.GetCurrentUser();
            _currentTime = DateTime.Now;

            // 初始化命令
            NavigateCommand = new RelayCommand<string>(Navigate);
            LogoutCommand = new RelayCommand(Logout);

            // 启动时钟更新
            StartClock();

            // 默认显示仪表板
            Navigate("Dashboard");
        }

        public User CurrentUser
        {
            get => _currentUser;
            set
            {
                _currentUser = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsAdmin));
                OnPropertyChanged(nameof(IsTeacher));
                OnPropertyChanged(nameof(IsStudent));
                OnPropertyChanged(nameof(IsAdminOrTeacher));
                OnPropertyChanged(nameof(CanAccessUserManagement));
                OnPropertyChanged(nameof(CanAccessQuestionBank));
                OnPropertyChanged(nameof(CanAccessExamPaper));
                OnPropertyChanged(nameof(CanAccessGradeManagement));
                OnPropertyChanged(nameof(CanAccessStatistics));
                OnPropertyChanged(nameof(CanAccessSystemSettings));
            }
        }

        public object CurrentView
        {
            get => _currentView;
            set
            {
                _currentView = value;
                OnPropertyChanged();
            }
        }

        public DateTime CurrentTime
        {
            get => _currentTime;
            set
            {
                _currentTime = value;
                OnPropertyChanged();
            }
        }

        public bool IsAdmin => CurrentUser?.Role == UserRole.Admin;
        public bool IsTeacher => CurrentUser?.Role == UserRole.Teacher;
        public bool IsStudent => CurrentUser?.Role == UserRole.Student;
        public bool IsAdminOrTeacher => IsAdmin || IsTeacher;

        // 权限检查属性
        public bool CanAccessUserManagement => CurrentUser != null && _permissionService.HasModuleAccess(CurrentUser.Role, "UserManagement");
        public bool CanAccessQuestionBank => CurrentUser != null && _permissionService.HasModuleAccess(CurrentUser.Role, "QuestionBankManagement");
        public bool CanAccessExamPaper => CurrentUser != null && _permissionService.HasModuleAccess(CurrentUser.Role, "ExamPaperManagement");
        public bool CanAccessGradeManagement => CurrentUser != null && _permissionService.HasModuleAccess(CurrentUser.Role, "GradeManagement");
        public bool CanAccessStatistics => CurrentUser != null && _permissionService.HasModuleAccess(CurrentUser.Role, "StatisticsReports");
        public bool CanAccessSystemSettings => CurrentUser != null && _permissionService.HasModuleAccess(CurrentUser.Role, "SystemSettings");

        public ICommand NavigateCommand { get; }
        public ICommand LogoutCommand { get; }

        private void Navigate(string viewName)
        {
            try
            {
                switch (viewName)
                {
                    case "Dashboard":
                        CurrentView = CreateDashboardView();
                        break;
                    case "UserManagement":
                        if (CanAccessUserManagement)
                            CurrentView = CreateUserManagementView();
                        else
                            ShowAccessDeniedMessage();
                        break;
                    case "QuestionBank":
                        if (CanAccessQuestionBank)
                            CurrentView = CreateQuestionBankView();
                        else
                            ShowAccessDeniedMessage();
                        break;
                    case "ExamPaper":
                        if (CanAccessExamPaper)
                            CurrentView = CreateExamPaperView();
                        else
                            ShowAccessDeniedMessage();
                        break;
                    case "ExamManagement":
                        CurrentView = CreateExamManagementView(); // 所有用户都可以访问
                        break;
                    case "ExamList":
                        CurrentView = CreateExamListView(); // 学生查看可参加的考试
                        break;
                    case "ExamResult":
                        CurrentView = CreateExamResultView(); // 查看考试结果
                        break;
                    case "GradeManagement":
                        if (CanAccessGradeManagement)
                            CurrentView = CreateGradeManagementView();
                        else
                            ShowAccessDeniedMessage();
                        break;
                    case "MyExams":
                        if (IsStudent)
                            CurrentView = CreateMyExamsView();
                        else
                            ShowAccessDeniedMessage();
                        break;
                    case "SystemSettings":
                        if (CanAccessSystemSettings)
                            CurrentView = CreateSystemSettingsView();
                        else
                            ShowAccessDeniedMessage();
                        break;
                    default:
                        CurrentView = CreateDashboardView();
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导航失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private object CreateDashboardView()
        {
            return new System.Windows.Controls.TextBlock
            {
                Text = "欢迎使用考试系统！\n\n这里是仪表板页面，将显示系统概览信息。",
                FontSize = 16,
                Margin = new Thickness(20),
                TextWrapping = TextWrapping.Wrap
            };
        }

        private object CreateUserManagementView()
        {
            return new UserManagementView();
        }

        private object CreateQuestionBankView()
        {
            var questionBankViewModel = Program.ServiceProvider.GetRequiredService<QuestionBankViewModel>();
            return new ExamSystem.WPF.Views.QuestionBankView(questionBankViewModel);
        }

        private object CreateExamPaperView()
        {
            var examPaperViewModel = Program.ServiceProvider.GetRequiredService<ExamPaperViewModel>();
            var examPaperView = new ExamPaperView
            {
                DataContext = examPaperViewModel
            };
            return examPaperView;
        }

        private object CreateExamManagementView()
        {
            return new System.Windows.Controls.TextBlock
            {
                Text = "考试管理模块\n\n这里将显示考试管理功能，包括考试安排、监考管理等。",
                FontSize = 16,
                Margin = new Thickness(20),
                TextWrapping = TextWrapping.Wrap
            };
        }

        private object CreateGradeManagementView()
        {
            return new System.Windows.Controls.TextBlock
            {
                Text = "成绩管理模块\n\n这里将显示成绩管理功能，包括成绩查看、统计分析等。",
                FontSize = 16,
                Margin = new Thickness(20),
                TextWrapping = TextWrapping.Wrap
            };
        }

        private object CreateMyExamsView()
        {
            return new System.Windows.Controls.TextBlock
            {
                Text = "我的考试模块\n\n这里将显示学生的考试列表和考试记录。",
                FontSize = 16,
                Margin = new Thickness(20),
                TextWrapping = TextWrapping.Wrap
            };
        }

        private object CreateExamListView()
        {
            return new System.Windows.Controls.TextBlock
            {
                Text = "考试列表\n\n这里将显示可参加的考试列表，学生可以选择考试进行参加。",
                FontSize = 16,
                Margin = new Thickness(20),
                TextWrapping = TextWrapping.Wrap
            };
        }

        private object CreateExamResultView()
        {
            return new System.Windows.Controls.TextBlock
            {
                Text = "考试结果\n\n这里将显示考试结果和详细的答题记录。",
                FontSize = 16,
                Margin = new Thickness(20),
                TextWrapping = TextWrapping.Wrap
            };
        }

        private object CreateSystemSettingsView()
        {
            return new System.Windows.Controls.TextBlock
            {
                Text = "系统设置模块\n\n这里将显示系统配置和参数设置功能。",
                FontSize = 16,
                Margin = new Thickness(20),
                TextWrapping = TextWrapping.Wrap
            };
        }

        private void Logout()
        {
            try
            {
                _authService.Logout();
                
                // 关闭当前主窗口
                Application.Current.MainWindow?.Close();
                
                // 显示登录窗口
                var loginWindow = new LoginWindow();
                Application.Current.MainWindow = loginWindow;
                loginWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"退出登录失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowAccessDeniedMessage()
        {
            MessageBox.Show("您没有权限访问此功能！", "访问拒绝", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void StartClock()
        {
            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (sender, e) => CurrentTime = DateTime.Now;
            timer.Start();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}