using System;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using ExamSystem.WPF.ViewModels;

namespace ExamSystem.WPF.Views
{
    /// <summary>
    /// 地图绘制题教师阅卷页面
    /// </summary>
    public partial class MapDrawingReviewView : UserControl
    {
        private MapDrawingReviewViewModel? _viewModel;

        public MapDrawingReviewView()
        {
            InitializeComponent();
            this.DataContextChanged += OnDataContextChanged;
        }

        /// <summary>
        /// 数据上下文变更事件处理
        /// </summary>
        private void OnDataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            _viewModel = DataContext as MapDrawingReviewViewModel;
        }

        /// <summary>
        /// 参考答案地图WebView导航完成事件处理
        /// </summary>
        private async void ReferenceMapWebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            try
            {
                var webView = sender as WebView2;
                if (webView?.CoreWebView2 != null && e.IsSuccess && _viewModel != null)
                {
                    // 注册消息接收处理程序
                    webView.CoreWebView2.WebMessageReceived += OnReferenceMapWebMessageReceived;
                    
                    // 初始化参考答案地图
                    await _viewModel.InitializeReferenceMapAsync(webView);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"参考答案地图WebView导航完成处理失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 学生答案地图WebView导航完成事件处理
        /// </summary>
        private async void StudentMapWebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            try
            {
                var webView = sender as WebView2;
                if (webView?.CoreWebView2 != null && e.IsSuccess && _viewModel != null)
                {
                    // 注册消息接收处理程序
                    webView.CoreWebView2.WebMessageReceived += OnStudentMapWebMessageReceived;
                    
                    // 初始化学生答案地图
                    await _viewModel.InitializeStudentMapAsync(webView);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"学生答案地图WebView导航完成处理失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 对比地图WebView导航完成事件处理
        /// </summary>
        private async void ComparisonMapWebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            try
            {
                var webView = sender as WebView2;
                if (webView?.CoreWebView2 != null && e.IsSuccess && _viewModel != null)
                {
                    // 注册消息接收处理程序
                    webView.CoreWebView2.WebMessageReceived += OnComparisonMapWebMessageReceived;
                    
                    // 初始化对比地图
                    await _viewModel.InitializeComparisonMapAsync(webView);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"对比地图WebView导航完成处理失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理来自参考答案地图WebView的消息
        /// </summary>
        private void OnReferenceMapWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var message = e.TryGetWebMessageAsString();
                if (!string.IsNullOrEmpty(message) && _viewModel != null)
                {
                    _viewModel.HandleReferenceMapMessage(message);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"处理参考答案地图WebView消息失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理来自学生答案地图WebView的消息
        /// </summary>
        private void OnStudentMapWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var message = e.TryGetWebMessageAsString();
                if (!string.IsNullOrEmpty(message) && _viewModel != null)
                {
                    _viewModel.HandleStudentMapMessage(message);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"处理学生答案地图WebView消息失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理来自对比地图WebView的消息
        /// </summary>
        private void OnComparisonMapWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var message = e.TryGetWebMessageAsString();
                if (!string.IsNullOrEmpty(message) && _viewModel != null)
                {
                    _viewModel.HandleComparisonMapMessage(message);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"处理对比地图WebView消息失败: {ex.Message}");
            }
        }
    }
}