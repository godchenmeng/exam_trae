using ExamSystem.WPF.ViewModels;
using System.Windows.Controls;

namespace ExamSystem.WPF.Views
{
    /// <summary>
    /// ExamPaperView.xaml 的交互逻辑
    /// </summary>
    public partial class ExamPaperView : UserControl
    {
        public ExamPaperView()
        {
            InitializeComponent();
        }

        public ExamPaperView(ExamPaperViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }
    }
}