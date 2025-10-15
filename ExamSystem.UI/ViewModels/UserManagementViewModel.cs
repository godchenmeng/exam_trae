using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;
using ExamSystem.Services.Interfaces;
using ExamSystem.UI.Views;

namespace ExamSystem.UI.ViewModels
{
    public partial class UserManagementViewModel : BaseViewModel
    {
        private readonly IUserService _userService;
        private readonly IAuthService _authService;

        [ObservableProperty]
        private ObservableCollection<User> users = new();

        [ObservableProperty]
        private ICollectionView usersView;

        [ObservableProperty]
        private User? selectedUser;

        [ObservableProperty]
        private string searchText = string.Empty;

        [ObservableProperty]
        private UserRole? selectedRole;

        [ObservableProperty]
        private bool? selectedStatus;

        [ObservableProperty]
        private int currentPage = 1;

        [ObservableProperty]
        private int pageSize = 20;

        [ObservableProperty]
        private int totalPages = 1;

        [ObservableProperty]
        private int totalCount = 0;

        [ObservableProperty]
        private bool isAddUserDialogOpen;

        [ObservableProperty]
        private bool isEditUserDialogOpen;

        [ObservableProperty]
        private User newUser = new();

        [ObservableProperty]
        private User editingUser = new();

        [ObservableProperty]
        private string newUserPassword = string.Empty;

        [ObservableProperty]
        private string confirmPassword = string.Empty;

        public UserManagementViewModel(IUserService userService, IAuthService authService)
        {
            _userService = userService;
            _authService = authService;

            // 初始化用户集合视图
            UsersView = CollectionViewSource.GetDefaultView(Users);
            UsersView.Filter = FilterUsers;

            // 加载数据
            _ = LoadUsersAsync();
        }

        public Array UserRoles => Enum.GetValues(typeof(UserRole));
        public Array StatusOptions => new object[] { true, false };

        private bool FilterUsers(object obj)
        {
            if (obj is not User user) return false;

            // 搜索文本过滤
            if (!string.IsNullOrEmpty(SearchText))
            {
                var searchLower = SearchText.ToLower();
                if (!user.Username.ToLower().Contains(searchLower) &&
                    !user.RealName.ToLower().Contains(searchLower) &&
                    !(user.Email?.ToLower().Contains(searchLower) ?? false))
                {
                    return false;
                }
            }

            // 角色过滤
            if (SelectedRole.HasValue && user.Role != SelectedRole.Value)
            {
                return false;
            }

            // 状态过滤
            if (SelectedStatus.HasValue && user.IsActive != SelectedStatus.Value)
            {
                return false;
            }

            return true;
        }

        [RelayCommand]
        private async Task LoadUsersAsync()
        {
            await ExecuteAsync(async () =>
            {
                var result = await _userService.GetUsersPagedAsync(CurrentPage, PageSize);
                
                Users.Clear();
                foreach (var user in result.Items)
                {
                    Users.Add(user);
                }

                TotalCount = result.TotalCount;
                TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);

                UsersView.Refresh();
            }, "正在加载用户数据...");
        }

        [RelayCommand]
        private void Search()
        {
            UsersView.Refresh();
        }

        [RelayCommand]
        private void ClearSearch()
        {
            SearchText = string.Empty;
            SelectedRole = null;
            SelectedStatus = null;
            UsersView.Refresh();
        }

        [RelayCommand]
        private void OpenAddUserDialog()
        {
            NewUser = new User
            {
                Role = UserRole.Student,
                IsActive = true,
                CreatedAt = DateTime.Now
            };
            NewUserPassword = string.Empty;
            ConfirmPassword = string.Empty;
            IsAddUserDialogOpen = true;
        }

        [RelayCommand]
        private void CloseAddUserDialog()
        {
            IsAddUserDialogOpen = false;
        }

        [RelayCommand]
        private async Task AddUserAsync()
        {
            if (!await ValidateNewUserAsync()) return;

            await ExecuteAsync(async () =>
            {
                var result = await _authService.RegisterAsync(NewUser, NewUserPassword);

                if (result.Success)
                {
                    IsAddUserDialogOpen = false;
                    // 重置表单
                    NewUser = new User();
                    NewUserPassword = string.Empty;
                    ConfirmPassword = string.Empty;
                    
                    await LoadUsersAsync();
                    ShowMessage("用户添加成功！", MessageType.Success);
                }
                else
                {
                    ShowMessage($"添加用户失败：{result.Message}", MessageType.Error);
                }
            }, "正在添加用户...");
        }

        [RelayCommand]
        private void OpenEditUserDialog()
        {
            if (SelectedUser == null) return;

            EditingUser = new User
            {
                UserId = SelectedUser.UserId,
                Username = SelectedUser.Username,
                RealName = SelectedUser.RealName,
                Email = SelectedUser.Email,
                Role = SelectedUser.Role,
                IsActive = SelectedUser.IsActive,
                CreatedAt = SelectedUser.CreatedAt,
                LastLoginTime = SelectedUser.LastLoginTime,
                LoginAttempts = SelectedUser.LoginAttempts,
                IsLocked = SelectedUser.IsLocked,
                LockoutEnd = SelectedUser.LockoutEnd
            };

            IsEditUserDialogOpen = true;
        }

        [RelayCommand]
        private void CloseEditUserDialog()
        {
            IsEditUserDialogOpen = false;
        }

        [RelayCommand]
        private async Task UpdateUserAsync()
        {
            if (!ValidateEditingUser()) return;

            await ExecuteAsync(async () =>
            {
                var result = await _userService.UpdateUserAsync(EditingUser);
                if (result)
                {
                    IsEditUserDialogOpen = false;
                    await LoadUsersAsync();
                    ShowMessage("用户信息更新成功！", MessageType.Success);
                }
                else
                {
                    ShowMessage("更新用户信息失败！", MessageType.Error);
                }
            }, "正在更新用户信息...");
        }

        [RelayCommand]
        private async Task DeleteUserAsync()
        {
            if (SelectedUser == null) return;

            if (SelectedUser.Username == "admin")
            {
                ShowMessage("不能删除系统管理员账户！", MessageType.Warning);
                return;
            }

            var confirmed = await ShowConfirmation(
                $"确定要删除用户 '{SelectedUser.RealName}' 吗？\n此操作不可撤销！");

            if (!confirmed) return;

            await ExecuteAsync(async () =>
            {
                var result = await _userService.DeleteUserAsync(SelectedUser.UserId);
                if (result)
                {
                    await LoadUsersAsync();
                    ShowMessage("用户删除成功！", MessageType.Success);
                }
                else
                {
                    ShowMessage("删除用户失败！", MessageType.Error);
                }
            }, "正在删除用户...");
        }

        [RelayCommand]
        private async Task LockUserAsync()
        {
            if (SelectedUser == null || SelectedUser.IsLocked) return;

            if (SelectedUser.Username == "admin")
            {
                ShowMessage("不能锁定系统管理员账户！", MessageType.Warning);
                return;
            }

            // 创建锁定时长选择对话框
            var lockDurationDialog = new LockDurationDialog();
            var dialogResult = lockDurationDialog.ShowDialog();

            if (dialogResult != true) return;

            var lockDuration = lockDurationDialog.LockDuration;
            var confirmed = ShowConfirmation(
                $"确定要锁定用户 '{SelectedUser.RealName}' {lockDuration.TotalHours}小时吗？");

            if (!confirmed) return;

            await ExecuteAsync(async () =>
            {
                var lockUntil = DateTime.Now.Add(lockDuration);
                var result = await _authService.LockUserAsync(SelectedUser.UserId, lockUntil);
                if (result)
                {
                    await LoadUsersAsync();
                    ShowMessage($"用户已锁定至 {lockUntil:yyyy-MM-dd HH:mm}", MessageType.Success);
                }
                else
                {
                    ShowMessage("锁定用户失败！", MessageType.Error);
                }
            }, "正在锁定用户...");
        }

        [RelayCommand]
        private async Task ToggleUserStatusAsync()
        {
            if (SelectedUser == null) return;

            if (SelectedUser.Username == "admin")
            {
                ShowMessage("不能禁用系统管理员账户！", MessageType.Warning);
                return;
            }

            var action = SelectedUser.IsActive ? "禁用" : "启用";
            var confirmed = await ShowConfirmation($"确定要{action}用户 '{SelectedUser.RealName}' 吗？");

            if (!confirmed) return;

            await ExecuteAsync(async () =>
            {
                var result = await _userService.ToggleUserStatusAsync(SelectedUser.UserId);
                if (result)
                {
                    await LoadUsersAsync();
                    ShowMessage($"用户{action}成功！", MessageType.Success);
                }
                else
                {
                    ShowMessage($"{action}用户失败！", MessageType.Error);
                }
            }, $"正在{action}用户...");
        }

        [RelayCommand]
        private async Task UnlockUserAsync()
        {
            if (SelectedUser == null || !SelectedUser.IsLocked) return;

            var confirmed = await ShowConfirmation($"确定要解锁用户 '{SelectedUser.RealName}' 吗？");

            if (!confirmed) return;

            await ExecuteAsync(async () =>
            {
                var result = await _authService.UnlockUserAsync(SelectedUser.UserId);
                if (result)
                {
                    await LoadUsersAsync();
                    ShowMessage("用户解锁成功！", MessageType.Success);
                }
                else
                {
                    ShowMessage("用户解锁失败！", MessageType.Error);
                }
            }, "正在解锁用户...");
        }

        [RelayCommand]
        private async Task ResetPasswordAsync()
        {
            if (SelectedUser == null) return;

            // 创建密码重置对话框
            var resetPasswordDialog = new ResetPasswordDialog();
            var dialogResult = resetPasswordDialog.ShowDialog();

            if (dialogResult != true) return;

            var newPassword = resetPasswordDialog.NewPassword;
            var confirmed = ShowConfirmation(
                $"确定要重置用户 '{SelectedUser.RealName}' 的密码吗？");

            if (!confirmed) return;

            await ExecuteAsync(async () =>
            {
                // 验证新密码强度
                var passwordValidation = _authService.ValidatePassword(newPassword);
                if (!passwordValidation.IsValid)
                {
                    ShowMessage($"密码强度不够！{passwordValidation.Message}", MessageType.Warning);
                    return;
                }

                var result = await _authService.ResetPasswordAsync(SelectedUser.UserId, newPassword);
                if (result.Success)
                {
                    ShowMessage("密码重置成功！", MessageType.Success);
                }
                else
                {
                    ShowMessage($"密码重置失败：{result.Message}", MessageType.Error);
                }
            }, "正在重置密码...");
        }

        [RelayCommand]
        private async Task ExportUsersAsync()
        {
            await ExecuteAsync(async () =>
            {
                var allUsers = await _userService.GetAllUsersAsync();
                var result = await _userService.ExportUsersAsync(allUsers);
                
                if (result.Success)
                {
                    ShowMessage($"用户数据导出成功！文件保存在：{result.FilePath}", MessageType.Success);
                }
                else
                {
                    ShowMessage($"导出失败：{result.Message}", MessageType.Error);
                }
            }, "正在导出用户数据...");
        }

        [RelayCommand]
        private async Task ImportUsersAsync()
        {
            // 这里应该打开文件选择对话框，简化处理
            var confirmed = await ShowConfirmation("确定要导入用户数据吗？请确保Excel文件格式正确。");
            if (!confirmed) return;

            await ExecuteAsync(async () =>
            {
                // 实际实现中应该选择文件
                var filePath = "users_import.xlsx"; // 示例文件路径
                var result = await _userService.ImportUsersAsync(filePath);
                
                if (result.Success)
                {
                    await LoadUsersAsync();
                    ShowMessage($"用户数据导入成功！导入了 {result.ImportedCount} 个用户。", MessageType.Success);
                }
                else
                {
                    ShowMessage($"导入失败：{result.Message}", MessageType.Error);
                }
            }, "正在导入用户数据...");
        }

        [RelayCommand]
        private async Task PreviousPageAsync()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                await LoadUsersAsync();
            }
        }

        [RelayCommand]
        private async Task NextPageAsync()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                await LoadUsersAsync();
            }
        }

        [RelayCommand]
        private async Task GoToPageAsync(int page)
        {
            if (page >= 1 && page <= TotalPages && page != CurrentPage)
            {
                CurrentPage = page;
                await LoadUsersAsync();
            }
        }

        private async Task<bool> ValidateNewUserAsync()
        {
            // 基本字段验证
            if (string.IsNullOrWhiteSpace(NewUser.Username))
            {
                ShowMessage("用户名不能为空！", MessageType.Warning);
                return false;
            }

            // 用户名格式验证
            if (NewUser.Username.Length < 3 || NewUser.Username.Length > 20)
            {
                ShowMessage("用户名长度必须在3-20个字符之间！", MessageType.Warning);
                return false;
            }

            // 用户名字符验证
            if (!System.Text.RegularExpressions.Regex.IsMatch(NewUser.Username, @"^[a-zA-Z0-9_]+$"))
            {
                ShowMessage("用户名只能包含字母、数字和下划线！", MessageType.Warning);
                return false;
            }

            // 检查用户名是否已存在
            try
            {
                var usernameExists = await _authService.IsUsernameExistsAsync(NewUser.Username);
                if (usernameExists)
                {
                    ShowMessage("用户名已存在，请选择其他用户名！", MessageType.Warning);
                    return false;
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"验证用户名时发生错误：{ex.Message}", MessageType.Error);
                return false;
            }

            if (string.IsNullOrWhiteSpace(NewUser.RealName))
            {
                ShowMessage("真实姓名不能为空！", MessageType.Warning);
                return false;
            }

            // 真实姓名长度验证
            if (NewUser.RealName.Length > 50)
            {
                ShowMessage("真实姓名长度不能超过50个字符！", MessageType.Warning);
                return false;
            }

            // 邮箱验证
            if (!string.IsNullOrWhiteSpace(NewUser.Email))
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(NewUser.Email, 
                    @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"))
                {
                    ShowMessage("邮箱格式不正确！", MessageType.Warning);
                    return false;
                }
            }

            // 手机号验证
            if (!string.IsNullOrWhiteSpace(NewUser.Phone))
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(NewUser.Phone, @"^1[3-9]\d{9}$"))
                {
                    ShowMessage("手机号格式不正确！", MessageType.Warning);
                    return false;
                }
            }

            if (string.IsNullOrWhiteSpace(NewUserPassword))
            {
                ShowMessage("密码不能为空！", MessageType.Warning);
                return false;
            }

            if (NewUserPassword != ConfirmPassword)
            {
                ShowMessage("两次输入的密码不一致！", MessageType.Warning);
                return false;
            }

            var passwordValidation = _authService.ValidatePassword(NewUserPassword);
            if (!passwordValidation.IsValid)
            {
                ShowMessage($"密码强度不够！{passwordValidation.Message}", MessageType.Warning);
                return false;
            }

            return true;
        }

        private bool ValidateEditingUser()
        {
            if (string.IsNullOrWhiteSpace(EditingUser.RealName))
            {
                ShowMessage("真实姓名不能为空！", MessageType.Warning);
                return false;
            }

            // 真实姓名长度验证
            if (EditingUser.RealName.Length > 50)
            {
                ShowMessage("真实姓名长度不能超过50个字符！", MessageType.Warning);
                return false;
            }

            // 邮箱验证
            if (!string.IsNullOrWhiteSpace(EditingUser.Email))
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(EditingUser.Email, 
                    @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"))
                {
                    ShowMessage("邮箱格式不正确！", MessageType.Warning);
                    return false;
                }
            }

            // 手机号验证
            if (!string.IsNullOrWhiteSpace(EditingUser.Phone))
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(EditingUser.Phone, @"^1[3-9]\d{9}$"))
                {
                    ShowMessage("手机号格式不正确！", MessageType.Warning);
                    return false;
                }
            }

            return true;
        }

        partial void OnSearchTextChanged(string value)
        {
            UsersView?.Refresh();
        }

        partial void OnSelectedRoleChanged(UserRole? value)
        {
            UsersView?.Refresh();
        }

        partial void OnSelectedStatusChanged(bool? value)
        {
            UsersView?.Refresh();
        }
    }
}