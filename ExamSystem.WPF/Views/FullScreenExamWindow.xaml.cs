using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using ExamSystem.WPF.ViewModels;

namespace ExamSystem.WPF.Views
{
    /// <summary>
    /// FullScreenExamWindow.xaml 的交互逻辑
    /// </summary>
    public partial class FullScreenExamWindow : Window
    {
        private bool _suppressOptionEvents = false;
        
        public FullScreenExamWindow()
        {
            InitializeComponent();
            
            // 禁用Alt+Tab、Alt+F4等快捷键
            this.KeyDown += Window_KeyDown;
            
            // 窗口关闭事件
            this.Closing += FullScreenExamWindow_Closing;
        }

        private void SingleChoiceOption_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton && radioButton.Tag is OptionViewModel option)
            {
                var viewModel = DataContext as FullScreenExamViewModel;
                if (viewModel != null)
                {
                    var beforeSelected = viewModel.CurrentQuestion?.Options != null
                        ? System.Linq.Enumerable.Count(viewModel.CurrentQuestion.Options, o => o.IsSelected)
                        : 0;
                    Serilog.Log.Information("UI: SingleChoiceOption_Click begin: QIndex={QIndex}, Option={Label}, beforeSelected={Before}", viewModel.CurrentQuestionIndex, option.Label, beforeSelected);

                    try
                    {
                        _suppressOptionEvents = true; // 抑制多选事件在单选更新期间被触发
                        viewModel.SelectSingleOptionCommand?.Execute(option);
                    }
                    finally
                    {
                        _suppressOptionEvents = false;
                    }

                    var afterSelected = viewModel.CurrentQuestion?.Options != null
                        ? System.Linq.Enumerable.Count(viewModel.CurrentQuestion.Options, o => o.IsSelected)
                        : 0;
                    Serilog.Log.Information("UI: SingleChoiceOption_Click end: QIndex={QIndex}, Option={Label}, afterSelected={After}", viewModel.CurrentQuestionIndex, option.Label, afterSelected);
                }
                else
                {
                    // 兜底执行，避免 DataContext 为空导致点击无效
                    viewModel?.SelectSingleOptionCommand?.Execute(option);
                }
            }
        }

        private void MultipleChoiceOption_Checked(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as FullScreenExamViewModel;
            if (viewModel == null)
            {
                e.Handled = true;
                return;
            }

            if (_suppressOptionEvents)
            {
                Serilog.Log.Debug("UI: MultipleChoiceOption_Checked suppressed by flag");
                e.Handled = true;
                return;
            }

            if (!viewModel.IsMultipleChoice)
            {
                Serilog.Log.Debug("UI: MultipleChoiceOption_Checked ignored, current question is not multiple choice");
                e.Handled = true;
                return;
            }

            if (sender is CheckBox checkBox && checkBox.DataContext is OptionViewModel option)
            {
                Serilog.Log.Debug("UI: MultipleChoiceOption_Checked begin: Option={Label}", option.Label);
                try
                {
                    _suppressOptionEvents = true;
                    viewModel.SetMultipleOption(option, true);
                }
                finally
                {
                    _suppressOptionEvents = false;
                }
                e.Handled = true;
                Serilog.Log.Debug("UI: MultipleChoiceOption_Checked end: Option={Label}", option.Label);
            }
        }

        private void MultipleChoiceOption_Unchecked(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as FullScreenExamViewModel;
            if (viewModel == null)
            {
                e.Handled = true;
                return;
            }

            if (_suppressOptionEvents)
            {
                Serilog.Log.Debug("UI: MultipleChoiceOption_Unchecked suppressed by flag");
                e.Handled = true;
                return;
            }

            if (!viewModel.IsMultipleChoice)
            {
                Serilog.Log.Debug("UI: MultipleChoiceOption_Unchecked ignored, current question is not multiple choice");
                e.Handled = true;
                return;
            }

            if (sender is CheckBox checkBox && checkBox.DataContext is OptionViewModel option)
            {
                Serilog.Log.Debug("UI: MultipleChoiceOption_Unchecked begin: Option={Label}", option.Label);
                try
                {
                    _suppressOptionEvents = true;
                    viewModel.SetMultipleOption(option, false);
                }
                finally
                {
                    _suppressOptionEvents = false;
                }
                e.Handled = true;
                Serilog.Log.Debug("UI: MultipleChoiceOption_Unchecked end: Option={Label}", option.Label);
            }
        }

        private void TrueFalseOption_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton && radioButton.Tag is string value)
            {
                var viewModel = DataContext as FullScreenExamViewModel;
                viewModel?.SelectTrueFalseCommand?.Execute(value);
            }
        }

        /// <summary>
        /// 键盘按键事件处理
        /// </summary>
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // 禁用一些可能导致退出全屏的快捷键
            if (e.Key == Key.F4 && (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
            {
                e.Handled = true; // 禁用Alt+F4
            }
            else if (e.Key == Key.Tab && (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
            {
                e.Handled = true; // 禁用Alt+Tab
            }
            else if (e.Key == Key.Escape)
            {
                e.Handled = true; // 禁用Esc键
            }
            else if (e.Key == Key.F11)
            {
                e.Handled = true; // 禁用F11全屏切换
            }
            else if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                // 禁用一些Ctrl组合键
                if (e.Key == Key.N || e.Key == Key.T || e.Key == Key.W || 
                    e.Key == Key.R || e.Key == Key.F5 || e.Key == Key.L)
                {
                    e.Handled = true;
                }
            }
            else if ((Keyboard.Modifiers & ModifierKeys.Windows) == ModifierKeys.Windows)
            {
                e.Handled = true; // 禁用Windows键组合
            }
        }

        /// <summary>
        /// 窗口关闭事件处理
        /// </summary>
        private void FullScreenExamWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 如果是通过ViewModel的退出命令关闭，则允许关闭
            if (DataContext is FullScreenExamViewModel viewModel && viewModel.IsExitConfirmed)
            {
                return;
            }

            // 否则弹出确认对话框
            var result = MessageBox.Show(
                "确定要退出考试吗？退出后将无法继续答题。",
                "退出确认",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (result == MessageBoxResult.No)
            {
                e.Cancel = true; // 取消关闭
            }
            else
            {
                // 用户确认退出，通知ViewModel
                if (DataContext is FullScreenExamViewModel vm)
                {
                    vm.ForceExit();
                }
            }
        }

        /// <summary>
        /// 设置考试数据
        /// </summary>
        public void SetExamData(int paperId, string paperTitle)
        {
            var viewModel = new FullScreenExamViewModel(paperId, paperTitle);
            DataContext = viewModel;
            
            // 订阅退出事件
            viewModel.ExitRequested += (s, e) =>
            {
                this.Close();
            };
        }
    }
}