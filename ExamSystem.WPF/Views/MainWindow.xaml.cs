using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ExamSystem.WPF.Views
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MainWindow> _logger;

        public MainWindow(IServiceProvider serviceProvider, ILogger<MainWindow> logger)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;
            _logger = logger;
            
            LoadPages();
        }

        private void LoadPages()
        {
            try
            {
                // 加载各个页面
                var questionBankView = _serviceProvider.GetRequiredService<QuestionBankView>();
                QuestionBankFrame.Content = questionBankView;

                var examPaperView = _serviceProvider.GetRequiredService<ExamPaperView>();
                ExamPaperFrame.Content = examPaperView;

                var examView = _serviceProvider.GetRequiredService<ExamView>();
                ExamFrame.Content = examView;

                var examResultView = _serviceProvider.GetRequiredService<ExamResultView>();
                ExamResultFrame.Content = examResultView;

                _logger.LogInformation("所有页面加载完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载页面时发生错误");
                MessageBox.Show($"加载页面时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}