using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ExamSystem.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ExamSystem.UI.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            DataContext = Program.ServiceProvider.GetRequiredService<LoginViewModel>();
            
            // 订阅登录成功事件
            if (DataContext is LoginViewModel viewModel)
            {
                viewModel.LoginSuccessful += OnLoginSuccessful;
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnLoginSuccessful(ExamSystem.Domain.Entities.User user)
        {
            // 创建并显示主窗口
            var mainWindow = Program.ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
            
            // 关闭登录窗口
            this.Close();
        }
    }
}