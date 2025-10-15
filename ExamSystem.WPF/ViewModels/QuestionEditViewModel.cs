using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;
using ExamSystem.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace ExamSystem.WPF.ViewModels
{
    /// <summary>
    /// 题目编辑对话框ViewModel
    /// </summary>
    public class QuestionEditViewModel : INotifyPropertyChanged
    {
        private readonly IQuestionService _questionService;
        private readonly ILogger<QuestionEditViewModel> _logger;
        
        private Question _question;
        private int _bankId;
        private string _validationMessage = string.Empty;
        private bool _hasValidationError;
        private bool _isLoading;
        private ObservableCollection<QuestionOptionViewModel> _options = new();

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler<bool>? SaveCompleted;

        public QuestionEditViewModel(
            IQuestionService questionService,
            ILogger<QuestionEditViewModel> logger)
        {
            _questionService = questionService;
            _logger = logger;
            _question = new Question();
            
            InitializeCommands();
            InitializeData();
        }

        #region 属性

        /// <summary>
        /// 题目实体
        /// </summary>
        public Question Question
        {
            get => _question;
            set
            {
                if (_question != value)
                {
                    _question = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DialogTitle));
                    OnPropertyChanged(nameof(SaveButtonText));
                    OnPropertyChanged(nameof(IsChoiceQuestion));
                    OnPropertyChanged(nameof(AnswerTooltip));
                    LoadOptions();
                }
            }
        }

        /// <summary>
        /// 题库ID
        /// </summary>
        public int BankId
        {
            get => _bankId;
            set => SetProperty(ref _bankId, value);
        }

        /// <summary>
        /// 验证消息
        /// </summary>
        public string ValidationMessage
        {
            get => _validationMessage;
            set => SetProperty(ref _validationMessage, value);
        }

        /// <summary>
        /// 是否有验证错误
        /// </summary>
        public bool HasValidationError
        {
            get => _hasValidationError;
            set => SetProperty(ref _hasValidationError, value);
        }

        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// 选项列表
        /// </summary>
        public ObservableCollection<QuestionOptionViewModel> Options
        {
            get => _options;
            set => SetProperty(ref _options, value);
        }

        /// <summary>
        /// 题目类型列表
        /// </summary>
        public Dictionary<QuestionType, string> QuestionTypes { get; } = new()
        {
            { QuestionType.SingleChoice, "单选题" },
            { QuestionType.MultipleChoice, "多选题" },
            { QuestionType.TrueFalse, "判断题" },
            { QuestionType.FillInBlank, "填空题" },
            { QuestionType.ShortAnswer, "简答题" },
            { QuestionType.Essay, "论述题" }
        };

        /// <summary>
        /// 难度等级列表
        /// </summary>
        public Dictionary<Difficulty, string> Difficulties { get; } = new()
        {
            { Difficulty.Easy, "简单" },
            { Difficulty.Medium, "中等" },
            { Difficulty.Hard, "困难" }
        };

        /// <summary>
        /// 对话框标题
        /// </summary>
        public string DialogTitle => Question.QuestionId == 0 ? "新建题目" : "编辑题目";

        /// <summary>
        /// 保存按钮文本
        /// </summary>
        public string SaveButtonText => Question.QuestionId == 0 ? "创建" : "保存";

        /// <summary>
        /// 是否为选择题
        /// </summary>
        public bool IsChoiceQuestion => Question.QuestionType == QuestionType.SingleChoice || 
                                       Question.QuestionType == QuestionType.MultipleChoice;

        /// <summary>
        /// 答案提示文本
        /// </summary>
        public string AnswerTooltip => Question.QuestionType switch
        {
            QuestionType.SingleChoice => "选择题答案格式：A 或 1",
            QuestionType.MultipleChoice => "多选题答案格式：A,B,C 或 1,2,3",
            QuestionType.TrueFalse => "判断题答案格式：True 或 False",
            QuestionType.FillInBlank => "填空题答案格式：答案1;答案2;答案3",
            _ => "请输入正确答案"
        };

        #endregion

        #region 命令

        public ICommand SaveCommand { get; private set; } = null!;
        public ICommand AddOptionCommand { get; private set; } = null!;
        public ICommand RemoveOptionCommand { get; private set; } = null!;
        public ICommand MoveOptionUpCommand { get; private set; } = null!;

        #endregion

        #region 方法

        private void InitializeCommands()
        {
            SaveCommand = new RelayCommand(async () => await SaveAsync(), CanSave);
            AddOptionCommand = new RelayCommand(AddOption);
            RemoveOptionCommand = new RelayCommand<QuestionOptionViewModel>(RemoveOption);
            MoveOptionUpCommand = new RelayCommand<QuestionOptionViewModel>(MoveOptionUp);
        }

        private void InitializeData()
        {
            // 初始化默认选项（如果是选择题）
            if (IsChoiceQuestion && Options.Count == 0)
            {
                AddOption();
                AddOption();
            }
        }

        /// <summary>
        /// 设置编辑的题目
        /// </summary>
        public void SetQuestion(Question? question, int bankId)
        {
            BankId = bankId;
            
            if (question == null)
            {
                Question = new Question 
                { 
                    BankId = bankId,
                    IsActive = true,
                    QuestionType = QuestionType.SingleChoice,
                    Difficulty = Difficulty.Medium,
                    Score = 1.0m
                };
            }
            else
            {
                // 创建副本以避免直接修改原对象
                Question = new Question
                {
                    QuestionId = question.QuestionId,
                    BankId = question.BankId,
                    QuestionType = question.QuestionType,
                    Content = question.Content,
                    Answer = question.Answer,
                    Analysis = question.Analysis,
                    Score = question.Score,
                    Difficulty = question.Difficulty,
                    Tags = question.Tags,
                    IsActive = question.IsActive,
                    CreatedAt = question.CreatedAt,
                    UpdatedAt = question.UpdatedAt
                };
            }
        }

        private void LoadOptions()
        {
            Options.Clear();
            
            if (IsChoiceQuestion && Question.Options?.Any() == true)
            {
                foreach (var option in Question.Options.OrderBy(o => o.OrderIndex))
                {
                    Options.Add(new QuestionOptionViewModel
                    {
                        OptionId = option.OptionId,
                        Content = option.Content,
                        IsCorrect = option.IsCorrect,
                        OrderIndex = option.OrderIndex
                    });
                }
            }
            else if (IsChoiceQuestion && Options.Count == 0)
            {
                // 添加默认选项
                AddOption();
                AddOption();
            }
        }

        private bool CanSave()
        {
            return !IsLoading && !string.IsNullOrWhiteSpace(Question.Content);
        }

        private async Task SaveAsync()
        {
            try
            {
                IsLoading = true;
                ValidationMessage = string.Empty;

                // 验证输入
                if (!ValidateInput())
                {
                    return;
                }

                // 准备选项数据
                if (IsChoiceQuestion)
                {
                    Question.Options = Options.Select((option, index) => new QuestionOption
                    {
                        OptionId = option.OptionId,
                        Content = option.Content,
                        IsCorrect = option.IsCorrect,
                        OrderIndex = index
                    }).ToList();
                }

                bool success;
                if (Question.QuestionId == 0)
                {
                    // 创建新题目
                    Question.BankId = BankId;
                    success = await _questionService.CreateQuestionAsync(Question);
                }
                else
                {
                    // 更新题目
                    success = await _questionService.UpdateQuestionAsync(Question);
                }

                if (success)
                {
                    SaveCompleted?.Invoke(this, true);
                }
                else
                {
                    ValidationMessage = "保存失败，请重试";
                    SaveCompleted?.Invoke(this, false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存题目失败");
                ValidationMessage = "保存失败：" + ex.Message;
                SaveCompleted?.Invoke(this, false);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool ValidateInput()
        {
            ValidationMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(Question.Content))
            {
                ValidationMessage = "请输入题目内容";
                return false;
            }

            if (string.IsNullOrWhiteSpace(Question.Answer))
            {
                ValidationMessage = "请输入正确答案";
                return false;
            }

            if (Question.Score <= 0)
            {
                ValidationMessage = "分值必须大于0";
                return false;
            }

            // 验证选择题选项
            if (IsChoiceQuestion)
            {
                if (Options.Count < 2)
                {
                    ValidationMessage = "选择题至少需要2个选项";
                    return false;
                }

                if (!Options.Any(o => o.IsCorrect))
                {
                    ValidationMessage = "请至少选择一个正确答案";
                    return false;
                }

                if (Question.QuestionType == QuestionType.SingleChoice && Options.Count(o => o.IsCorrect) > 1)
                {
                    ValidationMessage = "单选题只能有一个正确答案";
                    return false;
                }

                if (Options.Any(o => string.IsNullOrWhiteSpace(o.Content)))
                {
                    ValidationMessage = "选项内容不能为空";
                    return false;
                }
            }

            return true;
        }

        private void AddOption()
        {
            var newOption = new QuestionOptionViewModel
            {
                Content = $"选项 {(char)('A' + Options.Count)}",
                IsCorrect = false,
                OrderIndex = Options.Count
            };
            
            Options.Add(newOption);
        }

        private void RemoveOption(QuestionOptionViewModel? option)
        {
            if (option != null && Options.Count > 2)
            {
                Options.Remove(option);
                
                // 重新排序
                for (int i = 0; i < Options.Count; i++)
                {
                    Options[i].OrderIndex = i;
                }
            }
        }

        private void MoveOptionUp(QuestionOptionViewModel? option)
        {
            if (option == null) return;
            
            var index = Options.IndexOf(option);
            if (index > 0)
            {
                Options.Move(index, index - 1);
                
                // 重新排序
                for (int i = 0; i < Options.Count; i++)
                {
                    Options[i].OrderIndex = i;
                }
            }
        }

        #endregion

        #region INotifyPropertyChanged

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }

    /// <summary>
    /// 题目选项ViewModel
    /// </summary>
    public class QuestionOptionViewModel : INotifyPropertyChanged
    {
        private string _content = string.Empty;
        private bool _isCorrect;
        private int _orderIndex;

        public int OptionId { get; set; }

        public string Content
        {
            get => _content;
            set => SetProperty(ref _content, value);
        }

        public bool IsCorrect
        {
            get => _isCorrect;
            set => SetProperty(ref _isCorrect, value);
        }

        public int OrderIndex
        {
            get => _orderIndex;
            set => SetProperty(ref _orderIndex, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    /// <summary>
    /// 简单的RelayCommand实现
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute?.Invoke() ?? true;
        }

        public void Execute(object? parameter)
        {
            _execute();
        }
    }

    /// <summary>
    /// 带参数的RelayCommand实现
    /// </summary>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Func<T?, bool>? _canExecute;

        public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute?.Invoke((T?)parameter) ?? true;
        }

        public void Execute(object? parameter)
        {
            _execute((T?)parameter);
        }
    }
}