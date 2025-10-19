using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using ExamSystem.WPF.Commands;
using ExamSystem.Domain.Entities;
using ExamSystem.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace ExamSystem.WPF.ViewModels
{
    /// <summary>
    /// 题库编辑对话框ViewModel
    /// </summary>
    public class QuestionBankEditViewModel : INotifyPropertyChanged
    {
        private readonly IQuestionBankService _questionBankService;
        private readonly ILogger<QuestionBankEditViewModel> _logger;
        
        private QuestionBank _questionBank;
        private string _validationMessage = string.Empty;
        private bool _hasValidationError;
        private bool _isLoading;
        private User? _currentUser;

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler<bool>? SaveCompleted;

        public QuestionBankEditViewModel(
            IQuestionBankService questionBankService,
            ILogger<QuestionBankEditViewModel> logger)
        {
            _questionBankService = questionBankService;
            _logger = logger;
            _questionBank = new QuestionBank();
            
            SaveCommand = new RelayCommand(async () => await SaveAsync(), CanSave);
        }

        /// <summary>
        /// 当前用户
        /// </summary>
        public User? CurrentUser
        {
            get => _currentUser;
            set => _currentUser = value;
        }

        /// <summary>
        /// 设置当前用户
        /// </summary>
        /// <param name="user">当前登录用户</param>
        public void SetCurrentUser(User user)
        {
            _logger.LogInformation($"=== QuestionBankEditViewModel.SetCurrentUser ===");
            _logger.LogInformation($"接收到的用户: {user?.Username} ({user?.Role})");
            _logger.LogInformation($"用户ID: {user?.UserId}");
            _logger.LogInformation($"用户对象是否为null: {user == null}");
            
            CurrentUser = user;
            
            _logger.LogInformation($"设置后的CurrentUser: {CurrentUser?.Username} ({CurrentUser?.Role})");
            _logger.LogInformation($"CurrentUser是否为null: {CurrentUser == null}");
            _logger.LogInformation($"QuestionBankEditViewModel: 用户信息设置完成");
        }

        /// <summary>
        /// 题库实体
        /// </summary>
        public QuestionBank QuestionBank
        {
            get => _questionBank;
            set
            {
                if (_questionBank != value)
                {
                    _questionBank = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DialogTitle));
                    OnPropertyChanged(nameof(SaveButtonText));
                    OnPropertyChanged(nameof(IsEditMode));
                    ClearValidation();
                }
            }
        }

        /// <summary>
        /// 对话框标题
        /// </summary>
        public string DialogTitle => IsEditMode ? "编辑题库" : "新建题库";

        /// <summary>
        /// 保存按钮文本
        /// </summary>
        public string SaveButtonText => IsEditMode ? "更新" : "创建";

        /// <summary>
        /// 是否为编辑模式
        /// </summary>
        public bool IsEditMode => QuestionBank.BankId > 0;

        /// <summary>
        /// 验证错误信息
        /// </summary>
        public string ValidationMessage
        {
            get => _validationMessage;
            set
            {
                if (_validationMessage != value)
                {
                    _validationMessage = value;
                    OnPropertyChanged();
                    HasValidationError = !string.IsNullOrEmpty(value);
                }
            }
        }

        /// <summary>
        /// 是否有验证错误
        /// </summary>
        public bool HasValidationError
        {
            get => _hasValidationError;
            set
            {
                if (_hasValidationError != value)
                {
                    _hasValidationError = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                    ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// 保存命令
        /// </summary>
        public ICommand SaveCommand { get; }

        /// <summary>
        /// 设置编辑的题库
        /// </summary>
        public void SetQuestionBank(QuestionBank questionBank)
        {
            if (questionBank == null)
            {
                QuestionBank = new QuestionBank { IsActive = true };
            }
            else
            {
                // 创建副本以避免直接修改原对象
                QuestionBank = new QuestionBank
                {
                    BankId = questionBank.BankId,
                    Name = questionBank.Name,
                    Description = questionBank.Description,
                    IsActive = questionBank.IsActive,
                    CreatedAt = questionBank.CreatedAt,
                    UpdatedAt = questionBank.UpdatedAt
                };
            }
        }

        /// <summary>
        /// 验证输入
        /// </summary>
        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(QuestionBank.Name))
            {
                ValidationMessage = "题库名称不能为空";
                return false;
            }

            if (QuestionBank.Name.Length > 100)
            {
                ValidationMessage = "题库名称不能超过100个字符";
                return false;
            }

            if (!string.IsNullOrEmpty(QuestionBank.Description) && QuestionBank.Description.Length > 500)
            {
                ValidationMessage = "题库描述不能超过500个字符";
                return false;
            }

            ClearValidation();
            return true;
        }

        /// <summary>
        /// 清除验证错误
        /// </summary>
        private void ClearValidation()
        {
            ValidationMessage = string.Empty;
        }

        /// <summary>
        /// 检查是否可以保存
        /// </summary>
        private bool CanSave()
        {
            return !IsLoading && !string.IsNullOrWhiteSpace(QuestionBank.Name);
        }

        /// <summary>
        /// 保存题库
        /// </summary>
        private async Task SaveAsync()
        {
            try
            {
                if (!ValidateInput())
                    return;

                IsLoading = true;

                // 检查名称是否重复（编辑时排除自身）
                var nameExists = await _questionBankService.IsQuestionBankNameExistsAsync(
                    QuestionBank.Name, IsEditMode ? QuestionBank.BankId : null);

                if (nameExists)
                {
                    ValidationMessage = "题库名称已存在，请使用其他名称";
                    return;
                }

                bool success;
                if (IsEditMode)
                {
                    QuestionBank.UpdatedAt = DateTime.Now;
                    success = await _questionBankService.UpdateQuestionBankAsync(QuestionBank);
                }
                else
                {
                    // 新建模式，设置CreatorId
                    if (CurrentUser != null)
                    {
                        QuestionBank.CreatorId = CurrentUser.UserId;
                        _logger.LogInformation($"设置题库创建者ID: {CurrentUser.UserId} ({CurrentUser.Username})");
                    }
                    else
                    {
                        _logger.LogError("创建题库时CurrentUser为null，使用默认管理员用户ID");
                        // 使用默认管理员用户ID (1) 作为fallback
                        QuestionBank.CreatorId = 1;
                        _logger.LogWarning("使用默认管理员用户ID (1) 作为题库创建者");
                    }
                    
                    QuestionBank.CreatedAt = DateTime.Now;
                    QuestionBank.UpdatedAt = DateTime.Now;
                    
                    _logger.LogInformation($"准备创建题库: Name={QuestionBank.Name}, CreatorId={QuestionBank.CreatorId}");
                    success = await _questionBankService.CreateQuestionBankAsync(QuestionBank);
                }

                if (success)
                {
                    _logger.LogInformation($"题库{(IsEditMode ? "更新" : "创建")}成功: {QuestionBank.Name}");
                    SaveCompleted?.Invoke(this, true);
                }
                else
                {
                    ValidationMessage = $"题库{(IsEditMode ? "更新" : "创建")}失败，请重试";
                    _logger.LogWarning($"题库{(IsEditMode ? "更新" : "创建")}失败: {QuestionBank.Name}");
                }
            }
            catch (Exception ex)
            {
                ValidationMessage = $"操作失败: {ex.Message}";
                _logger.LogError(ex, $"题库{(IsEditMode ? "更新" : "创建")}时发生异常");
                SaveCompleted?.Invoke(this, false);
            }
            finally
            {
                IsLoading = false;
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}