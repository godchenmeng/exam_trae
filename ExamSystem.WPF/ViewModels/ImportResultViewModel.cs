using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ExamSystem.WPF.ViewModels
{
    /// <summary>
    /// 导入结果视图模型
    /// </summary>
    public class ImportResultViewModel : INotifyPropertyChanged
    {
        private int _totalCount;
        private int _successCount;
        private int _failureCount;
        private List<ImportedQuestionInfo> _successfulQuestions;
        private List<ImportFailureInfo> _failedQuestions;

        public int TotalCount
        {
            get => _totalCount;
            set
            {
                _totalCount = value;
                OnPropertyChanged();
            }
        }

        public int SuccessCount
        {
            get => _successCount;
            set
            {
                _successCount = value;
                OnPropertyChanged();
            }
        }

        public int FailureCount
        {
            get => _failureCount;
            set
            {
                _failureCount = value;
                OnPropertyChanged();
            }
        }

        public List<ImportedQuestionInfo> SuccessfulQuestions
        {
            get => _successfulQuestions;
            set
            {
                _successfulQuestions = value;
                OnPropertyChanged();
            }
        }

        public List<ImportFailureInfo> FailedQuestions
        {
            get => _failedQuestions;
            set
            {
                _failedQuestions = value;
                OnPropertyChanged();
            }
        }

        public ImportResultViewModel()
        {
            SuccessfulQuestions = new List<ImportedQuestionInfo>();
            FailedQuestions = new List<ImportFailureInfo>();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// 成功导入的题目信息
    /// </summary>
    public class ImportedQuestionInfo
    {
        public int RowNumber { get; set; }
        public string Title { get; set; }
        public string QuestionType { get; set; }
        public string Difficulty { get; set; }
        public int Score { get; set; }
        public string Tags { get; set; }
    }

    /// <summary>
    /// 导入失败的题目信息
    /// </summary>
    public class ImportFailureInfo
    {
        public int RowNumber { get; set; }
        public string Title { get; set; }
        public string ErrorMessage { get; set; }
        public string RawData { get; set; }
    }
}