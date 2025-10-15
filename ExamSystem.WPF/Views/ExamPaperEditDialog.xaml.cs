using ExamSystem.WPF.ViewModels;
using System.Windows;

namespace ExamSystem.WPF.Views
{
    /// <summary>
    /// ExamPaperEditDialog.xaml 的交互逻辑
    /// </summary>
    public partial class ExamPaperEditDialog : Window
    {
        private ExamPaperEditViewModel _viewModel;

        public ExamPaperEditDialog(ExamPaperEditViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            // 订阅保存完成事件
            _viewModel.SaveCompleted += OnSaveCompleted;
        }

        private void OnSaveCompleted(object sender, bool success)
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