using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ExamSystem.UI.Views;

namespace ExamSystem.UI
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // 确保数据库已创建
            using (var scope = Program.ServiceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ExamSystem.Data.ExamDbContext>();
                dbContext.Database.EnsureCreated();
            }
        }
    }
}