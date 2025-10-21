using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ExamSystem.WPF.ViewModels
{
    /// <summary>
    /// 考试确认对话框的ViewModel
    /// </summary>
    public class ExamConfirmationViewModel : INotifyPropertyChanged
    {
        private string _paperTitle = string.Empty;
        private int _duration;
        private decimal _totalScore;
        private int _questionCount;
        private string _description = string.Empty;

        /// <summary>
        /// 试卷标题
        /// </summary>
        public string PaperTitle
        {
            get => _paperTitle;
            set
            {
                _paperTitle = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 考试时长（分钟）
        /// </summary>
        public int Duration
        {
            get => _duration;
            set
            {
                _duration = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 总分
        /// </summary>
        public decimal TotalScore
        {
            get => _totalScore;
            set
            {
                _totalScore = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 题目数量
        /// </summary>
        public int QuestionCount
        {
            get => _questionCount;
            set
            {
                _questionCount = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 考试说明
        /// </summary>
        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}