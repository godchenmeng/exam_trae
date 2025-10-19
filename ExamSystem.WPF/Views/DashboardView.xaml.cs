using System;
using System.Windows;
using System.Windows.Controls;
using ExamSystem.WPF.ViewModels;

namespace ExamSystem.WPF.Views
{
    public partial class DashboardView : UserControl
    {
        public DashboardView(DashboardViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void CreateQuestionBankButton_Click(object sender, RoutedEventArgs e)
        {
            // 导航到题库管理页面并触发创建题库功能
            NavigateToQuestionBankPage();
        }

        private void AddQuestionButton_Click(object sender, RoutedEventArgs e)
        {
            // 导航到题库管理页面
            NavigateToQuestionBankPage();
        }

        private void NavigateToQuestionBankPage()
        {
            try
            {
                // 获取主窗口
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    // 切换到题库管理选项卡
                    var tabControl = mainWindow.FindName("MainTabControl") as System.Windows.Controls.TabControl;
                    if (tabControl != null)
                    {
                        // 查找题库管理选项卡（索引为1）
                        if (tabControl.Items.Count > 1)
                        {
                            tabControl.SelectedIndex = 1; // 题库管理选项卡
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导航到题库管理页面时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateExamPaperButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: 实现创建试卷功能
        }

        private void ViewStatisticsButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: 实现查看统计功能
        }

        private void UserManagementButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: 实现用户管理功能
        }

        private void SystemSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: 实现系统设置功能
        }

        private void DataBackupButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: 实现数据备份功能
        }

        private void HelpDocumentButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: 实现帮助文档功能
        }
    }
}