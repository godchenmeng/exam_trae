using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;
using ExamSystem.Services.Interfaces;
using ExamSystem.WPF.Commands;
using ExamSystem.WPF.Views;
using ExamSystem.WPF.ViewModels;
using Microsoft.Extensions.Logging;

namespace ExamSystem.WPF.ViewModels
{
    /// <summary>
    /// 学生考试列表视图模型
    /// </summary>
    public class StudentExamListViewModel : INotifyPropertyChanged
    {
        private readonly IExamPaperService _examPaperService;
        private readonly IExamService _examService;
        private readonly IPermissionService _permissionService;
        private readonly IAuthService _authService; // 注入认证服务
        private readonly ILogger<StudentExamListViewModel> _logger;
        private User? _currentUser;

        private ObservableCollection<StudentExamPaperViewModel> _examPapers = new();
        private ObservableCollection<StudentExamPaperViewModel> _filteredExamPapers = new();
        private string _searchText = string.Empty;
        private Difficulty? _selectedDifficulty;
        private string? _selectedStatus;
        private bool _isLoading = false;
        private bool _isEmpty = false;

        public StudentExamListViewModel(
            IExamPaperService examPaperService,
            IExamService examService,
            IPermissionService permissionService,
            IAuthService authService,
            ILogger<StudentExamListViewModel> logger)
        {
            _examPaperService = examPaperService;
            _examService = examService;
            _permissionService = permissionService;
            _authService = authService;
            _logger = logger;
            
            // 初始化命令
            RefreshCommand = new RelayCommand(async () => await LoadExamPapersAsync());
            StartExamCommand = new RelayCommand<StudentExamPaperViewModel>(async (paper) => await StartExamAsync(paper));

            // 初始化筛选选项
            InitializeFilterOptions();

            // 尝试从认证服务获取当前用户，防止未及时注入导致为空
            _currentUser = _authService.GetCurrentUser();
            if (_currentUser != null)
            {
                _logger.LogInformation("StudentExamListViewModel 在构造时获取到当前用户: {Username} (ID={UserId})", _currentUser.Username, _currentUser.UserId);
            }

            // 加载数据
            _ = LoadExamPapersAsync();
        }

        /// <summary>
        /// 设置当前用户上下文
        /// </summary>
        public void SetCurrentUser(User user)
        {
            _currentUser = user;
            _logger.LogInformation("StudentExamListViewModel 已接收当前用户: {Username} (ID={UserId})", _currentUser.Username, _currentUser.UserId);
        }

        #region 属性

        /// <summary>
        /// 考试试卷集合
        /// </summary>
        public ObservableCollection<StudentExamPaperViewModel> ExamPapers
        {
            get => _examPapers;
            set
            {
                _examPapers = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 筛选后的考试试卷集合
        /// </summary>
        public ObservableCollection<StudentExamPaperViewModel> FilteredExamPapers
        {
            get => _filteredExamPapers;
            set
            {
                _filteredExamPapers = value;
                OnPropertyChanged();
                IsEmpty = !_filteredExamPapers.Any() && !IsLoading;
            }
        }

        /// <summary>
        /// 搜索文本
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                ApplyFilters();
            }
        }

        /// <summary>
        /// 选中的难度
        /// </summary>
        public Difficulty? SelectedDifficulty
        {
            get => _selectedDifficulty;
            set
            {
                _selectedDifficulty = value;
                OnPropertyChanged();
                ApplyFilters();
            }
        }

        /// <summary>
        /// 选中的状态
        /// </summary>
        public string? SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                _selectedStatus = value;
                OnPropertyChanged();
                ApplyFilters();
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
                IsEmpty = !_filteredExamPapers.Any() && !IsLoading;
            }
        }

        /// <summary>
        /// 是否为空
        /// </summary>
        public bool IsEmpty
        {
            get => _isEmpty;
            set
            {
                _isEmpty = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 难度选项
        /// </summary>
        /// <summary>
        /// 难度选项
        /// </summary>
        public List<Difficulty?> DifficultyOptions { get; private set; } = new();

        /// <summary>
        /// 状态选项
        /// </summary>
        public List<string> StatusOptions { get; private set; } = new();

        #endregion

        #region 命令

        /// <summary>
        /// 刷新命令
        /// </summary>
        public ICommand RefreshCommand { get; }

        /// <summary>
        /// 开始考试命令
        /// </summary>
        public ICommand StartExamCommand { get; }

        #endregion

        #region 事件

        /// <summary>
        /// 开始考试事件
        /// </summary>
#pragma warning disable CS0067 // 事件从未使用
        public event EventHandler<ExamStartEventArgs>? ExamStartRequested;
#pragma warning restore CS0067

        #endregion

        #region 私有方法

        /// <summary>
        /// 初始化筛选选项
        /// </summary>
        private void InitializeFilterOptions()
        {
            // 难度选项
            DifficultyOptions.Add(null); // 全部
            foreach (Difficulty difficulty in Enum.GetValues<Difficulty>())
            {
                DifficultyOptions.Add(difficulty);
            }

            // 状态选项
            StatusOptions.Add("全部");
            StatusOptions.Add("未开始");
            StatusOptions.Add("进行中");
            StatusOptions.Add("已结束");
        }

        /// <summary>
        /// 加载考试列表
        /// </summary>
        private async Task LoadExamPapersAsync()
        {
            try
            {
                IsLoading = true;
                _logger.LogInformation("开始加载学生可参加的考试列表，用户ID: {UserId}", _currentUser?.UserId);

                // 获取所有已发布的试卷
                var papers = await _examPaperService.GetPublishedPapersAsync();
                
                // 转换为ViewModel
                var paperViewModels = papers.Select(p => new StudentExamPaperViewModel
                {
                    Id = p.PaperId,
                    Title = p.Name,
                    Description = p.Description,
                    Duration = p.Duration,
                    TotalScore = p.TotalScore,
                    Difficulty = Difficulty.Medium, // TODO: 如果实体包含难度字段，可替换为实际字段映射
                    QuestionCount = p.PaperQuestions?.Count ?? 0,
                    CreatedAt = p.CreatedAt,
                    CreatedBy = p.Creator?.RealName ?? p.Creator?.Username ?? "未知",
                    IsPublished = p.IsPublished
                }).ToList();

                ExamPapers = new ObservableCollection<StudentExamPaperViewModel>(paperViewModels);
                ApplyFilters();

                _logger.LogInformation("成功加载 {Count} 个可参加的考试", paperViewModels.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载考试列表失败");
                // 这里可以显示错误消息给用户
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 应用筛选条件
        /// </summary>
        private void ApplyFilters()
        {
            var filtered = ExamPapers.AsEnumerable();

            // 搜索筛选
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                filtered = filtered.Where(p => 
                    p.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    (p.Description?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            // 难度筛选
            if (SelectedDifficulty.HasValue)
            {
                filtered = filtered.Where(p => p.Difficulty == SelectedDifficulty.Value);
            }

            // 状态筛选
            if (!string.IsNullOrEmpty(SelectedStatus) && SelectedStatus != "全部")
            {
                // 这里可以根据实际需求实现状态筛选逻辑
                // 例如根据考试时间判断状态
            }

            FilteredExamPapers = new ObservableCollection<StudentExamPaperViewModel>(filtered);
        }

        /// <summary>
        /// 开始考试
        /// </summary>
        private async Task StartExamAsync(StudentExamPaperViewModel? paper)
        {
            if (paper == null) return;

            try
            {
                // 兜底：如果未注入到当前 ViewModel，则从认证服务再取一次
                if (_currentUser == null)
                {
                    _currentUser = _authService.GetCurrentUser();
                }

                if (_currentUser == null)
                {
                    _logger.LogWarning("未获取到当前用户，无法开始考试。请先登录。");
                    MessageBox.Show("未获取到当前用户，无法开始考试。请重新登录或稍后再试。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                _logger.LogInformation("学生 {UserId} 请求开始考试 {PaperId}", _currentUser.UserId, paper.Id);

                // 检查权限
                if (!_permissionService.HasPermission(_currentUser.Role, "TakeExam"))
                {
                    _logger.LogWarning("用户 {UserId} 没有参加考试的权限", _currentUser.UserId);
                    MessageBox.Show("您没有参加考试的权限，请联系管理员。", "权限不足", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 验证用户是否可以参加考试
                var validationResult = await _examService.ValidateUserExamEligibilityAsync(_currentUser.UserId, paper.Id);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("用户 {UserId} 无法参加考试 {PaperId}，原因: {ErrorMessage}", 
                        _currentUser.UserId, paper.Id, validationResult.ErrorMessage);
                    MessageBox.Show(validationResult.ErrorMessage, "无法开始考试", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 创建考试确认对话框
                var confirmationDialog = new Views.ExamConfirmationDialog();
                var confirmationViewModel = new ExamConfirmationViewModel
                {
                    PaperTitle = paper.Title,
                    Duration = paper.Duration,
                    TotalScore = paper.TotalScore,
                    QuestionCount = paper.QuestionCount,
                    Description = paper.Description ?? "暂无考试说明"
                };
                
                confirmationDialog.DataContext = confirmationViewModel;
                // 设置对话框 Owner，仅当主窗口已显示才设置，避免 InvalidOperationException
                var app = System.Windows.Application.Current;
                if (app?.MainWindow is System.Windows.Window mainWin && mainWin.IsVisible)
                {
                    confirmationDialog.Owner = mainWin;
                }
                
                // 显示确认对话框
                var result = confirmationDialog.ShowDialog();
                
                if (result == true && confirmationDialog.IsConfirmed)
                {
                    // 用户确认开始考试，启动全屏考试窗口
                    var fullScreenExamWindow = new FullScreenExamWindow();
                    fullScreenExamWindow.SetExamData(paper.Id, paper.Title);
                    // 仅当主窗口已显示才设置 Owner
                    if (app?.MainWindow is System.Windows.Window mainWindow && mainWindow.IsVisible)
                    {
                        fullScreenExamWindow.Owner = mainWindow;
                    }
                    fullScreenExamWindow.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "开始考试失败，试卷ID: {PaperId}", paper?.Id);
                MessageBox.Show("开始考试时发生错误，请稍后再试。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
    }

    /// <summary>
    /// 学生考试试卷视图模型
    /// </summary>
    public class StudentExamPaperViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Duration { get; set; }
        public decimal TotalScore { get; set; }
        public Difficulty Difficulty { get; set; }
        public int QuestionCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public bool IsPublished { get; set; }
    }

    /// <summary>
    /// 考试开始事件参数
    /// </summary>
    public class ExamStartEventArgs : EventArgs
    {
        public int PaperId { get; set; }
        public string PaperTitle { get; set; } = string.Empty;
        public int Duration { get; set; }
        public decimal TotalScore { get; set; }
    }
}