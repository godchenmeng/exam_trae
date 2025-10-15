using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using ExamSystem.UI.ViewModels;

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

        private void OnLoginSuccessful(object sender, System.EventArgs e)
        {
            // 打开主窗口
            var mainWindow = new MainWindow();
            mainWindow.Show();
            
            // 关闭登录窗口
            this.Close();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel viewModel && sender is PasswordBox passwordBox)
            {
                viewModel.Password = passwordBox.Password;
            }
        }
    }
}