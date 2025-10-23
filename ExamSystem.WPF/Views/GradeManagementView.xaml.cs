using ExamSystem.WPF.Dialogs;
using ExamSystem.WPF.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

namespace ExamSystem.WPF.Views
{
    /// <summary>
    /// GradeManagementView.xaml 的交互逻辑
    /// </summary>
    public partial class GradeManagementView : UserControl
    {
        public GradeManagementView()
        {
            InitializeComponent();
            
            // 将事件订阅放到 Loaded 以确保 DataContext 已正确设置
            Loaded += GradeManagementView_Loaded;
        }

        private async void GradeManagementView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is GradeManagementViewModel viewModel)
            {
                // 防止重复订阅：先尝试取消再订阅（若未订阅，-= 无副作用）
                viewModel.ViewRecordRequested -= OnViewRecordRequested;
                viewModel.GradeRecordRequested -= OnGradeRecordRequested;
                viewModel.ViewRecordRequested += OnViewRecordRequested;
                viewModel.GradeRecordRequested += OnGradeRecordRequested;

                // 默认加载数据
                await viewModel.LoadRecordsAsync();
            }
        }

        private void OnViewRecordRequested(object? sender, int recordId)
        {
            try
            {
                var dlg = new ExamRecordViewDialog(recordId)
                {
                    Owner = Window.GetWindow(this)
                };
                dlg.ShowDialog();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"打开试卷查看对话框失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void OnGradeRecordRequested(object? sender, int recordId)
        {
            try
            {
                // 优先从承载此视图的窗口获取主窗口实例，避免 Application.Current.MainWindow 未更新的情况
                var ownerWindow = Window.GetWindow(this) as MainWindow ?? Application.Current.MainWindow as MainWindow;
                var currentUserId = ownerWindow?.CurrentUser?.UserId ?? 0;

                if (currentUserId == 0)
                {
                    MessageBox.Show("无法获取当前用户信息。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 打开评分对话框
                var gradeDialog = new GradeAnswerDialog(recordId, currentUserId);
                gradeDialog.Owner = Window.GetWindow(this);
                gradeDialog.ShowDialog();

                // 评分完成后刷新列表
                if (DataContext is GradeManagementViewModel viewModel)
                {
                    await viewModel.LoadRecordsAsync();
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"打开评分对话框失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}