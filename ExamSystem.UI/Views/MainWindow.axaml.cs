using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ExamSystem.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ExamSystem.UI.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = Program.ServiceProvider.GetRequiredService<MainViewModel>();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}