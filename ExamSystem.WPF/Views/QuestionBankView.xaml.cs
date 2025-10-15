using ExamSystem.WPF.ViewModels;
using System.Windows.Controls;

namespace ExamSystem.WPF.Views;

/// <summary>
/// QuestionBankView.xaml 的交互逻辑
/// </summary>
public partial class QuestionBankView : UserControl
{
    public QuestionBankView()
    {
        InitializeComponent();
    }

    public QuestionBankView(QuestionBankViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
}