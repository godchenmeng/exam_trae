using System.Windows.Controls;
using ExamSystem.UI.ViewModels;

namespace ExamSystem.UI.Views
{
    /// <summary>
    /// UserManagementView.xaml 的交互逻辑
    /// </summary>
    public partial class UserManagementView : UserControl
    {
        public UserManagementView()
        {
            InitializeComponent();
            
            // 设置数据上下文
            DataContext = App.ServiceProvider.GetService(typeof(UserManagementViewModel));
        }
    }
}