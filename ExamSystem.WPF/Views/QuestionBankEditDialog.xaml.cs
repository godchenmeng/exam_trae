using System.Windows;
using ExamSystem.WPF.ViewModels;

namespace ExamSystem.WPF.Views
{
    /// <summary>
    /// QuestionBankEditDialog.xaml 的交互逻辑
    /// </summary>
    public partial class QuestionBankEditDialog : Window
    {
        public QuestionBankEditDialog()
        {
            InitializeComponent();
        }

        public QuestionBankEditDialog(QuestionBankEditViewModel viewModel) : this()
        {
            DataContext = viewModel;
            
            // 订阅保存成功事件
            if (viewModel != null)
            {
                viewModel.SaveCompleted += OnSaveCompleted;
            }
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

        protected override void OnClosed(System.EventArgs e)
        {
            // 取消订阅事件
            if (DataContext is QuestionBankEditViewModel viewModel)
            {
                viewModel.SaveCompleted -= OnSaveCompleted;
            }
            base.OnClosed(e);
        }
    }
}