using CommunityToolkit.Mvvm.Input;
using ExamSystem.Services.Interfaces;
using ExamSystem.Domain.Entities;
using ExamSystem.UI.Views;
using System.Windows;

namespace ExamSystem.UI.ViewModels;

/// <summary>
/// 登录页面ViewModel
/// </summary>
public partial class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _rememberMe;

    public LoginViewModel(IAuthService authService)
    {
        _authService = authService;
        LoadSavedCredentials();
    }

    /// <summary>
    /// 用户名
    /// </summary>
    public string Username
    {
        get => _username;
        set
        {
            if (SetProperty(ref _username, value))
            {
                ErrorMessage = string.Empty;
                LoginCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// 密码
    /// </summary>
    public string Password
    {
        get => _password;
        set
        {
            if (SetProperty(ref _password, value))
            {
                ErrorMessage = string.Empty;
                LoginCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    /// <summary>
    /// 记住我
    /// </summary>
    public bool RememberMe
    {
        get => _rememberMe;
        set => SetProperty(ref _rememberMe, value);
    }

    /// <summary>
    /// 登录命令
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync()
    {
        await ExecuteAsync(async () =>
        {
            ErrorMessage = string.Empty;

            var result = await _authService.LoginAsync(Username, Password);
            
            if (result.Success && result.User != null)
            {
                // 保存登录凭据
                if (RememberMe)
                {
                    SaveCredentials();
                }
                else
                {
                    ClearSavedCredentials();
                }

                // 设置当前用户
                CurrentUser.Instance.SetUser(result.User);

                // 打开主窗口
                var mainWindow = new MainWindow();
                mainWindow.Show();

                // 关闭登录窗口
                Application.Current.Windows.OfType<LoginWindow>().FirstOrDefault()?.Close();
            }
            else
            {
                ErrorMessage = result.Message;
            }
        }, "正在登录...");
    }

    /// <summary>
    /// 退出命令
    /// </summary>
    [RelayCommand]
    private void Exit()
    {
        Application.Current.Shutdown();
    }

    /// <summary>
    /// 清除错误消息命令
    /// </summary>
    [RelayCommand]
    private void ClearError()
    {
        ErrorMessage = string.Empty;
    }

    /// <summary>
    /// 是否可以登录
    /// </summary>
    private bool CanLogin()
    {
        return !string.IsNullOrWhiteSpace(Username) && 
               !string.IsNullOrWhiteSpace(Password) && 
               !IsBusy;
    }

    /// <summary>
    /// 加载保存的凭据
    /// </summary>
    private void LoadSavedCredentials()
    {
        try
        {
            var settings = Properties.Settings.Default;
            if (settings.RememberMe)
            {
                Username = settings.Username ?? string.Empty;
                RememberMe = true;
            }
        }
        catch
        {
            // 忽略加载错误
        }
    }

    /// <summary>
    /// 保存凭据
    /// </summary>
    private void SaveCredentials()
    {
        try
        {
            var settings = Properties.Settings.Default;
            settings.Username = Username;
            settings.RememberMe = RememberMe;
            settings.Save();
        }
        catch
        {
            // 忽略保存错误
        }
    }

    /// <summary>
    /// 清除保存的凭据
    /// </summary>
    private void ClearSavedCredentials()
    {
        try
        {
            var settings = Properties.Settings.Default;
            settings.Username = string.Empty;
            settings.RememberMe = false;
            settings.Save();
        }
        catch
        {
            // 忽略清除错误
        }
    }
}

/// <summary>
/// 当前用户单例
/// </summary>
public class CurrentUser
{
    private static CurrentUser? _instance;
    private static readonly object _lock = new();

    public static CurrentUser Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new CurrentUser();
                }
            }
            return _instance;
        }
    }

    public User? User { get; private set; }

    public void SetUser(User user)
    {
        User = user;
    }

    public void Clear()
    {
        User = null;
    }

    public bool IsLoggedIn => User != null;

    public bool IsAdmin => User?.Role == UserRole.Admin;
    public bool IsTeacher => User?.Role == UserRole.Teacher;
    public bool IsStudent => User?.Role == UserRole.Student;
}