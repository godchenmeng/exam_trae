using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using ExamSystem.Services.Interfaces;
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

        public event EventHandler<LoginSuccessEventArgs> LoginSuccess;
        public event EventHandler CloseRequested;

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

                _logger.LogInformation($"用户 {Username} 尝试登录");

                var result = await _authService.LoginAsync(Username, Password);

                if (result.IsSuccess)
                {
                    _logger.LogInformation($"用户 {Username} 登录成功");
                    
                    // 如果选择记住密码，保存凭据
                    if (RememberPassword)
                    {
                        // TODO: 实现记住密码功能
                        _logger.LogInformation("保存用户凭据");
                    }

                    // 触发登录成功事件
                    LoginSuccess?.Invoke(this, new LoginSuccessEventArgs(result.User, result.Token));
                }
                else
                {
                    ErrorMessage = result.ErrorMessage ?? "登录失败，请检查用户名和密码";
                    _logger.LogWarning($"用户 {Username} 登录失败: {ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "登录时发生错误，请稍后重试";
                _logger.LogError(ex, $"用户 {Username} 登录时发生异常");
            }
            finally
            {
                IsLoading = false;
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

    // 登录成功事件参数
    public class LoginSuccessEventArgs : EventArgs
    {
        public object User { get; }
        public string Token { get; }

        public LoginSuccessEventArgs(object user, string token)
        {
            User = user;
            Token = token;
        }
    }

    // 增强的RelayCommand，支持CanExecuteChanged事件
    public class RelayCommand : ICommand
    {
        private readonly Func<Task> _executeAsync;
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public RelayCommand(Func<Task> executeAsync, Func<bool> canExecute = null)
        {
            _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;

        public async void Execute(object parameter)
        {
            if (_executeAsync != null)
            {
                await _executeAsync();
            }
            else
            {
                _execute();
            }
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}