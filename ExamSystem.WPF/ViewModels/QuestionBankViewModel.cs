using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;
using ExamSystem.Services.Interfaces;
using ExamSystem.WPF.Commands;
using ExamSystem.WPF.ViewModels.Base;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
    private readonly IExcelImportService _excelImportService;
    private readonly IExcelExportService _excelExportService;
    private readonly ILogger<QuestionBankViewModel> _logger;
    private readonly ILoggerFactory _loggerFactory;

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
                OnPropertyChanged(nameof(CanImportQuestions));
                OnPropertyChanged(nameof(CanExportQuestions));
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

    // 当前用户属性
    private User? _currentUser;
    public User? CurrentUser
    {
        get => _currentUser;
        set => SetProperty(ref _currentUser, value);
    }

    // UI状态属性
    public bool IsQuestionBankSelected => SelectedQuestionBank != null;
    public bool IsQuestionSelected => SelectedQuestion != null;

    // 权限相关属性
    public bool CanCreateQuestionBank 
    { 
        get 
        { 
            // 取消权限检测，直接返回true
            return true;
        } 
    }
    public bool CanEditQuestionBank => IsQuestionBankSelected;
    public bool CanDeleteQuestionBank => IsQuestionBankSelected;
    public bool CanCreateQuestion 
    { 
        get 
        { 
            // 始终允许创建题目，不进行权限和题库选择检查
            return true;
        } 
    }
    public bool CanEditQuestion => IsQuestionSelected;
    public bool CanDeleteQuestion => IsQuestionSelected && CurrentUser != null && _permissionService.HasPermission(CurrentUser.Role, "DeleteQuestion");
    public bool CanImportQuestions => IsQuestionBankSelected;
    public bool CanExportQuestions => IsQuestionBankSelected && Questions.Any();

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
    public ICommand DownloadTemplateCommand { get; }
    public ICommand ImportQuestionsCommand { get; }
    public ICommand ExportQuestionsCommand { get; }

    #endregion

    public QuestionBankViewModel(
        IQuestionBankService questionBankService,
        IQuestionService questionService,
        IPermissionService permissionService,
        IExcelImportService excelImportService,
        IExcelExportService excelExportService,
        ILogger<QuestionBankViewModel> logger,
        ILoggerFactory loggerFactory)
    {
        _questionBankService = questionBankService;
        _questionService = questionService;
        _permissionService = permissionService;
        _excelImportService = excelImportService;
        _excelExportService = excelExportService;
        _logger = logger;
        _loggerFactory = loggerFactory;

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
        DownloadTemplateCommand = new RelayCommand(async () => await DownloadTemplateAsync());
        ImportQuestionsCommand = new RelayCommand(async () => await ImportQuestionsAsync(), () => CanImportQuestions);
        ExportQuestionsCommand = new RelayCommand(async () => await ExportQuestionsAsync(), () => CanExportQuestions);

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
            var editViewModel = new QuestionBankEditViewModel(_questionBankService, _loggerFactory.CreateLogger<QuestionBankEditViewModel>());
            if (CurrentUser != null)
            {
                editViewModel.SetCurrentUser(CurrentUser); // 使用SetCurrentUser方法（避免传入 null）
            }
            editViewModel.SetQuestionBank(null!); // 新建模式
            
            var dialog = new Views.QuestionBankEditDialog(editViewModel);
            
            // 安全设置Owner属性
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow != null && mainWindow.IsLoaded && mainWindow != dialog)
            {
                dialog.Owner = mainWindow;
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
            var editViewModel = new QuestionBankEditViewModel(_questionBankService, _loggerFactory.CreateLogger<QuestionBankEditViewModel>());
            if (CurrentUser != null)
            {
                editViewModel.SetCurrentUser(CurrentUser); // 使用SetCurrentUser方法（避免传入 null）
            }
             editViewModel.SetQuestionBank(SelectedQuestionBank);
             
             var dialog = new Views.QuestionBankEditDialog(editViewModel);
            
            // 安全设置Owner属性
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow != null && mainWindow.IsLoaded && mainWindow != dialog)
            {
                dialog.Owner = mainWindow;
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
        // 如果没有选择题库，提示用户先选择题库
        if (SelectedQuestionBank == null)
        {
            MessageBox.Show("请先选择一个题库，然后再创建题目。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var editViewModel = new QuestionEditViewModel(_questionService, _loggerFactory.CreateLogger<QuestionEditViewModel>());
            editViewModel.SetQuestion(null, SelectedQuestionBank.BankId);
            
            var dialog = new Views.QuestionEditDialog(editViewModel);
            
            // 安全设置Owner属性
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow != null && mainWindow.IsLoaded && mainWindow != dialog)
            {
                dialog.Owner = mainWindow;
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
            var editViewModel = new QuestionEditViewModel(_questionService, _loggerFactory.CreateLogger<QuestionEditViewModel>());
            editViewModel.SetQuestion(SelectedQuestion, SelectedQuestion.BankId);
            
            var dialog = new Views.QuestionEditDialog(editViewModel);
            
            // 安全设置Owner属性
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow != null && mainWindow.IsLoaded && mainWindow != dialog)
            {
                dialog.Owner = mainWindow;
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
    /// 设置当前用户
    /// </summary>
    /// <param name="user">当前登录用户</param>
    public void SetCurrentUser(User user)
    {
        _logger.LogInformation($"=== QuestionBankViewModel.SetCurrentUser ===");
        _logger.LogInformation($"接收到的用户: {user?.Username} ({user?.Role})");
        _logger.LogInformation($"用户ID: {user?.UserId}");
        _logger.LogInformation($"用户对象是否为null: {user == null}");
        
        CurrentUser = user;
        
        _logger.LogInformation($"设置后的CurrentUser: {CurrentUser?.Username} ({CurrentUser?.Role})");
        _logger.LogInformation($"CurrentUser是否为null: {CurrentUser == null}");
        
        UpdatePermissions();
        _logger.LogInformation($"QuestionBankViewModel: 用户信息设置完成");
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
        OnPropertyChanged(nameof(CanImportQuestions));
        OnPropertyChanged(nameof(CanExportQuestions));
    }

    #endregion

    #region 导入导出功能

    /// <summary>
    /// 下载题目导入模板
    /// </summary>
    private async Task DownloadTemplateAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "正在生成模板文件...";

            // 打开文件保存对话框
            var saveFileDialog = new SaveFileDialog
            {
                Title = "保存题目导入模板",
                Filter = "Excel文件 (*.xlsx)|*.xlsx",
                FileName = "题目导入模板.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                // 生成Excel模板
                var templateBytes = _excelExportService.ExportQuestionTemplate();
                
                // 保存模板文件
                await File.WriteAllBytesAsync(saveFileDialog.FileName, templateBytes);
                
                StatusMessage = "模板文件下载成功";
                MessageBox.Show("模板文件下载成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "下载模板文件时发生错误");
            StatusMessage = "下载模板文件失败";
            MessageBox.Show($"下载模板文件失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 导入题目
    /// </summary>
    private async Task ImportQuestionsAsync()
    {
        if (SelectedQuestionBank == null)
        {
            MessageBox.Show("请先选择题库", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            _logger.LogInformation("开始导入题目流程，选中题库: {BankName} (ID: {BankId})", 
                SelectedQuestionBank.Name, SelectedQuestionBank.BankId);

            // 打开文件选择对话框
            var openFileDialog = new OpenFileDialog
            {
                Title = "选择要导入的Excel文件",
                Filter = "Excel文件 (*.xlsx)|*.xlsx|所有文件 (*.*)|*.*",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _logger.LogInformation("用户选择了文件: {FileName}", openFileDialog.FileName);
                
                IsLoading = true;
                StatusMessage = "正在导入题目...";

                // 使用Excel导入服务导入题目
                using var fileStream = File.OpenRead(openFileDialog.FileName);
                _logger.LogInformation("开始调用Excel导入服务");
                
                var importResult = await _excelImportService.ImportQuestionsFromExcelAsync(fileStream, SelectedQuestionBank.BankId);
                
                _logger.LogInformation("Excel导入服务返回结果: 总数={TotalCount}, 成功={SuccessCount}, 失败={FailureCount}", 
                    importResult.TotalCount, importResult.SuccessCount, importResult.FailureCount);

                StatusMessage = $"导入完成：成功 {importResult.SuccessCount} 题，失败 {importResult.FailureCount} 题";
                
                // 刷新题目列表
                await LoadQuestionsAsync();

                // 显示导入结果报告
                _logger.LogInformation("准备显示导入结果对话框");
                ShowImportResultDialog(importResult);
            }
            else
            {
                _logger.LogInformation("用户取消了文件选择");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导入题目时发生异常");
            MessageBox.Show($"导入题目失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 显示导入结果对话框
    /// </summary>
    private void ShowImportResultDialog(ExamSystem.Services.Models.ImportResult importResult)
    {
        try
        {
            _logger.LogInformation("开始创建导入结果对话框，结果: 总数={TotalCount}, 成功={SuccessCount}, 失败={FailureCount}", 
                importResult.TotalCount, importResult.SuccessCount, importResult.FailureCount);

            // 转换为视图模型
            var viewModel = new ImportResultViewModel
            {
                TotalCount = importResult.TotalCount,
                SuccessCount = importResult.SuccessCount,
                FailureCount = importResult.FailureCount,
                SuccessfulQuestions = importResult.SuccessfulQuestions?.Select(q => new ImportedQuestionInfo
                {
                    RowNumber = q.RowNumber,
                    Title = q.Title,
                    QuestionType = q.QuestionType,
                    Difficulty = q.Difficulty,
                    Score = q.Score,
                    Tags = q.Tags
                }).ToList() ?? new List<ImportedQuestionInfo>(),
                FailedQuestions = importResult.FailedQuestions?.Select(f => new ImportFailureInfo
                {
                    RowNumber = f.RowNumber,
                    Title = f.Title,
                    ErrorMessage = f.ErrorMessage,
                    RawData = f.RawData
                }).ToList() ?? new List<ImportFailureInfo>()
            };

            _logger.LogInformation("创建ImportResultViewModel完成，成功题目数: {SuccessfulCount}, 失败题目数: {FailedCount}", 
                viewModel.SuccessfulQuestions.Count, viewModel.FailedQuestions.Count);

            // 显示对话框
            var dialog = new Views.ImportResultDialog(viewModel);
            
            _logger.LogInformation("创建ImportResultDialog完成，准备显示对话框");
            
            // 安全设置Owner属性
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow != null && mainWindow.IsLoaded && mainWindow != dialog)
            {
                dialog.Owner = mainWindow;
                _logger.LogInformation("设置对话框Owner为主窗口");
            }
            else
            {
                _logger.LogWarning("无法设置对话框Owner，主窗口状态: MainWindow={MainWindow}, IsLoaded={IsLoaded}", 
                    mainWindow != null, mainWindow?.IsLoaded);
            }
            
            _logger.LogInformation("准备显示导入结果对话框");
            dialog.ShowDialog();
            _logger.LogInformation("导入结果对话框已关闭");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "显示导入结果对话框时发生异常");
            MessageBox.Show($"显示导入结果时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 导出题目
    /// </summary>
    private async Task ExportQuestionsAsync()
    {
        if (SelectedQuestionBank == null)
        {
            MessageBox.Show("请先选择题库", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!Questions.Any())
        {
            MessageBox.Show("当前题库没有题目可导出", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            // 打开文件保存对话框
            var saveFileDialog = new SaveFileDialog
            {
                Title = "导出题目到Excel文件",
                Filter = "Excel文件 (*.xlsx)|*.xlsx",
                FileName = $"{SelectedQuestionBank.Name}_题目列表_{DateTime.Now:yyyyMMdd}.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                IsLoading = true;
                StatusMessage = "正在导出题目...";

                // 使用Excel导出服务导出题目
                var exportBytes = await _excelExportService.ExportQuestionsToExcelAsync(Questions.ToList(), SelectedQuestionBank.Name);
                await File.WriteAllBytesAsync(saveFileDialog.FileName, exportBytes);

                StatusMessage = $"成功导出 {Questions.Count} 道题目";
                MessageBox.Show($"成功导出 {Questions.Count} 道题目到文件：\n{saveFileDialog.FileName}", 
                    "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出题目时发生错误");
            StatusMessage = "导出题目失败";
            MessageBox.Show($"导出题目失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion
}