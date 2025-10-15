using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ExamSystem.UI.Views;
using Microsoft.Extensions.DependencyInjection;

namespace ExamSystem.UI
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // 创建并显示登录窗口
                var loginWindow = Program.ServiceProvider.GetRequiredService<LoginWindow>();
                desktop.MainWindow = loginWindow;
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}