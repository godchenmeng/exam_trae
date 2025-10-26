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
        public DashboardView(DashboardViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            this.SizeChanged += DashboardView_SizeChanged;
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

            // 初始化图表演示数据（后续将绑定到 ViewModel 数据）
            InitializeCharts();
        }

        private void InitializeCharts()
        {
            try
            {
                // 管理员趋势区：近7天用户增长（示例数据）
                var adminChart = new CartesianChart
                {
                    Height = 220,
                    Series = new ISeries[]
                    {
                        new LineSeries<double>
                        {
                            Values = new double[] { 12, 18, 15, 22, 28, 30, 35 },
                            Name = "新增用户",
                            GeometrySize = 6
                        },
                        new ColumnSeries<double>
                        {
                            Values = new double[] { 5, 8, 6, 10, 12, 9, 11 },
                            Name = "发布试卷",
                        }
                    }
                };
                AdminTrendChartHost.Child = adminChart;

                // 教师试卷表现：Top5试卷平均分（示例数据）
                var teacherChart = new CartesianChart
                {
                    Height = 220,
                    Series = new ISeries[]
                    {
                        new ColumnSeries<double>
                        {
                            Values = new double[] { 82, 75, 90, 68, 88 },
                            Name = "平均分"
                        }
                    }
                };
                TeacherPerformanceChartHost.Child = teacherChart;
            }
            catch (Exception)
            {
                // 非关键路径，忽略图表初始化错误
            }
        }

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