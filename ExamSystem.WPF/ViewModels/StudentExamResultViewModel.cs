using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ExamSystem.Domain.Entities;
using ExamSystem.Services.Interfaces;
using ExamSystem.WPF.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ExamSystem.WPF.ViewModels
{
    /// <summary>
    /// 学生考试结果列表ViewModel
    /// </summary>
    public class StudentExamResultViewModel : INotifyPropertyChanged
    {
        private readonly IExamService _examService;
        private readonly IAuthService _authService;
        private readonly ILogger<StudentExamResultViewModel> _logger;
        
        private bool _isLoading;
        private string _searchKeyword = string.Empty;
        private string _selectedSubject = "全部";
        private string _selectedTimeRange = "全部";
        private User? _currentUser;

        public StudentExamResultViewModel()
        {
            // 从依赖注入容器获取服务
            _examService = ((App)Application.Current).GetServices().GetRequiredService<IExamService>();
            _authService = ((App)Application.Current).GetServices().GetRequiredService<IAuthService>();
            _logger = ((App)Application.Current).GetServices().GetRequiredService<ILogger<StudentExamResultViewModel>>();

            // 初始化集合
            ExamResults = new ObservableCollection<StudentExamResultItemViewModel>();
            Subjects = new ObservableCollection<string> { "全部", "数学", "语文", "英语", "物理", "化学", "生物", "历史", "地理", "政治", "计算机" };
            TimeRanges = new ObservableCollection<string> { "全部", "最近一周", "最近一月", "最近三月", "最近半年", "最近一年" };

            // 获取当前用户
            _currentUser = _authService.GetCurrentUser();

            // 初始化命令
            InitializeCommands();

            // 加载数据
            _ = LoadExamResultsAsync();
        }

        /// <summary>
        /// 设置当前用户上下文
        /// </summary>
        public void SetCurrentUser(User user)
        {
            _currentUser = user;
            _logger.LogInformation("StudentExamResultViewModel 已接收当前用户: {Username} (ID={UserId})", _currentUser.Username, _currentUser.UserId);
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
                OnPropertyChanged(nameof(IsEmpty));
                OnPropertyChanged(nameof(HasResults));
            }
        }

        /// <summary>
        /// 搜索关键词
        /// </summary>
        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                _searchKeyword = value;
                OnPropertyChanged();
                _ = ApplyFiltersAsync();
            }
        }

        /// <summary>
        /// 选中的科目
        /// </summary>
        public string SelectedSubject
        {
            get => _selectedSubject;
            set
            {
                _selectedSubject = value;
                OnPropertyChanged();
                _ = ApplyFiltersAsync();
            }
        }

        /// <summary>
        /// 选中的时间范围
        /// </summary>
        public string SelectedTimeRange
        {
            get => _selectedTimeRange;
            set
            {
                _selectedTimeRange = value;
                OnPropertyChanged();
                _ = ApplyFiltersAsync();
            }
        }

        /// <summary>
        /// 考试结果集合
        /// </summary>
        public ObservableCollection<StudentExamResultItemViewModel> ExamResults { get; }

        /// <summary>
        /// 科目集合
        /// </summary>
        public ObservableCollection<string> Subjects { get; }

        /// <summary>
        /// 时间范围集合
        /// </summary>
        public ObservableCollection<string> TimeRanges { get; }

        /// <summary>
        /// 是否为空
        /// </summary>
        public bool IsEmpty => !IsLoading && ExamResults.Count == 0;

        /// <summary>
        /// 是否有结果
        /// </summary>
        public bool HasResults => !IsLoading && ExamResults.Count > 0;

        #endregion

        #region 命令

        public ICommand RefreshCommand { get; private set; } = null!;
        public ICommand ViewDetailCommand { get; private set; } = null!;
        public ICommand ViewWrongAnswersCommand { get; private set; } = null!;

        #endregion

        #region 事件

        public event EventHandler<ExamResultDetailEventArgs>? ExamResultDetailRequested;

        #endregion

        #region 私有方法

        /// <summary>
        /// 初始化命令
        /// </summary>
        private void InitializeCommands()
        {
            RefreshCommand = new RelayCommand(async () => await LoadExamResultsAsync());
            ViewDetailCommand = new RelayCommand<StudentExamResultItemViewModel>(ViewDetail);
        ViewWrongAnswersCommand = new RelayCommand<StudentExamResultItemViewModel>(ViewWrongAnswers);
        }

        /// <summary>
        /// 加载考试结果
        /// </summary>
        private async Task LoadExamResultsAsync()
        {
            try
            {
                IsLoading = true;
                ExamResults.Clear();

                _logger.LogInformation("开始加载学生考试结果");

                // 获取所有考试结果数据
                var allResults = await GetAllExamResultsAsync();

                foreach (var result in allResults.OrderByDescending(r => r.ExamDate))
                {
                    ExamResults.Add(result);
                }

                _logger.LogInformation($"成功加载 {ExamResults.Count} 条考试结果");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载考试结果失败");
                MessageBox.Show($"加载考试结果失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 应用筛选条件
        /// </summary>
        private async Task ApplyFiltersAsync()
        {
            try
            {
                IsLoading = true;
                
                if (_currentUser == null)
                {
                    _logger.LogWarning("当前用户为空，无法应用筛选条件");
                    ExamResults.Clear();
                    return;
                }
                
                // 从服务获取筛选后的考试结果数据
                var examResults = await _examService.GetStudentExamResultsAsync(
                    _currentUser.UserId, 
                    SearchKeyword, 
                    SelectedSubject == "全部" ? null : SelectedSubject, 
                    SelectedTimeRange == "全部" ? null : SelectedTimeRange);
                
                // 转换为 ViewModel 并应用额外的前端筛选
                var filteredResults = examResults.Select(result => new StudentExamResultItemViewModel
                {
                    Id = result.RecordId,
                    ExamTitle = result.ExamTitle,
                    Subject = result.Subject,
                    ExamDate = result.ExamDate,
                    Duration = result.Duration,
                    Score = result.Score,
                    TotalScore = result.TotalScore,
                    QuestionCount = result.QuestionCount,
                    Status = result.Status,
                    HasWrongAnswers = result.HasWrongAnswers
                }).AsEnumerable();

                // 按关键词筛选
                if (!string.IsNullOrWhiteSpace(SearchKeyword))
                {
                    filteredResults = filteredResults.Where(r => 
                        r.ExamTitle.Contains(SearchKeyword, StringComparison.OrdinalIgnoreCase) ||
                        r.Subject.Contains(SearchKeyword, StringComparison.OrdinalIgnoreCase));
                }

                // 按科目筛选
                if (SelectedSubject != "全部")
                {
                    filteredResults = filteredResults.Where(r => r.Subject == SelectedSubject);
                }

                // 按时间范围筛选
                if (SelectedTimeRange != "全部")
                {
                    var dateRange = GetDateRangeFromSelection(SelectedTimeRange);
                    if (dateRange.HasValue)
                    {
                        filteredResults = filteredResults.Where(r => r.ExamDate >= dateRange.Value);
                    }
                }

                // 更新UI
                ExamResults.Clear();
                foreach (var result in filteredResults.OrderByDescending(r => r.ExamDate))
                {
                    ExamResults.Add(result);
                }

                _logger.LogInformation($"筛选完成 - 关键词: {SearchKeyword}, 科目: {SelectedSubject}, 时间: {SelectedTimeRange}, 结果数量: {ExamResults.Count}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "应用筛选条件失败");
                MessageBox.Show($"筛选失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 获取所有考试结果（从服务获取真实数据）
        /// </summary>
        private async Task<StudentExamResultItemViewModel[]> GetAllExamResultsAsync()
        {
            if (_currentUser == null)
            {
                _logger.LogWarning("当前用户为空，无法获取考试结果");
                return Array.Empty<StudentExamResultItemViewModel>();
            }

            try
            {
                // 从服务获取学生考试结果
                var examResults = await _examService.GetStudentExamResultsAsync(_currentUser.UserId);
                
                // 转换为 ViewModel
                return examResults.Select(result => new StudentExamResultItemViewModel
                {
                    Id = result.RecordId,
                    ExamTitle = result.ExamTitle,
                    Subject = result.Subject,
                    ExamDate = result.ExamDate,
                    Duration = result.Duration,
                    Score = result.Score,
                    TotalScore = result.TotalScore,
                    QuestionCount = result.QuestionCount,
                    Status = result.Status,
                    HasWrongAnswers = result.HasWrongAnswers
                }).ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取考试结果失败，用户ID: {UserId}", _currentUser.UserId);
                throw;
            }
        }

        /// <summary>
        /// 根据时间范围选择获取对应的日期
        /// </summary>
        private DateTime? GetDateRangeFromSelection(string timeRange)
        {
            return timeRange switch
            {
                "最近一周" => DateTime.Now.AddDays(-7),
                "最近一月" => DateTime.Now.AddMonths(-1),
                "最近三月" => DateTime.Now.AddMonths(-3),
                "最近半年" => DateTime.Now.AddMonths(-6),
                "最近一年" => DateTime.Now.AddYears(-1),
                _ => null
            };
        }

        /// <summary>
        /// 查看详情
        /// </summary>
        private void ViewDetail(StudentExamResultItemViewModel? examResult)
        {
            if (examResult == null) return;

            // 触发查看详情事件
            ExamResultDetailRequested?.Invoke(this, new ExamResultDetailEventArgs
            {
                ExamRecordId = examResult.Id,
                ExamTitle = examResult.ExamTitle
            });
        }

        /// <summary>
        /// 查看错题分析
        /// </summary>
        private void ViewWrongAnswers(StudentExamResultItemViewModel? examResult)
        {
            if (examResult == null) return;

            try
            {
                _logger.LogInformation($"查看错题分析 - ID: {examResult.Id}, 标题: {examResult.ExamTitle}");

                // 这里可以打开错题分析窗口或导航到错题分析页面
                MessageBox.Show($"错题分析功能正在开发中\n考试：{examResult.ExamTitle}", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查看错题分析失败");
                MessageBox.Show($"查看错题分析失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// 学生考试结果ViewModel
    /// </summary>
    public class StudentExamResultItemViewModel : INotifyPropertyChanged
    {
        private decimal _score;
        private decimal _totalScore;

        public int Id { get; set; }
        public string ExamTitle { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public DateTime ExamDate { get; set; }
        public string Duration { get; set; } = string.Empty;
        public int QuestionCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool HasWrongAnswers { get; set; }

        public decimal Score
        {
            get => _score;
            set
            {
                _score = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ScorePercentage));
                OnPropertyChanged(nameof(ScoreLevel));
            }
        }

        public decimal TotalScore
        {
            get => _totalScore;
            set
            {
                _totalScore = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ScorePercentage));
                OnPropertyChanged(nameof(ScoreLevel));
            }
        }

        /// <summary>
        /// 分数百分比
        /// </summary>
        public decimal ScorePercentage => TotalScore > 0 ? (Score / TotalScore) * 100 : 0;

        /// <summary>
        /// 分数等级
        /// </summary>
        public string ScoreLevel
        {
            get
            {
                var percentage = ScorePercentage;
                if (percentage >= 90) return "Excellent";
                if (percentage >= 80) return "Good";
                if (percentage >= 60) return "Pass";
                return "Fail";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// 考试结果详情事件参数
    /// </summary>
    public class ExamResultDetailEventArgs : EventArgs
    {
        public int ExamRecordId { get; set; }
        public string ExamTitle { get; set; } = string.Empty;
    }
}