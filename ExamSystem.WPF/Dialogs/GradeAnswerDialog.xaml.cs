using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;
using ExamSystem.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Data;
using ExamSystem.WPF.Commands;

namespace ExamSystem.WPF.Dialogs
{
    public partial class GradeAnswerDialog : Window
    {
        private readonly IExamService _examService;
        private readonly int _recordId;
        private readonly int _graderUserId;

        public GradeAnswerDialogViewModel ViewModel { get; }

        public GradeAnswerDialog(int recordId, int graderUserId)
        {
            InitializeComponent();

            _recordId = recordId;
            _graderUserId = graderUserId;
            _examService = ((App)Application.Current).Services!.GetRequiredService<IExamService>();

            ViewModel = new GradeAnswerDialogViewModel(_examService, _recordId, _graderUserId);
            DataContext = ViewModel;

            Loaded += async (_, __) => await ViewModel.LoadAsync();
        }
    }

    public class GradeAnswerDialogViewModel : INotifyPropertyChanged
    {
        private readonly IExamService _examService;
        private readonly int _recordId;
        private readonly int _graderUserId;

        public ObservableCollection<SubjectiveAnswerItem> SubjectiveAnswers { get; } = new();
        public ICollectionView SubjectiveAnswersView { get; }
        public string ExamPaperName { get; private set; } = string.Empty;
        public string UserName { get; private set; } = string.Empty;
        public int UngradedCount => SubjectiveAnswers.Count(a => !a.IsGraded);

        private bool _showOnlyUngraded;
        public bool ShowOnlyUngraded
        {
            get => _showOnlyUngraded;
            set
            {
                _showOnlyUngraded = value;
                OnPropertyChanged();
                SubjectiveAnswersView.Refresh();
            }
        }

        private bool _isSaving;
        public bool IsSaving
        {
            get => _isSaving;
            private set
            {
                _isSaving = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSave));
                (SaveAllCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (SaveSelectedCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public bool CanSave => SubjectiveAnswers.Any(i => i.Score.HasValue) && !IsSaving;

        private bool _isAutoSave = false; // 默认关闭实时保存
        public bool IsAutoSave
        {
            get => _isAutoSave;
            set
            {
                _isAutoSave = value;
                OnPropertyChanged();
            }
        }

        private SubjectiveAnswerItem? _selectedSubjectiveAnswer;
        public SubjectiveAnswerItem? SelectedSubjectiveAnswer
        {
            get => _selectedSubjectiveAnswer;
            set
            {
                _selectedSubjectiveAnswer = value;
                OnPropertyChanged();
                (SaveSelectedCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public ICommand SaveAllCommand { get; }
        public ICommand AutoGradeCommand { get; }
        public ICommand SaveSelectedCommand { get; }
        public ICommand SetFullScoreCommand { get; }
        public ICommand SetZeroScoreCommand { get; }

        public GradeAnswerDialogViewModel(IExamService examService, int recordId, int graderUserId)
        {
            _examService = examService;
            _recordId = recordId;
            _graderUserId = graderUserId;

            SaveAllCommand = new RelayCommand(async () => await SaveAllAsync(), () => CanSave);
            AutoGradeCommand = new RelayCommand(async () => await AutoGradeObjectiveAsync());
            SaveSelectedCommand = new RelayCommand(async () =>
            {
                if (SelectedSubjectiveAnswer != null)
                {
                    await SaveSingleAsync(SelectedSubjectiveAnswer);
                }
            }, () => SelectedSubjectiveAnswer != null && !IsSaving);
            SetFullScoreCommand = new RelayCommand(() =>
            {
                if (SelectedSubjectiveAnswer != null)
                {
                    SelectedSubjectiveAnswer.Score = SelectedSubjectiveAnswer.MaxScore;
                }
            }, () => SelectedSubjectiveAnswer != null);
            SetZeroScoreCommand = new RelayCommand(() =>
            {
                if (SelectedSubjectiveAnswer != null)
                {
                    SelectedSubjectiveAnswer.Score = 0;
                }
            }, () => SelectedSubjectiveAnswer != null);

            SubjectiveAnswersView = CollectionViewSource.GetDefaultView(SubjectiveAnswers);
            SubjectiveAnswersView.Filter = o =>
            {
                if (o is not SubjectiveAnswerItem item) return false;
                return !ShowOnlyUngraded || !item.IsGraded;
            };
        }

        public async Task LoadAsync()
        {
            var record = await _examService.GetExamRecordAsync(_recordId);
            if (record == null)
            {
                MessageBox.Show("未找到考试记录。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ExamPaperName = record.ExamPaper?.Name ?? "";
            UserName = record.User?.RealName ?? record.User?.Username ?? record.UserId.ToString();
            OnPropertyChanged(nameof(ExamPaperName));
            OnPropertyChanged(nameof(UserName));

            // 进入人工评分前，尝试自动批改客观题
            await AutoGradeObjectiveAsync();

            var answers = await _examService.GetAnswerRecordsAsync(_recordId);

            SubjectiveAnswers.Clear();
            foreach (var ar in answers)
            {
                var isObjective = IsObjectiveQuestion(ar.Question?.QuestionType ?? QuestionType.SingleChoice);
                if (isObjective) continue; // 只展示主观题

                var paperQuestion = record.ExamPaper?.PaperQuestions?.FirstOrDefault(pq => pq.QuestionId == ar.QuestionId);
                var maxScore = paperQuestion?.Score ?? 0;

                var item = new SubjectiveAnswerItem
                {
                    AnswerRecordId = ar.AnswerId,
                    QuestionId = ar.QuestionId,
                    QuestionNumber = ar.Question?.QuestionId ?? 0,
                    QuestionType = ar.Question?.QuestionType ?? QuestionType.ShortAnswer,
                    Content = ar.Question?.Content ?? string.Empty,
                    StandardAnswer = ar.Question?.Answer ?? string.Empty,
                    UserAnswer = ar.UserAnswer ?? string.Empty,
                    MaxScore = maxScore,
                    Score = ar.Score,
                    Comment = ar.Comment,
                    IsGraded = ar.IsGraded,
                    GradeTime = ar.GradeTime,
                    GraderInfo = ar.GraderId.HasValue ? $"评分人ID：{ar.GraderId}" : null,
                };
                // 异常高亮：得分为空或低于 30% 阈值（未评分也视为异常）
                item.IsAnomaly = !item.IsGraded || (item.Score.HasValue && item.Score.Value < maxScore * 0.3m);
                item.PropertyChanged += (_, __) =>
                {
                    OnPropertyChanged(nameof(UngradedCount));
                    OnPropertyChanged(nameof(CanSave));
                    (SaveAllCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    SubjectiveAnswersView.Refresh();
                };

                SubjectiveAnswers.Add(item);
            }
            SubjectiveAnswersView.Refresh();

            // 默认选中第一题，便于右侧详情区展示
            SelectedSubjectiveAnswer = SubjectiveAnswers.FirstOrDefault();
        }

        private static bool IsObjectiveQuestion(QuestionType type)
        {
            return type == QuestionType.SingleChoice || type == QuestionType.MultipleChoice || type == QuestionType.TrueFalse;
            // 注意：填空题现在被归类为主观题，需要人工评分
        }

        private async Task AutoGradeObjectiveAsync()
        {
            try
            {
                await _examService.AutoGradeObjectiveQuestionsAsync(_recordId);
            }
            catch
            {
                // 忽略自动批改异常，保持人工评分可继续
            }
        }

        private void SubjectiveItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is SubjectiveAnswerItem item)
            {
                if (e.PropertyName == nameof(SubjectiveAnswerItem.Score) || e.PropertyName == nameof(SubjectiveAnswerItem.Comment))
                {
                    UpdateAnomaly(item);
                    OnPropertyChanged(nameof(UngradedCount));

                    // 取消实时保存功能
                    // if (IsAutoSave)
                    // {
                    //     _ = SaveSingleAsync(item);
                    // }
                }
            }
        }

        private void UpdateAnomaly(SubjectiveAnswerItem item)
        {
            var threshold = item.MaxScore * 0.3m;
            item.IsAnomaly = !item.Score.HasValue || item.Score.Value < threshold;
        }

        private async Task SaveSingleAsync(SubjectiveAnswerItem item)
        {
            if (IsSaving) return;
            IsSaving = true;
            try
            {
                // 基础校验：得分范围
                if (item.Score.HasValue && (item.Score < 0 || item.Score > item.MaxScore))
                {
                    MessageBox.Show($"题目 {item.QuestionNumber} 的得分超出范围 (0 - {item.MaxScore})。", "校验失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                await _examService.GradeSubjectiveAnswerAsync(_recordId, item.QuestionId, item.Score ?? 0, item.Comment ?? string.Empty, _graderUserId);
                item.IsGraded = true;
                item.GradeTime = DateTime.Now;
                if (string.IsNullOrEmpty(item.GraderInfo)) item.GraderInfo = _graderUserId.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存评分失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsSaving = false;
            }
        }

        private async Task SaveAllAsync()
        {
            try
            {
                IsSaving = true;

                foreach (var item in SubjectiveAnswers)
                {
                    // 基础校验：得分范围
                    if (item.Score.HasValue && (item.Score.Value < 0 || item.Score.Value > item.MaxScore))
                    {
                        MessageBox.Show($"题目 {item.QuestionNumber} 的得分超出范围 (0 - {item.MaxScore})。", "校验失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                        IsSaving = false;
                        return;
                    }
                }

                foreach (var item in SubjectiveAnswers)
                {
                    await _examService.GradeSubjectiveAnswerAsync(_recordId, item.QuestionId, item.Score ?? 0, item.Comment ?? string.Empty, _graderUserId);
                    item.IsGraded = true;
                    item.GradeTime = DateTime.Now;
                    if (string.IsNullOrEmpty(item.GraderInfo)) item.GraderInfo = _graderUserId.ToString();
                }

                OnPropertyChanged(nameof(UngradedCount));
                OnPropertyChanged(nameof(CanSave));
                MessageBox.Show("评分保存成功。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存评分失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsSaving = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class SubjectiveAnswerItem : INotifyPropertyChanged
    {
        public int AnswerRecordId { get; set; }
        public int QuestionId { get; set; }
        public int QuestionNumber { get; set; }
        public QuestionType QuestionType { get; set; }
        public string QuestionTypeDisplay => QuestionType switch
        {
            QuestionType.ShortAnswer => "简答题",
            QuestionType.Essay => "论述题",
            QuestionType.FillInBlank => "填空题",
            _ => QuestionType.ToString()
        };

        public string Content { get; set; } = string.Empty;
        public string StandardAnswer { get; set; } = string.Empty;
        public string UserAnswer { get; set; } = string.Empty;
        public decimal MaxScore { get; set; }

        private decimal? _score;
        public decimal? Score
        {
            get => _score;
            set
            {
                _score = value;
                OnPropertyChanged();
            }
        }

        private string? _comment;
        public string? Comment
        {
            get => _comment;
            set
            {
                _comment = value;
                OnPropertyChanged();
            }
        }

        private bool _isGraded;
        public bool IsGraded
        {
            get => _isGraded;
            set
            {
                _isGraded = value;
                OnPropertyChanged();
            }
        }

        private bool _isAnomaly;
        public bool IsAnomaly
        {
            get => _isAnomaly;
            set { _isAnomaly = value; OnPropertyChanged(); }
        }

        private DateTime? _gradeTime;
        public DateTime? GradeTime
        {
            get => _gradeTime;
            set { _gradeTime = value; OnPropertyChanged(); }
        }

        private string? _graderInfo;
        public string? GraderInfo
        {
            get => _graderInfo;
            set { _graderInfo = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}