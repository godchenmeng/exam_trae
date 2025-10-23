using System.Windows;
using System.Windows.Controls;
using ExamSystem.WPF.Dialogs;
using ExamSystem.WPF.ViewModels;

namespace ExamSystem.WPF.Views
{
    /// <summary>
    /// StudentExamResultView.xaml 的交互逻辑
    /// </summary>
    public partial class StudentExamResultView : UserControl
    {
        public StudentExamResultView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // 取消订阅旧的 ViewModel 事件
            if (e.OldValue is StudentExamResultViewModel oldViewModel)
            {
                oldViewModel.ExamResultDetailRequested -= OnExamResultDetailRequested;
            }

            // 订阅新的 ViewModel 事件
            if (e.NewValue is StudentExamResultViewModel newViewModel)
            {
                newViewModel.ExamResultDetailRequested += OnExamResultDetailRequested;
            }
        }

        /// <summary>
        /// 处理查看考试详情请求
        /// </summary>
        private void OnExamResultDetailRequested(object? sender, ExamResultDetailEventArgs e)
        {
            try
            {
                var dialog = new ExamRecordViewDialog(e.ExamRecordId)
                {
                    Owner = Window.GetWindow(this)
                };
                dialog.ShowDialog();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"打开考试详情对话框失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}