using System;
using System.Linq;
using System.Windows;
using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;
using ExamSystem.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ExamSystem.WPF.Views
{
    /// <summary>
    /// 编辑用户对话框
    /// 作者: Assistant
    /// 创建日期: 2025-11-09
    /// 版本: 1.0.0
    /// </summary>
    public partial class UserEditDialog : Window
    {
        private readonly int _userId;
        private readonly IUserService? _userService;
        private User? _originalUser;

        public UserEditDialog(int userId)
        {
            InitializeComponent();
            _userId = userId;
            _userService = ((App)Application.Current).GetServices().GetService<IUserService>();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_userService == null)
                {
                    MessageBox.Show("用户服务未初始化。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                _originalUser = await _userService.GetUserByIdAsync(_userId);
                if (_originalUser == null)
                {
                    MessageBox.Show("未找到该用户。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                    return;
                }

                UsernameTextBox.Text = _originalUser.Username;
                RealNameTextBox.Text = _originalUser.RealName ?? string.Empty;
                EmailTextBox.Text = _originalUser.Email ?? string.Empty;
                PhoneTextBox.Text = _originalUser.Phone ?? string.Empty;
                IsActiveCheckBox.IsChecked = _originalUser.IsActive;

                foreach (var item in RoleComboBox.Items.OfType<System.Windows.Controls.ComboBoxItem>())
                {
                    var tag = item.Tag?.ToString();
                    if (Enum.TryParse<UserRole>(tag, out var role) && role == _originalUser.Role)
                    {
                        RoleComboBox.SelectedItem = item;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorText.Text = ex.Message;
            }
        }

        private async void OnSaveClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_userService == null)
                {
                    MessageBox.Show("用户服务未初始化。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var username = UsernameTextBox.Text?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(username))
                {
                    ErrorText.Text = "用户名不能为空";
                    return;
                }

                var roleItem = RoleComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem;
                if (roleItem == null || !Enum.TryParse<UserRole>(roleItem.Tag?.ToString(), out var role))
                {
                    ErrorText.Text = "请选择角色";
                    return;
                }

                var userToUpdate = new User
                {
                    UserId = _userId,
                    Username = username,
                    RealName = RealNameTextBox.Text?.Trim(),
                    Email = EmailTextBox.Text?.Trim(),
                    Phone = PhoneTextBox.Text?.Trim(),
                    Role = role,
                    IsActive = IsActiveCheckBox.IsChecked == true
                };

                var ok = await _userService.UpdateUserAsync(userToUpdate);
                if (ok)
                {
                    MessageBox.Show("用户信息已保存。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
                else
                {
                    ErrorText.Text = "保存失败，可能是用户名或邮箱已被占用。";
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