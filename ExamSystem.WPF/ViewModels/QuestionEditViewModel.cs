using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using ExamSystem.WPF.Commands;
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
        private ObservableCollection<QuestionOptionEditViewModel> _options = new();

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
                    OnPropertyChanged(nameof(IsSingleChoice));
                    OnPropertyChanged(nameof(IsMultipleChoice));
                    OnPropertyChanged(nameof(IsTrueFalse));
                    OnPropertyChanged(nameof(IsFillInBlank));
                    OnPropertyChanged(nameof(ShowTraditionalAnswerInput));
                    OnPropertyChanged(nameof(AnswerTooltip));
                    OnPropertyChanged(nameof(SingleChoiceGroupName));
                    OnPropertyChanged(nameof(TrueFalseAnswer));
                    OnPropertyChanged(nameof(FillBlankContent));
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
        public ObservableCollection<QuestionOptionEditViewModel> Options
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

        // 为单选题提供唯一的分组名，避免不同题目的选项互相干扰
        private readonly string _radioGroupKey = Guid.NewGuid().ToString("N").Substring(0, 8);
        public string SingleChoiceGroupName => $"SCG_{(Question?.QuestionId > 0 ? Question.QuestionId.ToString() : _radioGroupKey)}";

        // 代理题型，切换时触发相关依赖属性更新
        public QuestionType SelectedQuestionType
        {
            get => Question.QuestionType;
            set
            {
                if (Question.QuestionType != value)
                {
                    Question.QuestionType = value;
                    OnPropertyChanged(nameof(Question));
                    OnPropertyChanged(nameof(SelectedQuestionType));
                    OnPropertyChanged(nameof(IsChoiceQuestion));
                    OnPropertyChanged(nameof(IsSingleChoice));
                    OnPropertyChanged(nameof(IsMultipleChoice));
                    OnPropertyChanged(nameof(IsTrueFalse));
                    OnPropertyChanged(nameof(IsFillInBlank));
                    OnPropertyChanged(nameof(ShowTraditionalAnswerInput));
                    OnPropertyChanged(nameof(AnswerTooltip));
                    OnPropertyChanged(nameof(TrueFalseAnswer));
                    OnPropertyChanged(nameof(FillBlankContent));

                    // 确保选择题有基础选项
                    if (IsChoiceQuestion && Options.Count == 0)
                    {
                        AddOption();
                        AddOption();
                    }

                    // 单选题确保唯一选中
                    if (IsSingleChoice)
                    {
                        var first = Options.FirstOrDefault(o => o.IsCorrect);
                        foreach (var opt in Options)
                        {
                            opt.IsCorrect = ReferenceEquals(opt, first);
                        }
                    }

                    PrepareAnswer();
                }
            }
        }

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
        /// 是否为单选题
        /// </summary>
        public bool IsSingleChoice => Question.QuestionType == QuestionType.SingleChoice;

        /// <summary>
        /// 是否为多选题
        /// </summary>
        public bool IsMultipleChoice => Question.QuestionType == QuestionType.MultipleChoice;

        /// <summary>
        /// 是否为判断题
        /// </summary>
        public bool IsTrueFalse => Question.QuestionType == QuestionType.TrueFalse;

        /// <summary>
        /// 是否为填空题
        /// </summary>
        public bool IsFillInBlank => Question.QuestionType == QuestionType.FillInBlank;

        /// <summary>
        /// 是否需要显示传统答案输入框（排除选择题、判断题）
        /// </summary>
        public bool ShowTraditionalAnswerInput => !IsChoiceQuestion && !IsTrueFalse;

        /// <summary>
        /// 判断题答案选择（True/False）
        /// </summary>
        public bool? TrueFalseAnswer
        {
            get
            {
                if (!IsTrueFalse || string.IsNullOrEmpty(Question.Answer))
                    return null;
                
                return Question.Answer.Trim().ToLower() switch
                {
                    "true" or "正确" or "对" or "t" or "1" => true,
                    "false" or "错误" or "错" or "f" or "0" => false,
                    _ => null
                };
            }
            set
            {
                if (IsTrueFalse)
                {
                    Question.Answer = value switch
                    {
                        true => "True",
                        false => "False",
                        _ => string.Empty
                    };
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Question));
                }
            }
        }

        /// <summary>
        /// 填空题内容（用于编辑时显示和插入空格）
        /// </summary>
        private string _fillBlankContent = string.Empty;
        public string FillBlankContent
        {
            get => IsFillInBlank ? Question.Content : _fillBlankContent;
            set
            {
                if (IsFillInBlank)
                {
                    Question.Content = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Question));
                }
                else
                {
                    SetProperty(ref _fillBlankContent, value);
                }
            }
        }

        /// <summary>
        /// 插入空格标记的命令
        /// </summary>
        public ICommand InsertBlankCommand { get; private set; } = null!;

        /// <summary>
        /// 插入空格标记
        /// </summary>
        private void InsertBlank()
        {
            if (!IsFillInBlank) return;

            const string blankMarker = "______";
            var currentContent = FillBlankContent ?? string.Empty;
            
            // 在当前内容末尾插入空格标记
            FillBlankContent = currentContent + blankMarker;
        }

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
            RemoveOptionCommand = new RelayCommand<QuestionOptionEditViewModel>(RemoveOption);
            MoveOptionUpCommand = new RelayCommand<QuestionOptionEditViewModel>(MoveOptionUp);
            InsertBlankCommand = new RelayCommand(InsertBlank);
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
                    UpdatedAt = question.UpdatedAt,
                    // 复制选项集合
                    Options = question.Options?.Select(o => new QuestionOption
                    {
                        OptionId = o.OptionId,
                        QuestionId = o.QuestionId,
                        Content = o.Content,
                        IsCorrect = o.IsCorrect,
                        OrderIndex = o.OrderIndex,
                        OptionLabel = o.OptionLabel
                    }).ToList() ?? new List<QuestionOption>()
                };
            }
            
            // 加载选项
            LoadOptions();
        }

        private void LoadOptions()
        {
            Options.Clear();
            
            if (IsChoiceQuestion && Question.Options?.Any() == true)
            {
                // 先添加选项，但不设置IsCorrect
                foreach (var option in Question.Options.OrderBy(o => o.OrderIndex))
                {
                    Options.Add(new QuestionOptionEditViewModel
                    {
                        OptionId = option.OptionId,
                        Content = option.Content,
                        IsCorrect = false, // 先设置为false，稍后通过答案设置
                        OrderIndex = option.OrderIndex,
                        OnIsCorrectChanged = () => PrepareAnswer() // 设置回调
                    });
                }
                
                // 根据现有答案设置选项的正确性
                if (!string.IsNullOrEmpty(Question.Answer))
                {
                    SetOptionsFromAnswer(Question.Answer);
                }
                else
                {
                    // 如果没有答案，则使用选项的原始IsCorrect值
                    for (int i = 0; i < Options.Count && i < Question.Options.Count; i++)
                    {
                        var originalOption = Question.Options.OrderBy(o => o.OrderIndex).ElementAt(i);
                        Options[i].OnIsCorrectChanged = null; // 临时禁用回调
                        Options[i].IsCorrect = originalOption.IsCorrect;
                        Options[i].OnIsCorrectChanged = () => PrepareAnswer(); // 恢复回调
                    }
                }
            }
            else if (IsChoiceQuestion && Options.Count == 0)
            {
                // 添加默认选项
                AddOption();
                AddOption();
            }
        }

        /// <summary>
        /// 根据答案字符串设置选项的正确性
        /// </summary>
        private void SetOptionsFromAnswer(string answer)
        {
            // 专用处理：判断题与填空题
            if (IsTrueFalse)
            {
                var a = (answer ?? string.Empty).Trim().ToLowerInvariant();
                if (a == "true" || a == "正确" || a == "对" || a == "t" || a == "1")
                {
                    Question.Answer = "True";
                }
                else if (a == "false" || a == "错误" || a == "错" || a == "f" || a == "0")
                {
                    Question.Answer = "False";
                }
                else
                {
                    Question.Answer = string.Empty;
                }
                OnPropertyChanged(nameof(Question));
                return;
            }
            if (IsFillInBlank)
            {
                var normalized = (answer ?? string.Empty)
                    .Replace("，", ";")
                    .Replace(",", ";")
                    .Replace("；", ";")
                    .Replace("|", ";")
                    .Replace("/", ";")
                    .Replace("\\", ";")
                    .Trim();

                var parts = normalized.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                      .Select(s => s.Trim())
                                      .Where(s => !string.IsNullOrEmpty(s))
                                      .ToArray();
                Question.Answer = string.Join(";", parts);
                OnPropertyChanged(nameof(Question));
                return;
            }

            if (string.IsNullOrEmpty(answer) || !Options.Any())
                return;

            // 临时禁用回调，避免循环调用
            var originalCallbacks = Options.Select(o => o.OnIsCorrectChanged).ToList();
            foreach (var option in Options)
            {
                option.OnIsCorrectChanged = null;
            }

            try
            {
                // 先清除所有选项的正确性
                foreach (var option in Options)
                {
                    option.IsCorrect = false;
                }

                // 统一答案分隔符，支持中文逗号、分号、竖线、空格等
                var normalized = answer
                    .Replace("，", ",")
                    .Replace("；", ",")
                    .Replace(";", ",")
                    .Replace("|", ",")
                    .Replace("/", ",")
                    .Replace("\\", ",")
                    .Trim();

                string[] parts;
                if (normalized.Contains(','))
                {
                    parts = normalized.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                      .Select(s => s.Trim()).ToArray();
                }
                else
                {
                    // 若无分隔符且全部是字母，按字符逐个解析（如 "ACD"）
                    if (normalized.All(ch => char.IsLetter(ch)))
                    {
                        parts = normalized.ToUpper().Select(ch => ch.ToString()).ToArray();
                    }
                    else
                    {
                        parts = new[] { normalized };
                    }
                }

                // 优先按字母 A,B,C... 解析；其次按1基索引；最后按OptionId匹配
                var upperParts = parts.Select(p => p.Trim().ToUpper()).ToArray();
                bool anyMatched = false;

                // 1) 字母映射到序号
                foreach (var part in upperParts)
                {
                    if (part.Length == 1 && part[0] >= 'A' && part[0] <= 'Z')
                    {
                        int idx = part[0] - 'A';
                        if (idx >= 0 && idx < Options.Count)
                        {
                            Options[idx].IsCorrect = true;
                            anyMatched = true;
                        }
                    }
                }

                // 2) 若没有匹配字母，尝试数字（1基序号）
                if (!anyMatched)
                {
                    foreach (var part in upperParts)
                    {
                        if (int.TryParse(part, out int num))
                        {
                            int idx = num - 1; // 1基
                            if (idx >= 0 && idx < Options.Count)
                            {
                                Options[idx].IsCorrect = true;
                                anyMatched = true;
                            }
                        }
                    }
                }

                // 3) 若仍未匹配，尝试按 OptionId 匹配
                if (!anyMatched)
                {
                    foreach (var part in upperParts)
                    {
                        if (int.TryParse(part, out int id))
                        {
                            var match = Options.FirstOrDefault(o => o.OptionId == id);
                            if (match != null)
                            {
                                match.IsCorrect = true;
                                anyMatched = true;
                            }
                        }
                    }
                }

                // 单选题确保唯一选中
                if (IsSingleChoice)
                {
                    var first = Options.FirstOrDefault(o => o.IsCorrect);
                    foreach (var opt in Options)
                    {
                        opt.IsCorrect = ReferenceEquals(opt, first);
                    }
                }
            }
            finally
            {
                // 恢复回调
                for (int i = 0; i < Options.Count && i < originalCallbacks.Count; i++)
                {
                    Options[i].OnIsCorrectChanged = originalCallbacks[i];
                }
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

                // 设置题目标题（如果为空，使用题目内容的前50个字符）
                if (string.IsNullOrWhiteSpace(Question.Title))
                {
                    Question.Title = Question.Content?.Length > 50 
                        ? Question.Content.Substring(0, 50) + "..." 
                        : Question.Content ?? "未命名题目";
                }

                // 准备选项数据（在验证之前）
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

                // 统一生成或规范化答案（所有题型）
                PrepareAnswer();

                // 验证输入（在答案准备之后）
                if (!ValidateInput())
                {
                    return;
                }

                _logger.LogInformation("准备保存题目: Title={Title}, Answer={Answer}, QuestionType={QuestionType}", 
                    Question.Title, Question.Answer, Question.QuestionType);

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

        /// <summary>
        /// 根据选中的选项准备答案格式
        /// </summary>
        private void PrepareAnswer()
        {
            // 判断题：统一规范答案
            if (IsTrueFalse)
            {
                bool? tf = TrueFalseAnswer;
                if (tf is null)
                {
                    var normalized = (Question.Answer ?? string.Empty).Trim().ToLowerInvariant();
                    tf = normalized switch
                    {
                        "true" or "正确" or "对" or "t" or "1" => true,
                        "false" or "错误" or "错" or "f" or "0" => false,
                        _ => null
                    };
                }

                Question.Answer = tf switch { true => "True", false => "False", _ => string.Empty };
                OnPropertyChanged(nameof(Question));
                _logger.LogInformation("规范化判断题答案: {Answer}", Question.Answer);
                return;
            }

            // 填空题：统一分隔符并去除多余空白
            if (IsFillInBlank)
            {
                var ans = (Question.Answer ?? string.Empty)
                    .Replace("，", ";")
                    .Replace(",", ";")
                    .Replace("；", ";")
                    .Replace("|", ";")
                    .Replace("/", ";")
                    .Replace("\\", ";")
                    .Trim();

                var parts = ans.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                               .Select(p => p.Trim())
                               .Where(p => !string.IsNullOrEmpty(p))
                               .ToArray();

                Question.Answer = string.Join(";", parts);

                // 校验空格数量与答案数量
                const string blankMarker = "______";
                var content = FillBlankContent ?? Question.Content ?? string.Empty;
                int blankCount = 0;
                if (!string.IsNullOrEmpty(content))
                {
                    int idx = 0;
                    while ((idx = content.IndexOf(blankMarker, idx, StringComparison.Ordinal)) >= 0)
                    {
                        blankCount++;
                        idx += blankMarker.Length;
                    }
                }
                if (blankCount > 0 && parts.Length != blankCount)
                {
                    _logger.LogWarning("填空题答案数量({AnswersCount})与空格数({BlankCount})不匹配", parts.Length, blankCount);
                }

                OnPropertyChanged(nameof(Question));
                _logger.LogInformation("规范化填空题答案: {Answer}", Question.Answer);
                return;
            }

            // 选择题：根据选中的选项准备答案
            if (!IsChoiceQuestion || Options == null || !Options.Any())
                return;

            var correctOptions = Options.Where(o => o.IsCorrect).ToList();
            if (!correctOptions.Any())
            {
                Question.Answer = string.Empty;
                OnPropertyChanged(nameof(Question));
                return;
            }

            // 生成选项字母（A, B, C, D...）
            var answerLetters = correctOptions.Select(option =>
            {
                var index = Options.IndexOf(option);
                return ((char)('A' + index)).ToString();
            }).OrderBy(x => x).ToList();

            if (Question.QuestionType == QuestionType.SingleChoice)
            {
                // 单选题：只取第一个正确答案
                Question.Answer = answerLetters.First();
            }
            else if (Question.QuestionType == QuestionType.MultipleChoice)
            {
                // 多选题：用逗号分隔
                Question.Answer = string.Join(",", answerLetters);
            }

            OnPropertyChanged(nameof(Question));
            _logger.LogInformation("自动生成答案: {Answer}, 题型: {QuestionType}", Question.Answer, Question.QuestionType);
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
            var newOption = new QuestionOptionEditViewModel
            {
                Content = $"选项 {(char)('A' + Options.Count)}",
                IsCorrect = false,
                OrderIndex = Options.Count,
                OnIsCorrectChanged = () => PrepareAnswer() // 设置回调
            };
            
            Options.Add(newOption);
        }

        private void RemoveOption(QuestionOptionEditViewModel? option)
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

        private void MoveOptionUp(QuestionOptionEditViewModel? option)
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
    public class QuestionOptionEditViewModel : INotifyPropertyChanged
    {
        private string _content = string.Empty;
        private bool _isCorrect;
        private int _orderIndex;
        private string _optionLabel = string.Empty;

        public int OptionId { get; set; }
        
        // 当正确性发生变化时的回调
        public Action? OnIsCorrectChanged { get; set; }

        public string Content
        {
            get => _content;
            set => SetProperty(ref _content, value);
        }

        public bool IsCorrect
        {
            get => _isCorrect;
            set 
            { 
                if (SetProperty(ref _isCorrect, value))
                {
                    // 当选项的正确性发生变化时，通知父ViewModel更新答案
                    OnIsCorrectChanged?.Invoke();
                }
            }
        }

        public int OrderIndex
        {
            get => _orderIndex;
            set 
            { 
                SetProperty(ref _orderIndex, value);
                UpdateOptionLabel();
            }
        }

        public string OptionLabel
        {
            get => _optionLabel;
            private set => SetProperty(ref _optionLabel, value);
        }

        private void UpdateOptionLabel()
        {
            OptionLabel = ((char)('A' + OrderIndex)).ToString();
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

}