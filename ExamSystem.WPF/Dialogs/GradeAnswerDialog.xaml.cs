using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;
using ExamSystem.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Data;
using ExamSystem.WPF.Commands;
using System.Net;
using System.Threading;
using System.Diagnostics;
using System.Text.Json;

namespace ExamSystem.WPF.Dialogs
{
    public partial class GradeAnswerDialog : Window
    {
        private readonly IExamService _examService;
        private readonly int _recordId;
        private readonly int _graderUserId;

        // HTTP服务器相关字段
        private HttpListener? _httpServer;
        private Thread? _serverThread;
        private int _serverPort = 8080;
        private bool _isServerReady = false;
        private readonly object _serverLock = new object();

        public GradeAnswerDialogViewModel ViewModel { get; }

        public GradeAnswerDialog(int recordId, int graderUserId)
        {
            InitializeComponent();

            _recordId = recordId;
            _graderUserId = graderUserId;
            _examService = ((App)Application.Current).Services!.GetRequiredService<IExamService>();

            ViewModel = new GradeAnswerDialogViewModel(_examService, _recordId, _graderUserId, this);
            DataContext = ViewModel;

            // WebView2 初始化事件
            MapWebView.CoreWebView2InitializationCompleted += MapWebView_CoreWebView2InitializationCompleted;

            // 窗口关闭事件
            this.Closing += GradeAnswerDialog_Closing;

            // 窗口加载完成后启动服务器和加载数据
            Loaded += async (_, __) => 
            {
                await InitializeAsync();
            };
        }

        /// <summary>
        /// 异步初始化流程
        /// </summary>
        private async Task InitializeAsync()
        {
            try
            {
                // 1. 启动HTTP服务器
                await StartEmbeddedHttpServerAsync();
                
                // 2. 加载考试数据
                await ViewModel.LoadAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GradeAnswerDialog 初始化失败: {ex.Message}");
                MessageBox.Show($"初始化失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 异步启动内置HTTP服务器
        /// </summary>
        private async Task StartEmbeddedHttpServerAsync()
        {
            await Task.Run(() =>
            {
                lock (_serverLock)
                {
                    if (_isServerReady) return;

                    try
                    {
                        Debug.WriteLine("GradeAnswerDialog 开始启动内置HTTP服务器...");

                        // 查找可用端口
                        for (int port = 8080; port <= 8090; port++)
                        {
                            try
                            {
                                Debug.WriteLine($"GradeAnswerDialog 尝试使用端口: {port}");
                                
                                _httpServer = new HttpListener();
                                _httpServer.Prefixes.Add($"http://localhost:{port}/");
                                _httpServer.Start();
                                _serverPort = port;
                                
                                Debug.WriteLine($"GradeAnswerDialog HTTP服务器启动成功，端口: {port}");
                                break;
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"GradeAnswerDialog 端口 {port} 不可用: {ex.Message}");
                                _httpServer?.Close();
                                _httpServer = null;
                                continue;
                            }
                        }

                        if (_httpServer == null)
                        {
                            throw new InvalidOperationException("无法找到可用端口启动HTTP服务器");
                        }

                        // 启动监听线程
                        _serverThread = new Thread(() =>
                        {
                            Debug.WriteLine("GradeAnswerDialog HTTP服务器监听线程已启动");
                            
                            while (_httpServer != null && _httpServer.IsListening)
                            {
                                try
                                {
                                    var context = _httpServer.GetContext();
                                    ThreadPool.QueueUserWorkItem(HandleRequest, context);
                                }
                                catch (HttpListenerException)
                                {
                                    // 服务器正常关闭时会抛出此异常，忽略
                                    break;
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"GradeAnswerDialog HTTP服务器监听异常: {ex.Message}");
                                    break;
                                }
                            }
                            
                            Debug.WriteLine("GradeAnswerDialog HTTP服务器监听线程已退出");
                        })
                        {
                            IsBackground = true
                        };
                        
                        _serverThread.Start();
                        _isServerReady = true;
                        Debug.WriteLine("GradeAnswerDialog HTTP服务器启动完成");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"GradeAnswerDialog 启动HTTP服务器失败: {ex.Message}");
                        throw;
                    }
                }
            });
        }

        /// <summary>
        /// 检查HTTP服务器是否就绪
        /// </summary>
        public bool IsHttpServerReady()
        {
            lock (_serverLock)
            {
                return _isServerReady && _httpServer != null && _httpServer.IsListening;
            }
        }

        /// <summary>
        /// 获取HTTP服务器端口
        /// </summary>
        public int GetServerPort()
        {
            lock (_serverLock)
            {
                return _serverPort;
            }
        }

        /// <summary>
        /// 等待HTTP服务器就绪
        /// </summary>
        public async Task<bool> WaitForServerReadyAsync(int timeoutMs = 5000)
        {
            var startTime = DateTime.Now;
            while ((DateTime.Now - startTime).TotalMilliseconds < timeoutMs)
            {
                if (IsHttpServerReady())
                {
                    return true;
                }
                await Task.Delay(100);
            }
            return false;
        }

    /// <summary>
    /// 启动内置HTTP服务器
    /// </summary>
    private void StartEmbeddedHttpServer()
    {
        try
        {
            Debug.WriteLine("GradeAnswerDialog 开始启动内置HTTP服务器...");
    
            // 查找可用端口
            for (int port = 8080; port <= 8090; port++)
            {
                try
                {
                    Debug.WriteLine($"GradeAnswerDialog 尝试使用端口: {port}");
                    
                    _httpServer = new HttpListener();
                    _httpServer.Prefixes.Add($"http://localhost:{port}/");
                    _httpServer.Start();
                    _serverPort = port;
                    
                    Debug.WriteLine($"GradeAnswerDialog HTTP服务器启动成功，端口: {port}");
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"GradeAnswerDialog 端口 {port} 不可用: {ex.Message}");
                    _httpServer?.Close();
                    _httpServer = null;
                    continue;
                }
            }
    
            if (_httpServer == null)
            {
                Debug.WriteLine("GradeAnswerDialog HTTP服务器启动失败：无可用端口");
                return;
            }
    
            // 启动监听线程
            _serverThread = new Thread(() =>
            {
                Debug.WriteLine("GradeAnswerDialog HTTP服务器监听线程已启动");
                
                while (_httpServer != null && _httpServer.IsListening)
                {
                    try
                    {
                        var context = _httpServer.GetContext();
                        ThreadPool.QueueUserWorkItem(HandleRequest, context);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"GradeAnswerDialog HTTP服务器监听异常: {ex.Message}");
                        break;
                    }
                }
                
                Debug.WriteLine("GradeAnswerDialog HTTP服务器监听线程已退出");
            })
            {
                IsBackground = true
            };
            
            _serverThread.Start();
            Debug.WriteLine("GradeAnswerDialog HTTP服务器启动完成");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"GradeAnswerDialog 启动HTTP服务器失败: {ex.Message}");
            
            // 尝试使用备用端口重新启动
            try
            {
                _serverPort = 9080;
                _httpServer = new HttpListener();
                _httpServer.Prefixes.Add($"http://localhost:{_serverPort}/");
                _httpServer.Start();
                Debug.WriteLine($"GradeAnswerDialog 使用备用端口 {_serverPort} 启动HTTP服务器成功");
            }
            catch (Exception backupEx)
            {
                Debug.WriteLine($"GradeAnswerDialog 备用端口启动也失败: {backupEx.Message}");
            }
        }
    }

    /// <summary>
    /// 处理HTTP请求
    /// </summary>
    private void HandleRequest(object? state)
    {
        if (state is not HttpListenerContext context) return;

        try
        {
            var request = context.Request;
            var response = context.Response;

            var url = request.Url?.LocalPath ?? "";
            var assetsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "BaiduMap");

            if (url == "/" || url == "/index.html")
            {
                url = "/index.html";
            }

            // 动态图标清单 API：/api/icons
            if (string.Equals(url, "/api/icons", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var iconsRoot = Path.Combine(assetsPath, "icon");
                    var result = new System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, object>>();

                    if (Directory.Exists(iconsRoot))
                    {
                        foreach (var dir in Directory.GetDirectories(iconsRoot))
                        {
                            var categoryName = Path.GetFileName(dir);
                            var icons = new System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, string>>();

                            foreach (var file in Directory.GetFiles(dir))
                            {
                                var ext = Path.GetExtension(file).ToLowerInvariant();
                                if (ext is ".png" or ".jpg" or ".jpeg" or ".svg" or ".gif" or ".webp")
                                {
                                    var fileName = Path.GetFileNameWithoutExtension(file);
                                    var fileBaseName = Path.GetFileName(file);
                                    var relUrl = $"/icon/{categoryName}/{fileBaseName}";
                                    icons.Add(new System.Collections.Generic.Dictionary<string, string>
                                    {
                                        ["name"] = fileName,
                                        ["url"] = relUrl
                                    });
                                }
                            }

                            result.Add(new System.Collections.Generic.Dictionary<string, object>
                            {
                                ["name"] = categoryName,
                                ["icons"] = icons
                            });
                        }
                    }

                    var payload = new System.Collections.Generic.Dictionary<string, object>
                    {
                        ["categories"] = result,
                        ["totalCount"] = result.Sum(c =>
                        {
                            if (c.TryGetValue("icons", out var iconsObj) && iconsObj is System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, string>> list)
                            {
                                return list.Count;
                            }
                            return 0;
                        })
                    };

                    var json = JsonSerializer.Serialize(payload);
                    var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                    response.StatusCode = 200;
                    response.ContentType = "application/json; charset=utf-8";
                    response.ContentLength64 = bytes.Length;
                    response.OutputStream.Write(bytes, 0, bytes.Length);
                    response.OutputStream.Close();
                    return;
                }
                catch (Exception apiEx)
                {
                    Debug.WriteLine($"GradeAnswerDialog /api/icons 生成失败: {apiEx.Message}");
                    var error = System.Text.Encoding.UTF8.GetBytes("{\"error\":\"failed to list icons\"}");
                    response.StatusCode = 500;
                    response.ContentType = "application/json; charset=utf-8";
                    response.ContentLength64 = error.Length;
                    response.OutputStream.Write(error, 0, error.Length);
                    response.OutputStream.Close();
                    return;
                }
            }

            var filePath = Path.Combine(assetsPath, url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

            if (File.Exists(filePath))
            {
                var content = File.ReadAllBytes(filePath);
                var extension = Path.GetExtension(filePath).ToLower();

                response.ContentType = extension switch
                {
                    ".html" => "text/html; charset=utf-8",
                    ".js" => "application/javascript; charset=utf-8",
                    ".css" => "text/css; charset=utf-8",
                    ".json" => "application/json; charset=utf-8",
                    ".png" => "image/png",
                    ".jpg" => "image/jpeg",
                    ".jpeg" => "image/jpeg",
                    ".svg" => "image/svg+xml",
                    ".gif" => "image/gif",
                    ".webp" => "image/webp",
                    _ => "application/octet-stream"
                };

                response.ContentLength64 = content.Length;
                response.OutputStream.Write(content, 0, content.Length);
            }
            else
            {
                response.StatusCode = 404;
                var notFound = System.Text.Encoding.UTF8.GetBytes("File not found");
                response.ContentLength64 = notFound.Length;
                response.OutputStream.Write(notFound, 0, notFound.Length);
            }

            response.OutputStream.Close();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"GradeAnswerDialog 处理HTTP请求失败: {ex.Message}");
        }
    }

    /// <summary>
    /// WebView2 初始化完成事件处理
    /// </summary>
    private async void MapWebView_CoreWebView2InitializationCompleted(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs e)
    {
        try
        {
            if (e.IsSuccess && MapWebView.CoreWebView2 != null)
            {
                Debug.WriteLine("WebView2 初始化成功");
                
                // 在Debug模式下自动开启开发者工具
                #if DEBUG
                Debug.WriteLine("Debug模式：开启开发者工具");
                MapWebView.CoreWebView2.OpenDevToolsWindow();
                Debug.WriteLine("开发者工具窗口已打开");
                #endif
                
                // 可以通过环境变量控制是否开启开发者工具
                var enableDevTools = Environment.GetEnvironmentVariable("WEBVIEW_DEVTOOLS");
                if (!string.IsNullOrEmpty(enableDevTools) && enableDevTools.ToLower() == "true")
                {
                    Debug.WriteLine("环境变量启用：开启开发者工具");
                    MapWebView.CoreWebView2.OpenDevToolsWindow();
                }
    
                // WebView2初始化完成后，如果有选中的地图题目，重新加载地图
                if (ViewModel.SelectedSubjectiveAnswer?.IsMapDrawingQuestion == true)
                {
                    Debug.WriteLine("WebView2初始化完成，重新加载地图数据");
                    await ViewModel.LoadMapDataAsync(ViewModel.SelectedSubjectiveAnswer);
                }
            }
            else
            {
                Debug.WriteLine($"WebView2 初始化失败: {e.InitializationException?.Message}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"WebView2初始化事件处理失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 窗口关闭事件处理
    /// </summary>
    private void GradeAnswerDialog_Closing(object? sender, CancelEventArgs e)
    {
        try
        {
            Debug.WriteLine("GradeAnswerDialog 正在关闭，停止HTTP服务器...");
            
            // 停止HTTP服务器
            _httpServer?.Stop();
            _httpServer?.Close();
            _httpServer = null;
            
            // 等待服务器线程结束
            if (_serverThread != null && _serverThread.IsAlive)
            {
                _serverThread.Join(1000); // 最多等待1秒
            }
            
            Debug.WriteLine("GradeAnswerDialog HTTP服务器已停止");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"GradeAnswerDialog 停止HTTP服务器时发生异常: {ex.Message}");
        }
    }
}

public class GradeAnswerDialogViewModel : INotifyPropertyChanged
 {
     private readonly IExamService _examService;
     private readonly int _recordId;
     private readonly int _graderUserId;
     private readonly GradeAnswerDialog _dialog;

    public ObservableCollection<SubjectiveAnswerItem> SubjectiveAnswers { get; } = new();
    public ICollectionView SubjectiveAnswersView { get; }
    public string ExamPaperName { get; private set; } = string.Empty;
    public string UserName { get; private set; } = string.Empty;
    public int UngradedCount => SubjectiveAnswers.Count(a => !a.IsGraded);

    private bool _showOnlyUngraded;
    public bool ShowOnlyUngraded
    {
        get => _showOnlyUngraded;
        set
        {
            _showOnlyUngraded = value;
            OnPropertyChanged();
            SubjectiveAnswersView.Refresh();
        }
    }

    private bool _isSaving;
    public bool IsSaving
    {
        get => _isSaving;
        private set
        {
            _isSaving = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanSave));
            (SaveAllCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (SaveSelectedCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    public bool CanSave => SubjectiveAnswers.Any(i => i.Score > 0) && !IsSaving;

    private bool _isAutoSave = false; // 默认关闭实时保存
    public bool IsAutoSave
    {
        get => _isAutoSave;
        set
        {
            _isAutoSave = value;
            OnPropertyChanged();
        }
    }

    private SubjectiveAnswerItem? _selectedSubjectiveAnswer;
    public SubjectiveAnswerItem? SelectedSubjectiveAnswer
    {
        get => _selectedSubjectiveAnswer;
        set
        {
            _selectedSubjectiveAnswer = value;
            OnPropertyChanged();
            (SaveSelectedCommand as RelayCommand)?.RaiseCanExecuteChanged();
            
            // 当选择地图绘制题时，加载地图数据
            if (value?.IsMapDrawingQuestion == true)
            {
                // 异步加载地图数据，避免阻塞UI
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await LoadMapDataAsync(value);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"异步加载地图数据失败: {ex.Message}");
                        // 在UI线程上更新错误状态
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            HasMapError = true;
                            MapErrorMessage = $"加载地图失败：{ex.Message}";
                        });
                    }
                });
            }
        }
    }

    // 地图相关属性
    private string? _mapViewerUrl;
    public string? MapViewerUrl
    {
        get => _mapViewerUrl;
        set
        {
            _mapViewerUrl = value;
            OnPropertyChanged();
        }
    }

    private bool _isMapLoaded;
    public bool IsMapLoaded
    {
        get => _isMapLoaded;
        set
        {
            _isMapLoaded = value;
            OnPropertyChanged();
        }
    }

    private bool _hasMapError;
    public bool HasMapError
    {
        get => _hasMapError;
        set
        {
            _hasMapError = value;
            OnPropertyChanged();
        }
    }

    private string? _mapErrorMessage;
    public string? MapErrorMessage
    {
        get => _mapErrorMessage;
        set
        {
            _mapErrorMessage = value;
            OnPropertyChanged();
        }
    }

    public ICommand SaveAllCommand { get; }
    public ICommand AutoGradeCommand { get; }
    public ICommand SaveSelectedCommand { get; }
    public ICommand SetFullScoreCommand { get; }
    public ICommand SetZeroScoreCommand { get; }

    public GradeAnswerDialogViewModel(IExamService examService, int recordId, int graderUserId, GradeAnswerDialog dialog)
    {
        _examService = examService;
        _recordId = recordId;
        _graderUserId = graderUserId;
        _dialog = dialog;

        SaveAllCommand = new RelayCommand(async () => await SaveAllAsync(), () => CanSave);
        AutoGradeCommand = new RelayCommand(async () => await AutoGradeObjectiveAsync());
        SaveSelectedCommand = new RelayCommand(async () =>
        {
            if (SelectedSubjectiveAnswer != null)
            {
                await SaveSingleAsync(SelectedSubjectiveAnswer);
            }
        }, () => SelectedSubjectiveAnswer != null && !IsSaving);
        SetFullScoreCommand = new RelayCommand(() =>
        {
            if (SelectedSubjectiveAnswer != null)
            {
                SelectedSubjectiveAnswer.Score = SelectedSubjectiveAnswer.MaxScore;
            }
        }, () => SelectedSubjectiveAnswer != null);
        SetZeroScoreCommand = new RelayCommand(() =>
        {
            if (SelectedSubjectiveAnswer != null)
            {
                SelectedSubjectiveAnswer.Score = 0;
            }
        }, () => SelectedSubjectiveAnswer != null);

        SubjectiveAnswersView = CollectionViewSource.GetDefaultView(SubjectiveAnswers);
        SubjectiveAnswersView.Filter = o =>
        {
            if (o is not SubjectiveAnswerItem item) return false;
            return !ShowOnlyUngraded || !item.IsGraded;
        };
    }

    public async Task LoadAsync()
    {
        var record = await _examService.GetExamRecordAsync(_recordId);
        if (record == null)
        {
            MessageBox.Show("未找到考试记录。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        ExamPaperName = record.ExamPaper?.Name ?? "";
        UserName = record.User?.RealName ?? record.User?.Username ?? record.UserId.ToString();
        OnPropertyChanged(nameof(ExamPaperName));
        OnPropertyChanged(nameof(UserName));

        // 进入人工评分前，尝试自动批改客观题
        await AutoGradeObjectiveAsync();

        var answers = await _examService.GetAnswerRecordsAsync(_recordId);

        SubjectiveAnswers.Clear();
        foreach (var ar in answers)
        {
            var isObjective = IsObjectiveQuestion(ar.Question?.QuestionType ?? QuestionType.SingleChoice);
            if (isObjective) continue; // 只展示主观题

            var paperQuestion = record.ExamPaper?.PaperQuestions?.FirstOrDefault(pq => pq.QuestionId == ar.QuestionId);
            var maxScore = paperQuestion?.Score ?? 0;

            var item = new SubjectiveAnswerItem
            {
                AnswerRecordId = ar.AnswerId,
                QuestionId = ar.QuestionId,
                QuestionNumber = ar.Question?.QuestionId ?? 0,
                QuestionType = ar.Question?.QuestionType ?? QuestionType.ShortAnswer,
                Content = ar.Question?.Content ?? string.Empty,
                StandardAnswer = ar.Question?.Answer ?? string.Empty,
                UserAnswer = ar.UserAnswer ?? string.Empty,
                MaxScore = maxScore,
                Score = ar.Score,
                Comment = ar.Comment,
                IsGraded = ar.IsGraded,
                GradeTime = ar.GradeTime,
                GraderInfo = ar.GraderId.HasValue ? $"评分人ID：{ar.GraderId}" : null,
                // 地图绘制题数据
                MapCenter = ar.MapCenter,
                MapZoom = ar.MapZoom,
                MapDrawingData = ar.MapDrawingData
            };
            // 异常高亮：得分为空或低于 30% 阈值（未评分也视为异常）
            item.IsAnomaly = !item.IsGraded || (item.Score > 0 && item.Score < maxScore * 0.3m);
            item.PropertyChanged += (_, __) =>
            {
                OnPropertyChanged(nameof(UngradedCount));
                OnPropertyChanged(nameof(CanSave));
                (SaveAllCommand as RelayCommand)?.RaiseCanExecuteChanged();
                SubjectiveAnswersView.Refresh();
            };

            SubjectiveAnswers.Add(item);
        }
        SubjectiveAnswersView.Refresh();

        // 默认选中第一题，便于右侧详情区展示
        SelectedSubjectiveAnswer = SubjectiveAnswers.FirstOrDefault();
    }

    private static bool IsObjectiveQuestion(QuestionType type)
    {
        return type == QuestionType.SingleChoice || type == QuestionType.MultipleChoice || type == QuestionType.TrueFalse;
        // 注意：填空题现在被归类为主观题，需要人工评分
    }

    private async Task AutoGradeObjectiveAsync()
    {
        try
        {
            await _examService.AutoGradeObjectiveQuestionsAsync(_recordId);
        }
        catch
        {
            // 忽略自动批改异常，保持人工评分可继续
        }
    }

    private void SubjectiveItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is SubjectiveAnswerItem item)
        {
            if (e.PropertyName == nameof(SubjectiveAnswerItem.Score) || e.PropertyName == nameof(SubjectiveAnswerItem.Comment))
            {
                UpdateAnomaly(item);
                OnPropertyChanged(nameof(UngradedCount));

                // 取消实时保存功能
                // if (IsAutoSave)
                // {
                //     _ = SaveSingleAsync(item);
                // }
            }
        }
    }

    private void UpdateAnomaly(SubjectiveAnswerItem item)
    {
        var threshold = item.MaxScore * 0.3m;
        item.IsAnomaly = item.Score > 0 && item.Score < threshold;
    }

    private async Task SaveSingleAsync(SubjectiveAnswerItem item)
    {
        if (IsSaving) return;
        IsSaving = true;
        try
        {
            // 基础校验：得分范围
            if (item.Score < 0 || item.Score > item.MaxScore)
            {
                MessageBox.Show($"题目 {item.QuestionNumber} 的得分超出范围 (0 - {item.MaxScore})。", "校验失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await _examService.GradeSubjectiveAnswerAsync(_recordId, item.QuestionId, item.Score, item.Comment ?? string.Empty, _graderUserId);
            item.IsGraded = true;
            item.GradeTime = DateTime.Now;
            if (string.IsNullOrEmpty(item.GraderInfo)) item.GraderInfo = _graderUserId.ToString();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存评分失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsSaving = false;
        }
    }

    private async Task SaveAllAsync()
    {
        try
        {
            IsSaving = true;

            foreach (var item in SubjectiveAnswers)
            {
                // 基础校验：得分范围
                if (item.Score < 0 || item.Score > item.MaxScore)
                {
                    MessageBox.Show($"题目 {item.QuestionNumber} 的得分超出范围 (0 - {item.MaxScore})。", "校验失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    IsSaving = false;
                    return;
                }
            }

            foreach (var item in SubjectiveAnswers)
            {
                await _examService.GradeSubjectiveAnswerAsync(_recordId, item.QuestionId, item.Score, item.Comment ?? string.Empty, _graderUserId);
                item.IsGraded = true;
                item.GradeTime = DateTime.Now;
                if (string.IsNullOrEmpty(item.GraderInfo)) item.GraderInfo = _graderUserId.ToString();
            }

            OnPropertyChanged(nameof(UngradedCount));
            OnPropertyChanged(nameof(CanSave));
            MessageBox.Show("评分保存成功。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存评分失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsSaving = false;
        }
    }

    /// <summary>
    /// 加载地图数据的异步方法
    /// </summary>
    /// <param name="mapQuestion">地图题目</param>
    public async Task LoadMapDataAsync(SubjectiveAnswerItem mapQuestion)
    {
        try
        {
            // 重置状态
            IsMapLoaded = false;
            HasMapError = false;
            MapErrorMessage = string.Empty;
            OnPropertyChanged(nameof(IsMapLoaded));
            OnPropertyChanged(nameof(HasMapError));
            OnPropertyChanged(nameof(MapErrorMessage));
    
            Debug.WriteLine($"开始加载地图数据，题目ID: {mapQuestion.QuestionId}");
    
            // 检查是否有地图绘制数据
            if (string.IsNullOrEmpty(mapQuestion.MapDrawingData))
            {
                throw new ArgumentException("该题目没有地图绘制数据");
            }
    
            // 等待HTTP服务器就绪
            Debug.WriteLine("等待HTTP服务器就绪...");
            if (!await _dialog.WaitForServerReadyAsync())
            {
                throw new InvalidOperationException("HTTP服务器未就绪，无法加载地图");
            }
    
            Debug.WriteLine("HTTP服务器已就绪");
    
            // 检查review.html文件是否存在
            var mapHtmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "BaiduMap", "review.html");
            if (!File.Exists(mapHtmlPath))
            {
                throw new FileNotFoundException($"地图文件不存在：{mapHtmlPath}");
            }
    
            Debug.WriteLine($"地图HTML文件存在: {mapHtmlPath}");
    
            // 解析地图数据
            string? mapDataForUrl = null;
            string? mapCenterForUrl = null;
            int mapZoomForUrl = 10;
    
            try
            {
                Debug.WriteLine("开始解析地图数据...");
                
                // 首先尝试解析新的嵌套结构：{"data": "{...}"}
                var outerJsonDoc = System.Text.Json.JsonDocument.Parse(mapQuestion.MapDrawingData);
                if (outerJsonDoc.RootElement.TryGetProperty("data", out var dataElement))
                {
                    Debug.WriteLine("检测到嵌套地图数据结构");
                    
                    // 解析内层JSON数据
                    var innerDataString = dataElement.GetString();
                    if (!string.IsNullOrEmpty(innerDataString))
                    {
                        var innerJsonDoc = System.Text.Json.JsonDocument.Parse(innerDataString);
                        
                        // 提取center信息
                        if (innerJsonDoc.RootElement.TryGetProperty("center", out var centerElement))
                        {
                            mapCenterForUrl = centerElement.GetRawText();
                            Debug.WriteLine($"提取到地图中心: {mapCenterForUrl}");
                        }
                        
                        // 提取zoom信息
                        if (innerJsonDoc.RootElement.TryGetProperty("zoom", out var zoomElement))
                        {
                            mapZoomForUrl = (int)Math.Round(zoomElement.GetDouble());
                            Debug.WriteLine($"提取到地图缩放: {mapZoomForUrl}");
                        }
    
                        // 提取overlays信息
                        if (innerJsonDoc.RootElement.TryGetProperty("overlays", out var overlaysElement))
                        {
                            mapDataForUrl = overlaysElement.GetRawText();
                            Debug.WriteLine($"提取到覆盖物数据: {mapDataForUrl?.Length ?? 0} 字符");
                        }
                    }
                }
                else
                {
                    Debug.WriteLine("使用向后兼容的地图数据解析");
                    // 向后兼容：直接解析原始数据
                    mapDataForUrl = mapQuestion.MapDrawingData;
                    mapCenterForUrl = mapQuestion.MapCenter;
                    if (mapQuestion.MapZoom.HasValue)
                    {
                        mapZoomForUrl = mapQuestion.MapZoom.Value;
                    }
                }
            }
            catch (Exception parseEx)
            {
                Debug.WriteLine($"解析地图数据失败，使用向后兼容模式: {parseEx.Message}");
                // 向后兼容：使用原始数据
                mapDataForUrl = mapQuestion.MapDrawingData;
                mapCenterForUrl = mapQuestion.MapCenter;
                if (mapQuestion.MapZoom.HasValue)
                {
                    mapZoomForUrl = mapQuestion.MapZoom.Value;
                }
            }
    
            // 构建地图查看器URL，使用内置HTTP服务器（不再使用URL参数传递学生答案）
            var serverPort = _dialog.GetServerPort();
            var baseUrl = $"http://localhost:{serverPort}/review.html";

            MapViewerUrl = baseUrl;
            IsMapLoaded = true;

            Debug.WriteLine($"地图加载成功，URL: {MapViewerUrl}");

            // 通知UI更新
            OnPropertyChanged(nameof(MapViewerUrl));
            OnPropertyChanged(nameof(IsMapLoaded));

            // 页面加载完成后，通过 PostWebMessageAsJson 向前端发送学生答案数据
            void SendMapDataToWebView()
            {
                try
                {
                    // 仅在存在覆盖物数据时发送
                    if (!string.IsNullOrEmpty(mapDataForUrl))
                    {
                        // 先在后台线程准备好要发送的 JSON 负载
                        var dataObj = System.Text.Json.JsonSerializer.Deserialize<object>(mapDataForUrl);

                        object? centerObj = null;
                        try
                        {
                            if (!string.IsNullOrEmpty(mapCenterForUrl))
                            {
                                var trimmed = mapCenterForUrl.Trim();
                                if (trimmed.StartsWith("{"))
                                {
                                    centerObj = System.Text.Json.JsonSerializer.Deserialize<object>(trimmed);
                                }
                                else
                                {
                                    var parts = trimmed.Split(',');
                                    if (parts.Length == 2 &&
                                        double.TryParse(parts[0], out var lng) &&
                                        double.TryParse(parts[1], out var lat))
                                    {
                                        centerObj = new { lng, lat };
                                    }
                                }
                            }
                        }
                        catch { /* ignore center parse errors */ }

                        int? zoomObj = mapZoomForUrl;
                        var payload = new { type = "loadStudentData", data = dataObj, center = centerObj, zoom = zoomObj };
                        var payloadJson = System.Text.Json.JsonSerializer.Serialize(payload);

                        // 在 UI 线程上访问 WebView 控件并发送消息，避免跨线程访问异常
                        _dialog?.MapWebView?.Dispatcher.Invoke(() =>
                        {
                            if (_dialog?.MapWebView?.CoreWebView2 == null)
                            {
                                Debug.WriteLine("WebView2 尚未初始化，无法发送消息");
                                return;
                            }
                            _dialog.MapWebView.CoreWebView2.PostWebMessageAsJson(payloadJson);
                            Debug.WriteLine("已通过 PostWebMessageAsJson 发送学生答案覆盖物数据（包含可选中心与缩放）");
                        });
                    }

                    // 可选：如果需要设置初始视图，可扩展前端支持 setCenter / setZoom 消息
                    // 当前前端未提供对应接口，这里先不发送中心点与缩放级别消息
                }
                catch (Exception msgEx)
                {
                    Debug.WriteLine($"发送学生答案数据失败: {msgEx.Message}");
                }
            }

            // 如果已经有 CoreWebView2，则尝试直接发送；否则等待导航完成后发送
            if (_dialog?.MapWebView != null)
            {
                // 一次性导航完成事件处理器
                void NavigationCompletedHandler(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
                {
                    try
                    {
                        _dialog.MapWebView.NavigationCompleted -= NavigationCompletedHandler;
                        if (e.IsSuccess)
                        {
                            SendMapDataToWebView();
                        }
                        else
                        {
                            Debug.WriteLine("review.html 导航失败，未发送学生答案数据");
                        }
                    }
                    catch (Exception handlerEx)
                    {
                        Debug.WriteLine($"NavigationCompletedHandler 异常: {handlerEx.Message}");
                    }
                }

                // 订阅和访问 WebView 必须在 UI 线程
                _dialog.MapWebView.Dispatcher.Invoke(() =>
                {
                    // 订阅导航完成事件（一次性）
                    _dialog.MapWebView.NavigationCompleted += NavigationCompletedHandler;

                    // 如果 CoreWebView2 已经初始化，且可能已加载完成，尝试直接发送
                    if (_dialog.MapWebView.CoreWebView2 != null)
                    {
                        SendMapDataToWebView();
                    }
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"加载地图数据失败: {ex.Message}");
            Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
            
            HasMapError = true;
            MapErrorMessage = $"加载地图数据失败：{ex.Message}";
            IsMapLoaded = false;
            
            // 通知UI更新错误状态
            OnPropertyChanged(nameof(HasMapError));
            OnPropertyChanged(nameof(MapErrorMessage));
            OnPropertyChanged(nameof(IsMapLoaded));
            
            // 不重新抛出异常，让UI能够显示错误信息
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
}

public class SubjectiveAnswerItem : INotifyPropertyChanged
{
    public int AnswerRecordId { get; set; }
    public int QuestionId { get; set; }
    public int QuestionNumber { get; set; }
    public QuestionType QuestionType { get; set; }
    public string QuestionTypeDisplay => QuestionType switch
    {
        QuestionType.ShortAnswer => "简答题",
        QuestionType.Essay => "论述题",
        QuestionType.FillInBlank => "填空题",
        QuestionType.MapDrawing => "地图绘制题",
        _ => QuestionType.ToString()
    };

    // 地图绘制题相关属性
    public bool IsMapDrawingQuestion => QuestionType == QuestionType.MapDrawing;
    public string? MapCenter { get; set; }
    public int? MapZoom { get; set; }
    public string? MapDrawingData { get; set; }

    public string Content { get; set; } = string.Empty;
    public string StandardAnswer { get; set; } = string.Empty;
    public string UserAnswer { get; set; } = string.Empty;
    public decimal MaxScore { get; set; }

    private decimal _score;
    public decimal Score
    {
        get => _score;
        set
        {
            _score = value;
            OnPropertyChanged();
        }
    }

    private string? _comment;
    public string? Comment
    {
        get => _comment;
        set
        {
            _comment = value;
            OnPropertyChanged();
        }
    }

    private bool _isGraded;
    public bool IsGraded
    {
        get => _isGraded;
        set
        {
            _isGraded = value;
            OnPropertyChanged();
        }
    }

    private bool _isAnomaly;
    public bool IsAnomaly
    {
        get => _isAnomaly;
        set { _isAnomaly = value; OnPropertyChanged(); }
    }

    private DateTime? _gradeTime;
    public DateTime? GradeTime
    {
        get => _gradeTime;
        set { _gradeTime = value; OnPropertyChanged(); }
    }

    private string? _graderInfo;
    public string? GraderInfo
    {
        get => _graderInfo;
        set { _graderInfo = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}