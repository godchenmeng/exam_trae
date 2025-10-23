using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using ExamSystem.WPF.Commands;
using ExamSystem.Services.Interfaces;
using ExamSystem.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace ExamSystem.WPF.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private readonly IAuthService _authService;
        private readonly ILogger<LoginViewModel> _logger;

        public LoginViewModel(IAuthService authService, ILogger<LoginViewModel> logger)
        {
            _authService = authService;
            _logger = logger;

            // 初始化属性
            _username = string.Empty;
            _password = string.Empty;
            _errorMessage = string.Empty;
            _isLoading = false;

            LoginCommand = new RelayCommand(async () => await LoginAsync(), CanLogin);
            CloseCommand = new RelayCommand(Close);
        }

        #region Properties

        private string _username;
        public string Username
        {
            get => _username;
            set
            {
                SetProperty(ref _username, value);
                ((RelayCommand)LoginCommand).RaiseCanExecuteChanged();
            }
        }

        private string _password;
        public string Password
        {
            get => _password;
            set
            {
                SetProperty(ref _password, value);
                ((RelayCommand)LoginCommand).RaiseCanExecuteChanged();
            }
        }

        private bool _rememberPassword;
        public bool RememberPassword
        {
            get => _rememberPassword;
            set => SetProperty(ref _rememberPassword, value);
        }

        private string _errorMessage;
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        #endregion

        #region Commands

        public ICommand LoginCommand { get; }
        public ICommand CloseCommand { get; }

        #endregion

        #region Events

        public event EventHandler<LoginSuccessEventArgs>? LoginSuccess;
        public event EventHandler? CloseRequested;

        #endregion

        #region Methods

        private bool CanLogin()
        {
            return !string.IsNullOrWhiteSpace(Username) && 
                   !string.IsNullOrWhiteSpace(Password) && 
                   !IsLoading;
        }

        private async Task LoginAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                _logger.LogInformation($"=== 开始登录流程 ===");
                _logger.LogInformation($"用户名: {Username}");
                _logger.LogInformation($"密码长度: {Password?.Length ?? 0}");
                _logger.LogInformation($"AuthService是否为null: {_authService == null}");

                if (_authService == null)
                {
                    _logger.LogError("AuthService未正确注入");
                    ErrorMessage = "系统初始化错误，请重启应用程序";
                    return;
                }

                // 基本输入校验，避免传入 null
                if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
                {
                    ErrorMessage = "请输入用户名和密码";
                    _logger.LogWarning("用户名或密码为空，终止登录请求");
                    return;
                }

                _logger.LogInformation("调用AuthService.LoginAsync方法");
                var result = await _authService.LoginAsync(Username, Password!);

                _logger.LogInformation($"=== 登录服务返回结果 ===");
                _logger.LogInformation($"成功: {result.Success}");
                _logger.LogInformation($"消息: {result.Message}");
                _logger.LogInformation($"用户对象是否为null: {result.User == null}");

                if (result.Success)
                {
                    _logger.LogInformation($"用户 {Username} 登录成功");
                    _logger.LogInformation($"上次登录时间: {(result.PreviousLoginAt.HasValue ? result.PreviousLoginAt.Value.ToString("yyyy-MM-dd HH:mm:ss") : "无记录")}");
                    
                    // 如果选择记住密码，保存凭据
                    if (RememberPassword)
                    {
                        // TODO: 实现记住密码功能
                        _logger.LogInformation("保存用户凭据");
                    }

                    // 触发登录成功事件（确保 User 非 null）
                    if (result.User == null)
                    {
                        _logger.LogError("登录结果 Success=true 但 User 为 null，终止事件触发");
                        ErrorMessage = "登录数据异常，请重试";
                        return;
                    }
                    _logger.LogInformation("触发登录成功事件");
                    LoginSuccess?.Invoke(this, new LoginSuccessEventArgs(result.User, "", result.PreviousLoginAt));
                }
                else
                {
                    ErrorMessage = result.Message ?? "登录失败，请检查用户名和密码";
                    _logger.LogWarning($"用户 {Username} 登录失败: {ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "登录过程中发生错误，请稍后再试";
                _logger.LogError(ex, $"=== 登录异常详情 ===");
                _logger.LogError(ex, $"异常类型: {ex.GetType().Name}");
                _logger.LogError(ex, $"异常消息: {ex.Message}");
                _logger.LogError(ex, $"堆栈跟踪: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    _logger.LogError(ex.InnerException, $"内部异常: {ex.InnerException.Message}");
                }
            }
            finally
            {
                IsLoading = false;
                _logger.LogInformation("=== 登录流程结束 ===");
            }
        }

        private void Close()
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        public void LoadSavedCredentials()
        {
            try
            {
                // TODO: 从配置或安全存储中加载保存的凭据
                // 这里可以实现从注册表、配置文件或Windows凭据管理器中读取
                _logger.LogInformation("加载保存的凭据");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载保存的凭据时发生错误");
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }

    // 登录成功事件参数
    public class LoginSuccessEventArgs : EventArgs
    {
        public User User { get; }
        public string Token { get; }
        public DateTime? PreviousLoginAt { get; }

        public LoginSuccessEventArgs(User user, string token, DateTime? previousLoginAt)
        {
            User = user;
            Token = token;
            PreviousLoginAt = previousLoginAt;
        }
    }

}