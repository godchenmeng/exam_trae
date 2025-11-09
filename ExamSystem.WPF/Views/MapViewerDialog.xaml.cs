using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace ExamSystem.WPF.Views
{
    /// <summary>
    /// 在地图中查看指定建筑位置的对话框。
    /// </summary>
    public partial class MapViewerDialog : Window
    {
        public string? BuildingName { get; set; }
        public string? CityName { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public int InitialZoom { get; set; } = 16;

        public MapViewerDialog()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            //txtTitle.Text = $"建筑：{BuildingName ?? "-"}";
            //txtCity.Text = $"城市：{CityName ?? "-"}";
            //txtCoord.Text = (Latitude.HasValue && Longitude.HasValue)
            //    ? $"坐标：{Longitude:F6}, {Latitude:F6}"
            //    : "坐标：-";

            try
            {
                await webView.EnsureCoreWebView2Async();
                var env = webView.CoreWebView2;
                env.Settings.IsStatusBarEnabled = false;
                env.Settings.AreDefaultContextMenusEnabled = false;
                env.Settings.AreDevToolsEnabled = true;

                var assetsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "BaiduMap");
                env.SetVirtualHostNameToFolderMapping("appassets", assetsPath, CoreWebView2HostResourceAccessKind.Allow);

                webView.Source = new Uri("https://appassets/building-viewer.html");

                env.NavigationCompleted += (s, args) =>
                {
                    try
                    {
                        var payload = new
                        {
                            type = "showBuilding",
                            payload = new
                            {
                                name = BuildingName,
                                city = CityName,
                                lat = Latitude,
                                lng = Longitude,
                                zoom = InitialZoom
                            }
                        };
                        var json = JsonSerializer.Serialize(payload);
                        env.PostWebMessageAsJson(json);
                    }
                    catch { }
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show("WebView 初始化失败: " + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}