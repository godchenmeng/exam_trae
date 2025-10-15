using System;
using System.Windows;

namespace ExamSystem.UI.Views
{
    /// <summary>
    /// 锁定时长选择对话框
    /// </summary>
    public partial class LockDurationDialog : Window
    {
        public TimeSpan LockDuration { get; private set; } = TimeSpan.FromHours(1);

        public LockDurationDialog()
        {
            InitializeComponent();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Lock1Hour.IsChecked == true)
                {
                    LockDuration = TimeSpan.FromHours(1);
                }
                else if (Lock6Hours.IsChecked == true)
                {
                    LockDuration = TimeSpan.FromHours(6);
                }
                else if (Lock24Hours.IsChecked == true)
                {
                    LockDuration = TimeSpan.FromHours(24);
                }
                else if (Lock7Days.IsChecked == true)
                {
                    LockDuration = TimeSpan.FromDays(7);
                }
                else if (LockCustom.IsChecked == true)
                {
                    if (double.TryParse(CustomHoursTextBox.Text, out double hours) && hours > 0)
                    {
                        LockDuration = TimeSpan.FromHours(hours);
                    }
                    else
                    {
                        MessageBox.Show("请输入有效的小时数！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                        CustomHoursTextBox.Focus();
                        return;
                    }
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"设置锁定时长时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}