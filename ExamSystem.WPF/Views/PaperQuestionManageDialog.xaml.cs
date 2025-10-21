using System.Windows;
using ExamSystem.WPF.ViewModels;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace ExamSystem.WPF.Views
{
    public partial class PaperQuestionManageDialog : Window
    {
        public PaperQuestionManageDialog(PaperQuestionManageViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            
            // 订阅保存完成事件
            viewModel.SaveCompleted += OnSaveCompleted;
        }
        
        private void OnSaveCompleted(object? sender, System.EventArgs e)
        {
            DialogResult = true;
            Close();
        }
        
        private void CancelButton_Click(object? sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
        
        protected override void OnClosed(System.EventArgs e)
        {
            // 取消订阅事件
            if (DataContext is PaperQuestionManageViewModel viewModel)
            {
                viewModel.SaveCompleted -= OnSaveCompleted;
            }
            base.OnClosed(e);
        }
        
        // 分值输入校验：允许正数与最多两位小数
        private static readonly Regex ScoreRegex = new Regex(@"^\d+(\.\d{0,2})?$", RegexOptions.Compiled);

        private void ScoreTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox tb)
            {
                var proposed = InsertTextAtCaret(tb.Text, tb.SelectionStart, tb.SelectionLength, e.Text);
                e.Handled = !IsValidScore(proposed);
            }
        }

        private void ScoreTextBox_OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox tb)
            {
                if (e.DataObject.GetDataPresent(DataFormats.Text))
                {
                    var pasteText = e.DataObject.GetData(DataFormats.Text) as string ?? string.Empty;
                    var proposed = InsertTextAtCaret(tb.Text, tb.SelectionStart, tb.SelectionLength, pasteText);
                    if (!IsValidScore(proposed))
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

        private static string InsertTextAtCaret(string original, int selectionStart, int selectionLength, string inserted)
        {
            if (selectionLength > 0)
            {
                original = original.Remove(selectionStart, selectionLength);
            }
            return original.Insert(selectionStart, inserted);
        }

        private static bool IsValidScore(string text)
        {
            if (string.IsNullOrEmpty(text))
                return true; // 允许空值以便编辑
            return ScoreRegex.IsMatch(text);
        }
    }
}