using System;
using System.Windows;
using ExamSystem.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ExamSystem.WPF.Views
{
    /// <summary>
    /// 重置密码对话框
    /// 作者: Assistant
    /// 创建日期: 2025-11-09
    /// 版本: 1.0.0
    /// </summary>
    public partial class ResetPasswordDialog : Window
    {
        private readonly int _userId;
        private readonly IAuthService? _authService;

        public ResetPasswordDialog(int userId)
        {
            InitializeComponent();
            _userId = userId;
            _authService = ((App)Application.Current).GetServices().GetService<IAuthService>();
        }

        private async void OnConfirmClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_authService == null)
                {
                    MessageBox.Show("认证服务未初始化。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var newPwd = NewPasswordBox.Password ?? string.Empty;
                var confirmPwd = ConfirmPasswordBox.Password ?? string.Empty;

                ErrorText.Text = string.Empty;

                if (string.IsNullOrWhiteSpace(newPwd))
                {
                    ErrorText.Text = "新密码不能为空";
                    return;
                }
                if (newPwd != confirmPwd)
                {
                    ErrorText.Text = "两次输入的密码不一致";
                    return;
                }

                var valid = _authService.ValidatePassword(newPwd);
                if (!valid.IsValid)
                {
                    ErrorText.Text = valid.Message ?? "密码不符合安全要求";
                    return;
                }

                var result = await _authService.ResetPasswordAsync(_userId, newPwd);
                if (result.Success)
                {
                    MessageBox.Show("密码重置成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
                else
                {
                    ErrorText.Text = result.Message ?? "密码重置失败";
                }
            }
            catch (Exception ex)
            {
                ErrorText.Text = ex.Message;
            }
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}