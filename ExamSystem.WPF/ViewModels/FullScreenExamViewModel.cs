using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using ExamSystem.Domain.DTOs;
using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;
using ExamSystem.Services.Interfaces;
using ExamSystem.WPF.Commands;
using ExamSystem.WPF.Views;
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
        private readonly IMapDrawingService _mapDrawingService;
        private readonly DispatcherTimer _timer;
        private readonly int _paperId;
        private readonly string _paperTitle;
        private FullScreenExamWindow? _examWindow;
        
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
            _mapDrawingService = ((App)Application.Current).GetServices().GetRequiredService<IMapDrawingService>();
            
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
        /// 设置考试窗口引用
        /// </summary>
        public void SetExamWindow(FullScreenExamWindow window)
        {
            _examWindow = window;
        }

        /// <summary>
        /// 收集所有地图绘制题的数据
        /// </summary>
        private async Task CollectAllMapDrawingDataAsync()
        {
            try
            {
                if (_examWindow == null || _answerRecords == null || _examRecord == null)
                {
                    return;
                }

                System.Diagnostics.Debug.WriteLine("FullScreenExamViewModel 开始收集所有地图绘制题数据...");

                // 遍历所有答题记录，找出地图绘制题
                for (int i = 0; i < _answerRecords.Count; i++)
                {
                    var answerRecord = _answerRecords[i];
                    var question = answerRecord.Question;
                    
                    if (question?.QuestionType == QuestionType.MapDrawing)
                    {
                        System.Diagnostics.Debug.WriteLine($"FullScreenExamViewModel 发现地图绘制题: 题目{i + 1}");
                        
                        string? mapData = null;
                        
                        // 如果当前题目就是地图绘制题，直接从WebView获取数据
                        if (CurrentQuestionIndex == i + 1)
                        {
                            var rawMapData = await _examWindow.GetMapDrawingDataAsync();
                            if (!string.IsNullOrEmpty(rawMapData))
                            {
                                try
                                {
                                    // 解析外层JSON，提取data字段
                                    var outerJsonDoc = System.Text.Json.JsonDocument.Parse(rawMapData);
                                    string? innerDataString = null;
                                    
                                    if (outerJsonDoc.RootElement.TryGetProperty("data", out var dataElement))
                                    {
                                        innerDataString = dataElement.GetString();
                                    }
                                    
                                    if (string.IsNullOrEmpty(innerDataString))
                                    {
                                        System.Diagnostics.Debug.WriteLine("警告: 未找到data字段或data字段为空");
                                        continue;
                                    }

                                    // 解析内层JSON数据，提取完整的地图信息
                                    var innerJsonDoc = System.Text.Json.JsonDocument.Parse(innerDataString);
                                    var hasOverlays = innerJsonDoc.RootElement.TryGetProperty("overlays", out var overlaysElement);
                                    var hasCenter = innerJsonDoc.RootElement.TryGetProperty("center", out var centerElement);
                                    var hasZoom = innerJsonDoc.RootElement.TryGetProperty("zoom", out var zoomElement);

                                    // 构建包含完整信息的地图数据对象
                                    var completeMapData = new Dictionary<string, object>();
                                    
                                    // 转换前端overlays数据格式为后端MapDrawingDto格式
                                    var convertedOverlays = hasOverlays ? ConvertOverlaysToMapDrawingData(overlaysElement) : new List<MapDrawingDto>();
                                    completeMapData["overlays"] = convertedOverlays;
                                    
                                    // 保留center和zoom信息
                                    if (hasCenter)
                                    {
                                        completeMapData["center"] = System.Text.Json.JsonSerializer.Deserialize<object>(centerElement.GetRawText());
                                    }
                                    if (hasZoom)
                                    {
                                        completeMapData["zoom"] = zoomElement.GetInt32();
                                    }
                                    
                                    mapData = System.Text.Json.JsonSerializer.Serialize(completeMapData);

                                    if (CurrentQuestion != null)
                                    {
                                        CurrentQuestion.MapDrawingAnswer = mapData;
                                    }
                                    answerRecord.UserAnswer = mapData;
                                    
                                    System.Diagnostics.Debug.WriteLine($"FullScreenExamViewModel 收集到当前题目地图数据: 转换了 {convertedOverlays.Count} 个图形");
                                }
                                catch (Exception convertEx)
                                {
                                    System.Diagnostics.Debug.WriteLine($"转换地图数据格式失败: {convertEx.Message}");
                                    // 如果转换失败，使用原始数据
                                    mapData = rawMapData;
                                    if (CurrentQuestion != null)
                                    {
                                        CurrentQuestion.MapDrawingAnswer = mapData;
                                    }
                                    answerRecord.UserAnswer = mapData;
                                }
                            }
                        }
                        // 对于其他地图绘制题，使用已保存的数据
                        else if (!string.IsNullOrEmpty(answerRecord.UserAnswer))
                        {
                            mapData = answerRecord.UserAnswer;
                            System.Diagnostics.Debug.WriteLine($"FullScreenExamViewModel 使用已保存的地图数据: 题目{i + 1}");
                        }

                        // 使用专门的地图绘制答案保存方法
                        if (!string.IsNullOrEmpty(mapData))
                        {
                            try
                            {
                                // 解析地图数据以提取中心点和缩放级别
                                string? mapCenter = null;
                                int? mapZoom = null;
                                
                                if (mapData.Contains("\"center\"") && mapData.Contains("\"zoom\""))
                                {
                                    try
                                    {
                                        var jsonDoc = System.Text.Json.JsonDocument.Parse(mapData);
                                        if (jsonDoc.RootElement.TryGetProperty("center", out var centerElement))
                                        {
                                            mapCenter = centerElement.GetRawText();
                                        }
                                        if (jsonDoc.RootElement.TryGetProperty("zoom", out var zoomElement))
                                        {
                                            mapZoom = zoomElement.GetInt32();
                                        }
                                    }
                                    catch (Exception parseEx)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"解析地图数据失败: {parseEx.Message}");
                                    }
                                }

                                var saveResult = await _examService.SaveMapDrawingAnswerAsync(
                                    _examRecord.RecordId, 
                                    question.QuestionId, 
                                    mapData, 
                                    mapCenter, 
                                    mapZoom);
                                
                                if (saveResult)
                                {
                                    System.Diagnostics.Debug.WriteLine($"地图绘制答案保存成功: 题目{question.QuestionId}");
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"地图绘制答案保存失败: 题目{question.QuestionId}");
                                }
                            }
                            catch (Exception saveEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"保存地图绘制答案异常: {saveEx.Message}");
                            }
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine("FullScreenExamViewModel 地图绘制题数据收集完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FullScreenExamViewModel 收集地图绘制数据失败: {ex.Message}");
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

        private void OnCurrentQuestionPropertyChanged(object? sender, PropertyChangedEventArgs e)
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

        // 地图绘制服务属性
        public IMapDrawingService MapDrawingService => _mapDrawingService;

        // 地图绘制题相关属性
        private bool _isMapDrawing = false;
        public bool IsMapDrawing
        {
            get => _isMapDrawing;
            set
            {
                _isMapDrawing = value;
                OnPropertyChanged();
            }
        }

        private int _mapDrawingOverlayCount = 0;
        public int MapDrawingOverlayCount
        {
            get => _mapDrawingOverlayCount;
            set
            {
                _mapDrawingOverlayCount = value;
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
        
        // 地图绘制题相关命令
        public ICommand ClearMapDrawingCommand { get; private set; } = null!;
        public ICommand SaveMapDrawingCommand { get; private set; } = null!;

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
            
            // 地图绘制题相关命令
            ClearMapDrawingCommand = new RelayCommand(ClearMapDrawing);
            SaveMapDrawingCommand = new RelayCommand(SaveMapDrawing);
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

                // 地图绘制题特殊处理 - 在设置CurrentQuestion之前设置，确保属性变化事件能正确触发
                if (question.QuestionType == QuestionType.MapDrawing)
                {
                    IsMapDrawing = true;
                    MapDrawingOverlayCount = 0;
                }
                else
                {
                    IsMapDrawing = false;
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
                        case QuestionType.MapDrawing:
                            // 地图绘制题答案通过WebView2处理
                            CurrentQuestion.MapDrawingAnswer = userAnswer;
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
                await Task.CompletedTask;
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
            System.Diagnostics.Debug.WriteLine($"IsSingleChoice={IsSingleChoice}, IsMultipleChoice={IsMultipleChoice}, IsTrueFalse={IsTrueFalse}, IsMapDrawing={IsMapDrawing}");
            
            OnPropertyChanged(nameof(IsSingleChoice));
            OnPropertyChanged(nameof(IsMultipleChoice));
            OnPropertyChanged(nameof(IsTrueFalse));
            OnPropertyChanged(nameof(IsFillInBlank));
            OnPropertyChanged(nameof(IsEssay));
            OnPropertyChanged(nameof(IsMapDrawing));
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
                QuestionType.MapDrawing => "地图绘制题",
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
        private void NavigateToQuestion(object? parameter)
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
                    else if (IsMapDrawing)
                    {

                        await CollectAllMapDrawingDataAsync();
                        userAnswer = CurrentQuestion.MapDrawingAnswer ?? string.Empty;
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

                    // 收集所有地图绘制题的数据
                    await CollectAllMapDrawingDataAsync();

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

        /// <summary>
        /// 获取当前答题记录
        /// </summary>
        public AnswerRecord? GetCurrentAnswerRecord()
        {
            if (CurrentQuestionIndex >= 1 && CurrentQuestionIndex <= _answerRecords.Count)
            {
                return _answerRecords[CurrentQuestionIndex - 1]; // 转换为0-based索引
            }
            return null;
        }

        /// <summary>
        /// 清除地图绘制
        /// </summary>
        private void ClearMapDrawing()
        {
            // 这个方法将通过WebView2与前端地图编辑器通信来清除绘制内容
            // 具体实现在FullScreenExamWindow.xaml.cs中
        }

        /// <summary>
        /// 保存地图绘制
        /// </summary>
        private void SaveMapDrawing()
        {
            // 这个方法将通过WebView2与前端地图编辑器通信来保存绘制内容
            // 具体实现在FullScreenExamWindow.xaml.cs中
        }

        #endregion

        #region 地图绘制数据转换方法

        /// <summary>
        /// 转换前端overlays数据为后端MapDrawingDto格式
        /// </summary>
        private List<MapDrawingDto> ConvertOverlaysToMapDrawingData(JsonElement overlaysElement)
        {
            var result = new List<MapDrawingDto>();

            try
            {
                System.Diagnostics.Debug.WriteLine($"FullScreenExamViewModel ConvertOverlaysToMapDrawingData 开始转换");
                System.Diagnostics.Debug.WriteLine($"FullScreenExamViewModel overlaysElement类型: {overlaysElement.ValueKind}");
                System.Diagnostics.Debug.WriteLine($"FullScreenExamViewModel overlaysElement内容: {overlaysElement.GetRawText()}");
                
                if (overlaysElement.ValueKind != JsonValueKind.Array)
                {
                    System.Diagnostics.Debug.WriteLine("FullScreenExamViewModel 警告: overlays不是数组格式");
                    return result;
                }

                var currentAnswerRecord = GetCurrentAnswerRecord();
                int answerId = currentAnswerRecord?.AnswerId ?? 0;

                int orderIndex = 0;
                foreach (var overlay in overlaysElement.EnumerateArray())
                {
                    System.Diagnostics.Debug.WriteLine($"FullScreenExamViewModel 处理第 {orderIndex} 个overlay: {overlay.GetRawText()}");
                    
                    var mapDrawingDto = new MapDrawingDto
                    {
                        AnswerId = answerId,
                        OrderIndex = orderIndex++,
                        CreatedAt = DateTime.Now
                    };

                    // 提取基本信息
                    if (overlay.TryGetProperty("type", out var typeElement))
                    {
                        mapDrawingDto.ShapeType = ConvertShapeType(typeElement.GetString());
                        System.Diagnostics.Debug.WriteLine($"FullScreenExamViewModel 图形类型: {typeElement.GetString()} -> {mapDrawingDto.ShapeType}");
                    }

                    // 提取标签信息 - 新格式使用name字段
                    if (overlay.TryGetProperty("name", out var nameElement))
                    {
                        mapDrawingDto.Label = nameElement.GetString();
                        System.Diagnostics.Debug.WriteLine($"FullScreenExamViewModel 标签(name): {mapDrawingDto.Label}");
                    }
                    else if (overlay.TryGetProperty("meta", out var metaElement) && 
                             metaElement.TryGetProperty("label", out var labelElement))
                    {
                        mapDrawingDto.Label = labelElement.GetString();
                        System.Diagnostics.Debug.WriteLine($"FullScreenExamViewModel 标签(meta.label): {mapDrawingDto.Label}");
                    }

                    // 提取坐标信息 - 支持新格式和旧格式
                    mapDrawingDto.Coordinates = ExtractCoordinatesFromNewFormat(overlay, mapDrawingDto.ShapeType);
                    System.Diagnostics.Debug.WriteLine($"FullScreenExamViewModel 坐标数量: {mapDrawingDto.Coordinates.Count}");
                    if (mapDrawingDto.Coordinates.Count > 0)
                    {
                        var firstCoord = mapDrawingDto.Coordinates[0];
                        System.Diagnostics.Debug.WriteLine($"FullScreenExamViewModel 第一个坐标: 经度={firstCoord.Longitude}, 纬度={firstCoord.Latitude}");
                    }
                    if (overlay.TryGetProperty("style", out var styleElement))
                    {
                        mapDrawingDto.Style = ExtractStyle(styleElement);
                    }
                    else
                    {
                        // 为新格式设置默认样式
                        mapDrawingDto.Style = new MapDrawingStyle
                        {
                            StrokeColor = "#ff0000",
                            FillColor = "#ff0000",
                            StrokeWidth = 2,
                            Opacity = 1.0,
                            IsFilled = false
                        };
                    }

                    result.Add(mapDrawingDto);
                }

                System.Diagnostics.Debug.WriteLine($"FullScreenExamViewModel 转换overlays数据完成，共 {result.Count} 个图形");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FullScreenExamViewModel 转换overlays数据格式失败: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// 转换图形类型
        /// </summary>
        /// <param name="frontendType">前端图形类型</param>
        /// <returns>后端图形类型</returns>
        private string ConvertShapeType(string? frontendType)
        {
            return frontendType?.ToLower() switch
            {
                "marker" => "Marker",
                "polyline" => "Line", 
                "polygon" => "Polygon",
                "circle" => "Circle",
                "rectangle" => "Rectangle",
                _ => "Point"
            };
        }

        /// <summary>
        /// 提取坐标信息
        /// </summary>
        /// <param name="geometryElement">几何信息JSON元素</param>
        /// <param name="shapeType">图形类型</param>
        /// <returns>坐标列表</returns>
        /// <summary>
        /// 从新格式的overlay数据中提取坐标信息
        /// </summary>
        /// <param name="overlay">overlay对象</param>
        /// <param name="shapeType">图形类型</param>
        /// <returns>坐标列表</returns>
        private List<MapCoordinate> ExtractCoordinatesFromNewFormat(JsonElement overlay, string shapeType)
        {
            var coordinates = new List<MapCoordinate>();

            try
            {
                switch (shapeType.ToLower())
                {
                    case "marker":
                    case "point":
                        // 新格式：{ point: { lng: 116.4, lat: 39.9 } }
                        if (overlay.TryGetProperty("point", out var pointElement))
                        {
                            if (pointElement.TryGetProperty("lng", out var lng) && 
                                pointElement.TryGetProperty("lat", out var lat))
                            {
                                coordinates.Add(new MapCoordinate 
                                { 
                                    Longitude = lng.GetDouble(), 
                                    Latitude = lat.GetDouble() 
                                });
                            }
                        }
                        // 兼容旧格式
                        else if (overlay.TryGetProperty("geometry", out var geometryElement))
                        {
                            coordinates = ExtractCoordinates(geometryElement, shapeType);
                        }
                        break;

                    case "line":
                    case "polygon":
                        // 新格式可能直接在overlay中有path字段，或者在geometry中
                        if (overlay.TryGetProperty("path", out var pathElement) && 
                            pathElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var point in pathElement.EnumerateArray())
                            {
                                if (point.TryGetProperty("lng", out var pLng) && 
                                    point.TryGetProperty("lat", out var pLat))
                                {
                                    coordinates.Add(new MapCoordinate 
                                    { 
                                        Longitude = pLng.GetDouble(), 
                                        Latitude = pLat.GetDouble() 
                                    });
                                }
                            }
                        }
                        // 兼容旧格式
                        else if (overlay.TryGetProperty("geometry", out var geometryElement))
                        {
                            coordinates = ExtractCoordinates(geometryElement, shapeType);
                        }
                        break;

                    case "circle":
                        // 新格式可能直接在overlay中有center和radius字段
                        if (overlay.TryGetProperty("center", out var centerElement))
                        {
                            if (centerElement.TryGetProperty("lng", out var cLng) && 
                                centerElement.TryGetProperty("lat", out var cLat))
                            {
                                coordinates.Add(new MapCoordinate 
                                { 
                                    Longitude = cLng.GetDouble(), 
                                    Latitude = cLat.GetDouble() 
                                });
                            }
                        }
                        // 半径信息
                        if (overlay.TryGetProperty("radius", out var radiusElement) && coordinates.Count > 0)
                        {
                            coordinates[0].Altitude = radiusElement.GetDouble();
                        }
                        // 兼容旧格式
                        else if (overlay.TryGetProperty("geometry", out var geometryElement))
                        {
                            coordinates = ExtractCoordinates(geometryElement, shapeType);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FullScreenExamViewModel 从新格式提取坐标信息失败，图形类型: {shapeType}, 错误: {ex.Message}");
            }

            return coordinates;
        }

        private List<MapCoordinate> ExtractCoordinates(JsonElement geometryElement, string shapeType)
        {
            var coordinates = new List<MapCoordinate>();

            try
            {
                switch (shapeType.ToLower())
                {
                    case "marker":
                    case "point":
                        // 点：{ lng: 116.4, lat: 39.9 }
                        if (geometryElement.TryGetProperty("lng", out var lng) && 
                            geometryElement.TryGetProperty("lat", out var lat))
                        {
                            coordinates.Add(new MapCoordinate 
                            { 
                                Longitude = lng.GetDouble(), 
                                Latitude = lat.GetDouble() 
                            });
                        }
                        break;

                    case "line":
                    case "polygon":
                        // 线/多边形：{ path: [ {lng:116.4,lat:39.9}, {lng:116.41,lat:39.91} ] }
                        if (geometryElement.TryGetProperty("path", out var pathElement) && 
                            pathElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var point in pathElement.EnumerateArray())
                            {
                                if (point.TryGetProperty("lng", out var pLng) && 
                                    point.TryGetProperty("lat", out var pLat))
                                {
                                    coordinates.Add(new MapCoordinate 
                                    { 
                                        Longitude = pLng.GetDouble(), 
                                        Latitude = pLat.GetDouble() 
                                    });
                                }
                            }
                        }
                        break;

                    case "circle":
                        // 圆：{ center: {lng:116.4,lat:39.9}, radius: 1000 }
                        if (geometryElement.TryGetProperty("center", out var centerElement))
                        {
                            if (centerElement.TryGetProperty("lng", out var cLng) && 
                                centerElement.TryGetProperty("lat", out var cLat))
                            {
                                coordinates.Add(new MapCoordinate 
                                { 
                                    Longitude = cLng.GetDouble(), 
                                    Latitude = cLat.GetDouble() 
                                });
                            }
                        }
                        // 半径信息可以存储在Altitude字段中
                        if (geometryElement.TryGetProperty("radius", out var radiusElement) && coordinates.Count > 0)
                        {
                            coordinates[0].Altitude = radiusElement.GetDouble();
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FullScreenExamViewModel 提取坐标信息失败，图形类型: {shapeType}, 错误: {ex.Message}");
            }

            return coordinates;
        }

        /// <summary>
        /// 提取样式信息
        /// </summary>
        /// <param name="styleElement">样式JSON元素</param>
        /// <returns>地图绘制样式</returns>
        private MapDrawingStyle ExtractStyle(JsonElement styleElement)
        {
            var style = new MapDrawingStyle();

            try
            {
                if (styleElement.TryGetProperty("strokeColor", out var strokeColor))
                {
                    style.StrokeColor = strokeColor.GetString();
                }

                if (styleElement.TryGetProperty("fillColor", out var fillColor))
                {
                    style.FillColor = fillColor.GetString();
                }

                if (styleElement.TryGetProperty("strokeWeight", out var strokeWeight))
                {
                    style.StrokeWidth = strokeWeight.GetInt32();
                }

                if (styleElement.TryGetProperty("strokeOpacity", out var strokeOpacity))
                {
                    style.Opacity = strokeOpacity.GetDouble();
                }

                if (styleElement.TryGetProperty("fillOpacity", out var fillOpacity))
                {
                    style.IsFilled = fillOpacity.GetDouble() > 0;
                }

                // 兼容 marker 图标样式
                if (styleElement.TryGetProperty("iconUrl", out var iconUrl))
                {
                    style.IconUrl = iconUrl.GetString();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FullScreenExamViewModel 提取样式信息失败: {ex.Message}");
            }

            return style;
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

        // 地图绘制题答案
        private string _mapDrawingAnswer = string.Empty;
        public string MapDrawingAnswer
        {
            get => _mapDrawingAnswer;
            set
            {
                _mapDrawingAnswer = value;
                OnPropertyChanged();
            }
        }

        // 地图绘制题配置JSON
        private string _mapDrawingConfigJson = string.Empty;
        public string MapDrawingConfigJson
        {
            get => _mapDrawingConfigJson;
            set
            {
                _mapDrawingConfigJson = value;
                OnPropertyChanged();
            }
        }

        // 地图绘制题绘制时长（秒）
        private int _drawDurationSeconds;
        public int DrawDurationSeconds
        {
            get => _drawDurationSeconds;
            set
            {
                _drawDurationSeconds = value;
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