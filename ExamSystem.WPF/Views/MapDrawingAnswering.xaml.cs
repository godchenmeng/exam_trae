using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using ExamSystem.Domain.Models;
using ExamSystem.Domain.Entities;

namespace ExamSystem.WPF.Views
{
    public partial class MapDrawingAnswering : UserControl
    {
        private Question? _currentQuestion;
        private DateTime _answerStartTime;
        private DispatcherTimer? _timer;
        private int _timeRemainingSeconds;

        public event EventHandler<MapDrawingAnswerEventArgs>? AnswerSubmitted;

        public MapDrawingAnswering()
        {
            InitializeComponent();
            InitializeWebView();
        }

        private async void InitializeWebView()
        {
            try
            {
                await MapWebView.EnsureCoreWebView2Async();
                
                // 监听来自 JS 的消息
                MapWebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
                
                // 加载本地 HTML 页面
                var appDir = AppDomain.CurrentDomain.BaseDirectory;
                var htmlPath = Path.Combine(appDir, "Assets", "Map", "index.html");
                
                if (File.Exists(htmlPath))
                {
                    var uri = new Uri(htmlPath).AbsoluteUri;
                    MapWebView.CoreWebView2.Navigate(uri);
                }
                else
                {
                    // 如果文件不存在，显示错误页面
                    var errorHtml = "<html><body><h2>地图页面加载失败</h2><p>找不到文件: " + htmlPath + "</p></body></html>";
                    MapWebView.NavigateToString(errorHtml);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"WebView2 初始化失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var messageJson = e.TryGetWebMessageAsString();
                if (string.IsNullOrEmpty(messageJson)) return;

                var message = JsonSerializer.Deserialize<BridgeMessage>(messageJson);
                if (message == null) return;

                switch (message.MessageType)
                {
                    case "SubmitAnswer":
                        HandleSubmitAnswer(message.Payload);
                        break;
                    default:
                        // 记录未知消息类型
                        System.Diagnostics.Debug.WriteLine($"Unknown message type: {message.MessageType}");
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"处理 WebView 消息时出错: {ex.Message}");
            }
        }

        private void HandleSubmitAnswer(JsonElement payload)
        {
            try
            {
                if (_currentQuestion == null) return;

                var questionId = payload.GetProperty("questionId").GetString();
                var overlaysJson = payload.GetProperty("overlays").GetRawText();
                var drawDuration = payload.GetProperty("drawDurationSeconds").GetInt32();

                // 触发答案提交事件
                var eventArgs = new MapDrawingAnswerEventArgs
                {
                    QuestionId = questionId ?? "",
                    OverlaysJson = overlaysJson,
                    DrawDurationSeconds = drawDuration
                };

                AnswerSubmitted?.Invoke(this, eventArgs);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"处理答案提交时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public Task LoadQuestionAsync(Question question)
        {
            _currentQuestion = question;
            _answerStartTime = DateTime.Now;

            // 更新 UI
            QuestionTitleText.Text = $"题目 {question.QuestionId}: {question.QuestionType}";
            QuestionContentText.Text = question.Content ?? "";
            ScoreText.Text = $"{question.Score} 分";

            // 设置倒计时
            _timeRemainingSeconds = question.TimeLimitSeconds > 0 ? question.TimeLimitSeconds : 1800; // 默认30分钟
            StartTimer();

            // 向 WebView 发送加载题目消息
            if (MapWebView.CoreWebView2 != null)
            {
                var loadMessage = new BridgeMessage
                {
                    MessageType = "LoadQuestion",
                    Payload = JsonSerializer.SerializeToElement(new
                    {
                        questionId = question.QuestionId.ToString(),
                        config = string.IsNullOrEmpty(question.MapDrawingConfigJson) 
                            ? new { allowedTools = new[] { "Polyline", "Polygon", "Marker" } }
                            : JsonSerializer.Deserialize<object>(question.MapDrawingConfigJson),
                        guidanceOverlays = string.IsNullOrEmpty(question.GuidanceOverlaysJson)
                            ? new OverlayDTO[0]
                            : JsonSerializer.Deserialize<OverlayDTO[]>(question.GuidanceOverlaysJson)
                    })
                };

                var messageJson = JsonSerializer.Serialize(loadMessage);
                MapWebView.CoreWebView2.PostWebMessageAsString(messageJson);
            }
            
            return Task.CompletedTask;
        }

        private void StartTimer()
        {
            _timer?.Stop();
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (s, e) =>
            {
                _timeRemainingSeconds--;
                UpdateTimeDisplay();

                if (_timeRemainingSeconds <= 0)
                {
                    _timer.Stop();
                    // 时间到，自动提交
                    SubmitAnswerButton_Click(null, null);
                }
            };
            _timer.Start();
            UpdateTimeDisplay();
        }

        private void UpdateTimeDisplay()
        {
            var minutes = _timeRemainingSeconds / 60;
            var seconds = _timeRemainingSeconds % 60;
            TimeRemainingText.Text = $"{minutes:D2}:{seconds:D2}";
            
            if (_timeRemainingSeconds <= 60)
            {
                TimeRemainingText.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private void ClearAnswerButton_Click(object? sender, RoutedEventArgs e)
        {
            if (MapWebView.CoreWebView2 != null)
            {
                var clearMessage = new BridgeMessage
                {
                    MessageType = "ClearAnswer",
                    Payload = JsonSerializer.SerializeToElement(new { })
                };

                var messageJson = JsonSerializer.Serialize(clearMessage);
                MapWebView.CoreWebView2.PostWebMessageAsString(messageJson);
            }
        }

        private void SubmitAnswerButton_Click(object? sender, RoutedEventArgs e)
        {
            if (MapWebView.CoreWebView2 != null)
            {
                var submitMessage = new BridgeMessage
                {
                    MessageType = "RequestSubmit",
                    Payload = JsonSerializer.SerializeToElement(new { })
                };

                var messageJson = JsonSerializer.Serialize(submitMessage);
                MapWebView.CoreWebView2.PostWebMessageAsString(messageJson);
            }
        }

        public void Dispose()
        {
            _timer?.Stop();
            _timer = null;
        }
    }

    // 桥接消息结构
    public class BridgeMessage
    {
        public string MessageType { get; set; } = "";
        public JsonElement Payload { get; set; }
    }

    // 答案提交事件参数
    public class MapDrawingAnswerEventArgs : EventArgs
    {
        public string QuestionId { get; set; } = "";
        public string OverlaysJson { get; set; } = "";
        public int DrawDurationSeconds { get; set; }
    }
}