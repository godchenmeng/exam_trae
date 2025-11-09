using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;
using ExamSystem.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace ExamSystem.WPF.Views
{
    public partial class UserManagementView : UserControl
    {
        private IUserService? _userService;
        private ILogger<UserManagementView>? _logger;
        private IAuthService? _authService;

        public ObservableCollection<UserDisplayModel> Users { get; set; } = new ObservableCollection<UserDisplayModel>();
        
        private int _currentPage = 1;
        private int _pageSize = 10;
        private int _totalCount = 0;
        private int _totalPages = 0;

        // 无参构造函数，用于 XAML 设计时支持
        public UserManagementView()
        {
            InitializeComponent();
            
            _userService = null;
            _logger = null;
            _authService = null;

            // 确保 Users 已初始化，并绑定到数据网格
            UsersDataGrid.ItemsSource = Users;

            Loaded += UserManagementView_Loaded;
        }

        public UserManagementView(IUserService userService, ILogger<UserManagementView> logger)
        {
            InitializeComponent();
            
            _userService = userService;
            _logger = logger;
            _authService = ((App)Application.Current).GetServices().GetService<IAuthService>();

            // 确保 Users 已初始化，并绑定到数据网格
            UsersDataGrid.ItemsSource = Users;

            Loaded += UserManagementView_Loaded;
        }

        private async void UserManagementView_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadUsersAsync();
        }

        private async Task LoadUsersAsync()
        {
            try
            {
                EnsureServices();
                if (_userService == null)
                {
                    _logger?.LogWarning("UserService 未初始化，跳过加载用户列表");
                    return;
                }

                var users = await _userService.GetAllUsersAsync();

                // 防御性保护，确保 Users 不为 null
                if (Users == null)
                {
                    Users = new ObservableCollection<UserDisplayModel>();
                    UsersDataGrid.ItemsSource = Users;
                }

                Users.Clear();
                foreach (var user in users)
                {
                    Users.Add(new UserDisplayModel(user));
                }

                _totalCount = users.Count;
                _totalPages = (int)Math.Ceiling((double)_totalCount / _pageSize);
                
                UpdatePaginationInfo();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载用户列表时发生错误");
                MessageBox.Show("加载用户列表时发生错误，请稍后重试。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EnsureServices()
        {
            try
            {
                var services = ((App)Application.Current).GetServices();
                _userService ??= services.GetService<IUserService>();
                _authService ??= services.GetService<IAuthService>();
                _logger ??= services.GetService<ILogger<UserManagementView>>();
            }
            catch
            {
                // 忽略解析失败，使用空服务以避免设计时异常
            }
        }

        private void UpdatePaginationInfo()
        {
            TotalCountTextBlock.Text = _totalCount.ToString();
            CurrentPageTextBlock.Text = _currentPage.ToString();
            TotalPagesTextBlock.Text = _totalPages.ToString();
            PageNumberTextBox.Text = _currentPage.ToString();

            FirstPageButton.IsEnabled = _currentPage > 1;
            PreviousPageButton.IsEnabled = _currentPage > 1;
            NextPageButton.IsEnabled = _currentPage < _totalPages;
            LastPageButton.IsEnabled = _currentPage < _totalPages;
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            await SearchUsersAsync();
        }

        private async Task SearchUsersAsync()
        {
            try
            {
                var searchText = SearchTextBox.Text?.Trim();
                var roleFilter = (RoleFilterComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
                var statusFilter = (StatusFilterComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();

                // TODO: 实现搜索和筛选逻辑
                await LoadUsersAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "搜索用户时发生错误");
                MessageBox.Show("搜索用户时发生错误，请稍后重试。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void AddUserButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new AddUserDialog
                {
                    Owner = Window.GetWindow(this)
                };

                var result = dialog.ShowDialog();
                if (result == true)
                {
                    // 新增成功后刷新列表
                    await LoadUsersAsync();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "添加用户对话框打开失败");
                MessageBox.Show("打开添加用户对话框失败。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ImportUsersButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: 实现批量导入用户功能
            MessageBox.Show("批量导入用户功能", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportUsersButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: 实现导出用户功能
            MessageBox.Show("导出用户功能", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void EditUserButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is UserDisplayModel user)
            {
                try
                {
                    EnsureServices();
                    if (_userService == null)
                    {
                        MessageBox.Show("服务未初始化，无法编辑用户。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    var dialog = new UserEditDialog(user.Id)
                    {
                        Owner = Window.GetWindow(this)
                    };
                    var result = dialog.ShowDialog();
                    if (result == true)
                    {
                        await LoadUsersAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "打开编辑用户对话框时发生错误");
                    MessageBox.Show($"打开编辑用户对话框时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ResetPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is UserDisplayModel user)
            {
                try
                {
                    EnsureServices();
                    if (_authService == null)
                    {
                        MessageBox.Show("服务未初始化，无法重置密码。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    var dialog = new ResetPasswordDialog(user.Id)
                    {
                        Owner = Window.GetWindow(this)
                    };
                    dialog.ShowDialog();
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "重置密码时发生错误");
                    MessageBox.Show($"重置密码时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void ToggleStatusButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is UserDisplayModel user)
            {
                var action = user.IsActive ? "禁用" : "启用";
                var result = MessageBox.Show($"确定要{action}用户 {user.Username} 吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        EnsureServices();
                        if (_userService == null)
                        {
                            MessageBox.Show("服务未初始化，无法切换状态。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }

                        var ok = await _userService.ToggleUserStatusAsync(user.Id);
                        if (ok)
                        {
                            MessageBox.Show($"用户{action}成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                            await LoadUsersAsync();
                        }
                        else
                        {
                            MessageBox.Show($"用户{action}失败，请稍后重试。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                {
                    _logger?.LogError(ex, $"{action}用户时发生错误");
                    MessageBox.Show($"{action}用户时发生错误，请稍后重试。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                }
            }
        }

        private async void DeleteUserButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is UserDisplayModel user)
            {
                var result = MessageBox.Show($"确定要删除用户 {user.Username} 吗？此操作不可恢复！", "警告", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        EnsureServices();
                        if (_userService == null)
                        {
                            _logger?.LogWarning("UserService 未初始化，无法删除用户");
                            MessageBox.Show("服务未初始化，无法删除用户。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }

                        var ok = await _userService.DeleteUserAsync(user.Id);
                        if (ok)
                        {
                            MessageBox.Show("用户删除成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                            await LoadUsersAsync();
                        }
                        else
                        {
                            MessageBox.Show("用户删除失败，可能存在关联数据。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "删除用户时发生错误");
                        MessageBox.Show("删除用户时发生错误，请稍后重试。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void UsersDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // TODO: 处理用户选择变化
        }

        // 分页事件处理
        private async void FirstPageButton_Click(object sender, RoutedEventArgs e)
        {
            _currentPage = 1;
            await LoadUsersAsync();
        }

        private async void PreviousPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                await LoadUsersAsync();
            }
        }

        private async void NextPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                await LoadUsersAsync();
            }
        }

        private async void LastPageButton_Click(object sender, RoutedEventArgs e)
        {
            _currentPage = _totalPages;
            await LoadUsersAsync();
        }

        private async void PageNumberTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (int.TryParse(PageNumberTextBox.Text, out int pageNumber) && 
                    pageNumber >= 1 && pageNumber <= _totalPages)
                {
                    _currentPage = pageNumber;
                    await LoadUsersAsync();
                }
                else
                {
                    PageNumberTextBox.Text = _currentPage.ToString();
                }
            }
        }

        private async void PageSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PageSizeComboBox.SelectedItem is ComboBoxItem item && 
                int.TryParse(item.Content.ToString(), out int pageSize))
            {
                _pageSize = pageSize;
                _currentPage = 1;
                await LoadUsersAsync();
            }
        }
    }

    // 用户显示模型
    public class UserDisplayModel
    {
        private readonly User _user;

        public UserDisplayModel(User user)
        {
            _user = user;
        }

        public int Id => _user.UserId;
        public string Username => _user.Username;
        public string FullName => _user.RealName ?? string.Empty;
        public string Email => _user.Email ?? string.Empty;
        public UserRole Role => _user.Role;
        public DateTime CreatedAt => _user.CreatedAt;
        public DateTime? LastLoginAt => _user.LastLoginAt;
        public bool IsActive => _user.IsActive;

        public string RoleDisplay => Role switch
        {
            UserRole.Admin => "管理员",
            UserRole.Teacher => "教师",
            UserRole.Student => "学生",
            _ => "未知"
        };

        public string StatusDisplay => IsActive ? "启用" : "禁用";
        public string StatusColor => IsActive ? "#4CAF50" : "#F44336";
        public string StatusToggleIcon => IsActive ? "🚫" : "✅";
        public string StatusToggleTooltip => IsActive ? "禁用用户" : "启用用户";
    }
}