using System.Windows;
using ExamSystem.WPF.ViewModels;

namespace ExamSystem.WPF.Views
{
    public partial class PaperQuestionManageDialog : Window
    {
        public PaperQuestionManageDialog(PaperQuestionManageViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            
            // 订阅保存完成事件
            viewModel.SaveCompleted += OnSaveCompleted;
        }
        
        private void OnSaveCompleted(object sender, System.EventArgs e)
        {
            DialogResult = true;
            Close();
        }
        
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
        
        protected override void OnClosed(System.EventArgs e)
        {
            // 取消订阅事件
            if (DataContext is PaperQuestionManageViewModel viewModel)
            {
                viewModel.SaveCompleted -= OnSaveCompleted;
            }
            base.OnClosed(e);
        }
    }
}