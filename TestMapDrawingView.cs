using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;
using ExamSystem.WPF.Views;

namespace ExamSystem.WPF.Test
{
    /// <summary>
    /// 地图绘制题视图测试程序
    /// 用于验证 WebView2 桥接和基本功能
    /// </summary>
    public partial class TestMapDrawingWindow : Window
    {
        private MapDrawingAnswering _mapView;

        public TestMapDrawingWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Title = "地图绘制题测试";
            Width = 1200;
            Height = 800;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // 测试按钮栏
            var buttonPanel = new StackPanel 
            { 
                Orientation = Orientation.Horizontal, 
                Margin = new Thickness(10),
                VerticalAlignment = VerticalAlignment.Center
            };

            var loadTestButton = new Button 
            { 
                Content = "加载测试题目", 
                Margin = new Thickness(0, 0, 10, 0),
                Padding = new Thickness(10, 5)
            };
            loadTestButton.Click += LoadTestButton_Click;

            buttonPanel.Children.Add(loadTestButton);
            Grid.SetRow(buttonPanel, 0);

            // 地图绘制视图
            _mapView = new MapDrawingAnswering();
            _mapView.AnswerSubmitted += OnAnswerSubmitted;
            Grid.SetRow(_mapView, 1);

            grid.Children.Add(buttonPanel);
            grid.Children.Add(_mapView);

            Content = grid;
        }

        private async void LoadTestButton_Click(object sender, RoutedEventArgs e)
        {
            // 创建测试题目
            var testQuestion = new Question
            {
                Id = 1001,
                Type = QuestionType.MapDrawing,
                Content = "请在地图上绘制消防车最优路径，从消防站到火灾现场，避开拥堵路段。",
                Score = 20,
                TimeLimitSeconds = 300, // 5分钟测试
                MapDrawingConfigJson = """
                {
                    "allowedTools": ["Polyline", "Marker"],
                    "mapCenter": { "lng": 116.404, "lat": 39.915 },
                    "mapZoom": 12
                }
                """,
                GuidanceOverlaysJson = """
                [
                    {
                        "id": "fire_station",
                        "type": "Marker",
                        "editable": false,
                        "visible": true,
                        "geometry": { "lng": 116.400, "lat": 39.910 },
                        "style": { "iconKey": "fire_station" },
                        "meta": { "label": "消防站", "category": "guidance" }
                    },
                    {
                        "id": "fire_scene",
                        "type": "Marker", 
                        "editable": false,
                        "visible": true,
                        "geometry": { "lng": 116.420, "lat": 39.925 },
                        "style": { "iconKey": "fire" },
                        "meta": { "label": "火灾现场", "category": "guidance" }
                    }
                ]
                """
            };

            await _mapView.LoadQuestionAsync(testQuestion);
            MessageBox.Show("测试题目已加载，请在地图中进行绘制操作。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnAnswerSubmitted(object? sender, MapDrawingAnswerEventArgs e)
        {
            var message = $"答案已提交！\n\n" +
                         $"题目ID: {e.QuestionId}\n" +
                         $"作答时长: {e.DrawDurationSeconds} 秒\n" +
                         $"覆盖物数据: {e.OverlaysJson}";

            MessageBox.Show(message, "答案提交", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        protected override void OnClosed(EventArgs e)
        {
            _mapView?.Dispose();
            base.OnClosed(e);
        }
    }
}

// 控制台启动程序
public class Program
{
    [STAThread]
    public static void Main()
    {
        var app = new Application();
        var window = new ExamSystem.WPF.Test.TestMapDrawingWindow();
        app.Run(window);
    }
}