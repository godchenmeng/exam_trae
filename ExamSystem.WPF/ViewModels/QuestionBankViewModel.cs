using ExamSystem.Domain.Entities;
using ExamSystem.Services.Interfaces;
using ExamSystem.WPF.Commands;
using ExamSystem.WPF.ViewModels.Base;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace ExamSystem.WPF.ViewModels;

/// <summary>
/// 题库管理视图模型
/// </summary>
public class QuestionBankViewModel : BaseViewModel
{
    private readonly IQuestionBankService _questionBankService;
    private readonly IQuestionService _questionService;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<QuestionBankViewModel> _logger;

    #region 属性

    private ObservableCollection<QuestionBank> _questionBanks = new();
    public ObservableCollection<QuestionBank> QuestionBanks
    {
        get => _questionBanks;
        set => SetProperty(ref _questionBanks, value);
    }

    private QuestionBank? _selectedQuestionBank;
    public QuestionBank? SelectedQuestionBank
    {
        get => _selectedQuestionBank;
        set
        {
            if (SetProperty(ref _selectedQuestionBank, value))
            {
                OnPropertyChanged(nameof(IsQuestionBankSelected));
                OnPropertyChanged(nameof(CanEditQuestionBank));
                OnPropertyChanged(nameof(CanDeleteQuestionBank));
                LoadQuestionsCommand?.Execute(null);
            }
        }
    }

    private ObservableCollection<Question> _questions = new();
    public ObservableCollection<Question> Questions
    {
        get => _questions;
        set => SetProperty(ref _questions, value);
    }

    private Question? _selectedQuestion;
    public Question? SelectedQuestion
    {
        get => _selectedQuestion;
        set
        {
            if (SetProperty(ref _selectedQuestion, value))
            {
                OnPropertyChanged(nameof(IsQuestionSelected));
                OnPropertyChanged(nameof(CanEditQuestion));
                OnPropertyChanged(nameof(CanDeleteQuestion));
            }
        }
    }

    private string _searchKeyword = string.Empty;
    public string SearchKeyword
    {
        get => _searchKeyword;
        set => SetProperty(ref _searchKeyword, value);
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    // UI状态属性
    public bool IsQuestionBankSelected => SelectedQuestionBank != null;
    public bool IsQuestionSelected => SelectedQuestion != null;

    // 权限相关属性
    public bool CanCreateQuestionBank => _permissionService.HasPermission(CurrentUser?.Role ?? UserRole.Student, "CreateQuestionBank");
    public bool CanEditQuestionBank => IsQuestionBankSelected && _permissionService.HasPermission(CurrentUser?.Role ?? UserRole.Student, "UpdateQuestionBank");
    public bool CanDeleteQuestionBank => IsQuestionBankSelected && _permissionService.HasPermission(CurrentUser?.Role ?? UserRole.Student, "DeleteQuestionBank");
    public bool CanCreateQuestion => IsQuestionBankSelected && _permissionService.HasPermission(CurrentUser?.Role ?? UserRole.Student, "CreateQuestion");
    public bool CanEditQuestion => IsQuestionSelected && _permissionService.HasPermission(CurrentUser?.Role ?? UserRole.Student, "UpdateQuestion");
    public bool CanDeleteQuestion => IsQuestionSelected && _permissionService.HasPermission(CurrentUser?.Role ?? UserRole.Student, "DeleteQuestion");

    #endregion

    #region 命令

    public ICommand LoadQuestionBanksCommand { get; }
    public ICommand LoadQuestionsCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand CreateQuestionBankCommand { get; }
    public ICommand EditQuestionBankCommand { get; }
    public ICommand DeleteQuestionBankCommand { get; }
    public ICommand CreateQuestionCommand { get; }
    public ICommand EditQuestionCommand { get; }
    public ICommand DeleteQuestionCommand { get; }
    public ICommand DuplicateQuestionCommand { get; }
    public ICommand RefreshCommand { get; }

    #endregion

    public QuestionBankViewModel(
        IQuestionBankService questionBankService,
        IQuestionService questionService,
        IPermissionService permissionService,
        ILogger<QuestionBankViewModel> logger)
    {
        _questionBankService = questionBankService;
        _questionService = questionService;
        _permissionService = permissionService;
        _logger = logger;

        // 初始化命令
        LoadQuestionBanksCommand = new RelayCommand(async () => await LoadQuestionBanksAsync());
        LoadQuestionsCommand = new RelayCommand(async () => await LoadQuestionsAsync());
        SearchCommand = new RelayCommand(async () => await SearchAsync());
        CreateQuestionBankCommand = new RelayCommand(CreateQuestionBank, () => CanCreateQuestionBank);
        EditQuestionBankCommand = new RelayCommand(EditQuestionBank, () => CanEditQuestionBank);
        DeleteQuestionBankCommand = new RelayCommand(async () => await DeleteQuestionBankAsync(), () => CanDeleteQuestionBank);
        CreateQuestionCommand = new RelayCommand(CreateQuestion, () => CanCreateQuestion);
        EditQuestionCommand = new RelayCommand(EditQuestion, () => CanEditQuestion);
        DeleteQuestionCommand = new RelayCommand(async () => await DeleteQuestionAsync(), () => CanDeleteQuestion);
        DuplicateQuestionCommand = new RelayCommand(async () => await DuplicateQuestionAsync(), () => IsQuestionSelected);
        RefreshCommand = new RelayCommand(async () => await RefreshAsync());

        // 加载数据
        _ = LoadQuestionBanksAsync();
    }

    #region 题库管理方法

    /// <summary>
    /// 加载题库列表
    /// </summary>
    private async Task LoadQuestionBanksAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "正在加载题库列表...";

            var questionBanks = await _questionBankService.GetAllQuestionBanksAsync();
            
            Application.Current.Dispatcher.Invoke(() =>
            {
                QuestionBanks.Clear();
                foreach (var bank in questionBanks)
                {
                    QuestionBanks.Add(bank);
                }
            });

            StatusMessage = $"已加载 {questionBanks.Count} 个题库";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载题库列表失败");
            StatusMessage = "加载题库列表失败";
            MessageBox.Show("加载题库列表失败，请重试。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 加载题目列表
    /// </summary>
    private async Task LoadQuestionsAsync()
    {
        if (SelectedQuestionBank == null) return;

        try
        {
            IsLoading = true;
            StatusMessage = "正在加载题目列表...";

            var questions = await _questionService.GetQuestionsByBankIdAsync(SelectedQuestionBank.BankId);
            
            Application.Current.Dispatcher.Invoke(() =>
            {
                Questions.Clear();
                foreach (var question in questions)
                {
                    Questions.Add(question);
                }
            });

            StatusMessage = $"已加载 {questions.Count} 道题目";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载题目列表失败");
            StatusMessage = "加载题目列表失败";
            MessageBox.Show("加载题目列表失败，请重试。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 搜索
    /// </summary>
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchKeyword))
        {
            await LoadQuestionBanksAsync();
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "正在搜索...";

            var allBanks = await _questionBankService.GetAllQuestionBanksAsync();
            var filteredBanks = allBanks.Where(b => 
                b.Name.Contains(SearchKeyword, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(b.Description) && b.Description.Contains(SearchKeyword, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            Application.Current.Dispatcher.Invoke(() =>
            {
                QuestionBanks.Clear();
                foreach (var bank in filteredBanks)
                {
                    QuestionBanks.Add(bank);
                }
            });

            StatusMessage = $"找到 {filteredBanks.Count} 个匹配的题库";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索失败");
            StatusMessage = "搜索失败";
            MessageBox.Show("搜索失败，请重试。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 创建题库
    /// </summary>
    private async void CreateQuestionBank()
    {
        try
        {
            var editViewModel = new QuestionBankEditViewModel(_questionBankService, _logger);
            editViewModel.SetQuestionBank(null); // 新建模式
            
            var dialog = new Views.QuestionBankEditDialog(editViewModel);
            if (Application.Current.MainWindow != null)
            {
                dialog.Owner = Application.Current.MainWindow;
            }
            
            if (dialog.ShowDialog() == true)
            {
                await LoadQuestionBanksAsync();
                StatusMessage = "题库创建成功";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建题库失败");
            MessageBox.Show("创建题库失败，请重试。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 编辑题库
    /// </summary>
    private async void EditQuestionBank()
    {
        if (SelectedQuestionBank == null) return;

        try
        {
            var editViewModel = new QuestionBankEditViewModel(_questionBankService, _logger);
            editViewModel.SetQuestionBank(SelectedQuestionBank);
            
            var dialog = new Views.QuestionBankEditDialog(editViewModel);
            if (Application.Current.MainWindow != null)
            {
                dialog.Owner = Application.Current.MainWindow;
            }
            
            if (dialog.ShowDialog() == true)
            {
                await LoadQuestionBanksAsync();
                StatusMessage = "题库更新成功";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "编辑题库失败");
            MessageBox.Show("编辑题库失败，请重试。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 删除题库
    /// </summary>
    private async Task DeleteQuestionBankAsync()
    {
        if (SelectedQuestionBank == null) return;

        try
        {
            var result = MessageBox.Show(
                $"确定要删除题库 '{SelectedQuestionBank.Name}' 吗？\n注意：删除题库将同时删除其中的所有题目。",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                IsLoading = true;
                StatusMessage = "正在删除题库...";

                var success = await _questionBankService.DeleteQuestionBankAsync(SelectedQuestionBank.BankId);
                if (success)
                {
                    StatusMessage = "题库删除成功";
                    await LoadQuestionBanksAsync();
                    SelectedQuestionBank = null;
                }
                else
                {
                    StatusMessage = "题库删除失败";
                    MessageBox.Show("删除题库失败，请重试。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除题库失败");
            StatusMessage = "删除题库失败";
            MessageBox.Show("删除题库失败，请重试。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion

    #region 题目管理方法

    /// <summary>
    /// 创建题目
    /// </summary>
    private async void CreateQuestion()
    {
        if (SelectedQuestionBank == null) return;

        try
        {
            var editViewModel = new QuestionEditViewModel(_questionService, _logger);
            editViewModel.SetQuestion(null, SelectedQuestionBank.BankId);
            
            var dialog = new Views.QuestionEditDialog(editViewModel);
            if (Application.Current.MainWindow != null)
            {
                dialog.Owner = Application.Current.MainWindow;
            }
            
            if (dialog.ShowDialog() == true)
            {
                await LoadQuestionsAsync();
                StatusMessage = "题目创建成功";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建题目失败");
            MessageBox.Show("创建题目失败，请重试。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 编辑题目
    /// </summary>
    private async void EditQuestion()
    {
        if (SelectedQuestion == null) return;

        try
        {
            var editViewModel = new QuestionEditViewModel(_questionService, _logger);
            editViewModel.SetQuestion(SelectedQuestion, SelectedQuestion.BankId);
            
            var dialog = new Views.QuestionEditDialog(editViewModel);
            if (Application.Current.MainWindow != null)
            {
                dialog.Owner = Application.Current.MainWindow;
            }
            
            if (dialog.ShowDialog() == true)
            {
                await LoadQuestionsAsync();
                StatusMessage = "题目更新成功";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "编辑题目失败");
            MessageBox.Show("编辑题目失败，请重试。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 删除题目
    /// </summary>
    private async Task DeleteQuestionAsync()
    {
        if (SelectedQuestion == null) return;

        try
        {
            var result = MessageBox.Show(
                $"确定要删除题目 '{SelectedQuestion.Content.Substring(0, Math.Min(50, SelectedQuestion.Content.Length))}...' 吗？",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                IsLoading = true;
                StatusMessage = "正在删除题目...";

                var success = await _questionService.DeleteQuestionAsync(SelectedQuestion.QuestionId);
                if (success)
                {
                    StatusMessage = "题目删除成功";
                    await LoadQuestionsAsync();
                    SelectedQuestion = null;
                }
                else
                {
                    StatusMessage = "题目删除失败";
                    MessageBox.Show("删除题目失败，该题目可能已被试卷引用。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除题目失败");
            StatusMessage = "删除题目失败";
            MessageBox.Show("删除题目失败，请重试。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 复制题目
    /// </summary>
    private async Task DuplicateQuestionAsync()
    {
        if (SelectedQuestion == null || SelectedQuestionBank == null) return;

        try
        {
            IsLoading = true;
            StatusMessage = "正在复制题目...";

            var duplicatedQuestion = await _questionService.DuplicateQuestionAsync(
                SelectedQuestion.QuestionId, 
                SelectedQuestionBank.BankId);

            if (duplicatedQuestion != null)
            {
                StatusMessage = "题目复制成功";
                await LoadQuestionsAsync();
            }
            else
            {
                StatusMessage = "题目复制失败";
                MessageBox.Show("复制题目失败，请重试。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "复制题目失败");
            StatusMessage = "复制题目失败";
            MessageBox.Show("复制题目失败，请重试。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion

    /// <summary>
    /// 刷新数据
    /// </summary>
    private async Task RefreshAsync()
    {
        await LoadQuestionBanksAsync();
        if (SelectedQuestionBank != null)
        {
            await LoadQuestionsAsync();
        }
    }

    /// <summary>
    /// 更新权限相关属性
    /// </summary>
    public void UpdatePermissions()
    {
        OnPropertyChanged(nameof(CanCreateQuestionBank));
        OnPropertyChanged(nameof(CanEditQuestionBank));
        OnPropertyChanged(nameof(CanDeleteQuestionBank));
        OnPropertyChanged(nameof(CanCreateQuestion));
        OnPropertyChanged(nameof(CanEditQuestion));
        OnPropertyChanged(nameof(CanDeleteQuestion));
    }
}