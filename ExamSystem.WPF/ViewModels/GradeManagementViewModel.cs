using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;
using ExamSystem.Services.Interfaces;
using ExamSystem.WPF.Commands;
using Microsoft.Extensions.Logging;

namespace ExamSystem.WPF.ViewModels
{
    /// <summary>
    /// 教师/管理员 成绩管理视图模型
    /// </summary>
    public class GradeManagementViewModel : INotifyPropertyChanged
    {
        private readonly IExamService _examService;
        private readonly IExamPaperService _examPaperService;
        private readonly ILogger<GradeManagementViewModel> _logger;
        private User? _currentUser;

        private bool _isLoading;
        private string _nameKeyword = string.Empty;
        private ObservableCollection<ExamRecordItemViewModel> _records = new();
        private ObservableCollection<ExamRecordItemViewModel> _allRecords = new();

        // 试卷筛选
        private ObservableCollection<PaperOption> _paperOptions = new();
        private int? _selectedPaperId = null; // null 表示“全部”

        // 状态筛选
        private ObservableCollection<StatusOption> _statusOptions = new();
        private ExamStatus? _selectedStatus = null; // null 表示“全部”

        public GradeManagementViewModel(IExamService examService, IExamPaperService examPaperService, ILogger<GradeManagementViewModel> logger)
        {
            _examService = examService;
            _examPaperService = examPaperService;
            _logger = logger;

            RefreshCommand = new RelayCommand(async () => await LoadRecordsAsync());
            ViewCommand = new RelayCommand<ExamRecordItemViewModel>(async (item) => await ViewRecordAsync(item));
            GradeCommand = new RelayCommand<ExamRecordItemViewModel>(async (item) => await GradeRecordAsync(item));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public void SetCurrentUser(User user) => _currentUser = user;

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        // 姓名关键词（模糊查询学生真实姓名）
        public string NameKeyword
        {
            get => _nameKeyword;
            set { _nameKeyword = value; OnPropertyChanged(); ApplyFilters(); }
        }

        // 向后兼容：旧的Keyword属性改为映射到NameKeyword
        public string Keyword
        {
            get => NameKeyword;
            set { NameKeyword = value; }
        }

        public ObservableCollection<ExamRecordItemViewModel> Records
        {
            get => _records;
            set { _records = value; OnPropertyChanged(); }
        }

        // 试卷下拉数据源
        public ObservableCollection<PaperOption> PaperOptions
        {
            get => _paperOptions;
            set { _paperOptions = value; OnPropertyChanged(); }
        }

        // 试卷下拉选中项（SelectedValue 绑定）
        public int? SelectedPaperId
        {
            get => _selectedPaperId;
            set { _selectedPaperId = value; OnPropertyChanged(); ApplyFilters(); }
        }

        // 状态下拉数据源
        public ObservableCollection<StatusOption> StatusOptions
        {
            get => _statusOptions;
            set { _statusOptions = value; OnPropertyChanged(); }
        }

        // 状态下拉选中项
        public ExamStatus? SelectedStatus
        {
            get => _selectedStatus;
            set { _selectedStatus = value; OnPropertyChanged(); ApplyFilters(); }
        }

        public ICommand RefreshCommand { get; }
        public ICommand ViewCommand { get; }
        public ICommand GradeCommand { get; }

        public event EventHandler<int>? ViewRecordRequested;
        public event EventHandler<int>? GradeRecordRequested;

        public async Task LoadRecordsAsync()
        {
            try
            {
                IsLoading = true;

                // 加载考试记录（不带筛选，取全部，再在客户端过滤）
                var list = await _examService.SearchExamRecordsAsync(string.Empty);
                var items = list.Select(er => 
                {
                    var hasUngraded = er.AnswerRecords?.Any(ar => !ar.IsGraded) ?? false;
                    return new ExamRecordItemViewModel
                    {
                        RecordId = er.RecordId,
                        PaperId = er.PaperId,
                        PaperName = er.ExamPaper?.Name ?? string.Empty,
                        // 使用真实姓名显示；若为空则退回为空字符串
                        UserName = er.User?.RealName ?? string.Empty,
                        StartTime = er.StartTime,
                        EndTime = er.EndTime,
                        SubmitTime = er.SubmitTime,
                        Score = er.TotalScore,
                        Status = er.Status,
                        // 根据是否有未评分题目来设置状态显示
                        StatusDisplay = hasUngraded ? "未评分" : "已评分",
                        HasUngraded = hasUngraded
                    };
                }).OrderByDescending(i => i.EndTime ?? i.SubmitTime ?? i.StartTime).ToList();

                _allRecords.Clear();
                foreach (var item in items)
                {
                    _allRecords.Add(item);
                }

                // 加载试卷下拉选项
                var examPapers = await _examPaperService.GetAllExamPapersAsync();
                PaperOptions.Clear();
                PaperOptions.Add(new PaperOption { Id = null, Name = "全部" });
                foreach (var p in examPapers.OrderBy(p => p.Name))
                {
                    PaperOptions.Add(new PaperOption { Id = p.PaperId, Name = p.Name });
                }
                if (SelectedPaperId == null)
                {
                    SelectedPaperId = null; // 保持“全部”
                }

                // 加载状态下拉选项（简化为评分相关状态）
                if (StatusOptions.Count == 0)
                {
                    StatusOptions.Add(new StatusOption { Value = null, Display = "全部" });
                    // 使用自定义状态选项，基于HasUngraded字段筛选
                    StatusOptions.Add(new StatusOption { Value = ExamStatus.Graded, Display = "已评分" });
                    StatusOptions.Add(new StatusOption { Value = ExamStatus.Submitted, Display = "未评分" });
                    SelectedStatus = null;
                }

                // 应用筛选到 Records
                ApplyFilters();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载成绩管理列表失败");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ApplyFilters()
        {
            try
            {
                var query = _allRecords.AsEnumerable();

                // 显示范围：包含 已提交/已完成/已评分 的记录
                query = query.Where(r => r.Status == ExamStatus.Submitted || r.Status == ExamStatus.Completed || r.Status == ExamStatus.Graded);

                if (SelectedPaperId.HasValue)
                {
                    query = query.Where(r => r.PaperId == SelectedPaperId.Value);
                }

                if (SelectedStatus.HasValue)
                {
                    // 下拉筛选：已评分(无未评分题目或状态为已评分) 或 未评分(存在未评分题目)
                    if (SelectedStatus.Value == ExamStatus.Graded)
                    {
                        query = query.Where(r => !r.HasUngraded || r.Status == ExamStatus.Graded);
                    }
                    else if (SelectedStatus.Value == ExamStatus.Submitted)
                    {
                        query = query.Where(r => r.HasUngraded);
                    }
                }

                if (!string.IsNullOrWhiteSpace(NameKeyword))
                {
                    var kw = NameKeyword.Trim();
                    query = query.Where(r => !string.IsNullOrEmpty(r.UserName) && r.UserName.Contains(kw, StringComparison.OrdinalIgnoreCase));
                }

                var filtered = query.ToList();

                Records.Clear();
                foreach (var item in filtered)
                {
                    Records.Add(item);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "应用筛选时发生错误");
            }
        }

        private static string MapStatusToChinese(ExamStatus status)
        {
            return status switch
            {
                ExamStatus.NotStarted => "未开始",
                ExamStatus.InProgress => "进行中",
                ExamStatus.Completed => "已完成",
                ExamStatus.Submitted => "已提交",
                ExamStatus.Graded => "已评分",
                ExamStatus.Timeout => "已超时",
                _ => status.ToString()
            };
        }

        private Task ViewRecordAsync(ExamRecordItemViewModel? item)
        {
            if (item == null) return Task.CompletedTask;
            ViewRecordRequested?.Invoke(this, item.RecordId);
            return Task.CompletedTask;
        }

        private Task GradeRecordAsync(ExamRecordItemViewModel? item)
        {
            if (item == null) return Task.CompletedTask;
            GradeRecordRequested?.Invoke(this, item.RecordId);
            return Task.CompletedTask;
        }
    }

    public class ExamRecordItemViewModel
    {
        public int RecordId { get; set; }
        public int PaperId { get; set; }
        public string PaperName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime? StartTime { get; set; }
        public DateTime? SubmitTime { get; set; }
        public DateTime? EndTime { get; set; }
        public decimal Score { get; set; }
        public ExamStatus Status { get; set; }
        public string StatusDisplay { get; set; } = string.Empty;
        public bool HasUngraded { get; set; }
    }

    // 下拉选项模型
    public class PaperOption
    {
        public int? Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class StatusOption
    {
        public ExamStatus? Value { get; set; }
        public string Display { get; set; } = string.Empty;
    }
}