using System.Windows;

namespace ExamSystem.UI.Views
{
    /// <summary>
    /// 密码重置对话框
    /// </summary>
    public partial class ResetPasswordDialog : Window
    {
        public string NewPassword { get; private set; } = string.Empty;

        public ResetPasswordDialog()
        {
            InitializeComponent();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            var newPassword = NewPasswordBox.Password;
            var confirmPassword = ConfirmPasswordBox.Password;

            // 验证密码
            if (string.IsNullOrWhiteSpace(newPassword))
            {
                MessageBox.Show("请输入新密码！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                NewPasswordBox.Focus();
                return;
            }

            if (newPassword != confirmPassword)
            {
                MessageBox.Show("两次输入的密码不一致！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                ConfirmPasswordBox.Focus();
                return;
            }

            NewPassword = newPassword;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}