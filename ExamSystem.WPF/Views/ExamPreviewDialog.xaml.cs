using System;
using System.Windows;
using ExamSystem.WPF.ViewModels;

namespace ExamSystem.WPF.Views
{
    /// <summary>
    /// ExamPreviewDialog.xaml 的交互逻辑
    /// </summary>
    public partial class ExamPreviewDialog : Window
    {
        public ExamPreviewDialog()
        {
            InitializeComponent();
        }

        public ExamPreviewDialog(ExamPreviewViewModel viewModel) : this()
        {
            DataContext = viewModel;
            
            // 订阅事件
            if (viewModel != null)
            {
                viewModel.StartExamRequested += OnStartExamRequested;
                viewModel.CancelRequested += OnCancelRequested;
            }
        }

        private void OnStartExamRequested(object? sender, EventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void OnCancelRequested(object? sender, EventArgs e)
        {
            DialogResult = false;
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            // 取消订阅事件
            if (DataContext is ExamPreviewViewModel viewModel)
            {
                viewModel.StartExamRequested -= OnStartExamRequested;
                viewModel.CancelRequested -= OnCancelRequested;
            }
            
            base.OnClosed(e);
        }
    }
}