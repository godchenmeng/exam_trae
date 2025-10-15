using System.Windows;
using ExamSystem.WPF.ViewModels;

namespace ExamSystem.WPF.Views
{
    /// <summary>
    /// ExamView.xaml 的交互逻辑
    /// </summary>
    public partial class ExamView : Window
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
        }

        private void OnExamCompleted(object? sender, EventArgs e)
        {
            // 考试完成，关闭窗口
            DialogResult = true;
            Close();
        }

        private void OnExamTimeout(object? sender, EventArgs e)
        {
            // 考试超时，显示提示并关闭窗口
            MessageBox.Show("考试时间已到，系统将自动提交您的答案。", "考试超时", 
                MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            // 取消订阅事件
            if (DataContext is ExamViewModel viewModel)
            {
                viewModel.ExamCompleted -= OnExamCompleted;
                viewModel.ExamTimeout -= OnExamTimeout;
                viewModel.Dispose();
            }
            
            base.OnClosed(e);
        }

        // 防止用户通过Alt+F4或其他方式关闭窗口
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (DataContext is ExamViewModel viewModel && viewModel.IsExamInProgress)
            {
                var result = MessageBox.Show("考试正在进行中，确定要退出吗？退出后答案将自动保存。", 
                    "确认退出", MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                    return;
                }
                
                // 保存当前答案
                viewModel.SaveCurrentAnswerCommand?.Execute(null);
            }
            
            base.OnClosing(e);
        }
    }
}