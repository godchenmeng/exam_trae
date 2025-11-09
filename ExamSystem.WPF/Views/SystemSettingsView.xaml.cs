using System.Windows.Controls;
using ExamSystem.WPF.ViewModels;

namespace ExamSystem.WPF.Views
{
  /// <summary>
  /// SystemSettingsView.xaml 的交互逻辑
  /// </summary>
  public partial class SystemSettingsView : UserControl
  {
    public SystemSettingsView()
    {
      InitializeComponent();
      DataContext = new SystemSettingsViewModel();
    }
  }
}