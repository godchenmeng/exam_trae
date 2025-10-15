using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using ExamSystem.Models.Entities;
using ExamSystem.Models.Enums;
using ExamSystem.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace ExamSystem.WPF.ViewModels
{
    public class UserManagementViewModel : INotifyPropertyChanged
    {
        private readonly IUserService _userService;
        private readonly ILogger<UserManagementViewModel> _logger;

        public UserManagementViewModel(IUserService userService, ILogger<UserManagementViewModel> logger)
        {
            _userService = userService;
            _logger = logger;

            Users = new ObservableCollection<UserDisplayModel>();
            FilteredUsers = new ObservableCollection<UserDisplayModel>();

            // 初始化命令
            LoadUsersCommand = new RelayCommand(async () => await LoadUsersAsync());
            SearchCommand = new RelayCommand(async () => await SearchUsersAsync());
            AddUserCommand = new RelayCommand(AddUser);
            ImportUsersCommand = new RelayCommand(ImportUsers);
            ExportUsersCommand = new RelayCommand(ExportUsers);
            EditUserCommand = new RelayCommand<UserDisplayModel>(EditUser);
            ResetPasswordCommand = new RelayCommand<UserDisplayModel>(ResetPassword);
            ToggleStatusCommand = new RelayCommand<UserDisplayModel>(ToggleStatus);
            DeleteUserCommand = new RelayCommand<UserDisplayModel>(DeleteUser);
            FirstPageCommand = new RelayCommand(FirstPage, () => CurrentPage > 1);
            PreviousPageCommand = new RelayCommand(PreviousPage, () => CurrentPage > 1);
            NextPageCommand = new RelayCommand(NextPage, () => CurrentPage < TotalPages);
            LastPageCommand = new RelayCommand(LastPage, () => CurrentPage < TotalPages);

            // 初始化筛选选项
            RoleOptions = Enum.GetValues<UserRole>().ToList();
            StatusOptions = new[] { "全部", "启用", "禁用" }.ToList();

            // 设置默认值
            SelectedRole = null;
            SelectedStatus = "全部";
            PageSize = 20;
            CurrentPage = 1;
        }

        #region Properties

        public ObservableCollection<UserDisplayModel> Users { get; }
        public ObservableCollection<UserDisplayModel> FilteredUsers { get; }

        private string _searchKeyword;
        public string SearchKeyword
        {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
        }

        private UserRole? _selectedRole;
        public UserRole? SelectedRole
        {
            get => _selectedRole;
            set => SetProperty(ref _selectedRole, value);
        }

        private string _selectedStatus;
        public string SelectedStatus
        {
            get => _selectedStatus;
            set => SetProperty(ref _selectedStatus, value);
        }

        public List<UserRole> RoleOptions { get; }
        public List<string> StatusOptions { get; }

        // 分页属性
        private int _currentPage;
        public int CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
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
            set => SetProperty(ref _totalCount, value);
        }

        private int _totalPages;
        public int TotalPages
        {
            get => _totalPages;
            set => SetProperty(ref _totalPages, value);
        }

        public string PaginationInfo => $"第 {CurrentPage} 页，共 {TotalPages} 页，总计 {TotalCount} 条记录";

        #endregion

        #region Commands

        public ICommand LoadUsersCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand AddUserCommand { get; }
        public ICommand ImportUsersCommand { get; }
        public ICommand ExportUsersCommand { get; }
        public ICommand EditUserCommand { get; }
        public ICommand ResetPasswordCommand { get; }
        public ICommand ToggleStatusCommand { get; }
        public ICommand DeleteUserCommand { get; }
        public ICommand FirstPageCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand LastPageCommand { get; }

        #endregion

        #region Methods

        public async Task LoadUsersAsync()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                
                Users.Clear();
                foreach (var user in users)
                {
                    Users.Add(new UserDisplayModel
                    {
                        Id = user.Id,
                        Username = user.Username,
                        FullName = user.FullName,
                        Email = user.Email,
                        Role = user.Role,
                        RoleDisplay = GetRoleDisplay(user.Role),
                        CreatedAt = user.CreatedAt,
                        LastLoginAt = user.LastLoginAt,
                        IsActive = user.IsActive,
                        StatusDisplay = user.IsActive ? "启用" : "禁用"
                    });
                }

                await ApplyFiltersAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载用户数据时发生错误");
            }
        }

        private async Task SearchUsersAsync()
        {
            await ApplyFiltersAsync();
        }

        private async Task ApplyFiltersAsync()
        {
            try
            {
                var filteredUsers = Users.AsEnumerable();

                // 应用搜索关键词筛选
                if (!string.IsNullOrWhiteSpace(SearchKeyword))
                {
                    filteredUsers = filteredUsers.Where(u =>
                        u.Username.Contains(SearchKeyword, StringComparison.OrdinalIgnoreCase) ||
                        u.FullName.Contains(SearchKeyword, StringComparison.OrdinalIgnoreCase) ||
                        u.Email.Contains(SearchKeyword, StringComparison.OrdinalIgnoreCase));
                }

                // 应用角色筛选
                if (SelectedRole.HasValue)
                {
                    filteredUsers = filteredUsers.Where(u => u.Role == SelectedRole.Value);
                }

                // 应用状态筛选
                if (SelectedStatus != "全部")
                {
                    var isActive = SelectedStatus == "启用";
                    filteredUsers = filteredUsers.Where(u => u.IsActive == isActive);
                }

                var filteredList = filteredUsers.ToList();
                TotalCount = filteredList.Count;
                TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);

                // 应用分页
                var pagedUsers = filteredList
                    .Skip((CurrentPage - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();

                FilteredUsers.Clear();
                foreach (var user in pagedUsers)
                {
                    FilteredUsers.Add(user);
                }

                OnPropertyChanged(nameof(PaginationInfo));
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "应用筛选时发生错误");
            }
        }

        private void AddUser()
        {
            // TODO: 实现添加用户功能
            _logger.LogInformation("添加用户功能被调用");
        }

        private void ImportUsers()
        {
            // TODO: 实现导入用户功能
            _logger.LogInformation("导入用户功能被调用");
        }

        private void ExportUsers()
        {
            // TODO: 实现导出用户功能
            _logger.LogInformation("导出用户功能被调用");
        }

        private void EditUser(UserDisplayModel user)
        {
            if (user == null) return;
            // TODO: 实现编辑用户功能
            _logger.LogInformation($"编辑用户功能被调用，用户ID: {user.Id}");
        }

        private async void ResetPassword(UserDisplayModel user)
        {
            if (user == null) return;
            
            try
            {
                // TODO: 实现重置密码功能
                _logger.LogInformation($"重置密码功能被调用，用户ID: {user.Id}");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"重置用户密码时发生错误，用户ID: {user.Id}");
            }
        }

        private async void ToggleStatus(UserDisplayModel user)
        {
            if (user == null) return;

            try
            {
                // TODO: 实现切换用户状态功能
                user.IsActive = !user.IsActive;
                user.StatusDisplay = user.IsActive ? "启用" : "禁用";
                
                _logger.LogInformation($"切换用户状态功能被调用，用户ID: {user.Id}，新状态: {user.StatusDisplay}");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"切换用户状态时发生错误，用户ID: {user.Id}");
            }
        }

        private async void DeleteUser(UserDisplayModel user)
        {
            if (user == null) return;

            try
            {
                // TODO: 实现删除用户功能（需要确认对话框）
                _logger.LogInformation($"删除用户功能被调用，用户ID: {user.Id}");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"删除用户时发生错误，用户ID: {user.Id}");
            }
        }

        private async void FirstPage()
        {
            CurrentPage = 1;
            await ApplyFiltersAsync();
        }

        private async void PreviousPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                await ApplyFiltersAsync();
            }
        }

        private async void NextPage()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                await ApplyFiltersAsync();
            }
        }

        private async void LastPage()
        {
            CurrentPage = TotalPages;
            await ApplyFiltersAsync();
        }

        private string GetRoleDisplay(UserRole role)
        {
            return role switch
            {
                UserRole.Admin => "管理员",
                UserRole.Teacher => "教师",
                UserRole.Student => "学生",
                _ => "未知"
            };
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }

    // 用户显示模型
    public class UserDisplayModel : INotifyPropertyChanged
    {
        private bool _isActive;
        private string _statusDisplay;

        public int Id { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public UserRole Role { get; set; }
        public string RoleDisplay { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }

        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        public string StatusDisplay
        {
            get => _statusDisplay;
            set => SetProperty(ref _statusDisplay, value);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    // 支持泛型参数的RelayCommand
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;

        public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke((T)parameter) ?? true;

        public void Execute(object parameter) => _execute((T)parameter);
    }
}