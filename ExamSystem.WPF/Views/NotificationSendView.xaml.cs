using System.Windows.Controls;
using ExamSystem.WPF.ViewModels;

namespace ExamSystem.WPF.Views
{
    /// <summary>
    /// NotificationSendView.xaml 的交互逻辑
    /// </summary>
    public partial class NotificationSendView : UserControl
    {
        public NotificationSendView()
        {
            InitializeComponent();
        }

        public NotificationSendView(NotificationSendViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}