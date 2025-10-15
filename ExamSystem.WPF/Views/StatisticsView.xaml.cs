using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ExamSystem.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace ExamSystem.WPF.Views
{
    public partial class StatisticsView : UserControl
    {
        private readonly IStatisticsService _statisticsService;
        private readonly IExamPaperService _examPaperService;
        private readonly ILogger<StatisticsView> _logger;

        public ObservableCollection<QuestionAnalysisModel> QuestionAnalyses { get; set; }
        public ObservableCollection<StudentRankingModel> StudentRankings { get; set; }

        public StatisticsView(
            IStatisticsService statisticsService,
            IExamPaperService examPaperService,
            ILogger<StatisticsView> logger)
        {
            InitializeComponent();
            
            _statisticsService = statisticsService;
            _examPaperService = examPaperService;
            _logger = logger;

            QuestionAnalyses = new ObservableCollection<QuestionAnalysisModel>();
            StudentRankings = new ObservableCollection<StudentRankingModel>();

            QuestionAnalysisDataGrid.ItemsSource = QuestionAnalyses;
            StudentRankingDataGrid.ItemsSource = StudentRankings;

            Loaded += StatisticsView_Loaded;
        }

        private async void StatisticsView_Loaded(object sender, RoutedEventArgs e)
        {
            await InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                // 初始化日期选择器
                StartDatePicker.SelectedDate = DateTime.Now.AddMonths(-1);
                EndDatePicker.SelectedDate = DateTime.Now;

                // 加载试卷列表
                await LoadExamPapersAsync();
                
                // 加载默认统计数据
                await LoadStatisticsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化统计页面时发生错误");
                MessageBox.Show("初始化统计页面时发生错误，请稍后重试。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadExamPapersAsync()
        {
            try
            {
                var examPapers = await _examPaperService.GetAllExamPapersAsync();
                
                ExamPaperComboBox.Items.Clear();
                ExamPaperComboBox.Items.Add(new ComboBoxItem { Content = "全部试卷", Tag = null });
                
                foreach (var paper in examPapers)
                {
                    ExamPaperComboBox.Items.Add(new ComboBoxItem 
                    { 
                        Content = paper.Title, 
                        Tag = paper.Id 
                    });
                }
                
                ExamPaperComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载试卷列表时发生错误");
            }
        }

        private async Task LoadStatisticsAsync()
        {
            try
            {
                // 加载概览统计
                await LoadOverviewStatisticsAsync();
                
                // 加载题目分析
                await LoadQuestionAnalysisAsync();
                
                // 加载学生排名
                await LoadStudentRankingAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载统计数据时发生错误");
                MessageBox.Show("加载统计数据时发生错误，请稍后重试。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadOverviewStatisticsAsync()
        {
            try
            {
                // TODO: 从统计服务获取真实数据
                // 模拟数据
                TotalStudentsTextBlock.Text = "156";
                AverageScoreTextBlock.Text = "78.5";
                PassRateTextBlock.Text = "82.1%";
                ExcellentRateTextBlock.Text = "23.7%";

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载概览统计时发生错误");
            }
        }

        private async Task LoadQuestionAnalysisAsync()
        {
            try
            {
                QuestionAnalyses.Clear();

                // TODO: 从统计服务获取真实数据
                // 模拟数据
                QuestionAnalyses.Add(new QuestionAnalysisModel
                {
                    QuestionNumber = "1",
                    QuestionType = "单选题",
                    QuestionContent = "下列哪个选项是正确的？",
                    CorrectRate = "85.2%",
                    DifficultyCoefficient = "0.85",
                    Discrimination = "0.42",
                    DifficultyLevel = "容易",
                    DifficultyColor = "#4CAF50"
                });

                QuestionAnalyses.Add(new QuestionAnalysisModel
                {
                    QuestionNumber = "2",
                    QuestionType = "多选题",
                    QuestionContent = "以下哪些选项是正确的？",
                    CorrectRate = "62.8%",
                    DifficultyCoefficient = "0.63",
                    Discrimination = "0.58",
                    DifficultyLevel = "中等",
                    DifficultyColor = "#FF9800"
                });

                QuestionAnalyses.Add(new QuestionAnalysisModel
                {
                    QuestionNumber = "3",
                    QuestionType = "填空题",
                    QuestionContent = "请填写正确答案：______",
                    CorrectRate = "45.3%",
                    DifficultyCoefficient = "0.45",
                    Discrimination = "0.72",
                    DifficultyLevel = "困难",
                    DifficultyColor = "#F44336"
                });

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载题目分析时发生错误");
            }
        }

        private async Task LoadStudentRankingAsync()
        {
            try
            {
                StudentRankings.Clear();

                // TODO: 从统计服务获取真实数据
                // 模拟数据
                StudentRankings.Add(new StudentRankingModel
                {
                    Rank = 1,
                    StudentId = "2021001",
                    StudentName = "张三",
                    ClassName = "计算机1班",
                    TotalScore = 95,
                    ObjectiveScore = 85,
                    SubjectiveScore = 10,
                    ExamDuration = "45分钟",
                    SubmitTime = DateTime.Now.AddHours(-2),
                    Grade = "优秀",
                    GradeColor = "#4CAF50"
                });

                StudentRankings.Add(new StudentRankingModel
                {
                    Rank = 2,
                    StudentId = "2021002",
                    StudentName = "李四",
                    ClassName = "计算机1班",
                    TotalScore = 88,
                    ObjectiveScore = 78,
                    SubjectiveScore = 10,
                    ExamDuration = "52分钟",
                    SubmitTime = DateTime.Now.AddHours(-1),
                    Grade = "良好",
                    GradeColor = "#2196F3"
                });

                StudentRankings.Add(new StudentRankingModel
                {
                    Rank = 3,
                    StudentId = "2021003",
                    StudentName = "王五",
                    ClassName = "计算机2班",
                    TotalScore = 76,
                    ObjectiveScore = 68,
                    SubjectiveScore = 8,
                    ExamDuration = "58分钟",
                    SubmitTime = DateTime.Now.AddMinutes(-30),
                    Grade = "中等",
                    GradeColor = "#FF9800"
                });

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载学生排名时发生错误");
            }
        }

        private async void QueryButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadStatisticsAsync();
        }

        private void ExportExcelButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // TODO: 实现Excel导出功能
                MessageBox.Show("Excel导出功能开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出Excel时发生错误");
                MessageBox.Show("导出Excel时发生错误，请稍后重试。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportPdfButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // TODO: 实现PDF导出功能
                MessageBox.Show("PDF导出功能开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出PDF时发生错误");
                MessageBox.Show("导出PDF时发生错误，请稍后重试。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // 题目分析模型
    public class QuestionAnalysisModel
    {
        public string QuestionNumber { get; set; }
        public string QuestionType { get; set; }
        public string QuestionContent { get; set; }
        public string CorrectRate { get; set; }
        public string DifficultyCoefficient { get; set; }
        public string Discrimination { get; set; }
        public string DifficultyLevel { get; set; }
        public string DifficultyColor { get; set; }
    }

    // 学生排名模型
    public class StudentRankingModel
    {
        public int Rank { get; set; }
        public string StudentId { get; set; }
        public string StudentName { get; set; }
        public string ClassName { get; set; }
        public int TotalScore { get; set; }
        public int ObjectiveScore { get; set; }
        public int SubjectiveScore { get; set; }
        public string ExamDuration { get; set; }
        public DateTime SubmitTime { get; set; }
        public string Grade { get; set; }
        public string GradeColor { get; set; }
    }
}