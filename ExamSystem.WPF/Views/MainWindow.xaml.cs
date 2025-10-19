using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ExamSystem.Domain.Entities;
using ExamSystem.WPF.ViewModels;
using ExamSystem.Services.Interfaces;

namespace ExamSystem.WPF.Views
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MainWindow> _logger;
        private User _currentUser;

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

        public MainWindow(IServiceProvider serviceProvider, ILogger<MainWindow> logger)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;
            _logger = logger;
            
            _logger.LogInformation("MainWindow 构造函数完成，延迟加载页面直到用户登录");
            // 不在构造函数中加载页面，等待用户登录后再加载
        }

        public void SetCurrentUser(User user)
        {
            _currentUser = user;
            _logger.LogInformation($"=== MainWindow.SetCurrentUser ===");
            
            if (user != null)
            {
                _logger.LogInformation($"设置当前用户: {user.Username} ({user.Role})");
                _logger.LogInformation($"用户ID: {user.UserId}");
                _logger.LogInformation($"用户对象是否为null: false");

                // 更新顶部显示用户名
                CurrentUsername = user.Username;
                
                // 用户登录后才加载页面
                _logger.LogInformation("开始加载页面...");
                LoadPages();
                
                // 将用户信息传递给各个ViewModel
                UpdateViewModelsWithCurrentUser();
            }
            else
            {
                _logger.LogWarning($"用户对象是否为null: true");
            }
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

                var examView = _serviceProvider.GetRequiredService<ExamView>();
                ExamFrame.Content = examView;

                var examResultView = _serviceProvider.GetRequiredService<ExamResultView>();
                ExamResultFrame.Content = examResultView;

                // 加载统计报表
                var statisticsView = _serviceProvider.GetRequiredService<StatisticsView>();
                StatisticsFrame.Content = statisticsView;

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
                    else
                    {
                        _logger.LogWarning("QuestionBankFrame.Content为null");
                    }
                }
                else
                {
                    _logger.LogWarning("QuestionBankFrame为null");
                }

                // 可以在这里添加其他需要用户信息的ViewModel更新
                _logger.LogInformation("所有ViewModel的用户信息已更新");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新ViewModel用户信息时发生错误");
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