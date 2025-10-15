using System.Windows;
using ExamSystem.WPF.ViewModels;

namespace ExamSystem.WPF.Views
{
    /// <summary>
    /// QuestionEditDialog.xaml 的交互逻辑
    /// </summary>
    public partial class QuestionEditDialog : Window
    {
        private readonly QuestionEditViewModel _viewModel;

        public QuestionEditDialog(QuestionEditViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
            
            // 订阅保存完成事件
            _viewModel.SaveCompleted += OnSaveCompleted;
            
            // 设置窗口属性
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }

        private void OnSaveCompleted(object? sender, bool success)
        {
            if (success)
            {
                DialogResult = true;
                Close();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            // 取消订阅事件
            if (_viewModel != null)
            {
                _viewModel.SaveCompleted -= OnSaveCompleted;
            }
            base.OnClosed(e);
        }
    }
}