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
  /// 添加用户对话框
  /// 作者: Assistant
  /// 创建日期: 2025-11-09
  /// 版本: 1.0.0
  /// </summary>
  public partial class AddUserDialog : Window
  {
    private readonly IAuthService? _authService;

    public AddUserDialog()
    {
      InitializeComponent();
      _authService = ((App)Application.Current).GetServices().GetService<IAuthService>();
      Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
      try
      {
        // 默认选择“学生”角色
        foreach (var item in RoleComboBox.Items.OfType<System.Windows.Controls.ComboBoxItem>())
        {
          var tag = item.Tag?.ToString();
          if (Enum.TryParse<UserRole>(tag, out var role) && role == UserRole.Student)
          {
            RoleComboBox.SelectedItem = item;
            break;
          }
        }

        IsActiveCheckBox.IsChecked = true;
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
        if (_authService == null)
        {
          MessageBox.Show("认证服务未初始化。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
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

        var password = PasswordBox.Password ?? string.Empty;
        var confirm = ConfirmPasswordBox.Password ?? string.Empty;

        if (password != confirm)
        {
          ErrorText.Text = "两次输入的密码不一致";
          return;
        }

        var validation = _authService.ValidatePassword(password);
        if (!validation.IsValid)
        {
          ErrorText.Text = validation.Message;
          return;
        }

        var newUser = new User
        {
          Username = username,
          RealName = RealNameTextBox.Text?.Trim(),
          Email = EmailTextBox.Text?.Trim(),
          Phone = PhoneTextBox.Text?.Trim(),
          Role = role,
          IsActive = IsActiveCheckBox.IsChecked == true
        };

        var result = await _authService.RegisterAsync(newUser, password);
        if (result.Success)
        {
          MessageBox.Show("用户创建成功。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
          DialogResult = true;
          Close();
        }
        else
        {
          ErrorText.Text = result.Message;
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