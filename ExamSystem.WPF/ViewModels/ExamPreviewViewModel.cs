using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using ExamSystem.Core.Entities;
using ExamSystem.Core.Services;
using Microsoft.Extensions.Logging;

namespace ExamSystem.WPF.ViewModels
{
    public class ExamPreviewViewModel : INotifyPropertyChanged
    {
        private readonly IExamPaperService _examPaperService;
        private readonly IQuestionService _questionService;
        private readonly ILogger<ExamPreviewViewModel> _logger;

        private bool _isLoading;
        private string _errorMessage = string.Empty;
        private ExamPaper? _examPaper;
        private ObservableCollection<Question> _questions = new();

        public ExamPreviewViewModel(
            IExamPaperService examPaperService,
            IQuestionService questionService,
            ILogger<ExamPreviewViewModel> logger)
        {
            _examPaperService = examPaperService;
            _questionService = questionService;
            _logger = logger;

            StartExamCommand = new RelayCommand(async () => await StartExamAsync(), CanStartExam);
            CancelCommand = new RelayCommand(() => OnCancelRequested());
        }

        #region Properties

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public ExamPaper? ExamPaper
        {
            get => _examPaper;
            set => SetProperty(ref _examPaper, value);
        }

        public ObservableCollection<Question> Questions
        {
            get => _questions;
            set => SetProperty(ref _questions, value);
        }

        // 考试信息属性
        public string ExamPaperName => ExamPaper?.Name ?? string.Empty;
        public int Duration => ExamPaper?.Duration ?? 0;
        public decimal TotalScore => ExamPaper?.TotalScore ?? 0;
        public decimal PassScore => ExamPaper?.PassScore ?? 0;
        public int QuestionCount => Questions.Count;
        public bool AllowRetake => ExamPaper?.AllowRetake ?? false;
        public bool RandomQuestions => ExamPaper?.RandomQuestions ?? false;
        public DateTime? StartTime => ExamPaper?.StartTime;
        public DateTime? EndTime => ExamPaper?.EndTime;
        public string Description => ExamPaper?.Description ?? string.Empty;

        // 格式化显示属性
        public string DurationText => $"{Duration} 分钟";
        public string TotalScoreText => $"{TotalScore} 分";
        public string PassScoreText => $"{PassScore} 分";
        public string QuestionCountText => $"{QuestionCount} 题";
        public string AllowRetakeText => AllowRetake ? "允许" : "不允许";
        public string RandomQuestionsText => RandomQuestions ? "是" : "否";
        public string StartTimeText => StartTime?.ToString("yyyy-MM-dd HH:mm") ?? "未设置";
        public string EndTimeText => EndTime?.ToString("yyyy-MM-dd HH:mm") ?? "未设置";

        #endregion

        #region Commands

        public ICommand StartExamCommand { get; }
        public ICommand CancelCommand { get; }

        #endregion

        #region Events

        public event EventHandler? StartExamRequested;
        public event EventHandler? CancelRequested;

        #endregion

        #region Public Methods

        public async Task InitializeAsync(int examPaperId)
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                // 加载试卷信息
                ExamPaper = await _examPaperService.GetByIdAsync(examPaperId);
                if (ExamPaper == null)
                {
                    ErrorMessage = "试卷不存在";
                    return;
                }

                // 加载试卷题目
                var questions = await _questionService.GetQuestionsByPaperIdAsync(examPaperId);
                Questions.Clear();
                foreach (var question in questions)
                {
                    Questions.Add(question);
                }

                // 通知属性更改
                OnPropertyChanged(nameof(ExamPaperName));
                OnPropertyChanged(nameof(Duration));
                OnPropertyChanged(nameof(TotalScore));
                OnPropertyChanged(nameof(PassScore));
                OnPropertyChanged(nameof(QuestionCount));
                OnPropertyChanged(nameof(AllowRetake));
                OnPropertyChanged(nameof(RandomQuestions));
                OnPropertyChanged(nameof(StartTime));
                OnPropertyChanged(nameof(EndTime));
                OnPropertyChanged(nameof(Description));
                OnPropertyChanged(nameof(DurationText));
                OnPropertyChanged(nameof(TotalScoreText));
                OnPropertyChanged(nameof(PassScoreText));
                OnPropertyChanged(nameof(QuestionCountText));
                OnPropertyChanged(nameof(AllowRetakeText));
                OnPropertyChanged(nameof(RandomQuestionsText));
                OnPropertyChanged(nameof(StartTimeText));
                OnPropertyChanged(nameof(EndTimeText));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化考试预览失败");
                ErrorMessage = "加载考试信息失败，请重试";
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Private Methods

        private bool CanStartExam()
        {
            if (ExamPaper == null || IsLoading)
                return false;

            // 检查考试时间
            var now = DateTime.Now;
            if (StartTime.HasValue && now < StartTime.Value)
                return false;

            if (EndTime.HasValue && now > EndTime.Value)
                return false;

            return true;
        }

        private async Task StartExamAsync()
        {
            try
            {
                if (ExamPaper == null)
                    return;

                // 这里可以添加额外的验证逻辑
                // 例如检查用户是否已经参加过考试等

                OnStartExamRequested();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "开始考试失败");
                ErrorMessage = "开始考试失败，请重试";
            }
        }

        private void OnStartExamRequested()
        {
            StartExamRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnCancelRequested()
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
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
            if (Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }
}