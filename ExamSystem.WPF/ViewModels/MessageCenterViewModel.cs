using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ExamSystem.Domain.Entities;
using ExamSystem.Services.Interfaces;
using ExamSystem.Services.Models;
using ExamSystem.WPF.Commands;
using ExamSystem.WPF.ViewModels.Base;

namespace ExamSystem.WPF.ViewModels
{
    /// <summary>
    /// 学生端消息中心 ViewModel
    /// </summary>
    public class MessageCenterViewModel : BaseViewModel
    {
        private readonly INotificationService _notificationService;

        public MessageCenterViewModel(INotificationService notificationService)
        {
            _notificationService = notificationService;
            Notifications = new ObservableCollection<NotificationDto>();

            PageSize = 10;

            RefreshCommand = new RelayCommand(async () => await LoadDataAsync());
            MarkAsReadCommand = new RelayCommand(async () => await MarkSelectedAsReadAsync(), () => SelectedNotification != null && !SelectedNotification.IsRead);
            NextPageCommand = new RelayCommand(async () => await ChangePageAsync(PageIndex + 1), () => PageIndex < TotalPages - 1);
            PrevPageCommand = new RelayCommand(async () => await ChangePageAsync(PageIndex - 1), () => PageIndex > 0);
        }

        private int _currentUserId;
        public void SetCurrentUser(User user)
        {
            _currentUserId = user.UserId;
            // 首次加载
            _ = LoadDataAsync();
        }

        // 新增：筛选索引（0=全部，1=未读，2=已读）
        private int _selectedFilterIndex = 0;
        public int SelectedFilterIndex
        {
            get => _selectedFilterIndex;
            set
            {
                if (SetProperty(ref _selectedFilterIndex, value))
                {
                    switch (value)
                    {
                        case 0: FilterIsRead = null; break;
                        case 1: FilterIsRead = false; break;
                        case 2: FilterIsRead = true; break;
                        default: FilterIsRead = null; break;
                    }
                    _ = LoadDataAsync();
                }
            }
        }

        public ObservableCollection<NotificationDto> Notifications { get; }

        private NotificationDto? _selectedNotification;
        public NotificationDto? SelectedNotification
        {
            get => _selectedNotification;
            set
            {
                if (SetProperty(ref _selectedNotification, value))
                {
                    // 更新标记已读按钮可用状态
                    (MarkAsReadCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        private bool? _filterIsRead = null; // null=全部, true=已读, false=未读
        public bool? FilterIsRead
        {
            get => _filterIsRead;
            set
            {
                if (SetProperty(ref _filterIsRead, value))
                {
                    _ = LoadDataAsync();
                }
            }
        }

        private int _pageIndex;
        public int PageIndex
        {
            get => _pageIndex;
            set
            {
                if (SetProperty(ref _pageIndex, value))
                {
                    (NextPageCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (PrevPageCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        private int _pageSize;
        public int PageSize
        {
            get => _pageSize;
            set => SetProperty(ref _pageSize, value);
        }

        private int _totalCount;
        public int TotalCount
        {
            get => _totalCount;
            set
            {
                if (SetProperty(ref _totalCount, value))
                {
                    OnPropertyChanged(nameof(TotalPages));
                    (NextPageCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (PrevPageCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);

        public ICommand RefreshCommand { get; }
        public ICommand MarkAsReadCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PrevPageCommand { get; }

        private async Task LoadDataAsync()
        {
            if (_currentUserId <= 0) return;

            var (items, totalCount) = await _notificationService.GetUserNotificationsAsync(_currentUserId, FilterIsRead, PageIndex, PageSize);

            Notifications.Clear();
            foreach (var item in items)
            {
                Notifications.Add(item);
            }
            TotalCount = totalCount;
        }

        private async Task ChangePageAsync(int newIndex)
        {
            if (newIndex < 0 || newIndex >= TotalPages) return;
            PageIndex = newIndex;
            await LoadDataAsync();
        }

        private async Task MarkSelectedAsReadAsync()
        {
            if (_currentUserId <= 0 || SelectedNotification == null) return;

            var ok = await _notificationService.MarkAsReadAsync(SelectedNotification.NotificationId, _currentUserId);
            if (ok)
            {
                SelectedNotification.IsRead = true;
                SelectedNotification.ReadAt = DateTime.Now;
                // 触发列表刷新
                _ = LoadDataAsync();
            }
        }
    }
}