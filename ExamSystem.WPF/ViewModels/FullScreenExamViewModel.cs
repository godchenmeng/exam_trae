using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;
using ExamSystem.Services.Interfaces;
using ExamSystem.WPF.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace ExamSystem.WPF.ViewModels
{
    /// <summary>
    /// 全屏考试窗口的ViewModel
    /// </summary>
    public class FullScreenExamViewModel : INotifyPropertyChanged
    {
        private readonly IExamService _examService;
        private readonly IAuthService _authService;
        private readonly DispatcherTimer _timer;
        private readonly int _paperId;
        private readonly string _paperTitle;
        
        private bool _isLoading;
        private string _examTitle = string.Empty;
        private int _currentQuestionIndex = 1;
        private int _totalQuestions;
        private TimeSpan _remainingTime;
        private string _remainingTimeText = "00:00:00";
        private ExamQuestionViewModel? _currentQuestion;
        private bool _isExitConfirmed;
        private ExamRecord? _examRecord;
        private List<AnswerRecord> _answerRecords = new();

        public FullScreenExamViewModel(int paperId, string paperTitle)
        {
            _paperId = paperId;
            _paperTitle = paperTitle;
            _examTitle = paperTitle;
            
            // 从依赖注入容器获取服务
            _examService = ((App)Application.Current).GetServices().GetRequiredService<IExamService>();
            _authService = ((App)Application.Current).GetServices().GetRequiredService<IAuthService>();
            
            // 初始化集合
            QuestionNavigations = new ObservableCollection<FullScreenQuestionNavigationViewModel>();
            
            // 初始化命令
            InitializeCommands();
            
            // 初始化计时器
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;
            
            // 加载考试数据
            _ = LoadExamDataAsync();
        }

        #region 属性

        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 考试标题
        /// </summary>
        public string ExamTitle
        {
            get => _examTitle;
            set
            {
                _examTitle = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 当前题目索引
        /// </summary>
        public int CurrentQuestionIndex
        {
            get => _currentQuestionIndex;
            set
            {
                _currentQuestionIndex = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanGoPrevious));
                OnPropertyChanged(nameof(CanGoNext));
            }
        }

        /// <summary>
        /// 总题目数
        /// </summary>
        public int TotalQuestions
        {
            get => _totalQuestions;
            set
            {
                _totalQuestions = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanGoPrevious));
                OnPropertyChanged(nameof(CanGoNext));
            }
        }

        /// <summary>
        /// 剩余时间文本
        /// </summary>
        public string RemainingTimeText
        {
            get => _remainingTimeText;
            set
            {
                _remainingTimeText = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 当前题目
        /// </summary>
        public ExamQuestionViewModel? CurrentQuestion
        {
            get => _currentQuestion;
            set
            {
                if (_currentQuestion == value) return;
                // 取消订阅旧题目的属性变化
                if (_currentQuestion != null)
                {
                    _currentQuestion.PropertyChanged -= OnCurrentQuestionPropertyChanged;
                    // 取消订阅旧题目选项的属性变化
                    if (_currentQuestion.Options != null)
                    {
                        foreach (var opt in _currentQuestion.Options)
                        {
                            opt.PropertyChanged -= OnOptionPropertyChanged;
                        }
                    }
                }
                _currentQuestion = value;
                OnPropertyChanged(nameof(CurrentQuestion));
                UpdateQuestionTypeProperties();
                // 订阅新题目的属性变化（用于更新答题卡状态及避免答案错乱）
                if (_currentQuestion != null)
                {
                    _currentQuestion.PropertyChanged += OnCurrentQuestionPropertyChanged;
                    // 订阅新题目各选项的IsSelected变化，用于实时更新答题卡状态
                    if (_currentQuestion.Options != null)
                    {
                        foreach (var opt in _currentQuestion.Options)
                        {
                            opt.PropertyChanged += OnOptionPropertyChanged;
                        }
                    }
                }
            }
        }

        private void OnCurrentQuestionPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ExamQuestionViewModel.FillInBlankAnswer) ||
                e.PropertyName == nameof(ExamQuestionViewModel.EssayAnswer) ||
                e.PropertyName == nameof(ExamQuestionViewModel.TrueFalseAnswer))
            {
                // 根据当前题型更新答题卡的已答状态
                var nav = QuestionNavigations.FirstOrDefault(n => n.QuestionIndex == CurrentQuestionIndex);
                bool answered = false;
                string userAnswer = string.Empty;
                if (IsFillInBlank)
                {
                    answered = !string.IsNullOrWhiteSpace(CurrentQuestion?.FillInBlankAnswer);
                    userAnswer = CurrentQuestion?.FillInBlankAnswer ?? string.Empty;
                }
                else if (IsEssay)
                {
                    answered = !string.IsNullOrWhiteSpace(CurrentQuestion?.EssayAnswer);
                    userAnswer = CurrentQuestion?.EssayAnswer ?? string.Empty;
                }
                else if (IsTrueFalse)
                {
                    answered = CurrentQuestion?.TrueFalseAnswer.HasValue ?? false;
                    userAnswer = (CurrentQuestion?.TrueFalseAnswer.HasValue ?? false) ? (CurrentQuestion.TrueFalseAnswer.Value ? "true" : "false") : string.Empty;
                }
                if (nav != null)
                {
                    nav.IsAnswered = answered;
                }
                // 同步到答案记录缓存
                var idx = CurrentQuestionIndex - 1;
                if (idx >= 0 && idx < _answerRecords.Count)
                {
                    _answerRecords[idx].UserAnswer = userAnswer;
                }
                OnPropertyChanged(nameof(AnsweredCount));
                OnPropertyChanged(nameof(UnansweredCount));
            }
        }

        // 题型判断属性增加对“论述题”的兼容
        public bool IsEssay => CurrentQuestion?.QuestionType == "简答题" || CurrentQuestion?.QuestionType == "论述题";

        // 题型布尔属性（基于中文题型）
        public bool IsSingleChoice => CurrentQuestion?.QuestionType == "单选题";
        public bool IsMultipleChoice => CurrentQuestion?.QuestionType == "多选题";
        public bool IsTrueFalse => CurrentQuestion?.QuestionType == "判断题";
        public bool IsFillInBlank => CurrentQuestion?.QuestionType == "填空题";

        // 题目导航集合
        public ObservableCollection<FullScreenQuestionNavigationViewModel> QuestionNavigations { get; }

        // 导航可用性（1-based 索引）
        public bool CanGoPrevious => CurrentQuestionIndex > 1;
        public bool CanGoNext => CurrentQuestionIndex < TotalQuestions;

        // 已答/未答计数
        public int AnsweredCount => _answerRecords.Count(ar => !string.IsNullOrWhiteSpace(ar.UserAnswer));
        public int UnansweredCount => TotalQuestions - AnsweredCount;

        // 退出确认状态
        public bool IsExitConfirmed
        {
            get => _isExitConfirmed;
            set
            {
                _isExitConfirmed = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region 命令

        public ICommand PreviousQuestionCommand { get; private set; } = null!;
        public ICommand NextQuestionCommand { get; private set; } = null!;
        public ICommand NavigateToQuestionCommand { get; private set; } = null!;
        public ICommand SelectSingleOptionCommand { get; private set; } = null!;
        public ICommand SelectMultipleOptionCommand { get; private set; } = null!;
        public ICommand SelectTrueFalseCommand { get; private set; } = null!;
        public ICommand SaveAnswerCommand { get; private set; } = null!;
        public ICommand SubmitExamCommand { get; private set; } = null!;
        public ICommand ExitExamCommand { get; private set; } = null!;

        #endregion

        #region 事件

        public event EventHandler? ExitRequested;

        #endregion

        #region 私有方法

        /// <summary>
        /// 初始化命令
        /// </summary>
        private void InitializeCommands()
        {
            PreviousQuestionCommand = new RelayCommand(PreviousQuestion, () => CanGoPrevious);
            NextQuestionCommand = new RelayCommand(NextQuestion, () => CanGoNext);
            NavigateToQuestionCommand = new RelayCommand<object>(NavigateToQuestion);
            SelectSingleOptionCommand = new RelayCommand<OptionViewModel>(SelectSingleOption);
            SelectMultipleOptionCommand = new RelayCommand<OptionViewModel>(SelectMultipleOption);
            SelectTrueFalseCommand = new RelayCommand<bool?>(SelectTrueFalse);
            SaveAnswerCommand = new RelayCommand(SaveAnswer);
            SubmitExamCommand = new RelayCommand(SubmitExam);
            ExitExamCommand = new RelayCommand(ExitExam);
        }

        /// <summary>
        /// 加载考试数据
        /// </summary>
        private async Task LoadExamDataAsync()
        {
            try
            {
                IsLoading = true;

                var currentUser = _authService.GetCurrentUser();
                if (currentUser == null)
                {
                    MessageBox.Show("当前用户未登录，无法开始考试。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    IsExitConfirmed = true;
                    ExitRequested?.Invoke(this, EventArgs.Empty);
                    return;
                }

                // 在启动考试前再次验证用户资格
                var validationResult = await _examService.ValidateUserExamEligibilityAsync(currentUser.UserId, _paperId);
                if (!validationResult.IsValid)
                {
                    MessageBox.Show($"无法开始考试：{validationResult.ErrorMessage}", "考试验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    IsExitConfirmed = true;
                    ExitRequested?.Invoke(this, EventArgs.Empty);
                    return;
                }

                _examRecord = await _examService.StartExamAsync(currentUser.UserId, _paperId);
                _answerRecords = _examRecord.AnswerRecords?.ToList() ?? new List<AnswerRecord>();

                ExamTitle = _examRecord.ExamPaper?.Name ?? _paperTitle;

                TotalQuestions = _answerRecords.Count;

                _remainingTime = TimeSpan.FromSeconds(_examRecord.RemainingTime);
                RemainingTimeText = _remainingTime.ToString(@"hh\:mm\:ss");

                // 初始化题目导航
                QuestionNavigations.Clear();
                for (int i = 1; i <= TotalQuestions; i++)
                {
                    var answered = !string.IsNullOrWhiteSpace(_answerRecords[i - 1].UserAnswer);
                    QuestionNavigations.Add(new FullScreenQuestionNavigationViewModel
                    {
                        QuestionNumber = i,
                        QuestionIndex = i,
                        IsAnswered = answered,
                        IsCurrent = i == 1
                    });
                }

                // 加载第一题
                await LoadQuestionAsync(1);

                // 开始计时
                _timer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载考试数据失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                IsExitConfirmed = true;
                ExitRequested?.Invoke(this, EventArgs.Empty);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 加载指定题目
        /// </summary>
        private async Task LoadQuestionAsync(int questionIndex)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"LoadQuestionAsync: 开始加载题目 {questionIndex}");
                
                if (_answerRecords == null || questionIndex < 1 || questionIndex > _answerRecords.Count)
                {
                    System.Diagnostics.Debug.WriteLine($"LoadQuestionAsync: 参数无效 - _answerRecords={_answerRecords?.Count}, questionIndex={questionIndex}");
                    return;
                }

                var answerRecord = _answerRecords[questionIndex - 1];
                var question = answerRecord.Question;
                if (question == null)
                {
                    System.Diagnostics.Debug.WriteLine($"LoadQuestionAsync: 题目为空");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"LoadQuestionAsync: 题目类型={question.QuestionType}, 选项数量={question.Options?.Count ?? 0}");

                var vm = new ExamQuestionViewModel
                {
                    Id = question.QuestionId,
                    Content = question.Content,
                    QuestionType = ToChineseQuestionType(question.QuestionType),
                    Score = GetQuestionScore(question.QuestionId),
                    Options = new ObservableCollection<OptionViewModel>()
                };

                System.Diagnostics.Debug.WriteLine($"LoadQuestionAsync: 创建ViewModel - QuestionType={vm.QuestionType}");

                // 选择题映射选项
                if (question.QuestionType == QuestionType.SingleChoice || question.QuestionType == QuestionType.MultipleChoice)
                {
                    var options = (question.Options ?? new List<QuestionOption>())
                        .OrderBy(o => o.OptionLabel)
                        .Select(o => new OptionViewModel
                        {
                            Label = o.OptionLabel,
                            Text = $"{o.OptionLabel}. {o.Content}",
                            IsSelected = false
                        }).ToList();

                    System.Diagnostics.Debug.WriteLine($"LoadQuestionAsync: 创建了 {options.Count} 个选项");

                    foreach (var opt in options)
                    {
                        vm.Options.Add(opt);
                        System.Diagnostics.Debug.WriteLine($"LoadQuestionAsync: 添加选项 - {opt.Label}: {opt.Text}");
                    }
                }

                CurrentQuestion = vm;
                System.Diagnostics.Debug.WriteLine($"LoadQuestionAsync: 设置CurrentQuestion完成，Options.Count={CurrentQuestion.Options.Count}");

                // 还原已保存答案
                var userAnswer = answerRecord.UserAnswer;
                if (!string.IsNullOrWhiteSpace(userAnswer))
                {
                    switch (question.QuestionType)
                    {
                        case QuestionType.SingleChoice:
                            foreach (var opt in CurrentQuestion.Options)
                                opt.IsSelected = opt.Label == userAnswer;
                            break;
                        case QuestionType.MultipleChoice:
                            var labels = userAnswer.Split(',').Select(s => s.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
                            foreach (var opt in CurrentQuestion.Options)
                                opt.IsSelected = labels.Contains(opt.Label);
                            break;
                        case QuestionType.TrueFalse:
                            if (bool.TryParse(userAnswer, out var tf))
                                CurrentQuestion.TrueFalseAnswer = tf;
                            break;
                        case QuestionType.FillInBlank:
                            CurrentQuestion.FillInBlankAnswer = userAnswer;
                            break;
                        case QuestionType.ShortAnswer:
                        case QuestionType.Essay:
                            CurrentQuestion.EssayAnswer = userAnswer;
                            break;
                    }
                }

                // 更新导航状态
                foreach (var nav in QuestionNavigations)
                {
                    nav.IsCurrent = nav.QuestionIndex == questionIndex;
                    nav.IsAnswered = !string.IsNullOrWhiteSpace(_answerRecords[nav.QuestionIndex - 1].UserAnswer);
                }

                CurrentQuestionIndex = questionIndex;
                OnPropertyChanged(nameof(AnsweredCount));
                OnPropertyChanged(nameof(UnansweredCount));
                
                System.Diagnostics.Debug.WriteLine($"LoadQuestionAsync: 完成加载题目 {questionIndex}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadQuestionAsync: 异常 - {ex.Message}");
                MessageBox.Show($"加载题目失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 更新题目类型属性
        /// </summary>
        private void UpdateQuestionTypeProperties()
        {
            // 添加调试输出
            System.Diagnostics.Debug.WriteLine($"UpdateQuestionTypeProperties: CurrentQuestion={CurrentQuestion?.QuestionType}");
            System.Diagnostics.Debug.WriteLine($"IsSingleChoice={IsSingleChoice}, IsMultipleChoice={IsMultipleChoice}, IsTrueFalse={IsTrueFalse}");
            
            OnPropertyChanged(nameof(IsSingleChoice));
            OnPropertyChanged(nameof(IsMultipleChoice));
            OnPropertyChanged(nameof(IsTrueFalse));
            OnPropertyChanged(nameof(IsFillInBlank));
            OnPropertyChanged(nameof(IsEssay));
        }

        // 当选项的IsSelected变化时刷新答题卡和缓存答案，避免事件互斥导致的状态错乱
        private void OnOptionPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(OptionViewModel.IsSelected))
            {
                return;
            }
            if (CurrentQuestion == null)
            {
                return;
            }

            string userAnswer = string.Empty;
            if (IsSingleChoice)
            {
                var selected = CurrentQuestion.Options.FirstOrDefault(o => o.IsSelected);
                userAnswer = selected?.Label ?? string.Empty;
            }
            else if (IsMultipleChoice)
            {
                var selectedLabels = CurrentQuestion.Options.Where(o => o.IsSelected).Select(o => o.Label);
                userAnswer = string.Join(",", selectedLabels);
            }

            var nav = QuestionNavigations.FirstOrDefault(q => q.QuestionIndex == CurrentQuestionIndex);
            if (nav != null)
            {
                nav.IsAnswered = !string.IsNullOrWhiteSpace(userAnswer);
            }

            var idx = CurrentQuestionIndex - 1;
            if (idx >= 0 && idx < _answerRecords.Count)
            {
                _answerRecords[idx].UserAnswer = userAnswer;
            }

            OnPropertyChanged(nameof(AnsweredCount));
            OnPropertyChanged(nameof(UnansweredCount));
        }

        private string ToChineseQuestionType(QuestionType type)
        {
            return type switch
            {
                QuestionType.SingleChoice => "单选题",
                QuestionType.MultipleChoice => "多选题",
                QuestionType.TrueFalse => "判断题",
                QuestionType.FillInBlank => "填空题",
                QuestionType.ShortAnswer => "简答题",
                QuestionType.Essay => "简答题",
                _ => "未知"
            };
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

        /// <summary>
        /// 计时器事件
        /// </summary>
        private void Timer_Tick(object? sender, EventArgs e)
        {
            _remainingTime = _remainingTime.Subtract(TimeSpan.FromSeconds(1));
            
            if (_remainingTime <= TimeSpan.Zero)
            {
                _timer.Stop();
                RemainingTimeText = "00:00:00";
                
                // 时间到，自动提交
                MessageBox.Show("考试时间已到，系统将自动提交试卷。", "时间到", MessageBoxButton.OK, MessageBoxImage.Information);
                SubmitExam();
            }
            else
            {
                RemainingTimeText = _remainingTime.ToString(@"hh\:mm\:ss");
                
                // 每30秒更新服务器端剩余时间
                if (_examRecord != null)
                {
                    var remainingSeconds = (int)Math.Round(_remainingTime.TotalSeconds);
                    if (remainingSeconds % 30 == 0)
                    {
                        _ = _examService.UpdateRemainingTimeAsync(_examRecord.RecordId, remainingSeconds);
                    }
                }
            }
        }

        /// <summary>
        /// 上一题
        /// </summary>
        private async void PreviousQuestion()
        {
            if (CanGoPrevious)
            {
                await SaveCurrentAnswer();
                await LoadQuestionAsync(CurrentQuestionIndex - 1);
            }
        }

        /// <summary>
        /// 下一题
        /// </summary>
        private async void NextQuestion()
        {
            if (CanGoNext)
            {
                await SaveCurrentAnswer();
                await LoadQuestionAsync(CurrentQuestionIndex + 1);
            }
        }

        /// <summary>
        /// 导航到指定题目
        /// </summary>
        private async void NavigateToQuestion(int questionIndex)
        {
            if (questionIndex != CurrentQuestionIndex)
            {
                await SaveCurrentAnswer();
                await LoadQuestionAsync(questionIndex);
            }
        }

        /// <summary>
        /// 导航到指定题目（对象参数重载）
        /// </summary>
        private async void NavigateToQuestion(object? parameter)
        {
            if (parameter != null && int.TryParse(parameter.ToString(), out int questionIndex))
            {
                NavigateToQuestion(questionIndex);
            }
        }

        /// <summary>
        /// 选择单选选项
        /// </summary>
        private void SelectSingleOption(OptionViewModel? option)
        {
            if (option == null || CurrentQuestion == null)
            {
                return;
            }

            // 短路：如果该选项已是唯一选中，则不重复触发，避免不必要的UI刷新
            var isOnlySelected = option.IsSelected && CurrentQuestion.Options.All(o => ReferenceEquals(o, option) ? true : !o.IsSelected);
            if (isOnlySelected)
            {
                Serilog.Log.Debug("VM: SelectSingleOption short-circuit: QIndex={QIndex}, Option={Label}", CurrentQuestionIndex, option.Label);
                return;
            }

            Serilog.Log.Debug("VM: SelectSingleOption begin: QIndex={QIndex}, Option={Label}", CurrentQuestionIndex, option.Label);

            foreach (var opt in CurrentQuestion.Options)
            {
                opt.IsSelected = opt == option;
            }

            var selectedCount = CurrentQuestion.Options.Count(o => o.IsSelected);
            Serilog.Log.Debug("VM: SelectSingleOption end: QIndex={QIndex}, selectedCount={Count}", CurrentQuestionIndex, selectedCount);
            
            // 标记当前题目为已答
            var nav = QuestionNavigations.FirstOrDefault(q => q.QuestionIndex == CurrentQuestionIndex);
            if (nav != null)
            {
                nav.IsAnswered = true;
            }
            
            OnPropertyChanged(nameof(AnsweredCount));
            OnPropertyChanged(nameof(UnansweredCount));
        }

        // 新增：命令包装方法，兼容现有 XAML 绑定，内部调用显式设置以避免递归
        private void SelectMultipleOption(OptionViewModel? option)
        {
            if (option == null || CurrentQuestion == null || !IsMultipleChoice)
            {
                return;
            }
            var target = !option.IsSelected;
            Serilog.Log.Debug("VM: SelectMultipleOption command wrapper: QIndex={QIndex}, Option={Label}, target={Target}", CurrentQuestionIndex, option.Label, target);
            SetMultipleOption(option, target);
        }

        /// <summary>
        /// 选择多选选项
        /// </summary>
        public void SetMultipleOption(OptionViewModel? option, bool selected)
        {
            if (option == null)
            {
                return;
            }

            if (option.IsSelected == selected)
            {
                Serilog.Log.Debug("VM: SetMultipleOption short-circuit: QIndex={QIndex}, Option={Label}, target={Target}", CurrentQuestionIndex, option.Label, selected);
                return;
            }

            Serilog.Log.Debug("VM: SetMultipleOption begin: QIndex={QIndex}, Option={Label}, target={Target}", CurrentQuestionIndex, option.Label, selected);
            option.IsSelected = selected;

            var nav = QuestionNavigations.FirstOrDefault(q => q.QuestionIndex == CurrentQuestionIndex);
            if (nav != null)
            {
                nav.IsAnswered = CurrentQuestion?.Options.Any(o => o.IsSelected) == true;
            }

            OnPropertyChanged(nameof(AnsweredCount));
            OnPropertyChanged(nameof(UnansweredCount));

            var selectedCount = CurrentQuestion?.Options.Count(o => o.IsSelected) ?? 0;
            Serilog.Log.Debug("VM: SetMultipleOption end: QIndex={QIndex}, selectedCount={Count}", CurrentQuestionIndex, selectedCount);
        }

        /// <summary>
        /// 选择判断题答案
        /// </summary>
        private void SelectTrueFalse(bool? answer)
        {
            if (CurrentQuestion != null)
            {
                CurrentQuestion.TrueFalseAnswer = answer;
                
                // 标记当前题目为已答
                var nav = QuestionNavigations.FirstOrDefault(q => q.QuestionIndex == CurrentQuestionIndex);
                if (nav != null)
                {
                    nav.IsAnswered = true;
                }
                
                OnPropertyChanged(nameof(AnsweredCount));
                OnPropertyChanged(nameof(UnansweredCount));
            }
        }

        /// <summary>
        /// 保存当前答案
        /// </summary>
        private async Task SaveCurrentAnswer()
        {
            try
            {
                if (CurrentQuestion != null && _examRecord != null)
                {
                    string userAnswer = string.Empty;

                    if (IsSingleChoice)
                    {
                        var selected = CurrentQuestion.Options.FirstOrDefault(o => o.IsSelected);
                        userAnswer = selected?.Label ?? string.Empty;
                    }
                    else if (IsMultipleChoice)
                    {
                        var selectedLabels = CurrentQuestion.Options.Where(o => o.IsSelected).Select(o => o.Label);
                        userAnswer = string.Join(",", selectedLabels);
                    }
                    else if (IsTrueFalse)
                    {
                        userAnswer = CurrentQuestion.TrueFalseAnswer?.ToString().ToLower() ?? string.Empty;
                    }
                    else if (IsFillInBlank)
                    {
                        userAnswer = CurrentQuestion.FillInBlankAnswer ?? string.Empty;
                    }
                    else if (IsEssay)
                    {
                        userAnswer = CurrentQuestion.EssayAnswer ?? string.Empty;
                    }

                    var idx = CurrentQuestionIndex - 1;
                    if (idx >= 0 && idx < _answerRecords.Count)
                    {
                        _answerRecords[idx].UserAnswer = userAnswer;
                    }

                    await _examService.SaveAnswerAsync(_examRecord.RecordId, CurrentQuestion.Id, userAnswer);

                    var nav = QuestionNavigations.FirstOrDefault(q => q.QuestionIndex == CurrentQuestionIndex);
                    if (nav != null)
                    {
                        nav.IsAnswered = !string.IsNullOrWhiteSpace(userAnswer);
                    }

                    OnPropertyChanged(nameof(AnsweredCount));
                    OnPropertyChanged(nameof(UnansweredCount));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存答案失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 保存答案命令
        /// </summary>
        private async void SaveAnswer()
        {
            await SaveCurrentAnswer();
            MessageBox.Show("答案已保存", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 提交考试
        /// </summary>
        private async void SubmitExam()
        {
            var result = MessageBox.Show(
                "确定要提交考试吗？提交后将无法修改答案。",
                "提交确认",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    IsLoading = true;
                    _timer.Stop();

                    // 保存当前答案
                    await SaveCurrentAnswer();

                    var success = false;
                    if (_examRecord != null)
                    {
                        success = await _examService.SubmitExamAsync(_examRecord.RecordId);
                    }

                    if (success)
                    {
                        MessageBox.Show("考试提交成功！", "提交成功", MessageBoxButton.OK, MessageBoxImage.Information);
                        IsExitConfirmed = true;
                        ExitRequested?.Invoke(this, EventArgs.Empty);
                    }
                    else
                    {
                        MessageBox.Show("提交考试失败，请稍后重试。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        _timer.Start();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"提交考试失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    _timer.Start();
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        /// <summary>
        /// 退出考试
        /// </summary>
        private void ExitExam()
        {
            var result = MessageBox.Show(
                "确定要退出考试吗？退出后考试进度将会保存，但无法继续答题。",
                "退出确认",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (result == MessageBoxResult.Yes)
            {
                _timer.Stop();
                IsExitConfirmed = true;
                ExitRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 强制退出
        /// </summary>
        public void ForceExit()
        {
            _timer.Stop();
            IsExitConfirmed = true;
        }

        #endregion

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// 考试题目ViewModel
    /// </summary>
    public class ExamQuestionViewModel : INotifyPropertyChanged
    {
        private bool? _trueFalseAnswer;
        private string _fillInBlankAnswer = string.Empty;
        private string _essayAnswer = string.Empty;

        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string QuestionType { get; set; } = string.Empty;
        public decimal Score { get; set; }
        public ObservableCollection<OptionViewModel> Options { get; set; } = new();

        public bool? TrueFalseAnswer
        {
            get => _trueFalseAnswer;
            set
            {
                _trueFalseAnswer = value;
                OnPropertyChanged();
            }
        }

        public string FillInBlankAnswer
        {
            get => _fillInBlankAnswer;
            set
            {
                _fillInBlankAnswer = value;
                OnPropertyChanged();
            }
        }

        public string EssayAnswer
        {
            get => _essayAnswer;
            set
            {
                _essayAnswer = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// 选项ViewModel
    /// </summary>
    public class OptionViewModel : INotifyPropertyChanged
    {
        private bool _isSelected;

        public string Label { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;

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

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// 全屏考试题目导航ViewModel
    /// </summary>
    public class FullScreenQuestionNavigationViewModel : INotifyPropertyChanged
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

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}