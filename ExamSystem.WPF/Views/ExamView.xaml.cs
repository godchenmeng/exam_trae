using System;
using System.Windows;
using System.Windows.Controls;
using ExamSystem.WPF.ViewModels;

namespace ExamSystem.WPF.Views
{
    /// <summary>
    /// ExamView.xaml 的交互逻辑
    /// </summary>
    public partial class ExamView : UserControl
    {
        public ExamView()
        {
            InitializeComponent();
        }

        public ExamView(ExamViewModel viewModel) : this()
        {
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
            // 取消订阅事件
            if (DataContext is ExamViewModel viewModel)
            {
                viewModel.ExamCompleted -= OnExamCompleted;
                viewModel.ExamTimeout -= OnExamTimeout;
                viewModel.Dispose();
            }
        }
    }
}