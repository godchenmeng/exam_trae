using System.Windows;

namespace ExamSystem.WPF.Views
{
    /// <summary>
    /// ExamConfirmationDialog.xaml 的交互逻辑
    /// </summary>
    public partial class ExamConfirmationDialog : Window
    {
        public ExamConfirmationDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 考试确认结果
        /// </summary>
        public bool IsConfirmed { get; private set; } = false;

        /// <summary>
        /// 开始考试按钮点击事件
        /// </summary>
        private void StartExamButton_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = true;
            DialogResult = true;
            Close();
        }

        /// <summary>
        /// 取消按钮点击事件
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = false;
            DialogResult = false;
            Close();
        }
    }
}