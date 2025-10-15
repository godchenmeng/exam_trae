using System.Windows;
using ExamSystem.UI.ViewModels;

namespace ExamSystem.UI.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            // 从依赖注入容器获取MainViewModel
            var mainViewModel = Program.ServiceProvider.GetService(typeof(MainViewModel)) as MainViewModel;
            DataContext = mainViewModel;
        }
    }
}