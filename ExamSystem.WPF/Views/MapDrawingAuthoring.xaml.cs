using ExamSystem.WPF.ViewModels;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shapes;

namespace ExamSystem.WPF.Views
{
    /// <summary>
    /// 地图绘制题出题页面
    /// </summary>
    public partial class MapDrawingAuthoring : Window
    {
        private WebView2 _webView;
        private MapDrawingAuthoringViewModel _viewModel;
        private HttpListener _httpServer;
        private Thread _serverThread;
        private int _serverPort = 0;

        public MapDrawingAuthoring()
        {
            InitializeComponent();
            StartEmbeddedHttpServer();
            InitializeWebViewAsync();
        }

        public MapDrawingAuthoring(MapDrawingAuthoringViewModel viewModel) : this()
        {
            _viewModel = viewModel;
            DataContext = viewModel;
            
            // 订阅ViewModel事件
            viewModel.SaveCompleted += OnSaveCompleted;
        }

        private void StartEmbeddedHttpServer()
        {
            try
            {
                // 查找可用端口
                _serverPort = FindAvailablePort(8500);
                
                _httpServer = new HttpListener();
                _httpServer.Prefixes.Add($"http://localhost:{_serverPort}/");
                _httpServer.Start();
                
                Debug.WriteLine($"内置HTTP服务器启动在端口: {_serverPort}");
                
                // 在后台线程处理HTTP请求
                _serverThread = new Thread(() =>
                {
                    try
                    {
                        while (_httpServer.IsListening)
                        {
                            var context = _httpServer.GetContext();
                            Task.Run(() => HandleHttpRequest(context));
                        }
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
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"启动HTTP服务器失败: {ex.Message}");
                MessageBox.Show($"启动内置HTTP服务器失败: {ex.Message}", "警告", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

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

        private async Task HandleHttpRequest(HttpListenerContext context)
        {
            try
            {
                var request = context.Request;
                var response = context.Response;
                
                var urlPath = request.Url.AbsolutePath.TrimStart('/');


                if (string.IsNullOrEmpty(urlPath))
                    urlPath = "index.html";
                
                var filePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, 
                    "Assets", "BaiduMap", urlPath);
                
                if (System.IO.File.Exists(filePath))
                {
                    var content = System.IO.File.ReadAllBytes(filePath);
                    var contentType = GetContentType(filePath);
                    
                    response.ContentType = contentType;
                    response.ContentLength64 = content.Length;
                    response.StatusCode = 200;
                    
                    response.OutputStream.Write(content, 0, content.Length);
                }
                else
                {
                    response.StatusCode = 404;
                    var errorBytes = System.Text.Encoding.UTF8.GetBytes("File not found");
                    response.OutputStream.Write(errorBytes, 0, errorBytes.Length);
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

        private string GetContentType(string filePath)
        {
            var extension = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
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

        private async void InitializeWebViewAsyncBak()
        {
            try
            {
                _webView = BaiduMapWebView;
                await _webView.EnsureCoreWebView2Async();

                // 配置WebView2设置
                var settings = _webView.CoreWebView2.Settings;
                settings.AreDevToolsEnabled = true;
                settings.AreDefaultContextMenusEnabled = true;
                settings.IsScriptEnabled = true;
                settings.AreHostObjectsAllowed = true;
                settings.IsWebMessageEnabled = true;
                settings.IsGeneralAutofillEnabled = false;
                settings.IsPasswordAutosaveEnabled = false;


                string folderPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "BaiduMap");
                _webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    "map.local",
                    folderPath,
                    CoreWebView2HostResourceAccessKind.Allow);

                _webView.Source = new Uri("http://map.local/index.html");

                _webView.NavigationCompleted += (s, e) =>
                {
                    if (!e.IsSuccess)
                        MessageBox.Show("地图加载失败，请检查AK配置或白名单。");
                    else
                        _webView.CoreWebView2.OpenDevToolsWindow(); // 调试查看错误
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化WebView失败: {ex.Message}");
            }
        }


        private async void InitializeWebViewAsync()
        {
            try
            {
                // 直接使用XAML中定义的WebView2控件
                _webView = BaiduMapWebView;
                
                // 设置WebView2环境选项
                var options = CoreWebView2Environment.CreateAsync(null, null, new CoreWebView2EnvironmentOptions
                {
                    AdditionalBrowserArguments = "--disable-web-security --disable-features=VizDisplayCompositor --allow-running-insecure-content --disable-site-isolation-trials"
                });
                
                // 确保CoreWebView2已初始化
                await _webView.EnsureCoreWebView2Async(await options);

                if (_webView.CoreWebView2 != null)
                {
                    _webView.NavigationCompleted += WebView_NavigationCompleted;
                    _webView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
                    _webView.CoreWebView2InitializationCompleted += WebView_CoreWebView2InitializationCompleted;

                    // 配置WebView2设置
                    var settings = _webView.CoreWebView2.Settings;
                    settings.AreDevToolsEnabled = true;
                    settings.AreDefaultContextMenusEnabled = true;
                    settings.IsScriptEnabled = true;
                    settings.AreHostObjectsAllowed = true;
                    settings.IsWebMessageEnabled = true;
                    settings.IsGeneralAutofillEnabled = false;
                    settings.IsPasswordAutosaveEnabled = false;


                    
                    // 设置用户代理，模拟标准浏览器
                    settings.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
                    
                    // 允许不安全的内联脚本和样式
                    try
                    {
                        _webView.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
                        _webView.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;
                        
                        // 添加权限请求处理
                        _webView.CoreWebView2.PermissionRequested += CoreWebView2_PermissionRequested;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"设置WebResourceRequested过滤器失败: {ex.Message}");
                    }

                    // 添加导航开始事件
                    _webView.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;
                    _webView.CoreWebView2.DOMContentLoaded += CoreWebView2_DOMContentLoaded;
                    
                    // 控制台消息监听暂时注释掉，避免版本兼容性问题
                    // _webView.CoreWebView2.ConsoleMessage += CoreWebView2_ConsoleMessage;

                    // 等待HTTP服务器启动完成
                    await Task.Delay(1000);
                    
                    // 使用内置HTTP服务器
                    if (_serverPort > 0)
                    {
                        var httpUrl = $"http://localhost:{_serverPort}/index.html";
                        Debug.WriteLine($"使用内置HTTP服务器: {httpUrl}");
                        _webView.CoreWebView2.Navigate(httpUrl);
                        // 注入全局变量，保存动态端口号
                        await _webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync($@"
                            window.localConfig = {{
                                httpPort: {_serverPort}
                            }};
                        ");
                    }
                    else
                    {
                        MessageBox.Show("内置HTTP服务器启动失败，无法加载地图", "错误", 
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                // 将WebView引用传递给ViewModel
                _viewModel?.SetWebView(_webView);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"InitializeWebViewAsync error: {ex}");
                MessageBox.Show($"初始化WebView失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            _webView.CoreWebView2.OpenDevToolsWindow();
            if (e.IsSuccess && _webView.CoreWebView2 != null)
            {
                // 设置消息监听
                _webView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
                
                if (_viewModel != null)
                {
                    _viewModel.SetWebView(_webView);
                    _viewModel.IsMapLoading = false;
                }
            }
            else
            {
                _viewModel.IsMapLoading = false;
                MessageBox.Show("地图页面导航失败，请检查地图文件与初始化", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CoreWebView2_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var message = e.TryGetWebMessageAsString();
                _viewModel?.HandleWebViewMessage(message);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"处理WebView消息时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnSaveCompleted(object sender, EventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            // 停止HTTP服务器
            try
            {
                _httpServer?.Stop();
                _httpServer?.Close();
                _serverThread?.Join(1000); // 等待最多1秒
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"停止HTTP服务器异常: {ex.Message}");
            }

            // 清理资源
            if (_viewModel != null)
            {
                _viewModel.SaveCompleted -= OnSaveCompleted;
            }

            if (_webView?.CoreWebView2 != null)
            {
                _webView.CoreWebView2.WebMessageReceived -= CoreWebView2_WebMessageReceived;
            }

            _webView?.Dispose();
            base.OnClosed(e);
        }

        private void WebView_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            if (!e.IsSuccess)
            {
                Debug.WriteLine($"CoreWebView2 初始化失败: {e.InitializationException}");
                MessageBox.Show("WebView2 初始化失败，请检查运行环境是否安装WebView2 Runtime。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CoreWebView2_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            Debug.WriteLine($"导航开始: {e.Uri}");
        }

        private void CoreWebView2_DOMContentLoaded(object sender, CoreWebView2DOMContentLoadedEventArgs e)
        {
            Debug.WriteLine("DOM内容加载完成");
        }

        private void CoreWebView2_ConsoleMessage(object sender, object e)
        {
            // 由于WebView2版本兼容性问题，暂时使用object类型
            Debug.WriteLine($"控制台消息: {e}");
        }

        private void CoreWebView2_PermissionRequested(object sender, CoreWebView2PermissionRequestedEventArgs e)
        {
            // 自动允许所有权限请求
            e.State = CoreWebView2PermissionState.Allow;
            Debug.WriteLine($"权限请求已允许: {e.PermissionKind} for {e.Uri}");
        }

        private void CoreWebView2_WebResourceRequested(object sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            try
            {
                var uri = e.Request.Uri;
                Debug.WriteLine($"资源请求: {uri}");
                
                // 为百度地图API请求添加必要的头部
                if (uri.Contains("api.map.baidu.com") || uri.Contains("maponline") || uri.Contains("baidu"))
                {
                    try
                    {
                        // 由于WebView2版本兼容性问题，暂时跳过头部设置
                        Debug.WriteLine("检测到百度地图API请求，但跳过头部设置以避免兼容性问题");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"设置请求头部时出错: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"WebResourceRequested事件处理出错: {ex.Message}");
            }
        }
    }
}