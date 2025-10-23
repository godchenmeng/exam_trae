using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Collections.Generic;
using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;
using ExamSystem.Services.Interfaces;
using ExamSystem.Services.Models;
using ExamSystem.WPF.Commands;
using ExamSystem.WPF.ViewModels.Base;

namespace ExamSystem.WPF.ViewModels
{
    /// <summary>
    /// 教师/管理员端通知发送 ViewModel
    /// </summary>
    public class NotificationSendViewModel : ExamSystem.WPF.ViewModels.Base.BaseViewModel
    {
        private readonly INotificationService _notificationService;
        private readonly IUserService _userService;
        private readonly IPermissionService _permissionService;

        public NotificationSendViewModel(INotificationService notificationService, IUserService userService, IPermissionService permissionService)
        {
            _notificationService = notificationService;
            _userService = userService;
            _permissionService = permissionService;

            SelectedPriority = NotificationPriority.Normal;
            SelectedScope = NotificationScope.AllStudents;

            SendCommand = new RelayCommand(async () => await SendAsync(), CanSend);
            LoadSpecificUsersCommand = new RelayCommand(async () => await LoadSpecificUsersAsync());
        }

        private int _currentUserId;
        private UserRole _currentUserRole;
        public void SetCurrentUser(User user)
        {
            _currentUserId = user.UserId;
            _currentUserRole = user.Role;
            (SendCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private string _title = string.Empty;
        public string Title
        {
            get => _title;
            set
            {
                if (SetProperty(ref _title, value))
                {
                    (SendCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        private string _content = string.Empty;
        public string Content
        {
            get => _content;
            set
            {
                if (SetProperty(ref _content, value))
                {
                    (SendCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        private NotificationPriority _selectedPriority;
        public NotificationPriority SelectedPriority
        {
            get => _selectedPriority;
            set => SetProperty(ref _selectedPriority, value);
        }

        // UI 绑定所需：优先级和范围的枚举列表
        public IEnumerable<NotificationPriority> Priorities => Enum.GetValues(typeof(NotificationPriority)).Cast<NotificationPriority>();
        public IEnumerable<NotificationScope> SendRanges => new[] { NotificationScope.AllStudents, NotificationScope.SpecificUsers };

        private NotificationScope _selectedScope;
        public NotificationScope SelectedScope
        {
            get => _selectedScope;
            set
            {
                if (SetProperty(ref _selectedScope, value))
                {
                    // 当切换为指定用户时，加载学生列表供选择
                    if (value == NotificationScope.SpecificUsers)
                    {
                        _ = LoadSpecificUsersAsync();
                    }
                    // 让 SelectedSendRange 的绑定也收到通知
                    OnPropertyChanged(nameof(SelectedSendRange));
                }
            }
        }

        // 供 XAML 绑定的别名，使视图无需改动
        public NotificationScope SelectedSendRange
        {
            get => SelectedScope;
            set => SelectedScope = value;
        }

        // 用户选择列表与选中集合（供 ListBox SelectedItems 行为绑定）
        public ObservableCollection<SelectableUser> AvailableUsers { get; } = new ObservableCollection<SelectableUser>();
        public ObservableCollection<SelectableUser> Users => AvailableUsers;
        public ObservableCollection<SelectableUser> SelectedUsers { get; } = new ObservableCollection<SelectableUser>();

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public ICommand SendCommand { get; }
        public ICommand LoadSpecificUsersCommand { get; }

        private bool CanSend()
        {
            if (_currentUserId <= 0) return false;
            if (!_permissionService.HasPermission(_currentUserRole, PermissionKeys.SendNotification)) return false;
            if (string.IsNullOrWhiteSpace(Title) || string.IsNullOrWhiteSpace(Content)) return false;
            if (SelectedScope == NotificationScope.SpecificUsers && !SelectedUsers.Any()) return false;
            return true;
        }

        private async Task LoadSpecificUsersAsync()
        {
            try
            {
                IsBusy = true;
                AvailableUsers.Clear();
                SelectedUsers.Clear();
                var students = await _userService.GetUsersByRoleAsync(UserRole.Student);
                foreach (var s in students)
                {
                    AvailableUsers.Add(new SelectableUser(s));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载学生列表失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SendAsync()
        {
            try
            {
                IsBusy = true;

                var targetIds = SelectedScope == NotificationScope.SpecificUsers
                    ? SelectedUsers.Select(u => u.UserId).ToList()
                    : null;

                var (ok, error) = await _notificationService.SendAsync(_currentUserId, Title, Content, SelectedPriority, SelectedScope, targetIds);
                if (ok)
                {
                    MessageBox.Show("通知发送成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    Title = string.Empty;
                    Content = string.Empty;
                    SelectedUsers.Clear();
                }
                else
                {
                    MessageBox.Show(error ?? "发送失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"发送通知时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    /// <summary>
    /// 供界面绑定选择的用户项
    /// </summary>
    public class SelectableUser : ExamSystem.WPF.ViewModels.Base.BaseViewModel
    {
        private readonly User _user;
        public SelectableUser(User user)
        {
            _user = user;
        }

        public int UserId => _user.UserId;
        public string DisplayName => string.IsNullOrWhiteSpace(_user.RealName) ? _user.Username : _user.RealName;

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }
}