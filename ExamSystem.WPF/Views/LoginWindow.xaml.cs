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
            
            // 设置默认焦点到用户名输入框
            Loaded += (s, e) => UsernameTextBox.Focus();
            
            // 绑定Enter键到登录命令
            KeyDown += LoginWindow_KeyDown;
        }

        private void LoginWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && _viewModel.LoginCommand.CanExecute(null))
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
                _logger.LogInformation("登录成功，打开主窗口");

                // 打开主窗口
                var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                mainWindow.Show();

                // 关闭登录窗口
                Close();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "打开主窗口时发生错误");
            }
        }

        private void OnCloseRequested(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        protected override void OnClosed(EventArgs e)
        {
            // 取消订阅事件，防止内存泄漏
            _viewModel.LoginSuccess -= OnLoginSuccess;
            _viewModel.CloseRequested -= OnCloseRequested;
            
            base.OnClosed(e);
        }
    }
}