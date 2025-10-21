using ExamSystem.Domain.Entities;
using ExamSystem.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using ExamSystem.WPF.Commands;
using ExamSystem.WPF.ViewModels.Base;
using ExamSystem.WPF.Views;

namespace ExamSystem.WPF.ViewModels;

/// <summary>
/// 试卷管理ViewModel
/// </summary>
public class ExamPaperViewModel : BaseViewModel
{
    private readonly IExamPaperService _examPaperService;
    private readonly IQuestionBankService _questionBankService;
    private readonly ILogger<ExamPaperViewModel> _logger;
    private readonly IQuestionService _questionService;
    private readonly ILoggerFactory _loggerFactory;
    // 新增：认证服务，用于获取当前登录用户
    private readonly IAuthService _authService;
    // 新增：当前用户缓存（优先使用MainWindow传入的用户）
    private User? _currentUser;

    #region 属性

    private ObservableCollection<ExamPaper> _examPapers = new();
    public ObservableCollection<ExamPaper> ExamPapers
    {
        get => _examPapers;
        set => SetProperty(ref _examPapers, value);
    }

    private ICollectionView? _examPapersView;
    public ICollectionView? ExamPapersView
    {
        get => _examPapersView;
        set => SetProperty(ref _examPapersView, value);
    }

    private ExamPaper? _selectedExamPaper;
    public ExamPaper? SelectedExamPaper
    {
        get => _selectedExamPaper;
        set
        {
            if (SetProperty(ref _selectedExamPaper, value))
            {
                OnPropertyChanged(nameof(IsExamPaperSelected));
                OnPropertyChanged(nameof(CanEditExamPaper));
                OnPropertyChanged(nameof(CanDeleteExamPaper));
                OnPropertyChanged(nameof(CanPublishExamPaper));
                OnPropertyChanged(nameof(CanUnpublishExamPaper));
                
                if (value != null)
                {
                    _ = LoadPaperQuestionsAsync();
                }
                else
                {
                    PaperQuestions.Clear();
                }
            }
        }
    }

    private ObservableCollection<PaperQuestion> _paperQuestions = new();
    public ObservableCollection<PaperQuestion> PaperQuestions
    {
        get => _paperQuestions;
        set => SetProperty(ref _paperQuestions, value);
    }

    private string _searchKeyword = string.Empty;
    public string SearchKeyword
    {
        get => _searchKeyword;
        set
        {
            if (SetProperty(ref _searchKeyword, value))
            {
                _ = SearchAsync();
            }
        }
    }

    private string _statusFilter = "全部";
    public string StatusFilter
    {
        get => _statusFilter;
        set
        {
            if (SetProperty(ref _statusFilter, value))
            {
                ApplyFilter();
            }
        }
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    // 计算属性
    public bool IsExamPaperSelected => SelectedExamPaper != null;
    public bool CanEditExamPaper => SelectedExamPaper != null;
    public bool CanDeleteExamPaper => SelectedExamPaper != null && SelectedExamPaper.Status == "草稿";
    public bool CanPublishExamPaper => SelectedExamPaper != null && SelectedExamPaper.Status == "草稿" && PaperQuestions.Any();
    public bool CanUnpublishExamPaper => SelectedExamPaper != null && SelectedExamPaper.Status == "已发布";

    public List<string> StatusOptions { get; } = new() { "全部", "草稿", "已发布", "已结束" };

    #endregion

    #region 命令

    public ICommand LoadExamPapersCommand { get; }
    public ICommand CreateExamPaperCommand { get; }
    public ICommand EditExamPaperCommand { get; }
    public ICommand DeleteExamPaperCommand { get; }
    public ICommand CopyExamPaperCommand { get; }
    public ICommand PublishExamPaperCommand { get; }
    public ICommand UnpublishExamPaperCommand { get; }
    public ICommand ManageQuestionsCommand { get; }
    public ICommand PreviewExamPaperCommand { get; }
    public ICommand RefreshCommand { get; }

    #endregion

    public ExamPaperViewModel(
        IExamPaperService examPaperService,
        IQuestionBankService questionBankService,
        IQuestionService questionService,
        ILogger<ExamPaperViewModel> logger,
        ILoggerFactory loggerFactory,
        // 新增：注入认证服务
        IAuthService authService)
    {
        _examPaperService = examPaperService;
        _questionBankService = questionBankService;
        _logger = logger;
        _questionService = questionService;
        _loggerFactory = loggerFactory;
        _authService = authService;

        // 初始化命令
        LoadExamPapersCommand = new RelayCommand(async () => await LoadExamPapersAsync());
        CreateExamPaperCommand = new RelayCommand(CreateExamPaper);
        EditExamPaperCommand = new RelayCommand(EditExamPaper, () => CanEditExamPaper);
        DeleteExamPaperCommand = new RelayCommand(async () => await DeleteExamPaperAsync(), () => CanDeleteExamPaper);
        CopyExamPaperCommand = new RelayCommand(CopyExamPaper, () => IsExamPaperSelected);
        PublishExamPaperCommand = new RelayCommand(async () => await PublishExamPaperAsync(), () => CanPublishExamPaper);
        UnpublishExamPaperCommand = new RelayCommand(async () => await UnpublishExamPaperAsync(), () => CanUnpublishExamPaper);
        ManageQuestionsCommand = new RelayCommand(ManageQuestions, () => IsExamPaperSelected);
        PreviewExamPaperCommand = new RelayCommand(PreviewExamPaper, () => IsExamPaperSelected);
        RefreshCommand = new RelayCommand(async () => await LoadExamPapersAsync());

        // 初始化数据
        _ = LoadExamPapersAsync();
    }

    #region 公共方法

    /// <summary>
    /// 由主窗口传入当前登录用户上下文
    /// </summary>
    public void SetCurrentUser(User user)
    {
        _currentUser = user;
        _logger.LogInformation("ExamPaperViewModel 已接收当前登录用户: {Username} (Id={UserId})", user.Username, user.UserId);
    }

    #endregion

    #region 方法

    /// <summary>
    /// 加载试卷列表
    /// </summary>
    private async Task LoadExamPapersAsync()
    {
        try
        {
            IsLoading = true;
            var examPapers = await _examPaperService.GetAllExamPapersAsync();
            
            ExamPapers.Clear();
            foreach (var paper in examPapers)
            {
                ExamPapers.Add(paper);
            }

            // 设置集合视图
            ExamPapersView = CollectionViewSource.GetDefaultView(ExamPapers);
            ApplyFilter();

            _logger.LogInformation("加载试卷列表成功，共 {Count} 个试卷", examPapers.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载试卷列表时发生错误");
            MessageBox.Show("加载试卷列表失败，请重试。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 加载试卷题目
    /// </summary>
    private async Task LoadPaperQuestionsAsync()
    {
        if (SelectedExamPaper == null) return;

        try
        {
            var questions = await _examPaperService.GetPaperQuestionsAsync(SelectedExamPaper.PaperId);
            
            PaperQuestions.Clear();
            foreach (var question in questions)
            {
                PaperQuestions.Add(question);
            }

            // 更新相关属性
            OnPropertyChanged(nameof(CanPublishExamPaper));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载试卷题目时发生错误");
        }
    }

    /// <summary>
    /// 搜索试卷
    /// </summary>
    private async Task SearchAsync()
    {
        try
        {
            IsLoading = true;
            
            var keyword = string.IsNullOrWhiteSpace(SearchKeyword) ? null : SearchKeyword.Trim();
            var status = StatusFilter == "全部" ? null : StatusFilter;
            
            var examPapers = await _examPaperService.SearchExamPapersAsync(keyword, null, status);
            
            ExamPapers.Clear();
            foreach (var paper in examPapers)
            {
                ExamPapers.Add(paper);
            }

            ExamPapersView = CollectionViewSource.GetDefaultView(ExamPapers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索试卷时发生错误");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 应用筛选
    /// </summary>
    private void ApplyFilter()
    {
        if (ExamPapersView == null) return;

        ExamPapersView.Filter = item =>
        {
            if (item is not ExamPaper paper) return false;

            // 状态筛选
            if (StatusFilter != "全部" && paper.Status != StatusFilter)
                return false;

            return true;
        };

        ExamPapersView.Refresh();
    }

    /// <summary>
    /// 创建试卷
    /// </summary>
    private async void CreateExamPaper()
    {
        try
        {
            var viewModel = new ExamPaperEditViewModel(_examPaperService);
            // 优先使用MainWindow传入的当前用户；若不可用，再尝试AuthService；最后兜底为1
            var currentUserId = _currentUser?.UserId
                                ?? _authService.GetCurrentUser()?.UserId
                                ?? 1;
            viewModel.InitializeForCreate(currentUserId);
            
            var dialog = new Views.ExamPaperEditDialog(viewModel);
            var result = dialog.ShowDialog();
            
            if (result == true)
            {
                // 重新加载试卷列表
                await LoadExamPapersAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建试卷时发生错误");
            MessageBox.Show("创建试卷时发生错误，请重试。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 编辑试卷
    /// </summary>
    private async void EditExamPaper()
    {
        if (SelectedExamPaper == null) return;

        try
        {
            var viewModel = new ExamPaperEditViewModel(_examPaperService);
            viewModel.InitializeForEdit(SelectedExamPaper);
            
            var dialog = new Views.ExamPaperEditDialog(viewModel);
            var result = dialog.ShowDialog();
            
            if (result == true)
            {
                // 重新加载试卷列表
                await LoadExamPapersAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "编辑试卷时发生错误");
            MessageBox.Show("编辑试卷时发生错误，请重试。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 删除试卷
    /// </summary>
    private async Task DeleteExamPaperAsync()
    {
        if (SelectedExamPaper == null) return;

        try
        {
            var result = MessageBox.Show(
                $"确定要删除试卷 '{SelectedExamPaper.Name}' 吗？\n删除后无法恢复。",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                IsLoading = true;
                var success = await _examPaperService.DeleteExamPaperAsync(SelectedExamPaper.PaperId);

                if (success)
                {
                    ExamPapers.Remove(SelectedExamPaper);
                    SelectedExamPaper = null;
                    MessageBox.Show("删除试卷成功。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    _logger.LogInformation("删除试卷成功: {PaperName}", SelectedExamPaper?.Name);
                }
                else
                {
                    MessageBox.Show("删除试卷失败，请重试。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除试卷时发生错误");
            MessageBox.Show("删除试卷时发生错误，请重试。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 复制试卷
    /// </summary>
    private void CopyExamPaper()
    {
        if (SelectedExamPaper == null) return;

        // TODO: 实现复制试卷功能
        MessageBox.Show($"复制试卷功能开发中...\n试卷: {SelectedExamPaper.Name}", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    /// <summary>
    /// 发布试卷
    /// </summary>
    private async Task PublishExamPaperAsync()
    {
        if (SelectedExamPaper == null) return;

        try
        {
            var result = MessageBox.Show(
                $"确定要发布试卷 '{SelectedExamPaper.Name}' 吗？\n发布后学生可以参加考试。",
                "确认发布",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                IsLoading = true;
                var success = await _examPaperService.PublishExamPaperAsync(SelectedExamPaper.PaperId);

                if (success)
                {
                    SelectedExamPaper.Status = "已发布";
                    OnPropertyChanged(nameof(CanPublishExamPaper));
                    OnPropertyChanged(nameof(CanUnpublishExamPaper));
                    OnPropertyChanged(nameof(CanDeleteExamPaper));
                    
                    MessageBox.Show("发布试卷成功。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    _logger.LogInformation("发布试卷成功: {PaperName}", SelectedExamPaper.Name);
                }
                else
                {
                    MessageBox.Show("发布试卷失败，请重试。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发布试卷时发生错误");
            MessageBox.Show("发布试卷时发生错误，请重试。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 取消发布试卷
    /// </summary>
    private async Task UnpublishExamPaperAsync()
    {
        if (SelectedExamPaper == null) return;

        try
        {
            var result = MessageBox.Show(
                $"确定要取消发布试卷 '{SelectedExamPaper.Name}' 吗？\n取消发布后学生无法参加考试。",
                "确认取消发布",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                IsLoading = true;
                var success = await _examPaperService.UnpublishExamPaperAsync(SelectedExamPaper.PaperId);

                if (success)
                {
                    SelectedExamPaper.Status = "草稿";
                    OnPropertyChanged(nameof(CanPublishExamPaper));
                    OnPropertyChanged(nameof(CanUnpublishExamPaper));
                    OnPropertyChanged(nameof(CanDeleteExamPaper));
                    
                    MessageBox.Show("取消发布试卷成功。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    _logger.LogInformation("取消发布试卷成功: {PaperName}", SelectedExamPaper.Name);
                }
                else
                {
                    MessageBox.Show("取消发布试卷失败，请重试。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取消发布试卷时发生错误");
            MessageBox.Show("取消发布试卷时发生错误，请重试。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 管理试卷题目
    /// </summary>
    private async void ManageQuestions()
    {
        if (SelectedExamPaper == null)
            return;

        try
        {
            var viewModel = new PaperQuestionManageViewModel(
                _examPaperService,
                _questionBankService,
                _questionService,
                _loggerFactory.CreateLogger<PaperQuestionManageViewModel>()
            );
            var dialog = new PaperQuestionManageDialog(viewModel);
            
            await viewModel.InitializeAsync(SelectedExamPaper.PaperId);
            
            if (dialog.ShowDialog() == true)
            {
                // 重新加载试卷列表以更新题目数量和总分
                await LoadExamPapersAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "打开题目管理对话框失败");
            MessageBox.Show($"打开题目管理失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 预览试卷
    /// </summary>
    private async void PreviewExamPaper()
    {
        if (SelectedExamPaper == null)
            return;

        try
        {
            var viewModel = new ExamPreviewViewModel(
                _examPaperService,
                _questionService,
                _loggerFactory.CreateLogger<ExamPreviewViewModel>()
            );
            var dialog = new ExamPreviewDialog(viewModel);
            
            await viewModel.InitializeAsync(SelectedExamPaper.PaperId);
            dialog.ShowDialog();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "打开试卷预览失败");
            MessageBox.Show($"打开试卷预览失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion
}