using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ExamSystem.WPF.Commands;
using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;
using ExamSystem.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace ExamSystem.WPF.ViewModels
{
    public class ExamResultViewModel : INotifyPropertyChanged
    {
        private readonly IExamService _examService;
        private readonly IExamPaperService _examPaperService;
        private readonly ILogger<ExamResultViewModel> _logger;

        private bool _isLoading;
        private string _errorMessage = string.Empty;
        private ExamRecord? _examRecord;
        private ExamPaper? _examPaper;
        private ObservableCollection<AnswerRecordViewModel> _answerRecords = new();

        public ExamResultViewModel(
            IExamService examService,
            IExamPaperService examPaperService,
            ILogger<ExamResultViewModel> logger)
        {
            _examService = examService;
            _examPaperService = examPaperService;
            _logger = logger;

            ViewExamPaperCommand = new RelayCommand(async () => await ViewExamPaperAsync());
            RetakeExamCommand = new RelayCommand(async () => await RetakeExamAsync(), () => CanRetake);
            ExportResultCommand = new RelayCommand(async () => await ExportResultAsync());
            BackCommand = new RelayCommand(() => OnBackRequested());
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

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        public ExamRecord? ExamRecord
        {
            get => _examRecord;
            set => SetProperty(ref _examRecord, value);
        }

        public ExamPaper? ExamPaper
        {
            get => _examPaper;
            set => SetProperty(ref _examPaper, value);
        }

        public ObservableCollection<AnswerRecordViewModel> AnswerRecords
        {
            get => _answerRecords;
            set => SetProperty(ref _answerRecords, value);
        }

        public bool HasExamRecord => ExamRecord != null;
        public bool HasAnswerRecords => AnswerRecords.Any();

        // 考试信息属性
        public string ExamPaperName => ExamPaper?.Name ?? string.Empty;
        public string ExamDateText => ExamRecord?.StartTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty;
        public string TimeSpentText
        {
            get
            {
                var span = GetTimeSpent();
                return span.HasValue ? FormatTimeSpan(span.Value) : string.Empty;
            }
        }
        public string TotalScoreText => $"{ExamPaper?.TotalScore ?? 0} 分";
        public string PassScoreText => $"{ExamPaper?.PassScore ?? 0} 分";
        public string ExamStatusText => GetStatusText(ExamRecord?.Status ?? ExamStatus.NotStarted);
        public Brush StatusColor => GetStatusColor(ExamRecord?.Status ?? ExamStatus.NotStarted);

        // 成绩属性
        public string ScoreText => $"{ExamRecord?.TotalScore ?? 0:F1} 分";
        public bool IsPassed => ExamRecord?.IsPassed ?? false;
        public string ResultText => IsPassed ? "通过" : "未通过";

        // 统计属性
        public int TotalQuestions => AnswerRecords.Count;
        public int CorrectAnswers => AnswerRecords.Count(x => x.IsCorrect);
        public int WrongAnswers => TotalQuestions - CorrectAnswers;
        public string AccuracyRateText => TotalQuestions > 0 ? $"{(double)CorrectAnswers / TotalQuestions * 100:F1}%" : "0%";

        // 操作属性
        public bool CanRetake => ExamPaper?.AllowRetake ?? false;

        #endregion

        #region Commands

        public ICommand ViewExamPaperCommand { get; }
        public ICommand RetakeExamCommand { get; }
        public ICommand ExportResultCommand { get; }
        public ICommand BackCommand { get; }

        #endregion

        #region Events

        public event EventHandler? BackRequested;
        public event EventHandler<int>? ViewExamPaperRequested;
        public event EventHandler<int>? RetakeExamRequested;

        #endregion

        #region Public Methods

        public async Task InitializeAsync(int examRecordId)
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                // 加载考试记录
                ExamRecord = await _examService.GetExamRecordAsync(examRecordId);
                if (ExamRecord == null)
                {
                    ErrorMessage = "考试记录不存在";
                    return;
                }

                // 加载试卷信息
                ExamPaper = await _examPaperService.GetExamPaperByIdAsync(ExamRecord.PaperId);
                if (ExamPaper == null)
                {
                    ErrorMessage = "试卷信息不存在";
                    return;
                }

                // 加载答题记录
                var answerRecords = await _examService.GetAnswerRecordsAsync(examRecordId);
                AnswerRecords.Clear();
                
                int questionNumber = 1;
                foreach (var record in answerRecords.OrderBy(x => x.QuestionId))
                {
                    AnswerRecords.Add(new AnswerRecordViewModel(record, questionNumber++));
                }

                // 通知所有属性更改
                OnAllPropertiesChanged();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化考试结果失败");
                ErrorMessage = "加载考试结果失败，请重试";
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Private Methods

        private async Task ViewExamPaperAsync()
        {
            try
            {
                if (ExamRecord == null)
                    return;

                ViewExamPaperRequested?.Invoke(this, ExamRecord.PaperId);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查看试卷失败");
                ErrorMessage = "查看试卷失败，请重试";
            }
        }

        private async Task RetakeExamAsync()
        {
            try
            {
                if (ExamRecord == null || !CanRetake)
                    return;

                RetakeExamRequested?.Invoke(this, ExamRecord.PaperId);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重新考试失败");
                ErrorMessage = "重新考试失败，请重试";
            }
        }

        private async Task ExportResultAsync()
        {
            try
            {
                // TODO: 实现导出功能
                // 可以导出为PDF或Excel格式
                _logger.LogInformation("导出考试结果功能待实现");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出结果失败");
                ErrorMessage = "导出结果失败，请重试";
            }
        }

        private void OnBackRequested()
        {
            BackRequested?.Invoke(this, EventArgs.Empty);
        }

        private string GetStatusText(ExamStatus status)
        {
            return status switch
            {
                ExamStatus.NotStarted => "未开始",
                ExamStatus.InProgress => "进行中",
                ExamStatus.Completed => "已完成",
                ExamStatus.Timeout => "超时",
                ExamStatus.Submitted => "已提交",
                _ => "未知"
            };
        }

        private Brush GetStatusColor(ExamStatus status)
        {
            // 优先使用全局资源中的主题色，以确保代码层与XAML主题一致
            SolidColorBrush Primary() => TryGetBrushResource("PrimaryBrush") ?? new SolidColorBrush(Color.FromRgb(255, 59, 48));
            SolidColorBrush Success() => new SolidColorBrush(Color.FromRgb(39, 174, 96));   // #27AE60
            SolidColorBrush Danger() => new SolidColorBrush(Color.FromRgb(231, 76, 60));    // #E74C3C
            SolidColorBrush Neutral() => new SolidColorBrush(Color.FromRgb(149, 165, 166)); // #95A5A6
            SolidColorBrush Default() => new SolidColorBrush(Color.FromRgb(127, 140, 141)); // #7F8C8D

            return status switch
            {
                ExamStatus.NotStarted => Neutral(),
                ExamStatus.InProgress => Primary(),
                ExamStatus.Completed => Success(),
                ExamStatus.Timeout => Danger(),
                ExamStatus.Submitted => Success(),
                _ => Default()
            };
        }

        private static SolidColorBrush? TryGetBrushResource(string key)
        {
            try
            {
                if (Application.Current?.Resources.Contains(key) == true)
                {
                    var brush = Application.Current.Resources[key] as SolidColorBrush;
                    if (brush != null)
                        return brush;
                }
            }
            catch { }
            return null;
        }

        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalHours >= 1)
                return $"{(int)timeSpan.TotalHours}小时{timeSpan.Minutes}分钟";
            else
                return $"{timeSpan.Minutes}分钟{timeSpan.Seconds}秒";
        }

        private TimeSpan? GetTimeSpent()
        {
            if (ExamRecord == null)
                return null;

            var start = ExamRecord.StartTime;
            var end = ExamRecord.SubmitTime ?? ExamRecord.EndTime;
            if (start.HasValue && end.HasValue && end.Value >= start.Value)
            {
                return end.Value - start.Value;
            }
            return null;
        }

        private void OnAllPropertiesChanged()
        {
            OnPropertyChanged(nameof(HasExamRecord));
            OnPropertyChanged(nameof(HasAnswerRecords));
            OnPropertyChanged(nameof(ExamPaperName));
            OnPropertyChanged(nameof(ExamDateText));
            OnPropertyChanged(nameof(TimeSpentText));
            OnPropertyChanged(nameof(TotalScoreText));
            OnPropertyChanged(nameof(PassScoreText));
            OnPropertyChanged(nameof(ExamStatusText));
            OnPropertyChanged(nameof(StatusColor));
            OnPropertyChanged(nameof(ScoreText));
            OnPropertyChanged(nameof(IsPassed));
            OnPropertyChanged(nameof(ResultText));
            OnPropertyChanged(nameof(TotalQuestions));
            OnPropertyChanged(nameof(CorrectAnswers));
            OnPropertyChanged(nameof(WrongAnswers));
            OnPropertyChanged(nameof(AccuracyRateText));
            OnPropertyChanged(nameof(CanRetake));
            OnPropertyChanged(nameof(HasError));
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

    // 答题记录视图模型
    public class AnswerRecordViewModel
    {
        public AnswerRecordViewModel(AnswerRecord answerRecord, int questionNumber)
        {
            AnswerRecord = answerRecord;
            QuestionNumber = questionNumber;
        }

        public AnswerRecord AnswerRecord { get; }
        public int QuestionNumber { get; }

        public string QuestionTypeText => GetQuestionTypeText(AnswerRecord.Question?.QuestionType ?? QuestionType.SingleChoice);
        public string QuestionContent => AnswerRecord.Question?.Content ?? string.Empty;
        public string UserAnswerText => AnswerRecord.UserAnswer ?? "未作答";
        public string CorrectAnswerText => AnswerRecord.Question?.Answer ?? string.Empty;
        public string ScoreText => $"{AnswerRecord.Score:F1}";
        public bool IsCorrect => AnswerRecord.IsCorrect;
        public string ResultText => IsCorrect ? "正确" : "错误";
        public Brush ResultColor => IsCorrect 
            ? new SolidColorBrush(Color.FromRgb(39, 174, 96))   // #27AE60
            : new SolidColorBrush(Color.FromRgb(231, 76, 60));  // #E74C3C

        private string GetQuestionTypeText(QuestionType type)
        {
            return type switch
            {
                QuestionType.SingleChoice => "单选题",
                QuestionType.MultipleChoice => "多选题",
                QuestionType.TrueFalse => "判断题",
                QuestionType.FillInBlank => "填空题",
                QuestionType.Essay => "问答题",
                _ => "未知"
            };
        }
    }
}