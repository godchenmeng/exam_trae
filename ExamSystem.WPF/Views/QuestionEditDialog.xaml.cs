using System;
using System.Windows;
using System.Windows.Controls;
using ExamSystem.WPF.ViewModels;

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
    }
}