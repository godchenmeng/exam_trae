using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace ExamSystem.WPF.Views
{
    public partial class MapLocationPickerDialog : Window
    {
        public double? SelectedLatitude { get; private set; }
        public double? SelectedLongitude { get; private set; }
        public string? SelectedCityKey { get; private set; }

        // 初始城市（可传中文名或key）
        public string? InitialCityName { get; set; }
        public string? InitialCityKey { get; set; }

        public MapLocationPickerDialog()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await webView.EnsureCoreWebView2Async();

                var env = webView.CoreWebView2;
                env.Settings.IsStatusBarEnabled = false;
                env.Settings.AreDefaultContextMenusEnabled = false;
                env.Settings.AreDevToolsEnabled = true; // 调试方便，如需关闭可设为 false

                // 将本地 Assets/BaiduMap 映射为虚拟主机 appassets
                var assetsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "BaiduMap");
                env.SetVirtualHostNameToFolderMapping("appassets", assetsPath, CoreWebView2HostResourceAccessKind.Allow);

                // 监听来自JS的消息
                env.WebMessageReceived += OnWebMessageReceived;

                // 导航到新建页面
                webView.Source = new Uri("https://appassets/location-picker.html");

                env.NavigationCompleted += (s, args) =>
                {
                    // 发送初始城市
                    try
                    {
                        var payload = new
                        {
                            type = "setCity",
                            payload = new { cityKey = InitialCityKey, cityName = InitialCityName }
                        };
                        var json = JsonSerializer.Serialize(payload);
                        env.PostWebMessageAsJson(json);
                    }
                    catch { }
                };
            }
            catch (Exception ex)
            {
                txtStatus.Text = "WebView 初始化失败: " + ex.Message;
            }
        }

        private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                // JS 端通过 window.chrome.webview.postMessage 发送的是对象，
                // 因此使用 WebMessageAsJson 获取，而不是 TryGetWebMessageAsString()
                var json = e.WebMessageAsJson;
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var type = root.GetProperty("type").GetString();
                var payload = root.GetProperty("payload");

                if (type == "locationSelected")
                {
                    double lat = payload.GetProperty("lat").GetDouble();
                    double lng = payload.GetProperty("lng").GetDouble();
                    string? cityKey = payload.TryGetProperty("cityKey", out var ck) ? ck.GetString() : null;

                    SelectedLatitude = lat;
                    SelectedLongitude = lng;
                    SelectedCityKey = cityKey;

                    txtStatus.Text = $"已选: {lng:F6}, {lat:F6}";
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                txtStatus.Text = "处理消息失败: " + ex.Message;
            }
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}