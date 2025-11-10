using System;
using System.Windows;
using System.Windows.Controls;
using ExamSystem.WPF.ViewModels;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.WPF;

namespace ExamSystem.WPF.Views
{
    public partial class DashboardView : UserControl
    {
        // 图表在XAML中通过绑定实现，无需在代码隐藏中维护演示数据

        public DashboardView(DashboardViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            this.SizeChanged += DashboardView_SizeChanged;
            this.Unloaded += DashboardView_Unloaded;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // 首次加载时触发数据加载
            if (DataContext is DashboardViewModel vm)
            {
                if (vm.LoadDataCommand.CanExecute(null))
                {
                    vm.LoadDataCommand.Execute(null);
                }
            }

            // 初始响应式布局调整
            UpdateResponsiveLayout(this.ActualWidth);

            // 图表在XAML中绑定到ViewModel的真实数据，无需初始化演示数据
        }

        private void DashboardView_Unloaded(object sender, RoutedEventArgs e)
        {
            // 无需手动清理图表资源，WPF控件生命周期会自动释放绑定的资源
        }

        // 移除演示图表初始化，改用XAML与ViewModel绑定的真实数据

        private void DashboardView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateResponsiveLayout(e.NewSize.Width);
        }

        private void UpdateResponsiveLayout(double width)
        {
            try
            {
                // 宽度小于 900 时，KPI 改为 2 列；否则为 4 列
                if (TeacherKpiGrid != null)
                {
                    TeacherKpiGrid.Columns = width < 900 ? 2 : 4;
                }
                if (StudentKpiGrid != null)
                {
                    StudentKpiGrid.Columns = width < 900 ? 2 : 4;
                }
            }
            catch
            {
                // 忽略非关键错误
            }
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
            // 切换到题库管理选项卡
            NavigateToTab("TabQuestionBank");
        }

        private void NavigateToTab(string tabName)
        {
            try
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow == null) return;

                var tabControl = mainWindow.FindName("MainTabControl") as TabControl;
                var tabItem = mainWindow.FindName(tabName) as TabItem;

                if (tabControl != null && tabItem != null)
                {
                    tabControl.SelectedItem = tabItem;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"切换选项卡时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateExamPaperButton_Click(object sender, RoutedEventArgs e)
        {
            // 导航到试卷管理
            NavigateToTab("TabExamPaper");
        }

        private void ViewStatisticsButton_Click(object sender, RoutedEventArgs e)
        {
            // 导航到统计报表
            NavigateToTab("TabStatistics");
        }

        private void UserManagementButton_Click(object sender, RoutedEventArgs e)
        {
            // 导航到用户管理
            NavigateToTab("TabUserManagement");
        }

        private void BuildingManagementButton_Click(object sender, RoutedEventArgs e)
        {
            // 导航到建筑物管理
            NavigateToTab("TabBuildingManagement");
        }

        private void SystemSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // 暂未实现系统设置模块，先提示
            MessageBox.Show("系统设置模块暂未实现，后续将提供设置窗口。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void DataBackupButton_Click(object sender, RoutedEventArgs e)
        {
            // 暂未实现数据备份模块，先提示
            MessageBox.Show("数据备份功能暂未实现，后续将支持备份/恢复。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void HelpDocumentButton_Click(object sender, RoutedEventArgs e)
        {
            // 导航到学习资源作为帮助文档入口（临时）
            NavigateToTab("TabLearningResources");
        }
    }
}