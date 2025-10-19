using System;
using System.Windows;
using ExamSystem.Services.Interfaces;

namespace ExamSystem.WPF.Views
{
    public partial class ChangePasswordDialog : Window
    {
        private readonly IAuthService _authService;
        private readonly int _userId;

        public ChangePasswordDialog(IAuthService authService, int userId)
        {
            InitializeComponent();
            _authService = authService;
            _userId = userId;
        }

        private async void OnConfirmClick(object sender, RoutedEventArgs e)
        {
            // 禁用按钮并清空错误提示，避免重复提交与信息干扰
            ConfirmButton.IsEnabled = false;
            ErrorTextBlock.Visibility = Visibility.Collapsed;
            ErrorTextBlock.Text = string.Empty;

            try
            {
                var oldPassword = OldPasswordBox.Password?.Trim();
                var newPassword = NewPasswordBox.Password?.Trim();
                var confirmPassword = ConfirmPasswordBox.Password?.Trim();

                if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword))
                {
                    ErrorTextBlock.Text = "请输入完整的密码信息。";
                    ErrorTextBlock.Visibility = Visibility.Visible;
                    return;
                }

                if (newPassword != confirmPassword)
                {
                    ErrorTextBlock.Text = "两次输入的新密码不一致。";
                    ErrorTextBlock.Visibility = Visibility.Visible;
                    return;
                }

                // 校验密码强度（即时反馈在内联错误区）
                var validation = _authService.ValidatePassword(newPassword);
                if (!validation.IsValid)
                {
                    ErrorTextBlock.Text = validation.Message ?? "密码不符合要求，请调整后重试。";
                    ErrorTextBlock.Visibility = Visibility.Visible;
                    return;
                }

                var result = await _authService.ChangePasswordAsync(_userId, oldPassword!, newPassword!);
                if (result.Success)
                {
                    MessageBox.Show("密码修改成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
                else
                {
                    ErrorTextBlock.Text = result.Message ?? "密码修改失败，请稍后再试。";
                    ErrorTextBlock.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                ErrorTextBlock.Text = $"修改密码过程中发生错误：{ex.Message}";
                ErrorTextBlock.Visibility = Visibility.Visible;
            }
            finally
            {
                // 恢复按钮可用状态
                ConfirmButton.IsEnabled = true;
            }
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}