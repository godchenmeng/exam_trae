using System;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ExamSystem.Domain.Entities;
using ExamSystem.WPF.ViewModels;
using ExamSystem.Services.Interfaces;
using ExamSystem.Services.Models;

namespace ExamSystem.WPF.Views
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MainWindow> _logger;
        private readonly IPermissionService _permissionService;
        private User? _currentUser;

        // 公共的 CurrentUser 属性，供其他组件访问
        public User? CurrentUser => _currentUser;

        // 供 XAML 绑定显示的属性（改为依赖属性，确保 UI 能实时更新）
        public static readonly DependencyProperty CurrentUsernameProperty = DependencyProperty.Register(
            nameof(CurrentUsername), typeof(string), typeof(MainWindow), new PropertyMetadata("未登录"));
        public string CurrentUsername
        {
            get => (string)GetValue(CurrentUsernameProperty);
            set => SetValue(CurrentUsernameProperty, value);
        }

        public static readonly DependencyProperty PreviousLoginAtProperty = DependencyProperty.Register(
            nameof(PreviousLoginAt), typeof(DateTime?), typeof(MainWindow), new PropertyMetadata(null));
        public DateTime? PreviousLoginAt
        {
            get => (DateTime?)GetValue(PreviousLoginAtProperty);
            set => SetValue(PreviousLoginAtProperty, value);
        }

        public MainWindow(IServiceProvider serviceProvider, ILogger<MainWindow> logger, IPermissionService permissionService)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;
            _logger = logger;
            _permissionService = permissionService;
            
            _logger.LogInformation("MainWindow 构造函数完成，延迟加载页面直到用户登录");
            // 不在构造函数中加载页面，等待用户登录后再加载
        }

        // 允许通过自定义标题栏拖动窗体，同时支持双击最大化/还原
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.ClickCount == 2)
                {
                    WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
                    return;
                }
                if (e.ButtonState == MouseButtonState.Pressed)
                {
                    DragMove();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "拖动窗口时发生异常");
            }
        }

        private void ApplyModuleVisibility()
        {
            if (_currentUser == null)
            {
                // 未登录用户不显示任何业务模块
                TabDashboard.Visibility = Visibility.Collapsed;
                TabQuestionBank.Visibility = Visibility.Collapsed;
                TabExamPaper.Visibility = Visibility.Collapsed;
                TabExamManagement.Visibility = Visibility.Collapsed;
                TabExamResult.Visibility = Visibility.Collapsed;
                TabStatistics.Visibility = Visibility.Collapsed;
                TabUserManagement.Visibility = Visibility.Collapsed;
                TabMessageCenter.Visibility = Visibility.Collapsed;
                TabLearningResources.Visibility = Visibility.Collapsed;
                // 新增：成绩管理入口
                if (TabGradeManagement != null)
                {
                    TabGradeManagement.Visibility = Visibility.Collapsed;
                }
                return;
            }
        
            var role = _currentUser.Role;
        
            // 旧模块：如果已有模块键值常量，建议后续替换为常量
            TabDashboard.Visibility = Visibility.Visible; // 仪表板默认所有角色可见
            TabQuestionBank.Visibility = _permissionService.HasModuleAccess(role, "QuestionBankManagement") ? Visibility.Visible : Visibility.Collapsed;
            TabExamPaper.Visibility = _permissionService.HasModuleAccess(role, "ExamPaperManagement") ? Visibility.Visible : Visibility.Collapsed;
            // 考试中心仅学生可见
            TabExamManagement.Visibility = (_currentUser.Role == ExamSystem.Domain.Enums.UserRole.Student
                                            && _permissionService.HasModuleAccess(role, ModuleKeys.ExamManagement))
                                            ? Visibility.Visible : Visibility.Collapsed;
            // 成绩查询仅学生可见（使用权限键 ViewOwnGrades）
            TabExamResult.Visibility = (_currentUser.Role == ExamSystem.Domain.Enums.UserRole.Student
                                        && _permissionService.HasPermission(role, PermissionKeys.ViewOwnGrades))
                                        ? Visibility.Visible : Visibility.Collapsed;
            TabStatistics.Visibility = _permissionService.HasModuleAccess(role, "StatisticsReports") ? Visibility.Visible : Visibility.Collapsed;
            TabUserManagement.Visibility = _permissionService.HasModuleAccess(role, "UserManagement") ? Visibility.Visible : Visibility.Collapsed;
            
            // 新增模块：使用常量类
            TabMessageCenter.Visibility = _permissionService.HasModuleAccess(role, ModuleKeys.MessageCenter) ? Visibility.Visible : Visibility.Collapsed;
            TabLearningResources.Visibility = _permissionService.HasModuleAccess(role, ModuleKeys.LearningResources) ? Visibility.Visible : Visibility.Collapsed;
            
            // 新增：成绩管理入口仅教师/管理员可见
            if (TabGradeManagement != null)
            {
                TabGradeManagement.Visibility = _permissionService.HasModuleAccess(role, "GradeManagement") ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public void SetCurrentUser(User user)
        {
            _currentUser = user;
            CurrentUsername = _currentUser?.Username ?? "未登录";
            
            // 根据当前用户角色应用模块可见性
            ApplyModuleVisibility();
            
            // 登录后再加载页面，避免无权用户加载不必要资源
            LoadPages();
            
            // 更新 ViewModel 的用户上下文
            UpdateViewModelsWithCurrentUser();
        }

        private void LoadPages()
        {
            try
            {
                // 加载仪表板
                var dashboardView = _serviceProvider.GetRequiredService<DashboardView>();
                DashboardFrame.Content = dashboardView;

                // 加载各个页面
                var questionBankView = _serviceProvider.GetRequiredService<QuestionBankView>();
                QuestionBankFrame.Content = questionBankView;
                
                // 立即为QuestionBankView设置用户信息
                if (_currentUser != null && questionBankView.DataContext is QuestionBankViewModel questionBankViewModel)
                {
                    _logger.LogInformation("在LoadPages中为QuestionBankViewModel设置用户信息");
                    questionBankViewModel.SetCurrentUser(_currentUser);
                }

                var examPaperView = _serviceProvider.GetRequiredService<ExamPaperView>();
                ExamPaperFrame.Content = examPaperView;

                // 根据用户角色加载不同的考试管理视图
                if (_currentUser != null && _currentUser.Role == ExamSystem.Domain.Enums.UserRole.Student)
                {
                    // 学生：加载学生考试列表视图
                    var studentExamListView = _serviceProvider.GetRequiredService<StudentExamListView>();
                    ExamFrame.Content = studentExamListView;
                }
                else
                {
                    // 教师/管理员：加载管理员考试视图
                    var examView = _serviceProvider.GetRequiredService<ExamView>();
                    ExamFrame.Content = examView;
                }

                // 根据用户角色加载不同的考试结果视图
                if (_currentUser != null && _currentUser.Role == ExamSystem.Domain.Enums.UserRole.Student)
                {
                    // 学生：加载学生考试结果视图
                    var studentExamResultView = _serviceProvider.GetRequiredService<StudentExamResultView>();
                    ExamResultFrame.Content = studentExamResultView;
                }
                else
                {
                    // 教师/管理员：加载管理员考试结果视图
                    var examResultView = _serviceProvider.GetRequiredService<ExamResultView>();
                    ExamResultFrame.Content = examResultView;
                }

                // 新增：消息中心
                if (_currentUser != null && _currentUser.Role == ExamSystem.Domain.Enums.UserRole.Student)
                {
                    // 学生：加载消息中心查看页
                    var mcView = _serviceProvider.GetRequiredService<MessageCenterView>();
                    MessageCenterFrame.Content = mcView;
                    // 注入 ViewModel 并设置当前用户
                    var mcVm = _serviceProvider.GetRequiredService<MessageCenterViewModel>();
                    mcView.DataContext = mcVm;
                    mcVm.SetCurrentUser(_currentUser);
                }
                else
                {
                    // 教师/管理员：加载通知发送页
                    var sendView = _serviceProvider.GetRequiredService<NotificationSendView>();
                    MessageCenterFrame.Content = sendView;
                    if (sendView.DataContext is NotificationSendViewModel sendVm && _currentUser != null)
                    {
                        sendVm.SetCurrentUser(_currentUser);
                    }
                }

                // 新增：学习资源
                var learningResourcesView = _serviceProvider.GetRequiredService<LearningResourcesView>();
                LearningResourcesFrame.Content = learningResourcesView;

                // 加载统计报表
                var statisticsView = _serviceProvider.GetRequiredService<StatisticsView>();
                StatisticsFrame.Content = statisticsView;

                // 加载成绩管理（仅教师和管理员可见）
                if (_currentUser != null && (_currentUser.Role == ExamSystem.Domain.Enums.UserRole.Teacher || _currentUser.Role == ExamSystem.Domain.Enums.UserRole.Admin))
                {
                    var gradeManagementView = _serviceProvider.GetRequiredService<GradeManagementView>();
                    GradeManagementFrame.Content = gradeManagementView;
                    // 注入 ViewModel 并设置当前用户
                    var gradeManagementVm = _serviceProvider.GetRequiredService<GradeManagementViewModel>();
                    gradeManagementView.DataContext = gradeManagementVm;
                    gradeManagementVm.SetCurrentUser(_currentUser);
                }

                // 加载用户管理
                var userManagementView = _serviceProvider.GetRequiredService<UserManagementView>();
                UserManagementFrame.Content = userManagementView;

                _logger.LogInformation("所有页面加载完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载页面时发生错误");
                MessageBox.Show($"加载页面时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateViewModelsWithCurrentUser()
        {
            if (_currentUser == null) 
            {
                _logger.LogWarning("=== UpdateViewModelsWithCurrentUser: CurrentUser为null ===");
                return;
            }

            try
            {
                _logger.LogInformation($"=== UpdateViewModelsWithCurrentUser ===");
                _logger.LogInformation($"当前用户: {_currentUser.Username} ({_currentUser.Role})");
                
                // 详细调试QuestionBankFrame的内容
                _logger.LogInformation($"QuestionBankFrame是否为null: {QuestionBankFrame == null}");
                if (QuestionBankFrame != null)
                {
                    _logger.LogInformation($"QuestionBankFrame.Content是否为null: {QuestionBankFrame.Content == null}");
                    if (QuestionBankFrame.Content != null)
                    {
                        _logger.LogInformation($"QuestionBankFrame.Content的类型: {QuestionBankFrame.Content.GetType().FullName}");
                        
                        if (QuestionBankFrame.Content is QuestionBankView questionBankView)
                        {
                            _logger.LogInformation("QuestionBankFrame.Content是QuestionBankView类型");
                            _logger.LogInformation($"QuestionBankView.DataContext是否为null: {questionBankView.DataContext == null}");
                            if (questionBankView.DataContext != null)
                            {
                                _logger.LogInformation($"QuestionBankView.DataContext的类型: {questionBankView.DataContext.GetType().FullName}");
                                
                                if (questionBankView.DataContext is QuestionBankViewModel questionBankViewModel)
                                {
                                    _logger.LogInformation("找到QuestionBankViewModel，正在设置用户信息");
                                    questionBankViewModel.SetCurrentUser(_currentUser);
                                    _logger.LogInformation("QuestionBankViewModel用户信息设置完成");
                                }
                                else
                                {
                                    _logger.LogWarning("QuestionBankView.DataContext不是QuestionBankViewModel类型");
                                }
                            }
                            else
                            {
                                _logger.LogWarning("QuestionBankView.DataContext为null");
                            }
                        }
                        else
                        {
                            _logger.LogWarning("QuestionBankFrame.Content不是QuestionBankView类型");
                        }
                    }
                }

                // 同步更新 ExamPaperViewModel 的用户信息
                _logger.LogInformation($"ExamPaperFrame是否为null: {ExamPaperFrame == null}");
                if (ExamPaperFrame != null)
                {
                    _logger.LogInformation($"ExamPaperFrame.Content是否为null: {ExamPaperFrame.Content == null}");
                    if (ExamPaperFrame.Content is ExamPaperView examPaperView)
                    {
                        _logger.LogInformation("ExamPaperFrame.Content是ExamPaperView类型");
                        if (examPaperView.DataContext is ExamPaperViewModel examPaperViewModel)
                        {
                            _logger.LogInformation("找到ExamPaperViewModel，正在设置用户信息");
                            examPaperViewModel.SetCurrentUser(_currentUser);
                            _logger.LogInformation("ExamPaperViewModel用户信息设置完成");
                        }
                        else
                        {
                            _logger.LogWarning("ExamPaperView.DataContext不是ExamPaperViewModel类型或为null");
                        }
                    }
                    else
                    {
                        _logger.LogWarning("ExamPaperFrame.Content不是ExamPaperView类型或为null");
                    }
                }

                // 同步更新 学生考试列表 StudentExamListViewModel 的用户信息
                _logger.LogInformation($"ExamFrame是否为null: {ExamFrame == null}");
                if (ExamFrame != null)
                {
                    _logger.LogInformation($"ExamFrame.Content是否为null: {ExamFrame.Content == null}");
                    if (ExamFrame.Content is StudentExamListView studentExamListView)
                    {
                        _logger.LogInformation("ExamFrame.Content是StudentExamListView类型");
                        if (studentExamListView.DataContext is StudentExamListViewModel studentVm)
                        {
                            _logger.LogInformation("找到StudentExamListViewModel，正在设置用户信息");
                            studentVm.SetCurrentUser(_currentUser);
                            _logger.LogInformation("StudentExamListViewModel用户信息设置完成");
                        }
                        else
                        {
                            _logger.LogWarning("StudentExamListView.DataContext不是StudentExamListViewModel类型或为null");
                        }
                    }
                    else
                    {
                        _logger.LogInformation("ExamFrame.Content不是StudentExamListView类型（可能为教师/管理员视图）");
                    }
                }

                // 同步更新 学生考试结果 StudentExamResultViewModel 的用户信息并刷新数据
                _logger.LogInformation($"ExamResultFrame是否为null: {ExamResultFrame == null}");
                if (ExamResultFrame != null)
                {
                    _logger.LogInformation($"ExamResultFrame.Content是否为null: {ExamResultFrame.Content == null}");
                    if (ExamResultFrame.Content is StudentExamResultView studentExamResultView)
                    {
                        _logger.LogInformation("ExamResultFrame.Content是StudentExamResultView类型");
                        if (studentExamResultView.DataContext is StudentExamResultViewModel resultVm)
                        {
                            _logger.LogInformation("找到StudentExamResultViewModel，正在设置用户信息并刷新数据");
                            resultVm.SetCurrentUser(_currentUser);
                            // 触发刷新命令以加载数据
                            resultVm.RefreshCommand?.Execute(null);
                            _logger.LogInformation("StudentExamResultViewModel用户信息设置完成并已请求刷新");
                        }
                        else
                        {
                            _logger.LogWarning("StudentExamResultView.DataContext不是StudentExamResultViewModel类型或为null");
                        }
                    }
                    else
                    {
                        _logger.LogInformation("ExamResultFrame.Content不是StudentExamResultView类型（可能为教师/管理员视图）");
                    }
                }

                // 可以在这里添加其他需要用户信息的ViewModel更新
                _logger.LogInformation("所有ViewModel的用户信息已更新");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateViewModelsWithCurrentUser 时发生异常");
            }
        }

        // 修改密码按钮事件
        private async void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentUser == null)
                {
                    MessageBox.Show("请先登录后再尝试修改密码。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var authService = _serviceProvider.GetRequiredService<IAuthService>();
                var dialog = new ChangePasswordDialog(authService, _currentUser.UserId)
                {
                    Owner = this
                };
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "打开修改密码对话框时发生错误");
                MessageBox.Show($"打开修改密码对话框时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 退出系统按钮事件
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var authService = _serviceProvider.GetRequiredService<IAuthService>();
                authService.Logout();

                var loginWindow = _serviceProvider.GetRequiredService<LoginWindow>();
                loginWindow.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "退出系统时发生错误");
                MessageBox.Show($"退出系统时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}