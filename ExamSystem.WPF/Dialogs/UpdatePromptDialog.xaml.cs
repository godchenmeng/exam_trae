using System;
using System.Windows;

namespace ExamSystem.WPF.Dialogs
{
    public partial class UpdatePromptDialog : Window
    {
        public enum PromptChoice { UpdateNow, RemindLater, IgnoreThisVersion }

        public string LatestVersion { get; set; } = string.Empty;
        public string CurrentVersion { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;

        public PromptChoice Choice { get; private set; } = PromptChoice.RemindLater;

        public UpdatePromptDialog()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void UpdateNow_Click(object sender, RoutedEventArgs e)
        {
            Choice = PromptChoice.UpdateNow;
            DialogResult = true;
            Close();
        }

        private void RemindLater_Click(object sender, RoutedEventArgs e)
        {
            Choice = PromptChoice.RemindLater;
            DialogResult = true;
            Close();
        }

        private void Ignore_Click(object sender, RoutedEventArgs e)
        {
            Choice = PromptChoice.IgnoreThisVersion;
            DialogResult = true;
            Close();
        }
    }
}