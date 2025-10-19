using ExamSystem.WPF.ViewModels;
using System;
using System.Windows;
using System.Windows.Input;
using System.Text.RegularExpressions;

namespace ExamSystem.WPF.Views
{
    /// <summary>
    /// ExamPaperEditDialog.xaml 的交互逻辑
    /// </summary>
    public partial class ExamPaperEditDialog : Window
    {
        private ExamPaperEditViewModel _viewModel;

        public ExamPaperEditDialog(ExamPaperEditViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            // 订阅保存完成事件
            _viewModel.SaveCompleted += OnSaveCompleted;
        }

        private void OnSaveCompleted(object sender, bool success)
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

        // 限制文本框仅输入数字（键入和粘贴）
        private static readonly Regex NumericRegex = new Regex("^[0-9]+$");

        private void NumericOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !NumericRegex.IsMatch(e.Text);
        }

        private void NumericOnly_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                var text = (string)e.DataObject.GetData(typeof(string));
                if (!NumericRegex.IsMatch(text))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }
    }
}