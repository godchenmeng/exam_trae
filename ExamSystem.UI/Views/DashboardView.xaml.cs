using System.Windows.Controls;
using ExamSystem.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ExamSystem.UI.Views
{
    /// <summary>
    /// DashboardView.xaml 的交互逻辑
    /// </summary>
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
            DataContext = App.ServiceProvider.GetRequiredService<DashboardViewModel>();
        }
    }
}