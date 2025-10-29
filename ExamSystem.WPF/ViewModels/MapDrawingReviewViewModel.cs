using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Wpf;
using ExamSystem.Services.Interfaces;

namespace ExamSystem.WPF.ViewModels
{
    /// <summary>
    /// 地图绘制题教师阅卷页面ViewModel
    /// </summary>
    public class MapDrawingReviewViewModel : INotifyPropertyChanged
    {
        #region 字段

        private readonly ILogger<MapDrawingReviewViewModel> _logger;
        private readonly IExamService _examService;
        private readonly IQuestionService _questionService;

        private string _questionTitle = "";
        private string _studentName = "";
        private string _studentId = "";
        private DateTime _submitTime = DateTime.Now;
        private bool _isReferenceMapLoading = true;
        private bool _isStudentMapLoading = true;
        private bool _isComparisonMapLoading = true;
        private bool _isSideBySideMode = true;
        private bool _isOverlayMode = false;
        private bool _isSwitchMode = false;
        private bool _showReferenceLayer = true;
        private bool _showStudentLayer = true;
        private bool _showGuideLayer = false;
        private int _referenceShapeCount = 0;
        private int _studentShapeCount = 0;
        private double _matchPercentage = 0.0;
        private DateTime _reviewTime = DateTime.Now;
        private string _reviewStatus = "待评分";
        private DateTime _lastSaveTime = DateTime.Now;
        private string _comments = "";
        private double _totalScore = 0;
        private double _maxTotalScore = 100;

        #endregion

        #region 属性

        /// <summary>
        /// 题目标题
        /// </summary>
        public string QuestionTitle
        {
            get => _questionTitle;
            set => SetProperty(ref _questionTitle, value);
        }

        /// <summary>
        /// 学生姓名
        /// </summary>
        public string StudentName
        {
            get => _studentName;
            set => SetProperty(ref _studentName, value);
        }

        /// <summary>
        /// 学生学号
        /// </summary>
        public string StudentId
        {
            get => _studentId;
            set => SetProperty(ref _studentId, value);
        }

        /// <summary>
        /// 提交时间
        /// </summary>
        public DateTime SubmitTime
        {
            get => _submitTime;
            set => SetProperty(ref _submitTime, value);
        }

        /// <summary>
        /// 参考答案地图是否正在加载
        /// </summary>
        public bool IsReferenceMapLoading
        {
            get => _isReferenceMapLoading;
            set => SetProperty(ref _isReferenceMapLoading, value);
        }

        /// <summary>
        /// 学生答案地图是否正在加载
        /// </summary>
        public bool IsStudentMapLoading
        {
            get => _isStudentMapLoading;
            set => SetProperty(ref _isStudentMapLoading, value);
        }

        /// <summary>
        /// 对比地图是否正在加载
        /// </summary>
        public bool IsComparisonMapLoading
        {
            get => _isComparisonMapLoading;
            set => SetProperty(ref _isComparisonMapLoading, value);
        }

        /// <summary>
        /// 是否为并排模式
        /// </summary>
        public bool IsSideBySideMode
        {
            get => _isSideBySideMode;
            set
            {
                if (SetProperty(ref _isSideBySideMode, value) && value)
                {
                    IsOverlayMode = false;
                    IsSwitchMode = false;
                    OnPropertyChanged(nameof(SideBySideVisibility));
                    OnPropertyChanged(nameof(OverlayVisibility));
                }
            }
        }

        /// <summary>
        /// 是否为叠加模式
        /// </summary>
        public bool IsOverlayMode
        {
            get => _isOverlayMode;
            set
            {
                if (SetProperty(ref _isOverlayMode, value) && value)
                {
                    IsSideBySideMode = false;
                    IsSwitchMode = false;
                    OnPropertyChanged(nameof(SideBySideVisibility));
                    OnPropertyChanged(nameof(OverlayVisibility));
                }
            }
        }

        /// <summary>
        /// 是否为切换模式
        /// </summary>
        public bool IsSwitchMode
        {
            get => _isSwitchMode;
            set
            {
                if (SetProperty(ref _isSwitchMode, value) && value)
                {
                    IsSideBySideMode = false;
                    IsOverlayMode = false;
                    OnPropertyChanged(nameof(SideBySideVisibility));
                    OnPropertyChanged(nameof(OverlayVisibility));
                }
            }
        }

        /// <summary>
        /// 并排模式可见性
        /// </summary>
        public Visibility SideBySideVisibility => IsSideBySideMode ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// 叠加模式可见性
        /// </summary>
        public Visibility OverlayVisibility => (IsOverlayMode || IsSwitchMode) ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// 是否显示参考图层
        /// </summary>
        public bool ShowReferenceLayer
        {
            get => _showReferenceLayer;
            set => SetProperty(ref _showReferenceLayer, value);
        }

        /// <summary>
        /// 是否显示学生图层
        /// </summary>
        public bool ShowStudentLayer
        {
            get => _showStudentLayer;
            set => SetProperty(ref _showStudentLayer, value);
        }

        /// <summary>
        /// 是否显示指引图层
        /// </summary>
        public bool ShowGuideLayer
        {
            get => _showGuideLayer;
            set => SetProperty(ref _showGuideLayer, value);
        }

        /// <summary>
        /// 参考答案图形数量
        /// </summary>
        public int ReferenceShapeCount
        {
            get => _referenceShapeCount;
            set => SetProperty(ref _referenceShapeCount, value);
        }

        /// <summary>
        /// 学生答案图形数量
        /// </summary>
        public int StudentShapeCount
        {
            get => _studentShapeCount;
            set => SetProperty(ref _studentShapeCount, value);
        }

        /// <summary>
        /// 匹配度百分比
        /// </summary>
        public double MatchPercentage
        {
            get => _matchPercentage;
            set => SetProperty(ref _matchPercentage, value);
        }

        /// <summary>
        /// 评分时间
        /// </summary>
        public DateTime ReviewTime
        {
            get => _reviewTime;
            set => SetProperty(ref _reviewTime, value);
        }

        /// <summary>
        /// 评分状态
        /// </summary>
        public string ReviewStatus
        {
            get => _reviewStatus;
            set => SetProperty(ref _reviewStatus, value);
        }

        /// <summary>
        /// 最后保存时间
        /// </summary>
        public DateTime LastSaveTime
        {
            get => _lastSaveTime;
            set => SetProperty(ref _lastSaveTime, value);
        }

        /// <summary>
        /// 评语
        /// </summary>
        public string Comments
        {
            get => _comments;
            set => SetProperty(ref _comments, value);
        }

        /// <summary>
        /// 总分
        /// </summary>
        public double TotalScore
        {
            get => _totalScore;
            set => SetProperty(ref _totalScore, value);
        }

        /// <summary>
        /// 最大总分
        /// </summary>
        public double MaxTotalScore
        {
            get => _maxTotalScore;
            set => SetProperty(ref _maxTotalScore, value);
        }

        /// <summary>
        /// 评分标准集合
        /// </summary>
        public ObservableCollection<ReviewRubricItem> ReviewRubric { get; } = new();

        /// <summary>
        /// 评分项目集合
        /// </summary>
        public ObservableCollection<ScoreItem> ScoreItems { get; } = new();

        #endregion

        #region 命令

        public ICommand ResetViewCommand { get; }
        public ICommand ToggleViewModeCommand { get; }
        public ICommand SaveScoreCommand { get; }
        public ICommand PreviousAnswerCommand { get; }
        public ICommand NextAnswerCommand { get; }
        public ICommand SaveAndContinueCommand { get; }
        public ICommand CompleteReviewCommand { get; }

        #endregion

        #region 构造函数

        public MapDrawingReviewViewModel(
            ILogger<MapDrawingReviewViewModel> logger,
            IExamService examService,
            IQuestionService questionService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _examService = examService ?? throw new ArgumentNullException(nameof(examService));
            _questionService = questionService ?? throw new ArgumentNullException(nameof(questionService));

            // 初始化命令
            ResetViewCommand = new RelayCommand(ResetView);
            ToggleViewModeCommand = new RelayCommand(ToggleViewMode);
            SaveScoreCommand = new RelayCommand(async () => await SaveScoreAsync());
            PreviousAnswerCommand = new RelayCommand(async () => await LoadPreviousAnswerAsync());
            NextAnswerCommand = new RelayCommand(async () => await LoadNextAnswerAsync());
            SaveAndContinueCommand = new RelayCommand(async () => await SaveAndContinueAsync());
            CompleteReviewCommand = new RelayCommand(async () => await CompleteReviewAsync());

            // 初始化数据
            InitializeData();
        }

        #endregion

        #region 方法

        /// <summary>
        /// 初始化数据
        /// </summary>
        private void InitializeData()
        {
            // 初始化评分标准
            ReviewRubric.Add(new ReviewRubricItem
            {
                Criterion = "位置准确性",
                Description = "绘制的地理要素位置是否准确",
                MaxScore = 30
            });
            ReviewRubric.Add(new ReviewRubricItem
            {
                Criterion = "形状完整性",
                Description = "绘制的图形是否完整、规范",
                MaxScore = 25
            });
            ReviewRubric.Add(new ReviewRubricItem
            {
                Criterion = "数量正确性",
                Description = "绘制的要素数量是否正确",
                MaxScore = 25
            });
            ReviewRubric.Add(new ReviewRubricItem
            {
                Criterion = "细节准确性",
                Description = "细节处理是否准确",
                MaxScore = 20
            });

            // 初始化评分项目
            foreach (var rubric in ReviewRubric)
            {
                ScoreItems.Add(new ScoreItem
                {
                    CriterionName = rubric.Criterion,
                    MaxScore = rubric.MaxScore,
                    Score = 0
                });
            }

            // 监听评分变化
            foreach (var item in ScoreItems)
            {
                item.PropertyChanged += OnScoreItemChanged;
            }
        }

        /// <summary>
        /// 评分项目变化事件处理
        /// </summary>
        private void OnScoreItemChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ScoreItem.Score))
            {
                UpdateTotalScore();
            }
        }

        /// <summary>
        /// 更新总分
        /// </summary>
        private void UpdateTotalScore()
        {
            TotalScore = ScoreItems.Sum(item => item.Score);
        }

        /// <summary>
        /// 重置视图
        /// </summary>
        private void ResetView()
        {
            try
            {
                // 重置视图模式
                IsSideBySideMode = true;
                ShowReferenceLayer = true;
                ShowStudentLayer = true;
                ShowGuideLayer = false;

                _logger.LogInformation("视图已重置");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重置视图失败");
            }
        }

        /// <summary>
        /// 切换视图模式
        /// </summary>
        private void ToggleViewMode()
        {
            try
            {
                if (IsSideBySideMode)
                {
                    IsOverlayMode = true;
                }
                else if (IsOverlayMode)
                {
                    IsSwitchMode = true;
                }
                else
                {
                    IsSideBySideMode = true;
                }

                _logger.LogInformation("视图模式已切换");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "切换视图模式失败");
            }
        }

        /// <summary>
        /// 保存评分
        /// </summary>
        private Task SaveScoreAsync()
        {
            try
            {
                // 实现保存评分逻辑
                // await _examService.SaveReviewScoreAsync(...);

                LastSaveTime = DateTime.Now;
                ReviewStatus = "已评分";

                _logger.LogInformation("评分已保存");
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存评分失败");
                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// 加载上一份答案
        /// </summary>
        private async Task LoadPreviousAnswerAsync()
        {
            try
            {
                // 实现加载上一份答案逻辑
                await Task.Delay(100);
                _logger.LogInformation("已加载上一份答案");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载上一份答案失败");
            }
        }

        /// <summary>
        /// 加载下一份答案
        /// </summary>
        private async Task LoadNextAnswerAsync()
        {
            try
            {
                // 实现加载下一份答案逻辑
                await Task.Delay(100);
                _logger.LogInformation("已加载下一份答案");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载下一份答案失败");
            }
        }

        /// <summary>
        /// 保存并继续
        /// </summary>
        private async Task SaveAndContinueAsync()
        {
            try
            {
                await SaveScoreAsync();
                await LoadNextAnswerAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存并继续失败");
            }
        }

        /// <summary>
        /// 完成评分
        /// </summary>
        private async Task CompleteReviewAsync()
        {
            try
            {
                await SaveScoreAsync();
                ReviewStatus = "评分完成";
                _logger.LogInformation("评分已完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "完成评分失败");
            }
        }

        /// <summary>
        /// 初始化参考答案地图
        /// </summary>
        public Task InitializeReferenceMapAsync(WebView2 webView)
        {
            try
            {
                var htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Map", "review.html");
                if (File.Exists(htmlPath))
                {
                    webView.Source = new Uri(htmlPath);
                }
                else
                {
                    _logger.LogWarning("地图评分HTML文件不存在: {HtmlPath}", htmlPath);
                }
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化参考答案地图失败");
                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// 初始化学生答案地图
        /// </summary>
        public Task InitializeStudentMapAsync(WebView2 webView)
        {
            try
            {
                var htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Map", "review.html");
                if (File.Exists(htmlPath))
                {
                    webView.Source = new Uri(htmlPath);
                }
                else
                {
                    _logger.LogWarning("地图评分HTML文件不存在: {HtmlPath}", htmlPath);
                }
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化学生答案地图失败");
                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// 初始化对比地图
        /// </summary>
        public Task InitializeComparisonMapAsync(WebView2 webView)
        {
            try
            {
                var htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Map", "review.html");
                if (File.Exists(htmlPath))
                {
                    webView.Source = new Uri(htmlPath);
                }
                else
                {
                    _logger.LogWarning("地图评分HTML文件不存在: {HtmlPath}", htmlPath);
                }
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化对比地图失败");
                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// 处理参考答案地图消息
        /// </summary>
        public void HandleReferenceMapMessage(string message)
        {
            try
            {
                var messageObj = JsonSerializer.Deserialize<WebViewMessage>(message);
                
                switch (messageObj?.Type)
                {
                    case "mapReady":
                        IsReferenceMapLoading = false;
                        break;
                        
                    case "shapeCountChanged":
                        if (messageObj.Data?.ContainsKey("count") == true && 
                            int.TryParse(messageObj.Data["count"].ToString(), out int count))
                        {
                            ReferenceShapeCount = count;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理参考答案地图消息失败: {Message}", message);
            }
        }

        /// <summary>
        /// 处理学生答案地图消息
        /// </summary>
        public void HandleStudentMapMessage(string message)
        {
            try
            {
                var messageObj = JsonSerializer.Deserialize<WebViewMessage>(message);
                
                switch (messageObj?.Type)
                {
                    case "mapReady":
                        IsStudentMapLoading = false;
                        break;
                        
                    case "shapeCountChanged":
                        if (messageObj.Data?.ContainsKey("count") == true && 
                            int.TryParse(messageObj.Data["count"].ToString(), out int count))
                        {
                            StudentShapeCount = count;
                            CalculateMatchPercentage();
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理学生答案地图消息失败: {Message}", message);
            }
        }

        /// <summary>
        /// 处理对比地图消息
        /// </summary>
        public void HandleComparisonMapMessage(string message)
        {
            try
            {
                var messageObj = JsonSerializer.Deserialize<WebViewMessage>(message);
                
                switch (messageObj?.Type)
                {
                    case "mapReady":
                        IsComparisonMapLoading = false;
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理对比地图消息失败: {Message}", message);
            }
        }

        /// <summary>
        /// 计算匹配度
        /// </summary>
        private void CalculateMatchPercentage()
        {
            if (ReferenceShapeCount == 0)
            {
                MatchPercentage = 0;
                return;
            }

            // 简单的匹配度计算，实际应该基于位置、形状等因素
            var ratio = Math.Min(StudentShapeCount, ReferenceShapeCount) / (double)ReferenceShapeCount;
            MatchPercentage = Math.Round(ratio * 100, 1);
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

    /// <summary>
    /// 评分标准项目
    /// </summary>
    public class ReviewRubricItem
    {
        public string Criterion { get; set; } = "";
        public string Description { get; set; } = "";
        public double MaxScore { get; set; }
    }

    /// <summary>
    /// 评分项目
    /// </summary>
    public class ScoreItem : INotifyPropertyChanged
    {
        private double _score;

        public string CriterionName { get; set; } = "";
        public double MaxScore { get; set; }

        public double Score
        {
            get => _score;
            set
            {
                if (Math.Abs(_score - value) > 0.01)
                {
                    _score = Math.Max(0, Math.Min(value, MaxScore));
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}