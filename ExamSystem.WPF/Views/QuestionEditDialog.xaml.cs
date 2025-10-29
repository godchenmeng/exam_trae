using System;
using System.Windows;
using System.Windows.Controls;
using ExamSystem.WPF.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ExamSystem.WPF.Views
{
    /// <summary>
    /// QuestionEditDialog.xaml 的交互逻辑
    /// </summary>
    public partial class QuestionEditDialog : Window
    {
        private readonly QuestionEditViewModel _viewModel;

        public QuestionEditDialog(QuestionEditViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
            
            // 订阅保存完成事件
            _viewModel.SaveCompleted += OnSaveCompleted;
            
            // 设置窗口属性
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }

        private void OnSaveCompleted(object? sender, bool success)
        {
            if (success)
            {
                DialogResult = true;
                Close();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            // 取消订阅事件
            if (_viewModel != null)
            {
                _viewModel.SaveCompleted -= OnSaveCompleted;
            }
            base.OnClosed(e);
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.QuestionEditViewModel vm && sender is RadioButton rb && rb.DataContext is ViewModels.QuestionOptionEditViewModel option)
            {
                // 保证单选题只有一个选项被选中
                foreach (var opt in vm.Options)
                {
                    if (!ReferenceEquals(opt, option) && opt.IsCorrect)
                    {
                        opt.IsCorrect = false;
                    }
                }
                option.IsCorrect = true;
            }
        }

        private void RadioButton_Unchecked(object sender, RoutedEventArgs e)
        {
            // 单选题取消选中不做额外处理
        }

        private void OpenMapDrawingEditorButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 通过 App 获取 DI 容器
                if (Application.Current is not App app)
                {
                    MessageBox.Show("无法获取应用服务，请重试。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                var provider = app.GetServices();
                var vm = provider.GetRequiredService<MapDrawingAuthoringViewModel>();

                // 将当前对话框的基础信息传入地图编辑器
                vm.Question.BankId = _viewModel.BankId;
                vm.Question.QuestionType = Domain.Enums.QuestionType.MapDrawing;
                vm.Question.Title = string.IsNullOrWhiteSpace(_viewModel.Question.Title) ? "地图绘制题" : _viewModel.Question.Title;
                vm.Question.Content = _viewModel.Question.Content;
                vm.Question.Score = _viewModel.Question.Score > 0 ? _viewModel.Question.Score : 10m;

                // 创建并显示地图绘制编辑器窗口
                var dialog = new MapDrawingAuthoring(vm);
                if (Application.Current.MainWindow != null && Application.Current.MainWindow != dialog)
                {
                    dialog.Owner = Application.Current.MainWindow;
                }
                var result = dialog.ShowDialog();
                if (result == true)
                {
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开地图绘制编辑器失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}