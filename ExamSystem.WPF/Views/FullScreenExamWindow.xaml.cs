using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using ExamSystem.WPF.ViewModels;
using System.Net;
using System.Threading;
using System.IO;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System.Text.Json;
using System.Collections.Generic;
using ExamSystem.Domain.Enums;
using ExamSystem.Domain.DTOs;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace ExamSystem.WPF.Views
{
    /// <summary>
    /// FullScreenExamWindow.xaml 的交互逻辑
    /// </summary>
    public partial class FullScreenExamWindow : Window
    {
        private bool _suppressOptionEvents = false;
        
        // HTTP服务器相关字段
        private HttpListener? _httpServer;
        private Thread? _serverThread;
        private int _serverPort = 8080;
        private bool _isMapInitialized = false;
        private bool _isInitializing = false; // 添加初始化状态标志
        private readonly object _initializationLock = new object(); // 添加初始化锁
        private FullScreenExamViewModel? _viewModel;
        
        public FullScreenExamWindow()
        {
            InitializeComponent();
            
            // 禁用Alt+Tab、Alt+F4等快捷键
            this.KeyDown += Window_KeyDown;
            
            // 窗口关闭事件
            this.Closing += FullScreenExamWindow_Closing;
            
            // 窗口加载完成事件
            this.Loaded += FullScreenExamWindow_Loaded;
            
            // 启动内置HTTP服务器
            StartEmbeddedHttpServer();
            
            // 监听DataContext变化
            this.DataContextChanged += OnDataContextChanged;
        }

        /// <summary>
        /// 窗口加载完成事件处理
        /// </summary>
        private async void FullScreenExamWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 窗口加载完成后再次检查是否需要初始化地图（作为备用检查）
            await Task.Delay(100); // 稍微延迟确保所有初始化完成
            await CheckInitialMapDrawingState();
        }

        /// <summary>
        /// DataContext变化事件处理
        /// </summary>
        private async void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // 取消订阅旧的ViewModel
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }

            // 订阅新的ViewModel
            _viewModel = DataContext as FullScreenExamViewModel;
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;
                
                // 检查初始状态：如果第一题就是地图绘制题，需要立即初始化地图
                await CheckInitialMapDrawingState();
            }
        }

        /// <summary>
        /// 检查初始的地图绘制状态
        /// </summary>
        private async Task CheckInitialMapDrawingState()
        {
            System.Diagnostics.Debug.WriteLine($"CheckInitialMapDrawingState: 开始检查初始状态");
            System.Diagnostics.Debug.WriteLine($"CheckInitialMapDrawingState: _viewModel != null: {_viewModel != null}");
            System.Diagnostics.Debug.WriteLine($"CheckInitialMapDrawingState: CurrentQuestion != null: {_viewModel?.CurrentQuestion != null}");
            System.Diagnostics.Debug.WriteLine($"CheckInitialMapDrawingState: QuestionType: {_viewModel?.CurrentQuestion?.QuestionType}");
            System.Diagnostics.Debug.WriteLine($"CheckInitialMapDrawingState: IsMapDrawing: {_viewModel?.IsMapDrawing}");
            System.Diagnostics.Debug.WriteLine($"CheckInitialMapDrawingState: _isMapInitialized: {_isMapInitialized}");
            
            if ((_viewModel?.CurrentQuestion?.QuestionType == "地图绘制题" || _viewModel?.IsMapDrawing == true) && !_isMapInitialized)
            {
                System.Diagnostics.Debug.WriteLine("CheckInitialMapDrawingState: 检测到初始题目为地图绘制题，开始初始化地图");
                await InitializeMapForDrawingAsync();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("CheckInitialMapDrawingState: 不需要初始化地图或已经初始化过");
            }
        }

        /// <summary>
        /// ViewModel属性变化事件处理
        /// </summary>
        private async void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // 过滤掉定时器相关的属性变化，避免频繁触发
            if (e.PropertyName == nameof(FullScreenExamViewModel.RemainingTimeText))
            {
                return; // 忽略定时器更新
            }

            System.Diagnostics.Debug.WriteLine($"OnViewModelPropertyChanged: PropertyName={e.PropertyName}");
            System.Diagnostics.Debug.WriteLine($"OnViewModelPropertyChanged: IsMapDrawing={_viewModel?.IsMapDrawing}");
            System.Diagnostics.Debug.WriteLine($"OnViewModelPropertyChanged: CurrentQuestion.QuestionType={_viewModel?.CurrentQuestion?.QuestionType}");

            if (e.PropertyName == nameof(FullScreenExamViewModel.CurrentQuestion))
            {
                System.Diagnostics.Debug.WriteLine("OnViewModelPropertyChanged: CurrentQuestion 属性变化");
                
                if (_viewModel?.CurrentQuestion?.QuestionType == "地图绘制题")
                {
                    System.Diagnostics.Debug.WriteLine("OnViewModelPropertyChanged: 检测到地图绘制题，准备初始化地图");
                    await InitializeMapForDrawingAsync();
                }
            }
            else if (e.PropertyName == nameof(FullScreenExamViewModel.IsMapDrawing))
            {
                System.Diagnostics.Debug.WriteLine($"OnViewModelPropertyChanged: IsMapDrawing 属性变化为 {_viewModel?.IsMapDrawing}");
                
                if (_viewModel?.IsMapDrawing == true)
                {
                    System.Diagnostics.Debug.WriteLine("OnViewModelPropertyChanged: IsMapDrawing=true，准备初始化地图");
                    await InitializeMapForDrawingAsync();
                }
            }
        }

        /// <summary>
        /// 初始化地图绘制功能
        /// </summary>
        private async Task InitializeMapForDrawingAsync()
        {
            // 使用锁机制防止重复初始化
            lock (_initializationLock)
            {
                if (_isMapInitialized || _isInitializing)
                {
                    Debug.WriteLine($"FullScreenExamWindow 地图初始化跳过: _isMapInitialized={_isMapInitialized}, _isInitializing={_isInitializing}");
                    return;
                }
                _isInitializing = true; // 设置初始化状态
            }

            try
            {
                Debug.WriteLine("FullScreenExamWindow 开始初始化地图绘制功能...");

                var webView = MapWebView;
                if (webView == null)
                {
                    Debug.WriteLine("FullScreenExamWindow MapWebView为null，无法初始化");
                    return;
                }

                // 检查 WebView2 是否已经初始化过
                if (webView.CoreWebView2 != null)
                {
                    Debug.WriteLine("FullScreenExamWindow WebView2 已经初始化过，直接配置地图");
                    await LoadMapQuestionConfig(webView);
                    
                    lock (_initializationLock)
                    {
                        _isMapInitialized = true;
                        _isInitializing = false;
                    }
                    return;
                }

                await Dispatcher.InvokeAsync(async () => await InitializeMapWebViewAsync());
                
                // 初始化完成后更新状态
                lock (_initializationLock)
                {
                    _isMapInitialized = true;
                    _isInitializing = false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"FullScreenExamWindow 初始化地图失败: {ex.Message}");
                
                // 重置初始化状态
                lock (_initializationLock)
                {
                    _isInitializing = false;
                }
                
                // 如果是重复初始化错误，尝试直接使用已有的 WebView2
                if (ex.Message.Contains("CoreWebView2Environment") && MapWebView?.CoreWebView2 != null)
                {
                    Debug.WriteLine("FullScreenExamWindow 检测到重复初始化错误，尝试使用已有的 WebView2");
                    try
                    {
                        await LoadMapQuestionConfig(MapWebView);
                        lock (_initializationLock)
                        {
                            _isMapInitialized = true;
                        }
                    }
                    catch (Exception loadEx)
                    {
                        Debug.WriteLine($"FullScreenExamWindow 使用已有 WebView2 失败: {loadEx.Message}");
                        MessageBox.Show($"初始化地图失败: {loadEx.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show($"初始化地图失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SingleChoiceOption_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton && radioButton.Tag is OptionViewModel option)
            {
                var viewModel = DataContext as FullScreenExamViewModel;
                if (viewModel != null)
                {
                    var beforeSelected = viewModel.CurrentQuestion?.Options != null
                        ? System.Linq.Enumerable.Count(viewModel.CurrentQuestion.Options, o => o.IsSelected)
                        : 0;
                    Serilog.Log.Information("UI: SingleChoiceOption_Click begin: QIndex={QIndex}, Option={Label}, beforeSelected={Before}", viewModel.CurrentQuestionIndex, option.Label, beforeSelected);

                    try
                    {
                        _suppressOptionEvents = true; // 抑制多选事件在单选更新期间被触发
                        viewModel.SelectSingleOptionCommand?.Execute(option);
                    }
                    finally
                    {
                        _suppressOptionEvents = false;
                    }

                    var afterSelected = viewModel.CurrentQuestion?.Options != null
                        ? System.Linq.Enumerable.Count(viewModel.CurrentQuestion.Options, o => o.IsSelected)
                        : 0;
                    Serilog.Log.Information("UI: SingleChoiceOption_Click end: QIndex={QIndex}, Option={Label}, afterSelected={After}", viewModel.CurrentQuestionIndex, option.Label, afterSelected);
                }
                else
                {
                    // 兜底执行，避免 DataContext 为空导致点击无效
                    viewModel?.SelectSingleOptionCommand?.Execute(option);
                }
            }
        }

        private void MultipleChoiceOption_Checked(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as FullScreenExamViewModel;
            if (viewModel == null)
            {
                e.Handled = true;
                return;
            }

            if (_suppressOptionEvents)
            {
                Serilog.Log.Debug("UI: MultipleChoiceOption_Checked suppressed by flag");
                e.Handled = true;
                return;
            }

            if (!viewModel.IsMultipleChoice)
            {
                Serilog.Log.Debug("UI: MultipleChoiceOption_Checked ignored, current question is not multiple choice");
                e.Handled = true;
                return;
            }

            if (sender is CheckBox checkBox && checkBox.DataContext is OptionViewModel option)
            {
                Serilog.Log.Debug("UI: MultipleChoiceOption_Checked begin: Option={Label}", option.Label);
                try
                {
                    _suppressOptionEvents = true;
                    viewModel.SetMultipleOption(option, true);
                }
                finally
                {
                    _suppressOptionEvents = false;
                }
                e.Handled = true;
                Serilog.Log.Debug("UI: MultipleChoiceOption_Checked end: Option={Label}", option.Label);
            }
        }

        private void MultipleChoiceOption_Unchecked(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as FullScreenExamViewModel;
            if (viewModel == null)
            {
                e.Handled = true;
                return;
            }

            if (_suppressOptionEvents)
            {
                Serilog.Log.Debug("UI: MultipleChoiceOption_Unchecked suppressed by flag");
                e.Handled = true;
                return;
            }

            if (!viewModel.IsMultipleChoice)
            {
                Serilog.Log.Debug("UI: MultipleChoiceOption_Unchecked ignored, current question is not multiple choice");
                e.Handled = true;
                return;
            }

            if (sender is CheckBox checkBox && checkBox.DataContext is OptionViewModel option)
            {
                Serilog.Log.Debug("UI: MultipleChoiceOption_Unchecked begin: Option={Label}", option.Label);
                try
                {
                    _suppressOptionEvents = true;
                    viewModel.SetMultipleOption(option, false);
                }
                finally
                {
                    _suppressOptionEvents = false;
                }
                e.Handled = true;
                Serilog.Log.Debug("UI: MultipleChoiceOption_Unchecked end: Option={Label}", option.Label);
            }
        }

        private void TrueFalseOption_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton && radioButton.Tag is string value)
            {
                var viewModel = DataContext as FullScreenExamViewModel;
                viewModel?.SelectTrueFalseCommand?.Execute(value);
            }
        }

        /// <summary>
        /// 键盘按键事件处理
        /// </summary>
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // 禁用一些可能导致退出全屏的快捷键
            if (e.Key == Key.F4 && (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
            {
                e.Handled = true; // 禁用Alt+F4
            }
            else if (e.Key == Key.Tab && (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
            {
                e.Handled = true; // 禁用Alt+Tab
            }
            else if (e.Key == Key.Escape)
            {
                e.Handled = true; // 禁用Esc键
            }
            else if (e.Key == Key.F11)
            {
                e.Handled = true; // 禁用F11全屏切换
            }
            else if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                // 禁用一些Ctrl组合键
                if (e.Key == Key.N || e.Key == Key.T || e.Key == Key.W || 
                    e.Key == Key.R || e.Key == Key.F5 || e.Key == Key.L)
                {
                    e.Handled = true;
                }
            }
            else if ((Keyboard.Modifiers & ModifierKeys.Windows) == ModifierKeys.Windows)
            {
                e.Handled = true; // 禁用Windows键组合
            }
        }

        /// <summary>
        /// 窗口关闭事件处理
        /// </summary>
        private void FullScreenExamWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                // 清理HTTP服务器
                if (_httpServer != null)
                {
                    Debug.WriteLine("FullScreenExamWindow 正在关闭HTTP服务器...");
                    _httpServer.Stop();
                    _httpServer.Close();
                    _httpServer = null;
                }

                if (_serverThread != null && _serverThread.IsAlive)
                {
                    Debug.WriteLine("FullScreenExamWindow 正在停止HTTP服务器线程...");
                    _serverThread.Join(1000); // 等待最多1秒
                }

                // 清理WebView
                if (MapWebView?.CoreWebView2 != null)
                {
                    Debug.WriteLine("FullScreenExamWindow 正在清理WebView...");
                    MapWebView.CoreWebView2.WebMessageReceived -= OnWebMessageReceived;
                }

                Debug.WriteLine("FullScreenExamWindow 资源清理完成");

                // 如果是通过ViewModel的退出命令关闭，则允许关闭
                if (DataContext is FullScreenExamViewModel viewModel && viewModel.IsExitConfirmed)
                {
                    return;
                }

                // 否则弹出确认对话框
                var result = MessageBox.Show(
                    "确定要退出考试吗？退出后将无法继续答题。",
                    "退出确认",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning,
                    MessageBoxResult.No);

                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true; // 取消关闭
                }
                else
                {
                    // 用户确认退出，通知ViewModel
                    if (DataContext is FullScreenExamViewModel vm)
                    {
                        vm.ForceExit();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"FullScreenExamWindow 关闭窗口时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 启动内置HTTP服务器
        /// </summary>
        private void StartEmbeddedHttpServer()
        {
            try
            {
                Debug.WriteLine("FullScreenExamWindow 开始启动内置HTTP服务器...");

                // 查找可用端口
                for (int port = 8080; port <= 8090; port++)
                {
                    try
                    {
                        Debug.WriteLine($"FullScreenExamWindow 尝试使用端口: {port}");
                        
                        _httpServer = new HttpListener();
                        _httpServer.Prefixes.Add($"http://localhost:{port}/");
                        _httpServer.Start();
                        _serverPort = port;
                        
                        Debug.WriteLine($"FullScreenExamWindow HTTP服务器启动成功，端口: {port}");
                        break;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"FullScreenExamWindow 端口 {port} 不可用: {ex.Message}");
                        _httpServer?.Close();
                        _httpServer = null;
                        continue;
                    }
                }

                if (_httpServer == null)
                {
                    Debug.WriteLine("FullScreenExamWindow HTTP服务器启动失败：无可用端口");
                    return;
                }

                // 启动监听线程
                _serverThread = new Thread(() =>
                {
                    Debug.WriteLine("FullScreenExamWindow HTTP服务器监听线程已启动");
                    
                    while (_httpServer != null && _httpServer.IsListening)
                    {
                        try
                        {
                            var context = _httpServer.GetContext();
                            ThreadPool.QueueUserWorkItem(HandleRequest, context);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"FullScreenExamWindow HTTP服务器监听异常: {ex.Message}");
                            break;
                        }
                    }
                    
                    Debug.WriteLine("FullScreenExamWindow HTTP服务器监听线程已退出");
                })
                {
                    IsBackground = true
                };
                
                _serverThread.Start();
                Debug.WriteLine("FullScreenExamWindow HTTP服务器启动完成");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"FullScreenExamWindow 启动HTTP服务器失败: {ex.Message}");
                
                // 尝试使用备用端口重新启动
                try
                {
                    _serverPort = 9080;
                    _httpServer = new HttpListener();
                    _httpServer.Prefixes.Add($"http://localhost:{_serverPort}/");
                    _httpServer.Start();
                    Debug.WriteLine($"FullScreenExamWindow 使用备用端口 {_serverPort} 启动HTTP服务器成功");
                }
                catch (Exception backupEx)
                {
                    Debug.WriteLine($"FullScreenExamWindow 备用端口启动也失败: {backupEx.Message}");
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
                        Debug.WriteLine($"FullScreenExamWindow /api/icons 生成失败: {apiEx.Message}");
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
                Debug.WriteLine($"FullScreenExamWindow 处理HTTP请求失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 地图WebView导航完成事件处理
        /// </summary>
        private async void MapWebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            try
            {
                Debug.WriteLine($"FullScreenExamWindow 地图WebView导航完成，成功: {e.IsSuccess}");
                var webView = sender as WebView2;
                if (webView?.CoreWebView2 != null && e.IsSuccess)
                {
                    
                    Debug.WriteLine("FullScreenExamWindow WebView2导航成功，开始处理");
                    
                    // 订阅WebView消息
                    webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

                    // 发送握手消息，通知前端WPF已准备好接收消息
                    try
                    {
                        var readyMessage = new { type = "wpfReady" };
                        var readyJson = JsonSerializer.Serialize(readyMessage);
                        webView.CoreWebView2.PostWebMessageAsJson(readyJson);
                        Debug.WriteLine("FullScreenExamWindow 已向WebView发送 wpfReady 握手消息");
                    }
                    catch (Exception sendReadyEx)
                    {
                        Debug.WriteLine($"FullScreenExamWindow 发送 wpfReady 握手失败: {sendReadyEx.Message}");
                    }

                    // 如果当前是地图绘制题，加载题目配置
                    if (_viewModel?.CurrentQuestion?.QuestionType == "地图绘制题")
                    {
                        Debug.WriteLine("FullScreenExamWindow 当前是地图绘制题，加载题目配置");
                        await LoadMapQuestionConfig(webView);
                    }

                    // 隐藏加载指示器
                    await Dispatcher.InvokeAsync(() =>
                    {
                        if (MapLoadingIndicator != null)
                        {
                            MapLoadingIndicator.Visibility = Visibility.Collapsed;
                            Debug.WriteLine("FullScreenExamWindow 加载指示器已隐藏");
                        }
                    });
                }
                else
                {
                    Debug.WriteLine($"FullScreenExamWindow WebView2导航失败或WebView为空: IsSuccess={e.IsSuccess}, WebView={webView != null}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"FullScreenExamWindow 地图WebView导航完成处理失败: {ex.Message}");
                Debug.WriteLine($"FullScreenExamWindow 异常堆栈: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// WebView2 尺寸变化事件处理（已禁用动态高度调整）
        /// </summary>
        private void MapWebView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // 不再进行动态高度调整，使用固定高度
            Debug.WriteLine($"FullScreenExamWindow WebView2 尺寸变化: {e.NewSize.Width}x{e.NewSize.Height}（固定高度模式）");
        }

        /// <summary>
        /// 初始化地图WebView（当切换到地图绘制题时调用）
        /// </summary>
        private async Task InitializeMapWebViewAsync()
        {
            try
            {
                Debug.WriteLine($"FullScreenExamWindow InitializeMapWebViewAsync: _isMapInitialized={_isMapInitialized}, _serverPort={_serverPort}");

                if (_isMapInitialized)
                {
                    Debug.WriteLine("FullScreenExamWindow 地图已初始化，退出");
                    return;
                }

                var webView = MapWebView;

                if (webView == null)
                {
                    Debug.WriteLine("FullScreenExamWindow MapWebView为null，无法初始化");
                    return;
                }

                // 检查 WebView2 是否已经初始化过
                if (webView.CoreWebView2 != null)
                {
                    Debug.WriteLine("FullScreenExamWindow WebView2 已经初始化过，直接配置地图");
                    await LoadMapQuestionConfig(webView);
                    _isMapInitialized = true;
                    return;
                }

                

                Debug.WriteLine("FullScreenExamWindow 开始设置WebView2环境选项...");

                // 设置WebView2环境选项
                var options = CoreWebView2Environment.CreateAsync(null, null, new CoreWebView2EnvironmentOptions
                {
                    AdditionalBrowserArguments = "--disable-web-security --allow-running-insecure-content"
                });

                Debug.WriteLine("FullScreenExamWindow 开始初始化CoreWebView2...");

                // 确保CoreWebView2已初始化
                await webView.EnsureCoreWebView2Async(await options);

                if (webView.CoreWebView2 != null)
                {
                    Debug.WriteLine("FullScreenExamWindow CoreWebView2初始化成功，开始配置设置...");
                    webView.CoreWebView2.OpenDevToolsWindow();
                    // 配置WebView2设置
                    var settings = webView.CoreWebView2.Settings;
                    settings.IsScriptEnabled = true;
                    settings.AreDefaultScriptDialogsEnabled = true;
                    settings.IsWebMessageEnabled = true;
                    settings.AreDevToolsEnabled = true;
                    settings.IsGeneralAutofillEnabled = false;
                    settings.AreHostObjectsAllowed = true;
                    settings.AreBrowserAcceleratorKeysEnabled = false;

                    Debug.WriteLine("FullScreenExamWindow WebView2设置配置完成");

                    // 添加Web资源过滤器
                    webView.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);

                    // 设置权限处理
                    webView.CoreWebView2.PermissionRequested += (s, e) =>
                    {
                        e.State = CoreWebView2PermissionState.Allow;
                    };

                    // 构建地图页面URL
                    var httpUrl = $"http://localhost:{_serverPort}/index.html";

                    Debug.WriteLine($"FullScreenExamWindow 使用内置HTTP服务器加载地图: {httpUrl}");

                    // 注入全局变量
                    await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync($@"
                        window.examSystemConfig = {{
                            serverPort: {_serverPort},
                            isFullScreen: true
                        }};
                    ");

                    Debug.WriteLine("FullScreenExamWindow 全局变量注入完成，开始导航到地图页面...");

                    webView.CoreWebView2.Navigate(httpUrl);

                    _isMapInitialized = true;
                    Debug.WriteLine("FullScreenExamWindow 地图初始化完成");
                }
                else
                {
                    Debug.WriteLine("FullScreenExamWindow CoreWebView2初始化失败");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"FullScreenExamWindow 初始化地图失败: {ex.Message}");
                
                // 如果是重复初始化错误，尝试直接使用已有的 WebView2
                if (ex.Message.Contains("CoreWebView2Environment") && MapWebView?.CoreWebView2 != null)
                {
                    Debug.WriteLine("FullScreenExamWindow 检测到重复初始化错误，尝试使用已有的 WebView2");
                    try
                    {
                        await LoadMapQuestionConfig(MapWebView);
                        _isMapInitialized = true;
                        Debug.WriteLine("FullScreenExamWindow 使用已有 WebView2 初始化成功");
                    }
                    catch (Exception loadEx)
                    {
                        Debug.WriteLine($"FullScreenExamWindow 使用已有 WebView2 失败: {loadEx.Message}");
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
                Debug.WriteLine("FullScreenExamWindow 开始加载地图题目配置...");
                if (_viewModel?.CurrentQuestion?.QuestionType == "地图绘制题")
                {
                    var question = _viewModel.CurrentQuestion;

                    // 构建地图配置消息
                    var configMessage = new
                    {
                        type = "loadQuestion",
                        data = new
                        {
                            questionId = question.Id,
                            mapConfig = !string.IsNullOrEmpty(question.MapDrawingConfigJson) ?
                                System.Text.Json.JsonSerializer.Deserialize<object>(question.MapDrawingConfigJson) : null,
                            existingAnswer = !string.IsNullOrEmpty(question.MapDrawingAnswer) ?
                                System.Text.Json.JsonSerializer.Deserialize<object>(question.MapDrawingAnswer) : null
                        }
                    };

                    var messageJson = System.Text.Json.JsonSerializer.Serialize(configMessage);
                    Debug.WriteLine($"FullScreenExamWindow 发送地图配置消息: {messageJson}");

                    if (webView.CoreWebView2 != null)
                    {
                        
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FullScreenExamWindow 加载地图题目配置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理来自WebView的消息
        /// </summary>
        private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                // 安全地获取消息字符串
                string? messageJson = null;
                try
                {
                    messageJson = e.WebMessageAsJson;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"FullScreenExamWindow 获取WebView消息字符串失败: {ex.Message}");
                    return;
                }

                if (string.IsNullOrEmpty(messageJson))
                {
                    Debug.WriteLine("FullScreenExamWindow 收到空的WebView消息");
                    return;
                }

                Debug.WriteLine($"FullScreenExamWindow 收到WebView消息: {messageJson}");

                // 安全地解析JSON
                JsonElement message;
                try
                {
                    message = JsonSerializer.Deserialize<JsonElement>(messageJson);
                }
                catch (JsonException ex)
                {
                    Debug.WriteLine($"FullScreenExamWindow JSON解析失败: {ex.Message}, 消息内容: {messageJson}");
                    return;
                }

                // 安全地获取消息类型
                string? messageType = null;
                try
                {
                    if (message.TryGetProperty("type", out var typeProperty))
                    {
                        messageType = typeProperty.GetString();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"FullScreenExamWindow 获取消息类型失败: {ex.Message}");
                    return;
                }

                if (string.IsNullOrEmpty(messageType))
                {
                    Debug.WriteLine("FullScreenExamWindow 消息类型为空或未找到");
                    return;
                }

                switch (messageType)
                {
                    case "pageReady":
                        _ = HandleBuildingDataRequest("guiyang");
                        break;
                    case "overlayCountChanged":
                        var count = message.GetProperty("count").GetInt32();
                        if (_viewModel != null)
                        {
                            _viewModel.MapDrawingOverlayCount = count;
                        }
                        break;

                    case "mapDrawingData":
                        // 处理地图绘制数据保存
                        HandleMapDrawingData(message.GetProperty("data").ToString() ?? "");
                        break;

                    case "autoSave":
                        // 处理自动保存
                        var autoSaveData = message.GetProperty("data").ToString() ?? "";
                        HandleAutoSaveMapDrawingData(autoSaveData);
                        break;

                    case "requestBuildingData":
                        // 处理建筑数据请求
                        Debug.WriteLine($"FullScreenExamWindow 收到requestBuildingData消息: {messageJson}");
                        
                        string cityName = "";
                        try
                        {
                            // 尝试从data.cityName获取城市名称
                            if (message.TryGetProperty("data", out var dataProperty) && 
                                dataProperty.TryGetProperty("cityName", out var cityNameProperty))
                            {
                                cityName = cityNameProperty.GetString() ?? "";
                                Debug.WriteLine($"FullScreenExamWindow 从data.cityName获取城市名称: {cityName}");
                            }
                            // 如果data.cityName不存在，尝试直接从cityName获取
                            else if (message.TryGetProperty("cityName", out var directCityNameProperty))
                            {
                                cityName = directCityNameProperty.GetString() ?? "";
                                Debug.WriteLine($"FullScreenExamWindow 从cityName获取城市名称: {cityName}");
                            }
                            else
                            {
                                Debug.WriteLine("FullScreenExamWindow 错误: 无法从消息中提取城市名称");
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"FullScreenExamWindow 提取城市名称失败: {ex.Message}");
                            break;
                        }
                        
                        if (!string.IsNullOrEmpty(cityName))
                        {
                            Debug.WriteLine($"FullScreenExamWindow 开始处理建筑数据请求，城市: {cityName}");
                            _ = HandleBuildingDataRequest(cityName);
                        }
                        else
                        {
                            Debug.WriteLine("FullScreenExamWindow 错误: 城市名称为空");
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"FullScreenExamWindow 处理WebView消息失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理地图绘制数据
        /// </summary>
        private void HandleMapDrawingData(string data)
        {
            try
            {
                if (_viewModel?.CurrentQuestion != null)
                {
                    // 更新当前题目的地图绘制答案
                    _viewModel.CurrentQuestion.MapDrawingAnswer = data;

                    // 异步保存答案和地图绘制数据
                    var currentAnswerRecord = _viewModel.GetCurrentAnswerRecord();
                    _ = SaveMapDrawingDataAsync(currentAnswerRecord?.AnswerId ?? 0, data);

                    System.Diagnostics.Debug.WriteLine($"FullScreenExamWindow 地图绘制数据已更新: {data.Length} 字符");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FullScreenExamWindow 处理地图绘制数据失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理自动保存地图绘制数据
        /// </summary>
        private void HandleAutoSaveMapDrawingData(string data)
        {
            try
            {
                var currentAnswerRecord = _viewModel?.GetCurrentAnswerRecord();
                if (currentAnswerRecord != null)
                {
                    // 异步自动保存
                    _ = SaveMapDrawingDataAsync(currentAnswerRecord.AnswerId, data, isAutoSave: true);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"FullScreenExamWindow 处理自动保存失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 异步保存地图绘制数据到数据库
        /// </summary>
        private async Task SaveMapDrawingDataAsync(int answerId, string jsonData, bool isAutoSave = false)
        {
            try
            {
                var saveType = isAutoSave ? "自动保存" : "手动保存";
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
                        System.Diagnostics.Debug.WriteLine($"FullScreenExamWindow 地图绘制数据{saveType}成功: {response.SavedCount} 个图形 - {DateTime.Now:HH:mm:ss}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"FullScreenExamWindow 地图绘制数据{saveType}失败: {response.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                var saveType = isAutoSave ? "自动保存" : "手动保存";
                System.Diagnostics.Debug.WriteLine($"FullScreenExamWindow {saveType}地图绘制数据异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理建筑数据请求
        /// </summary>
        private async Task HandleBuildingDataRequest(string cityName)
        {
            try
            {
                Debug.WriteLine($"FullScreenExamWindow 收到建筑数据请求，城市键值: {cityName}");
                
                // 城市键值映射：前端传递的键值 -> 数据库中的城市键值
                var cityMapping = new Dictionary<string, string>
                {
                    // 贵州省城市映射到测试数据城市
                    { "guiyang", "贵阳市" },      // 贵阳 -> 北京（用于测试）
                    { "zunyi", "遵义市" },       // 遵义 -> 上海（用于测试）
                    { "liupanshui", "六盘水市" },   // 六盘水 -> 北京
                    { "anshun", "安顺市" },      // 安顺 -> 上海
                    { "bijie", "毕节市" },        // 毕节 -> 北京
                    { "tongren", "铜仁市" },     // 铜仁 -> 上海
                    { "qiandongnan", "黔东南苗族侗族自治州" },  // 黔东南 -> 北京
                    { "qiannan", "黔南布依族苗族自治州" },     // 黔南 -> 上海
                    { "qianxinan", "黔西南布依族苗族自治州" }
                };

                // 获取映射后的城市键值
                var mappedCityName = cityMapping.ContainsKey(cityName) ? cityMapping[cityName] : cityName;
                Debug.WriteLine($"FullScreenExamWindow 城市键值映射: {cityName} -> {mappedCityName}");

                // 从依赖注入容器获取BuildingRepository
                var serviceProvider = ((App)Application.Current).GetServices();
                var buildingRepository = serviceProvider.GetRequiredService<ExamSystem.Infrastructure.Repositories.IBuildingRepository>();

                // 获取该城市的所有建筑数据
                var buildings = await buildingRepository.GetBuildingsByCityAsync(mappedCityName);
                Debug.WriteLine($"FullScreenExamWindow 查询到建筑数据数量: {buildings?.Count() ?? 0}");

                // 转换为前端需要的格式
                var buildingData = buildings.Select(b => new
                {
                    id = b.Id,
                    name = b.OrgName,
                    type = b.OrgType, // 1-消防队站；2-专职队；3-重点建筑
                    latitude = b.GetCoordinates()?[1] ?? 0, // 纬度
                    longitude = b.GetCoordinates()?[0] ?? 0, // 经度
                    address = b.Address ?? "",
                    phone = "", // Building实体没有phone字段
                    description = b.GetOrgTypeDescription()
                }).ToList();

                Debug.WriteLine($"FullScreenExamWindow 转换后的建筑数据: {System.Text.Json.JsonSerializer.Serialize(buildingData)}");

                // 发送建筑数据到WebView
                var responseMessage = new
                {
                    type = "buildingDataResponse",
                    cityName = cityName, // 返回原始城市键值
                    buildings = buildingData
                };

                var responseJson = System.Text.Json.JsonSerializer.Serialize(responseMessage);
                Debug.WriteLine($"FullScreenExamWindow 发送建筑数据响应: {responseJson}");

                if (MapWebView?.CoreWebView2 != null)
                {
                    MapWebView.CoreWebView2.PostWebMessageAsJson(responseJson);
                    Debug.WriteLine("FullScreenExamWindow 建筑数据已发送到WebView");
                }
                else
                {
                    Debug.WriteLine("FullScreenExamWindow 错误: WebView未初始化");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"FullScreenExamWindow 处理建筑数据请求失败: {ex.Message}");
                
                // 发送错误响应
                var errorMessage = new
                {
                    type = "buildingDataError",
                    cityName = cityName,
                    error = ex.Message
                };

                var errorJson = System.Text.Json.JsonSerializer.Serialize(errorMessage);
                if (MapWebView?.CoreWebView2 != null)
                {
                    MapWebView.CoreWebView2.PostWebMessageAsJson(errorJson);
                }
            }
        }

        /// <summary>
        /// 设置考试数据
        /// </summary>
        public void SetExamData(int paperId, string paperTitle)
        {
            var viewModel = new FullScreenExamViewModel(paperId, paperTitle);
            DataContext = viewModel;
            
            // 订阅退出事件
            viewModel.ExitRequested += (s, e) =>
            {
                this.Close();
            };
        }
    }
}