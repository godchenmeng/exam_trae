using ExamSystem.Domain.Entities;
using ExamSystem.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Printing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Controls;
using ExamSystem.WPF.Commands;
using ExamSystem.Domain.Enums;
using System.Windows.Media;

namespace ExamSystem.WPF.Dialogs
{
    public partial class ExamRecordViewDialog : Window
    {
        private readonly IExamService _examService;
        private readonly int _recordId;

        public ExamRecordViewDialogViewModel ViewModel { get; }

        public ExamRecordViewDialog(int recordId)
        {
            InitializeComponent();
            _recordId = recordId;
            _examService = ((App)Application.Current).Services!.GetRequiredService<IExamService>();
            ViewModel = new ExamRecordViewDialogViewModel(_examService, _recordId);
            DataContext = ViewModel;
            Loaded += async (_, __) => await ViewModel.LoadAsync();
        }
    }

    public class ExamRecordViewDialogViewModel : System.ComponentModel.INotifyPropertyChanged
    {
        private readonly IExamService _examService;
        private readonly int _recordId;

        private string _examPaperTitle = string.Empty;
        private string _userName = string.Empty;
        private decimal _totalScore;
        private string _status = string.Empty;

        public string ExamPaperTitle 
        { 
            get => _examPaperTitle; 
            private set 
            { 
                _examPaperTitle = value; 
                OnPropertyChanged(); 
            } 
        }
        
        public string UserName 
        { 
            get => _userName; 
            private set 
            { 
                _userName = value; 
                OnPropertyChanged(); 
            } 
        }
        
        public decimal TotalScore 
        { 
            get => _totalScore; 
            private set 
            { 
                _totalScore = value; 
                OnPropertyChanged(); 
            } 
        }
        
        public string Status 
        { 
            get => _status; 
            private set 
            { 
                _status = value; 
                OnPropertyChanged(); 
            } 
        }

        public ObservableCollection<QuestionViewItem> Questions { get; } = new();

        public ICommand ExportPdfCommand { get; }
        public ICommand PrintCommand { get; }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }

        public ExamRecordViewDialogViewModel(IExamService examService, int recordId)
        {
            _examService = examService;
            _recordId = recordId;
            ExportPdfCommand = new RelayCommand(async () => await ExportPdfAsync());
            PrintCommand = new RelayCommand(async () => await PrintAsync());
        }

        public async Task LoadAsync()
        {
            var record = await _examService.GetExamRecordAsync(_recordId);
            if (record == null) return;
            ExamPaperTitle = record.ExamPaper?.Name ?? string.Empty;
            // 使用真实姓名优先，其次退回到用户名，最后使用用户ID
            UserName = record.User?.RealName ?? record.User?.Username ?? record.UserId.ToString();

            var answers = await _examService.GetAnswerRecordsAsync(_recordId);

            // 重新统计总分：将所有有分数的题目加总（未评分视为0分）
            TotalScore = answers.Sum(a => a.Score);
            // 顶部状态显示中文
            Status = MapStatusToChinese(record.Status);

            Questions.Clear();
            foreach (var ar in answers.OrderBy(a => a.Question?.QuestionId))
            {
                var pq = record.ExamPaper?.PaperQuestions?.FirstOrDefault(x => x.QuestionId == ar.QuestionId);
                Questions.Add(new QuestionViewItem
                {
                    // 如果有试卷中的顺序则使用，否则回退到题目ID
                    QuestionNumber = (pq != null && pq.OrderIndex > 0) ? pq.OrderIndex : (ar.Question?.QuestionId ?? ar.QuestionId),
                    QuestionType = ar.Question?.QuestionType ?? QuestionType.SingleChoice,
                    Content = ar.Question?.Content ?? string.Empty,
                    StandardAnswer = ar.Question?.Answer ?? string.Empty,
                    UserAnswer = ar.UserAnswer ?? string.Empty,
                    IsCorrect = ar.IsCorrect,
                    MaxScore = pq?.Score ?? (ar.Question?.Score ?? 0m),
                    Score = ar.Score,
                    Comment = ar.Comment ?? string.Empty,
                    Options = ar.Question?.Options?.OrderBy(o => o.OrderIndex).ToList() ?? new System.Collections.Generic.List<QuestionOption>(),
                });
            }
        }

        private string MapStatusToChinese(ExamStatus status)
        {
            return status switch
            {
                ExamStatus.NotStarted => "未开始",
                ExamStatus.InProgress => "进行中",
                ExamStatus.Completed => "已完成",
                ExamStatus.Submitted => "已提交",
                ExamStatus.Graded => "已评分",
                ExamStatus.Timeout => "已超时",
                _ => status.ToString()
            };
        }

        private FlowDocument BuildFlowDocument()
        {
            var doc = new FlowDocument();
            doc.FontSize = 12;
            doc.PagePadding = new Thickness(48);
            doc.ColumnWidth = double.PositiveInfinity;

            var title = new Paragraph(new Run($"试卷：{ExamPaperTitle}    学生：{UserName}    分数：{TotalScore}    状态：{Status}"))
            {
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 12)
            };
            doc.Blocks.Add(title);

            int idx = 1;
            foreach (var q in Questions)
            {
                var header = new Paragraph(new Run($"第{q.QuestionNumber}题（{MapQuestionTypeToChinese(q.QuestionType)}）"))
                {
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 8, 0, 4)
                };
                doc.Blocks.Add(header);

                doc.Blocks.Add(new Paragraph(new Run(q.Content)));
                // 新增：对于单选/多选题，显示选项列表
                if (q.QuestionType == QuestionType.SingleChoice || q.QuestionType == QuestionType.MultipleChoice)
                {
                    if (q.Options != null && q.Options.Count > 0)
                    {
                        var optsTitle = new Paragraph(new Run("选项：")) { Margin = new Thickness(0, 2, 0, 2) };
                        doc.Blocks.Add(optsTitle);
                        // 标注标准答案与学生作答的选项标签集合
                        var stdSet = ParseAnswerLabels(q.StandardAnswer);
                        var usrSet = ParseAnswerLabels(q.UserAnswer);
                        foreach (var opt in q.Options)
                        {
                            var para = new Paragraph { Margin = new Thickness(12, 0, 0, 0) };
                            para.Inlines.Add(new Run($"{opt.OptionLabel}. {opt.Content}"));
                            if (stdSet.Contains(opt.OptionLabel))
                            {
                                para.Inlines.Add(new Run(" [标准]") { Foreground = Brushes.Green });
                            }
                            if (usrSet.Contains(opt.OptionLabel))
                            {
                                para.Inlines.Add(new Run(" [作答]") { Foreground = Brushes.Blue });
                            }
                            doc.Blocks.Add(para);
                        }
                    }
                }
                doc.Blocks.Add(new Paragraph(new Run($"标准答案：{q.StandardAnswer}")));
                doc.Blocks.Add(new Paragraph(new Run($"学生作答：{q.UserAnswer}")));
                doc.Blocks.Add(new Paragraph(new Run($"是否正确：{(q.IsCorrect ? "正确" : "错误")}")));
                doc.Blocks.Add(new Paragraph(new Run($"得分：{q.Score} / {q.MaxScore}")));
                idx++;
            }

            return doc;
        }

        private async Task ExportPdfAsync()
        {
            // 通过打印对话框选择 "Microsoft Print to PDF" 可导出为 PDF
            await PrintAsync(true);
        }

        private async Task PrintAsync(bool suggestPdf = false)
        {
            await Task.Yield();
            try
            {
                var dlg = new PrintDialog();
                if (dlg.ShowDialog() == true)
                {
                    var doc = BuildFlowDocument();
                    IDocumentPaginatorSource dps = doc;
                    dlg.PrintDocument(dps.DocumentPaginator, suggestPdf ? "导出试卷PDF" : "打印试卷");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打印/导出失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 新增：题型到中文的映射，修复 CS0103（MapQuestionTypeToChinese 未定义）
        private static string MapQuestionTypeToChinese(QuestionType type)
        {
            return type switch
            {
                QuestionType.SingleChoice => "单选题",
                QuestionType.MultipleChoice => "多选题",
                QuestionType.TrueFalse => "判断题",
                QuestionType.FillInBlank => "填空题",
                QuestionType.ShortAnswer => "简答题",
                QuestionType.Essay => "问答题",
                _ => "未知"
            };
        }

        // 解析答案标签（支持 A,B 或用分号/斜杠等分隔符的格式）
        private static System.Collections.Generic.HashSet<string> ParseAnswerLabels(string? labels)
        {
            var set = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(labels)) return set;
            foreach (var part in labels.Split(new[] { ',', ';', '/', '|', '，', '；' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var t = part.Trim();
                if (!string.IsNullOrEmpty(t)) set.Add(t);
            }
            return set;
        }
    }

    public class QuestionViewItem
    {
        public int QuestionNumber { get; set; }
        public QuestionType QuestionType { get; set; }
        public string Content { get; set; } = string.Empty;
        public string StandardAnswer { get; set; } = string.Empty;
        public string UserAnswer { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public decimal MaxScore { get; set; }
        public decimal Score { get; set; }
        public string Comment { get; set; } = string.Empty;
        // 新增：选择题选项列表
        public System.Collections.Generic.List<QuestionOption> Options { get; set; } = new System.Collections.Generic.List<QuestionOption>();
    }

}