using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;
using ExamSystem.Services.Interfaces;
using ExamSystem.WPF.Commands;
using Microsoft.Extensions.Logging;

namespace ExamSystem.WPF.ViewModels
{
    /// <summary>
    /// 考试视图模型
    /// </summary>
    public class ExamViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly IExamService _examService;
        private readonly ILogger<ExamViewModel> _logger;
        private readonly DispatcherTimer _timer;
        
        private ExamRecord? _examRecord;
        private List<AnswerRecord> _answerRecords = new();
        private int _currentQuestionIndex = 0;
        private bool _isLoading = false;
        private bool _isExamInProgress = true;
        private int _remainingSeconds = 0;

        public ExamViewModel(IExamService examService, ILogger<ExamViewModel> logger)
        {
            _examService = examService;
            _logger = logger;
            
            // 初始化命令
            PreviousQuestionCommand = new RelayCommand(PreviousQuestion, () => CanGoPrevious);
            NextQuestionCommand = new RelayCommand(NextQuestion, () => CanGoNext);
            NavigateToQuestionCommand = new RelayCommand<int>(NavigateToQuestion);
            SelectSingleOptionCommand = new RelayCommand<QuestionOptionViewModel>(SelectSingleOption);
            SelectMultipleOptionCommand = new RelayCommand<QuestionOptionViewModel>(SelectMultipleOption);
            SelectTrueFalseCommand = new RelayCommand<bool?>(SelectTrueFalse);
            SaveAnswerCommand = new RelayCommand(async () => await SaveCurrentAnswerAsync());
            SubmitExamCommand = new RelayCommand(async () => await SubmitExamAsync());
            SaveCurrentAnswerCommand = new RelayCommand(async () => await SaveCurrentAnswerAsync());
            
            // 初始化集合
            QuestionNavigations = new ObservableCollection<QuestionNavigationViewModel>();
            
            // 初始化计时器
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += OnTimerTick;
        }

        #region Properties

        public string ExamPaperName => _examRecord?.ExamPaper?.Name ?? "";
        public decimal TotalScore => _examRecord?.ExamPaper?.TotalScore ?? 0;
        public int CurrentQuestionIndex => _currentQuestionIndex + 1;
        public int TotalQuestions => _answerRecords.Count;
        public int AnsweredCount => _answerRecords.Count(ar => !string.IsNullOrWhiteSpace(ar.UserAnswer));
        public int UnansweredCount => TotalQuestions - AnsweredCount;

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public bool IsExamInProgress
        {
            get => _isExamInProgress;
            set
            {
                _isExamInProgress = value;
                OnPropertyChanged();
            }
        }

        public string RemainingTimeText
        {
            get
            {
                var timeSpan = TimeSpan.FromSeconds(_remainingSeconds);
                return $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
            }
        }

        public QuestionViewModel? CurrentQuestion { get; private set; }

        public ObservableCollection<QuestionNavigationViewModel> QuestionNavigations { get; }

        // 题目类型判断
        public bool IsSingleChoice => CurrentQuestion?.QuestionType == QuestionType.SingleChoice;
        public bool IsMultipleChoice => CurrentQuestion?.QuestionType == QuestionType.MultipleChoice;
        public bool IsTrueFalse => CurrentQuestion?.QuestionType == QuestionType.TrueFalse;
        public bool IsFillInBlank => CurrentQuestion?.QuestionType == QuestionType.FillInBlank;
        public bool IsEssay => CurrentQuestion?.QuestionType == QuestionType.Essay;

        // 答案属性
        public bool? TrueFalseAnswer { get; set; }
        public string FillInBlankAnswer { get; set; } = "";
        public string EssayAnswer { get; set; } = "";

        public bool CanGoPrevious => _currentQuestionIndex > 0;
        public bool CanGoNext => _currentQuestionIndex < TotalQuestions - 1;

        #endregion

        #region Commands

        public ICommand PreviousQuestionCommand { get; }
        public ICommand NextQuestionCommand { get; }
        public ICommand NavigateToQuestionCommand { get; }
        public ICommand SelectSingleOptionCommand { get; }
        public ICommand SelectMultipleOptionCommand { get; }
        public ICommand SelectTrueFalseCommand { get; }
        public ICommand SaveAnswerCommand { get; }
        public ICommand SubmitExamCommand { get; }
        public ICommand SaveCurrentAnswerCommand { get; }

        #endregion

        #region Events

        public event EventHandler? ExamCompleted;
        public event EventHandler? ExamTimeout;

        #endregion

        #region Public Methods

        /// <summary>
        /// 初始化考试
        /// </summary>
        public async Task InitializeAsync(int recordId)
        {
            try
            {
                IsLoading = true;
                
                _examRecord = await _examService.GetExamProgressAsync(recordId);
                if (_examRecord == null)
                {
                    throw new InvalidOperationException("考试记录不存在");
                }

                _answerRecords = _examRecord.AnswerRecords?.ToList() ?? new List<AnswerRecord>();
                
                // 初始化题目导航
                InitializeQuestionNavigations();
                
                // 设置剩余时间
                _remainingSeconds = _examRecord.RemainingTime;
                
                // 加载第一题
                LoadQuestion(0);
                
                // 启动计时器
                _timer.Start();
                
                _logger.LogInformation("考试初始化完成，记录ID: {RecordId}", recordId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化考试失败，记录ID: {RecordId}", recordId);
                throw;
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 开始新考试
        /// </summary>
        public async Task StartNewExamAsync(int userId, int paperId)
        {
            try
            {
                IsLoading = true;
                
                _examRecord = await _examService.StartExamAsync(userId, paperId);
                _answerRecords = _examRecord.AnswerRecords?.ToList() ?? new List<AnswerRecord>();
                
                // 初始化题目导航
                InitializeQuestionNavigations();
                
                // 设置剩余时间
                _remainingSeconds = _examRecord.RemainingTime;
                
                // 加载第一题
                LoadQuestion(0);
                
                // 启动计时器
                _timer.Start();
                
                _logger.LogInformation("开始新考试，用户ID: {UserId}，试卷ID: {PaperId}，记录ID: {RecordId}", 
                    userId, paperId, _examRecord.RecordId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "开始考试失败，用户ID: {UserId}，试卷ID: {PaperId}", userId, paperId);
                throw;
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Private Methods

        private void InitializeQuestionNavigations()
        {
            QuestionNavigations.Clear();
            
            for (int i = 0; i < _answerRecords.Count; i++)
            {
                var navigation = new QuestionNavigationViewModel
                {
                    QuestionNumber = i + 1,
                    QuestionIndex = i,
                    IsAnswered = !string.IsNullOrWhiteSpace(_answerRecords[i].UserAnswer),
                    IsCurrent = i == _currentQuestionIndex
                };
                
                QuestionNavigations.Add(navigation);
            }
        }

        private void LoadQuestion(int index)
        {
            if (index < 0 || index >= _answerRecords.Count)
                return;

            _currentQuestionIndex = index;
            var answerRecord = _answerRecords[index];
            var question = answerRecord.Question;

            if (question == null)
                return;

            // 创建题目视图模型
            CurrentQuestion = new QuestionViewModel
            {
                QuestionId = question.QuestionId,
                Content = question.Content,
                QuestionType = question.QuestionType,
                Score = GetQuestionScore(question.QuestionId),
                Options = CreateQuestionOptions(question)
            };

            // 加载已保存的答案
            LoadSavedAnswer(answerRecord);

            // 更新导航状态
            UpdateQuestionNavigations();

            // 通知属性更改
            OnPropertyChanged(nameof(CurrentQuestion));
            OnPropertyChanged(nameof(CurrentQuestionIndex));
            OnPropertyChanged(nameof(IsSingleChoice));
            OnPropertyChanged(nameof(IsMultipleChoice));
            OnPropertyChanged(nameof(IsTrueFalse));
            OnPropertyChanged(nameof(IsFillInBlank));
            OnPropertyChanged(nameof(IsEssay));
            OnPropertyChanged(nameof(CanGoPrevious));
            OnPropertyChanged(nameof(CanGoNext));
        }

        private List<QuestionOptionViewModel> CreateQuestionOptions(Question question)
        {
            var options = new List<QuestionOptionViewModel>();
            
            if (question.Options != null && question.Options.Any())
            {
                foreach (var option in question.Options.OrderBy(o => o.OptionLabel))
                {
                    options.Add(new QuestionOptionViewModel
                    {
                        OptionId = option.OptionId,
                        Label = option.OptionLabel,
                        Content = $"{option.OptionLabel}. {option.Content}",
                        IsSelected = false
                    });
                }
            }
            
            return options;
        }

        private decimal GetQuestionScore(int questionId)
        {
            if (_examRecord?.ExamPaper?.PaperQuestions != null)
            {
                var paperQuestion = _examRecord.ExamPaper.PaperQuestions
                    .FirstOrDefault(pq => pq.QuestionId == questionId);
                return paperQuestion?.Score ?? 0;
            }
            return 0;
        }

        private void LoadSavedAnswer(AnswerRecord answerRecord)
        {
            if (string.IsNullOrWhiteSpace(answerRecord.UserAnswer))
                return;

            var userAnswer = answerRecord.UserAnswer;

            switch (CurrentQuestion?.QuestionType)
            {
                case QuestionType.SingleChoice:
                    LoadSingleChoiceAnswer(userAnswer);
                    break;
                case QuestionType.MultipleChoice:
                    LoadMultipleChoiceAnswer(userAnswer);
                    break;
                case QuestionType.TrueFalse:
                    TrueFalseAnswer = bool.Parse(userAnswer);
                    break;
                case QuestionType.FillInBlank:
                    FillInBlankAnswer = userAnswer;
                    break;
                case QuestionType.Essay:
                    EssayAnswer = userAnswer;
                    break;
            }

            OnPropertyChanged(nameof(TrueFalseAnswer));
            OnPropertyChanged(nameof(FillInBlankAnswer));
            OnPropertyChanged(nameof(EssayAnswer));
        }

        private void LoadSingleChoiceAnswer(string userAnswer)
        {
            if (CurrentQuestion?.Options != null)
            {
                foreach (var option in CurrentQuestion.Options)
                {
                    option.IsSelected = option.Label == userAnswer;
                }
            }
        }

        private void LoadMultipleChoiceAnswer(string userAnswer)
        {
            if (CurrentQuestion?.Options != null)
            {
                var selectedOptions = userAnswer.Split(',').Select(s => s.Trim()).ToList();
                foreach (var option in CurrentQuestion.Options)
                {
                    option.IsSelected = selectedOptions.Contains(option.Label);
                }
            }
        }

        private void UpdateQuestionNavigations()
        {
            for (int i = 0; i < QuestionNavigations.Count; i++)
            {
                var navigation = QuestionNavigations[i];
                navigation.IsCurrent = i == _currentQuestionIndex;
                navigation.IsAnswered = !string.IsNullOrWhiteSpace(_answerRecords[i].UserAnswer);
            }

            OnPropertyChanged(nameof(AnsweredCount));
            OnPropertyChanged(nameof(UnansweredCount));
        }

        private async Task SaveCurrentAnswerAsync()
        {
            try
            {
                if (_examRecord == null || CurrentQuestion == null)
                    return;

                var answerRecord = _answerRecords[_currentQuestionIndex];
                var userAnswer = GetCurrentAnswer();

                answerRecord.UserAnswer = userAnswer;
                
                await _examService.SaveAnswerAsync(_examRecord.RecordId, CurrentQuestion.QuestionId, userAnswer);
                
                UpdateQuestionNavigations();
                
                _logger.LogDebug("保存答案成功，题目ID: {QuestionId}", CurrentQuestion.QuestionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存答案失败，题目ID: {QuestionId}", CurrentQuestion?.QuestionId);
            }
        }

        private string GetCurrentAnswer()
        {
            if (CurrentQuestion == null)
                return "";

            switch (CurrentQuestion.QuestionType)
            {
                case QuestionType.SingleChoice:
                    var selectedOption = CurrentQuestion.Options?.FirstOrDefault(o => o.IsSelected);
                    return selectedOption?.Label ?? "";

                case QuestionType.MultipleChoice:
                    var selectedOptions = CurrentQuestion.Options?.Where(o => o.IsSelected).Select(o => o.Label);
                    return string.Join(",", selectedOptions ?? Enumerable.Empty<string>());

                case QuestionType.TrueFalse:
                    return TrueFalseAnswer?.ToString().ToLower() ?? "";

                case QuestionType.FillInBlank:
                    return FillInBlankAnswer ?? "";

                case QuestionType.Essay:
                    return EssayAnswer ?? "";

                default:
                    return "";
            }
        }

        private async Task SubmitExamAsync()
        {
            try
            {
                if (_examRecord == null)
                    return;

                // 保存当前答案
                await SaveCurrentAnswerAsync();

                // 确认提交
                var result = System.Windows.MessageBox.Show(
                    $"确定要提交考试吗？\n\n已答题目：{AnsweredCount}\n未答题目：{UnansweredCount}",
                    "确认提交",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);

                if (result != System.Windows.MessageBoxResult.Yes)
                    return;

                IsLoading = true;
                
                // 提交考试
                var success = await _examService.SubmitExamAsync(_examRecord.RecordId);
                
                if (success)
                {
                    IsExamInProgress = false;
                    _timer.Stop();
                    
                    System.Windows.MessageBox.Show("考试提交成功！", "提示", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    
                    ExamCompleted?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    System.Windows.MessageBox.Show("考试提交失败，请重试。", "错误", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "提交考试失败，记录ID: {RecordId}", _examRecord?.RecordId);
                System.Windows.MessageBox.Show("提交考试时发生错误，请重试。", "错误", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void PreviousQuestion()
        {
            if (CanGoPrevious)
            {
                // 保存当前答案
                _ = SaveCurrentAnswerAsync();
                LoadQuestion(_currentQuestionIndex - 1);
            }
        }

        private void NextQuestion()
        {
            if (CanGoNext)
            {
                // 保存当前答案
                _ = SaveCurrentAnswerAsync();
                LoadQuestion(_currentQuestionIndex + 1);
            }
        }

        private void NavigateToQuestion(int questionIndex)
        {
            if (questionIndex >= 0 && questionIndex < _answerRecords.Count)
            {
                // 保存当前答案
                _ = SaveCurrentAnswerAsync();
                LoadQuestion(questionIndex);
            }
        }

        private void SelectSingleOption(QuestionOptionViewModel option)
        {
            if (CurrentQuestion?.Options != null)
            {
                foreach (var opt in CurrentQuestion.Options)
                {
                    opt.IsSelected = opt == option;
                }
            }
        }

        private void SelectMultipleOption(QuestionOptionViewModel option)
        {
            option.IsSelected = !option.IsSelected;
        }

        private void SelectTrueFalse(bool? value)
        {
            TrueFalseAnswer = value;
            OnPropertyChanged(nameof(TrueFalseAnswer));
        }

        private async void OnTimerTick(object? sender, EventArgs e)
        {
            _remainingSeconds--;
            OnPropertyChanged(nameof(RemainingTimeText));

            // 更新服务器端的剩余时间
            if (_examRecord != null && _remainingSeconds % 30 == 0) // 每30秒更新一次
            {
                await _examService.UpdateRemainingTimeAsync(_examRecord.RecordId, _remainingSeconds);
            }

            // 检查是否超时
            if (_remainingSeconds <= 0)
            {
                _timer.Stop();
                IsExamInProgress = false;
                
                // 自动提交考试
                if (_examRecord != null)
                {
                    await SaveCurrentAnswerAsync();
                    await _examService.SubmitExamAsync(_examRecord.RecordId);
                }
                
                ExamTimeout?.Invoke(this, EventArgs.Empty);
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _timer?.Stop();
        }

        #endregion
    }

    /// <summary>
    /// 题目视图模型
    /// </summary>
    public class QuestionViewModel
    {
        public int QuestionId { get; set; }
        public string Content { get; set; } = "";
        public QuestionType QuestionType { get; set; }
        public decimal Score { get; set; }
        public List<QuestionOptionViewModel> Options { get; set; } = new();
    }

    /// <summary>
    /// 题目选项视图模型
    /// </summary>
    public class QuestionOptionViewModel : INotifyPropertyChanged
    {
        private bool _isSelected;

        public int OptionId { get; set; }
        public string Label { get; set; } = "";
        public string Content { get; set; } = "";

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// 题目导航视图模型
    /// </summary>
    public class QuestionNavigationViewModel : INotifyPropertyChanged
    {
        private bool _isAnswered;
        private bool _isCurrent;

        public int QuestionNumber { get; set; }
        public int QuestionIndex { get; set; }

        public bool IsAnswered
        {
            get => _isAnswered;
            set
            {
                _isAnswered = value;
                OnPropertyChanged();
            }
        }

        public bool IsCurrent
        {
            get => _isCurrent;
            set
            {
                _isCurrent = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}