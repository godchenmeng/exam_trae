using System.Windows.Controls;
using ExamSystem.WPF.ViewModels;

namespace ExamSystem.WPF.Views
{
    /// <summary>
    /// BuildingManagementView.xaml 的交互逻辑
    /// </summary>
    public partial class BuildingManagementView : UserControl
    {
        public BuildingManagementView()
        {
            InitializeComponent();
        }

        public BuildingManagementView(BuildingManagementViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }
    }
}