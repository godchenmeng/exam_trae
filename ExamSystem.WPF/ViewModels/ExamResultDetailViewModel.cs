using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using ExamSystem.Domain.Entities;
using ExamSystem.Services.Interfaces;
using ExamSystem.WPF.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace ExamSystem.WPF.ViewModels
{
    public class ExamResultDetailViewModel : INotifyPropertyChanged
    {
        private readonly IExamService _examService;
        private readonly IServiceProvider _serviceProvider;

        private string _examTitle = string.Empty;
        private DateTime _examDate;
        private string _duration = string.Empty;
        private int _totalQuestions;
        private string _status = string.Empty;
        private double _totalScore;
        private double _score;
        private double _accuracyRate;
        private int _correctCount;
        private int _wrongCount;
        private string _teacherComment = string.Empty;
        private bool _hasTeacherComment;
        private bool _isLoading;

        public ExamResultDetailViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _examService = serviceProvider.GetRequiredService<IExamService>();
            
            QuestionDetails = new ObservableCollection<QuestionDetailViewModel>();
            
            BackCommand = new RelayCommand(ExecuteBack);
            ExportReportCommand = new RelayCommand(ExecuteExportReport);
        }

        #region Properties

        public string ExamTitle
        {
            get => _examTitle;
            set => SetProperty(ref _examTitle, value);
        }

        public DateTime ExamDate
        {
            get => _examDate;
            set => SetProperty(ref _examDate, value);
        }

        public string Duration
        {
            get => _duration;
            set => SetProperty(ref _duration, value);
        }

        public int TotalQuestions
        {
            get => _totalQuestions;
            set => SetProperty(ref _totalQuestions, value);
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public double TotalScore
        {
            get => _totalScore;
            set => SetProperty(ref _totalScore, value);
        }

        public double Score
        {
            get => _score;
            set
            {
                SetProperty(ref _score, value);
                UpdateAccuracyRate();
            }
        }

        public double AccuracyRate
        {
            get => _accuracyRate;
            private set => SetProperty(ref _accuracyRate, value);
        }

        public int CorrectCount
        {
            get => _correctCount;
            set => SetProperty(ref _correctCount, value);
        }

        public int WrongCount
        {
            get => _wrongCount;
            set => SetProperty(ref _wrongCount, value);
        }

        public string TeacherComment
        {
            get => _teacherComment;
            set
            {
                SetProperty(ref _teacherComment, value);
                HasTeacherComment = !string.IsNullOrWhiteSpace(value);
            }
        }

        public bool HasTeacherComment
        {
            get => _hasTeacherComment;
            private set => SetProperty(ref _hasTeacherComment, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ObservableCollection<QuestionDetailViewModel> QuestionDetails { get; }

        #endregion

        #region Commands

        public ICommand BackCommand { get; }
        public ICommand ExportReportCommand { get; }

        public event EventHandler? BackRequested;

        private void ExecuteBack()
        {
            BackRequested?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Events
// Duplicate BackRequested event removed; using the nullable event declared in Commands region
// public event EventHandler BackRequested;
#endregion

        #region Public Methods

        public async Task LoadExamResultDetailAsync(int examRecordId)
        {
            try
            {
                IsLoading = true;
                
                // 模拟加载考试结果详情数据
                await Task.Delay(500);
                
                // 这里应该调用实际的服务方法获取考试结果详情
                // var examResult = await _examService.GetExamResultDetailAsync(examRecordId);
                
                // 模拟数据
                LoadMockExamResultDetail(examRecordId);
            }
            catch (Exception ex)
            {
                // 处理异常
                System.Windows.MessageBox.Show($"加载考试结果详情失败：{ex.Message}", "错误", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Private Methods

        private void LoadMockExamResultDetail(int examRecordId)
        {
            // 模拟考试结果详情数据
            ExamTitle = "高等数学期中考试";
            ExamDate = DateTime.Now.AddDays(-7);
            Duration = "120分钟";
            TotalQuestions = 20;
            Status = "已完成";
            TotalScore = 100;
            Score = 85;
            CorrectCount = 17;
            WrongCount = 3;
            TeacherComment = "整体表现良好，基础知识掌握扎实。在复合函数求导方面还需要加强练习，建议多做相关题目。";

            QuestionDetails.Clear();

            // 模拟题目详情
            for (int i = 1; i <= TotalQuestions; i++)
            {
                var questionDetail = new QuestionDetailViewModel
                {
                    QuestionNumber = i,
                    QuestionType = i <= 10 ? "单选题" : i <= 15 ? "多选题" : "简答题",
                    Score = i <= 15 ? 3 : 10,
                    Content = $"这是第{i}题的题目内容，用于测试学生对相关知识点的掌握程度...",
                    CorrectAnswer = i <= 15 ? "A" : "这是标准答案的详细内容...",
                    StudentAnswer = i <= 17 ? (i <= 15 ? "A" : "这是学生的答案内容...") : "",
                    EarnedScore = i <= 17 ? (i <= 15 ? 3 : 8) : 0,
                    Explanation = "这是题目的详细解析，帮助学生理解解题思路和相关知识点..."
                };

                // 为选择题添加选项
                if (i <= 15)
                {
                    questionDetail.Options.Add(new OptionDetailViewModel { Text = "A. 选项A内容", IsCorrect = true, IsStudentAnswer = i <= 17 });
                    questionDetail.Options.Add(new OptionDetailViewModel { Text = "B. 选项B内容", IsCorrect = false, IsStudentAnswer = false });
                    questionDetail.Options.Add(new OptionDetailViewModel { Text = "C. 选项C内容", IsCorrect = false, IsStudentAnswer = false });
                    questionDetail.Options.Add(new OptionDetailViewModel { Text = "D. 选项D内容", IsCorrect = false, IsStudentAnswer = false });
                }

                QuestionDetails.Add(questionDetail);
            }
        }

        private void UpdateAccuracyRate()
        {
            if (TotalScore > 0)
            {
                AccuracyRate = (Score / TotalScore) * 100;
            }
        }

        private void ExecuteExportReport()
        {
            // 实现导出报告功能
            System.Windows.MessageBox.Show("导出报告功能开发中...", "提示", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }

    #region Helper Classes

    public class QuestionDetailViewModel : INotifyPropertyChanged
    {
        private int _questionNumber;
        private string _questionType = string.Empty;
        private double _score;
        private double _earnedScore;
        private string _content = string.Empty;
        private string _correctAnswer = string.Empty;
        private string _studentAnswer = string.Empty;
        private string _explanation = string.Empty;

        public QuestionDetailViewModel()
        {
            Options = new ObservableCollection<OptionDetailViewModel>();
        }

        public int QuestionNumber
        {
            get => _questionNumber;
            set => SetProperty(ref _questionNumber, value);
        }

        public string QuestionType
        {
            get => _questionType;
            set => SetProperty(ref _questionType, value);
        }

        public double Score
        {
            get => _score;
            set => SetProperty(ref _score, value);
        }

        public double EarnedScore
        {
            get => _earnedScore;
            set
            {
                SetProperty(ref _earnedScore, value);
                OnPropertyChanged(nameof(IsFullScore));
                OnPropertyChanged(nameof(IsZeroScore));
            }
        }

        public string Content
        {
            get => _content;
            set => SetProperty(ref _content, value);
        }

        public string CorrectAnswer
        {
            get => _correctAnswer;
            set => SetProperty(ref _correctAnswer, value);
        }

        public string StudentAnswer
        {
            get => _studentAnswer;
            set => SetProperty(ref _studentAnswer, value);
        }

        public string Explanation
        {
            get => _explanation;
            set
            {
                SetProperty(ref _explanation, value);
                OnPropertyChanged(nameof(HasExplanation));
            }
        }

        public ObservableCollection<OptionDetailViewModel> Options { get; }

        public bool HasOptions => Options.Count > 0;
        public bool HasTextAnswer => !HasOptions && !string.IsNullOrWhiteSpace(StudentAnswer);
        public bool HasExplanation => !string.IsNullOrWhiteSpace(Explanation);
        public bool IsFullScore => Math.Abs(EarnedScore - Score) < 0.01;
        public bool IsZeroScore => Math.Abs(EarnedScore) < 0.01;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    public class OptionDetailViewModel : INotifyPropertyChanged
    {
        private string _text = string.Empty;
        private bool _isCorrect;
        private bool _isStudentAnswer;

        public string Text
        {
            get => _text;
            set => SetProperty(ref _text, value);
        }

        public bool IsCorrect
        {
            get => _isCorrect;
            set => SetProperty(ref _isCorrect, value);
        }

        public bool IsStudentAnswer
        {
            get => _isStudentAnswer;
            set => SetProperty(ref _isStudentAnswer, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    #endregion
}