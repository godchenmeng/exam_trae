using System.Windows.Controls;
using ExamSystem.WPF.ViewModels;

namespace ExamSystem.WPF.Views
{
    /// <summary>
    /// StudentExamListView.xaml 的交互逻辑
    /// </summary>
    public partial class StudentExamListView : UserControl
    {
        public StudentExamListView(StudentExamListViewModel viewModel)
        {
            InitializeComponent();
            this.DataContext = viewModel;
        }
    }
}