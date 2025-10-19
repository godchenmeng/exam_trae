using System;
using System.Windows;
using System.Windows.Input;
using ExamSystem.WPF.ViewModels;
using ExamSystem.WPF.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ExamSystem.WPF.Views
{
    public partial class LoginWindow : Window
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<LoginWindow> _logger;
        private readonly LoginViewModel _viewModel;

        // 无参构造函数，用于XAML设计时支持
        public LoginWindow()
        {
            InitializeComponent();
            
            // 设计时模式下的默认初始化
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }
            
            // 运行时从依赖注入容器获取服务
            var app = (App)Application.Current;
            if (app?.Services != null)
            {
                var viewModel = app.Services.GetRequiredService<LoginViewModel>();
                var serviceProvider = app.Services;
                var logger = app.Services.GetRequiredService<ILogger<LoginWindow>>();
                
                _viewModel = viewModel;
                _serviceProvider = serviceProvider;
                _logger = logger;
                
                DataContext = _viewModel;
                
                // 订阅ViewModel事件
                _viewModel.LoginSuccess += OnLoginSuccess;
                _viewModel.CloseRequested += OnCloseRequested;
                
                // 设置默认焦点到用户名输入框
                Loaded += (s, e) => UsernameTextBox.Focus();
                
                // 绑定Enter键到登录命令
                KeyDown += LoginWindow_KeyDown;
            }
        }

        public LoginWindow(LoginViewModel viewModel, IServiceProvider serviceProvider, ILogger<LoginWindow> logger)
        {
            InitializeComponent();
            
            _viewModel = viewModel;
            _serviceProvider = serviceProvider;
            _logger = logger;
            
            DataContext = _viewModel;
            
            // 订阅ViewModel事件
            _viewModel.LoginSuccess += OnLoginSuccess;
            _viewModel.CloseRequested += OnCloseRequested;
            
            // 处理PasswordBox的密码绑定
            PasswordBox.PasswordChanged += (s, e) => 
            {
                if (_viewModel != null)
                {
                    _viewModel.Password = PasswordBox.Password;
                }
            };
            
            // 设置默认焦点到用户名输入框
            Loaded += (s, e) => UsernameTextBox.Focus();
            
            // 绑定Enter键到登录命令
            KeyDown += LoginWindow_KeyDown;
        }

        private void LoginWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && _viewModel?.LoginCommand?.CanExecute(null) == true)
            {
                _viewModel.LoginCommand.Execute(null);
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void OnLoginSuccess(object sender, LoginSuccessEventArgs e)
        {
            try
            {
                _logger?.LogInformation("=== LoginWindow.OnLoginSuccess ===");
                _logger?.LogInformation($"登录成功的用户: {e.User?.Username} ({e.User?.Role})");
                _logger?.LogInformation($"用户ID: {e.User?.UserId}");
                _logger?.LogInformation($"用户对象是否为null: {e.User == null}");

                // 打开主窗口
                var mainWindow = _serviceProvider?.GetRequiredService<MainWindow>();
                if (mainWindow != null)
                {
                    _logger?.LogInformation("获取到MainWindow实例，准备设置用户信息");
                    // 设置当前用户信息
                    mainWindow.SetCurrentUser(e.User);
                    // 传递上次登录时间
                    mainWindow.PreviousLoginAt = e.PreviousLoginAt;
                    _logger?.LogInformation("用户信息设置完成，显示主窗口");
                    mainWindow.Show();
                }
                else
                {
                    _logger?.LogError("无法获取MainWindow实例");
                }

                // 关闭登录窗口
                _logger?.LogInformation("关闭登录窗口");
                Close();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "打开主窗口时发生错误");
            }
        }

        private void OnCloseRequested(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        protected override void OnClosed(EventArgs e)
        {
            // 取消订阅事件，防止内存泄漏
            if (_viewModel != null)
            {
                _viewModel.LoginSuccess -= OnLoginSuccess;
                _viewModel.CloseRequested -= OnCloseRequested;
            }
            
            base.OnClosed(e);
        }
    }
}