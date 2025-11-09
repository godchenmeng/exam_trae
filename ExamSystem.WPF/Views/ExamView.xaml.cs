using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using ExamSystem.Domain.Enums;
using ExamSystem.Domain.DTOs;
using ExamSystem.Domain.Entities;
using ExamSystem.WPF.ViewModels;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace ExamSystem.WPF.Views
{
    /// <summary>
    /// ExamView.xaml 的交互逻辑
    /// </summary>
    public partial class ExamView : UserControl
    {
        private ExamViewModel? _viewModel;
        private DispatcherTimer? _autoSaveTimer;
        private const int AUTO_SAVE_INTERVAL_SECONDS = 30;
        
        // HTTP服务器相关字段
        private HttpListener? _httpServer;
        private Thread? _serverThread;
        private int _serverPort = 0;
        private bool _isMapInitialized = false;

        public ExamView()
        {
            InitializeComponent();
            this.DataContextChanged += OnDataContextChanged;
            InitializeAutoSaveTimer();
            StartEmbeddedHttpServer();
        }

        /// <summary>
        /// 初始化自动保存定时器
        /// </summary>
        private void InitializeAutoSaveTimer()
        {
            _autoSaveTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(AUTO_SAVE_INTERVAL_SECONDS)
            };
            _autoSaveTimer.Tick += OnAutoSaveTick;
        }

        /// <summary>
        /// 自动保存定时器触发事件
        /// </summary>
        private void OnAutoSaveTick(object? sender, EventArgs e)
        {
            try
            {
                // 只有在地图绘制题时才执行自动保存
                if (_viewModel?.CurrentQuestion?.QuestionType == QuestionType.MapDrawing && 
                    MapWebView?.CoreWebView2 != null)
                {
                    // 向前端请求当前绘制数据
                    MapWebView.CoreWebView2.PostWebMessageAsString(
                        System.Text.Json.JsonSerializer.Serialize(new { type = "requestCurrentData" }));
                    
                    System.Diagnostics.Debug.WriteLine($"自动保存触发 - {DateTime.Now:HH:mm:ss}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"自动保存失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 启动自动保存定时器
        /// </summary>
        public void StartAutoSave()
        {
            if (_autoSaveTimer != null && !_autoSaveTimer.IsEnabled)
            {
                _autoSaveTimer.Start();
                System.Diagnostics.Debug.WriteLine("自动保存定时器已启动");
            }
        }

        /// <summary>
        /// 停止自动保存定时器
        /// </summary>
        public void StopAutoSave()
        {
            if (_autoSaveTimer != null && _autoSaveTimer.IsEnabled)
            {
                _autoSaveTimer.Stop();
                System.Diagnostics.Debug.WriteLine("自动保存定时器已停止");
            }
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // 取消订阅旧的ViewModel事件
            if (e.OldValue is ExamViewModel oldViewModel)
            {
                oldViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }

            if (e.NewValue is ExamViewModel viewModel)
            {
                _viewModel = viewModel;
                _viewModel.SetExamView(this);
                
                // 订阅ViewModel属性变化事件
                viewModel.PropertyChanged += OnViewModelPropertyChanged;
            }
        }

        /// <summary>
        /// 处理ViewModel属性变化事件
        /// </summary>
        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            Debug.WriteLine($"OnViewModelPropertyChanged: PropertyName={e.PropertyName}, IsMapDrawing={_viewModel?.IsMapDrawing}, CurrentQuestion={_viewModel?.CurrentQuestion?.QuestionType}");
            
            // 当CurrentQuestion变化时，检查是否是地图绘制题
            if (e.PropertyName == nameof(ExamViewModel.CurrentQuestion))
            {
                Debug.WriteLine($"CurrentQuestion变化: {_viewModel?.CurrentQuestion?.QuestionType}");
                
                if (_viewModel?.IsMapDrawing == true)
                {
                    Debug.WriteLine("检测到地图绘制题，开始初始化地图...");
                    
                    // 异步初始化地图，避免阻塞UI
                    _ = Task.Run(async () =>
                    {
                        await Dispatcher.InvokeAsync(async () =>
                        {
                            if (!_isMapInitialized)
                            {
                                Debug.WriteLine("地图未初始化，开始初始化...");
                                await InitializeMapWebViewAsync();
                            }
                            else
                            {
                                Debug.WriteLine("地图已初始化，跳过初始化步骤");
                            }
                        });
                    });
                }
            }
            
            // 也监听IsMapDrawing属性变化（作为备用）
            if (e.PropertyName == nameof(ExamViewModel.IsMapDrawing) && _viewModel?.IsMapDrawing == true)
            {
                Debug.WriteLine("通过IsMapDrawing属性检测到地图绘制题，开始初始化地图...");
                
                // 异步初始化地图，避免阻塞UI
                _ = Task.Run(async () =>
                {
                    await Dispatcher.InvokeAsync(async () =>
                    {
                        if (!_isMapInitialized)
                        {
                            Debug.WriteLine("地图未初始化，开始初始化...");
                            await InitializeMapWebViewAsync();
                        }
                        else
                        {
                            Debug.WriteLine("地图已初始化，跳过初始化步骤");
                        }
                    });
                });
            }
        }

        /// <summary>
        /// 公开MapWebView属性供ViewModel访问
        /// </summary>
        public WebView2 MapWebViewControl => MapWebView;

        public ExamView(ExamViewModel viewModel) : this()
        {
            _viewModel = viewModel;
            DataContext = viewModel;
            
            // 订阅事件
            if (viewModel != null)
            {
                viewModel.ExamCompleted += OnExamCompleted;
                viewModel.ExamTimeout += OnExamTimeout;
            }
            
            // 订阅 Unloaded 事件以清理资源
            Unloaded += OnUnloaded;
        }

        /// <summary>
        /// 地图WebView导航完成事件处理
        /// </summary>
        private async void MapWebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            try
            {
                var webView = sender as WebView2;
                if (webView?.CoreWebView2 != null && e.IsSuccess)
                {
                    // 注册消息接收处理程序
                    webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
                    
                    // 如果当前是地图绘制题，加载题目配置
                    if (_viewModel?.CurrentQuestion?.QuestionType == QuestionType.MapDrawing)
                    {
                        await LoadMapQuestionConfig(webView);
                    }
                }
            }
            catch (Exception ex)
            {
                // 记录错误但不影响用户体验
                Debug.WriteLine($"地图WebView导航完成处理失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 初始化地图WebView（当切换到地图绘制题时调用）
        /// </summary>
        private async Task InitializeMapWebViewAsync()
        {
            try
            {
                Debug.WriteLine($"InitializeMapWebViewAsync开始: _isMapInitialized={_isMapInitialized}, _serverPort={_serverPort}");
                
                if (_isMapInitialized)
                {
                    Debug.WriteLine("地图已初始化，退出");
                    return;
                }
                
                if (_serverPort <= 0)
                {
                    Debug.WriteLine("HTTP服务器端口无效，退出初始化");
                    return;
                }

                var webView = MapWebView;
                if (webView == null)
                {
                    Debug.WriteLine("MapWebView为null，退出初始化");
                    return;
                }

                // 检查 WebView2 是否已经初始化过
                if (webView.CoreWebView2 != null)
                {
                    Debug.WriteLine("WebView2 已经初始化过，直接配置地图");
                    await LoadMapQuestionConfig(webView);
                    _isMapInitialized = true;
                    return;
                }

                Debug.WriteLine("开始设置WebView2环境选项...");
                
                // 设置WebView2环境选项
                var options = CoreWebView2Environment.CreateAsync(null, null, new CoreWebView2EnvironmentOptions
                {
                    AdditionalBrowserArguments = "--disable-web-security --disable-features=VizDisplayCompositor --allow-running-insecure-content --disable-site-isolation-trials"
                });
                
                Debug.WriteLine("开始初始化CoreWebView2...");
                
                // 确保CoreWebView2已初始化
                await webView.EnsureCoreWebView2Async(await options);

                if (webView.CoreWebView2 != null)
                {
                    Debug.WriteLine("CoreWebView2初始化成功，开始配置设置...");
                    
                    // 配置WebView2设置
                    var settings = webView.CoreWebView2.Settings;
                    settings.AreDevToolsEnabled = true;
                    settings.AreDefaultContextMenusEnabled = true;
                    settings.IsScriptEnabled = true;
                    settings.AreHostObjectsAllowed = true;
                    settings.IsWebMessageEnabled = true;
                    settings.IsGeneralAutofillEnabled = false;
                    settings.IsPasswordAutosaveEnabled = false;
                    
                    // 设置用户代理，模拟标准浏览器
                    settings.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
                    
                    Debug.WriteLine("WebView2设置配置完成");
                    
                    // 允许不安全的内联脚本和样式
                    try
                    {
                        webView.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
                        
                        // 添加权限请求处理
                        webView.CoreWebView2.PermissionRequested += (s, e) =>
                        {
                            e.State = CoreWebView2PermissionState.Allow;
                        };
                        
                        Debug.WriteLine("WebResourceRequested过滤器设置成功");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"设置WebResourceRequested过滤器失败: {ex.Message}");
                    }

                    // 等待HTTP服务器启动完成
                    Debug.WriteLine("等待HTTP服务器启动完成...");
                    await Task.Delay(500);
                    
                    // 使用内置HTTP服务器
                    var httpUrl = $"http://localhost:{_serverPort}/index.html";
                    Debug.WriteLine($"使用内置HTTP服务器加载地图: {httpUrl}");
                    
                    // 注入全局变量，保存动态端口号
                    await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync($@"
                        window.localConfig = {{
                            httpPort: {_serverPort}
                        }};
                        console.log('注入localConfig:', window.localConfig);
                    ");
                    
                    Debug.WriteLine("全局变量注入完成，开始导航到地图页面...");
                    
                    webView.CoreWebView2.Navigate(httpUrl);
                    _isMapInitialized = true;
                    
                    Debug.WriteLine("地图初始化完成");
                }
                else
                {
                    Debug.WriteLine("CoreWebView2初始化失败");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"InitializeMapWebViewAsync error: {ex}");
                
                // 如果是重复初始化错误，尝试直接使用已有的 WebView2
                if (ex.Message.Contains("CoreWebView2Environment") && MapWebView?.CoreWebView2 != null)
                {
                    Debug.WriteLine("检测到重复初始化错误，尝试使用已有的 WebView2");
                    try
                    {
                        await LoadMapQuestionConfig(MapWebView);
                        _isMapInitialized = true;
                        Debug.WriteLine("使用已有 WebView2 初始化成功");
                    }
                    catch (Exception loadEx)
                    {
                        Debug.WriteLine($"使用已有 WebView2 失败: {loadEx.Message}");
                        MessageBox.Show($"初始化地图失败: {loadEx.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show($"初始化地图失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// 加载地图题目配置
        /// </summary>
        private async Task LoadMapQuestionConfig(WebView2 webView)
        {
            try
            {
                if (_viewModel?.CurrentQuestion?.QuestionType == QuestionType.MapDrawing)
                {
                    // 获取当前题目的完整信息
                    var currentAnswerRecord = _viewModel.GetCurrentAnswerRecord();
                    var question = currentAnswerRecord?.Question;
                    
                    if (question != null)
                    {
                        // 构建地图配置消息
                        var configMessage = new
                        {
                            questionId = question.QuestionId,
                            mapConfig = !string.IsNullOrEmpty(question.MapDrawingConfigJson) ? 
                                System.Text.Json.JsonSerializer.Deserialize<object>(question.MapDrawingConfigJson) : null,
                            guidanceOverlays = !string.IsNullOrEmpty(question.GuidanceOverlaysJson) ? 
                                System.Text.Json.JsonSerializer.Deserialize<object>(question.GuidanceOverlaysJson) : null,
                            timeLimitSeconds = question.TimeLimitSeconds
                        };
                        
                        // 发送题目配置到前端
                        if (_viewModel != null)
                        {
                            await _viewModel.SendMessageToWebViewAsync(webView, "loadQuestion", configMessage);
                        }
                        
                        // 如果有已保存的答案，也加载到前端
                        var savedAnswer = currentAnswerRecord?.UserAnswer;
                        if (!string.IsNullOrEmpty(savedAnswer))
                        {
                            try
                            {
                                var answerData = System.Text.Json.JsonSerializer.Deserialize<object>(savedAnswer);
                                if (_viewModel != null && answerData != null)
                                {
                                    await _viewModel.SendMessageToWebViewAsync(webView, "loadAnswer", answerData);
                                }
                            }
                            catch (System.Text.Json.JsonException)
                            {
                                // 如果答案不是有效的JSON，忽略错误
                                System.Diagnostics.Debug.WriteLine("已保存的答案不是有效的JSON格式");
                            }
                        }
                        
                        // 启动自动保存定时器
                        StartAutoSave();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载地图题目配置失败: {ex.Message}");
            }
        }

        private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var message = System.Text.Json.JsonSerializer.Deserialize<WebViewMessage>(e.TryGetWebMessageAsString());
                
                if (message != null && _viewModel != null)
                {
                    switch (message.Type)
                    {
                        case "overlayCountChanged":
                            if (int.TryParse(message.Data?.ToString(), out int count))
                            {
                                _viewModel.MapDrawingOverlayCount = count;
                            }
                            break;
                        case "toolChanged":
                            _viewModel.CurrentDrawingTool = message.Data?.ToString() ?? "point";
                            break;
                        case "mapDrawingData":
                            // 处理地图绘制数据保存
                            HandleMapDrawingData(message.Data?.ToString() ?? "");
                            break;
                        case "currentDataResponse":
                            // 处理自动保存的当前数据响应
                            HandleAutoSaveData(message.Data?.ToString() ?? "");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"处理WebView消息错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理自动保存的数据响应
        /// </summary>
        private void HandleAutoSaveData(string data)
        {
            try
            {
                if (_viewModel != null && !string.IsNullOrEmpty(data))
                {
                    var currentAnswerRecord = _viewModel.GetCurrentAnswerRecord();
                    if (currentAnswerRecord != null)
                    {
                        // 异步保存自动保存的数据
                        _ = SaveMapDrawingDataAsync(currentAnswerRecord.AnswerId, data, isAutoSave: true);
                        
                        System.Diagnostics.Debug.WriteLine($"自动保存数据处理: {data.Length} 字符");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"处理自动保存数据异常: {ex.Message}");
            }
        }

        private void HandleMapDrawingData(string data)
        {
            try
            {
                if (_viewModel != null && !string.IsNullOrEmpty(data))
                {
                    // 更新当前答题记录的答案
                    var currentAnswerRecord = _viewModel.GetCurrentAnswerRecord();
                    if (currentAnswerRecord != null)
                    {
                        // 解析前端overlays数据并转换为后端格式
                        var overlaysElement = JsonSerializer.Deserialize<JsonElement>(data);
                        var convertedOverlays = ConvertOverlaysToMapDrawingData(overlaysElement);
                        var convertedOverlaysJson = JsonSerializer.Serialize(convertedOverlays);
                        
                        currentAnswerRecord.UserAnswer = convertedOverlaysJson;
                        currentAnswerRecord.AnswerTime = DateTime.Now;
                        
                        // 异步保存答案和地图绘制数据
                        _ = SaveMapDrawingDataAsync(currentAnswerRecord.AnswerId, convertedOverlaysJson);
                        
                        System.Diagnostics.Debug.WriteLine($"ExamView 地图绘制数据已更新: {convertedOverlays.Count} 个图形");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ExamView 处理地图绘制数据失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 异步保存地图绘制数据到数据库
        /// </summary>
        private async Task SaveMapDrawingDataAsync(int answerId, string jsonData, bool isAutoSave = false)
        {
            try
            {
                // 解析JSON数据
                var drawingData = System.Text.Json.JsonSerializer.Deserialize<List<MapDrawingDto>>(jsonData);
                if (drawingData != null && _viewModel?.MapDrawingService != null)
                {
                    var request = new SaveMapDrawingRequest
                    {
                        AnswerId = answerId,
                        DrawingData = drawingData
                    };

                    var response = await _viewModel.MapDrawingService.SaveMapDrawingDataAsync(request);
                    
                    if (response.Success)
                    {
                        // 更新UI状态
                        _viewModel.LastSaveTime = response.SaveTime;
                        _viewModel.IsAutoSaveEnabled = true;
                        
                        var saveType = isAutoSave ? "自动保存" : "手动保存";
                        System.Diagnostics.Debug.WriteLine($"地图绘制数据{saveType}成功: {response.SavedCount} 个图形 - {DateTime.Now:HH:mm:ss}");
                    }
                    else
                    {
                        var saveType = isAutoSave ? "自动保存" : "手动保存";
                        System.Diagnostics.Debug.WriteLine($"地图绘制数据{saveType}失败: {response.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                var saveType = isAutoSave ? "自动保存" : "手动保存";
                System.Diagnostics.Debug.WriteLine($"{saveType}地图绘制数据异常: {ex.Message}");
            }
        }

        private void OnExamCompleted(object? sender, EventArgs e)
        {
            // 考试完成，通知父窗口或导航
            MessageBox.Show("考试已完成！", "考试完成", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnExamTimeout(object? sender, EventArgs e)
        {
            // 考试超时，显示提示
            MessageBox.Show("考试时间已到，系统将自动提交您的答案。", "考试超时", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            // 停止自动保存定时器
            StopAutoSave();
            
            // 释放定时器资源
            _autoSaveTimer?.Stop();
            _autoSaveTimer = null;
            
            // 停止HTTP服务器
            StopEmbeddedHttpServer();
            
            // 取消事件订阅
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }
            
            // 取消订阅事件
            if (DataContext is ExamViewModel viewModel)
            {
                viewModel.ExamCompleted -= OnExamCompleted;
                viewModel.ExamTimeout -= OnExamTimeout;
                viewModel.Dispose();
            }
        }

        #region HTTP服务器相关方法

        /// <summary>
        /// 启动内置HTTP服务器
        /// </summary>
        private void StartEmbeddedHttpServer()
        {
            try
            {
                Debug.WriteLine("开始启动内置HTTP服务器...");
                
                // 查找可用端口
                _serverPort = FindAvailablePort(8500);
                Debug.WriteLine($"找到可用端口: {_serverPort}");
                
                _httpServer = new HttpListener();
                _httpServer.Prefixes.Add($"http://localhost:{_serverPort}/");
                _httpServer.Start();
                
                Debug.WriteLine($"内置HTTP服务器启动成功，端口: {_serverPort}");
                
                // 在后台线程处理HTTP请求
                _serverThread = new Thread(() =>
                {
                    try
                    {
                        Debug.WriteLine("HTTP服务器线程开始监听请求...");
                        while (_httpServer.IsListening)
                        {
                            var context = _httpServer.GetContext();
                            Task.Run(() => HandleHttpRequest(context));
                        }
                    }
                    catch (HttpListenerException ex) when (ex.ErrorCode == 995) // ERROR_OPERATION_ABORTED
                    {
                        Debug.WriteLine("HTTP服务器正常关闭");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"HTTP服务器线程异常: {ex.Message}");
                    }
                })
                {
                    IsBackground = true
                };
                _serverThread.Start();
                
                Debug.WriteLine("HTTP服务器后台线程已启动");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"启动HTTP服务器失败: {ex.Message}");
                Debug.WriteLine($"异常详情: {ex}");
                
                // 如果启动失败，尝试使用备用端口
                if (_serverPort > 0)
                {
                    Debug.WriteLine("尝试使用备用端口重新启动...");
                    _serverPort = FindAvailablePort(_serverPort + 10);
                    try
                    {
                        _httpServer = new HttpListener();
                        _httpServer.Prefixes.Add($"http://localhost:{_serverPort}/");
                        _httpServer.Start();
                        Debug.WriteLine($"使用备用端口启动成功: {_serverPort}");
                    }
                    catch (Exception retryEx)
                    {
                        Debug.WriteLine($"备用端口启动也失败: {retryEx.Message}");
                        _serverPort = 0; // 标记启动失败
                    }
                }
            }
        }

        /// <summary>
        /// 停止内置HTTP服务器
        /// </summary>
        private void StopEmbeddedHttpServer()
        {
            try
            {
                _httpServer?.Stop();
                _httpServer?.Close();
                _httpServer = null;
                
                _serverThread?.Join(1000); // 等待最多1秒
                _serverThread = null;
                
                Debug.WriteLine("内置HTTP服务器已停止");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"停止HTTP服务器失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 查找可用端口
        /// </summary>
        private int FindAvailablePort(int startPort)
        {
            for (int port = startPort; port < startPort + 100; port++)
            {
                try
                {
                    var listener = new HttpListener();
                    listener.Prefixes.Add($"http://localhost:{port}/");
                    listener.Start();
                    listener.Stop();
                    return port;
                }
                catch
                {
                    continue;
                }
            }
            return startPort; // 如果都不可用，返回默认端口
        }

        /// <summary>
        /// 处理HTTP请求
        /// </summary>
        private async Task HandleHttpRequest(HttpListenerContext context)
        {
            try
            {
                var request = context.Request;
                var response = context.Response;
                
                var urlPath = request?.Url?.AbsolutePath?.TrimStart('/') ?? string.Empty;

                if (string.IsNullOrEmpty(urlPath))
                    urlPath = "index.html";
                
                var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, 
                    "Assets", "BaiduMap", urlPath);
                
                if (File.Exists(filePath))
                {
                    var content = await File.ReadAllBytesAsync(filePath);
                    var contentType = GetContentType(filePath);
                    
                    response.ContentType = contentType;
                    response.ContentLength64 = content.Length;
                    response.StatusCode = 200;
                    
                    await response.OutputStream.WriteAsync(content, 0, content.Length);
                }
                else
                {
                    response.StatusCode = 404;
                    var errorBytes = System.Text.Encoding.UTF8.GetBytes("File not found");
                    await response.OutputStream.WriteAsync(errorBytes, 0, errorBytes.Length);
                }
                
                response.OutputStream.Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"处理HTTP请求异常: {ex.Message}");
                try
                {
                    context.Response.StatusCode = 500;
                    context.Response.OutputStream.Close();
                }
                catch { }
            }
        }

        /// <summary>
        /// 获取文件内容类型
        /// </summary>
        private string GetContentType(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".html" => "text/html; charset=utf-8",
                ".css" => "text/css; charset=utf-8",
                ".js" => "application/javascript; charset=utf-8",
                ".json" => "application/json; charset=utf-8",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".svg" => "image/svg+xml",
                ".ico" => "image/x-icon",
                _ => "application/octet-stream"
            };
        }

        /// <summary>
        /// 转换前端overlays数据格式为后端MapDrawingDto格式
        /// </summary>
        /// <param name="overlaysElement">前端overlays数据</param>
        /// <returns>转换后的MapDrawingDto列表</returns>
        private List<MapDrawingDto> ConvertOverlaysToMapDrawingData(JsonElement overlaysElement)
        {
            var result = new List<MapDrawingDto>();
            
            try
            {
                if (overlaysElement.ValueKind != JsonValueKind.Array)
                {
                    Debug.WriteLine("ExamView 警告: overlays不是数组格式");
                    return result;
                }

                var currentAnswerRecord = _viewModel?.GetCurrentAnswerRecord();
                int answerId = currentAnswerRecord?.AnswerId ?? 0;

                int orderIndex = 0;
                foreach (var overlay in overlaysElement.EnumerateArray())
                {
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
                    }

                    if (overlay.TryGetProperty("meta", out var metaElement) && 
                        metaElement.TryGetProperty("label", out var labelElement))
                    {
                        mapDrawingDto.Label = labelElement.GetString();
                    }

                    // 提取坐标信息
                    if (overlay.TryGetProperty("geometry", out var geometryElement))
                    {
                        mapDrawingDto.Coordinates = ExtractCoordinates(geometryElement, mapDrawingDto.ShapeType);
                    }

                    // 提取样式信息
                    if (overlay.TryGetProperty("style", out var styleElement))
                    {
                        mapDrawingDto.Style = ExtractStyle(styleElement);
                    }

                    result.Add(mapDrawingDto);
                }

                Debug.WriteLine($"ExamView 转换overlays数据完成，共 {result.Count} 个图形");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ExamView 转换overlays数据格式失败: {ex.Message}");
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
                Debug.WriteLine($"ExamView 提取坐标信息失败，图形类型: {shapeType}, 错误: {ex.Message}");
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
                Debug.WriteLine($"ExamView 提取样式信息失败: {ex.Message}");
            }

            return style;
        }

        #endregion
    }

    public class WebViewMessage
    {
        public string Type { get; set; } = "";
        public object? Data { get; set; }
    }
}