using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using ExamSystem.Models.Entities;
using ExamSystem.Models.Enums;
using ExamSystem.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace ExamSystem.WPF.ViewModels
{
    public class StatisticsViewModel : INotifyPropertyChanged
    {
        private readonly IStatisticsService _statisticsService;
        private readonly IExamPaperService _examPaperService;
        private readonly ILogger<StatisticsViewModel> _logger;

        public StatisticsViewModel(
            IStatisticsService statisticsService,
            IExamPaperService examPaperService,
            ILogger<StatisticsViewModel> logger)
        {
            _statisticsService = statisticsService;
            _examPaperService = examPaperService;
            _logger = logger;

            ExamPapers = new ObservableCollection<ExamPaper>();
            QuestionAnalysis = new ObservableCollection<QuestionAnalysisModel>();
            StudentRanking = new ObservableCollection<StudentRankingModel>();

            // 初始化命令
            LoadDataCommand = new RelayCommand(async () => await LoadDataAsync());
            QueryCommand = new RelayCommand(async () => await QueryStatisticsAsync());
            ExportExcelCommand = new RelayCommand(ExportToExcel);
            ExportPdfCommand = new RelayCommand(ExportToPdf);

            // 初始化日期范围
            StartDate = DateTime.Now.AddMonths(-1);
            EndDate = DateTime.Now;
        }

        #region Properties

        // 筛选条件
        private DateTime _startDate;
        public DateTime StartDate
        {
            get => _startDate;
            set => SetProperty(ref _startDate, value);
        }

        private DateTime _endDate;
        public DateTime EndDate
        {
            get => _endDate;
            set => SetProperty(ref _endDate, value);
        }

        private ExamPaper _selectedExamPaper;
        public ExamPaper SelectedExamPaper
        {
            get => _selectedExamPaper;
            set => SetProperty(ref _selectedExamPaper, value);
        }

        private string _selectedClass;
        public string SelectedClass
        {
            get => _selectedClass;
            set => SetProperty(ref _selectedClass, value);
        }

        public ObservableCollection<ExamPaper> ExamPapers { get; }

        // 概览统计
        private int _totalStudents;
        public int TotalStudents
        {
            get => _totalStudents;
            set => SetProperty(ref _totalStudents, value);
        }

        private double _averageScore;
        public double AverageScore
        {
            get => _averageScore;
            set => SetProperty(ref _averageScore, value);
        }

        private double _passRate;
        public double PassRate
        {
            get => _passRate;
            set => SetProperty(ref _passRate, value);
        }

        private double _excellentRate;
        public double ExcellentRate
        {
            get => _excellentRate;
            set => SetProperty(ref _excellentRate, value);
        }

        // 题目分析
        public ObservableCollection<QuestionAnalysisModel> QuestionAnalysis { get; }

        // 学生排名
        public ObservableCollection<StudentRankingModel> StudentRanking { get; }

        #endregion

        #region Commands

        public ICommand LoadDataCommand { get; }
        public ICommand QueryCommand { get; }
        public ICommand ExportExcelCommand { get; }
        public ICommand ExportPdfCommand { get; }

        #endregion

        #region Methods

        public async Task LoadDataAsync()
        {
            try
            {
                // 加载试卷列表
                await LoadExamPapersAsync();
                
                // 加载统计数据
                await LoadOverviewStatisticsAsync();
                await LoadQuestionAnalysisAsync();
                await LoadStudentRankingAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载统计数据时发生错误");
            }
        }

        private async Task LoadExamPapersAsync()
        {
            try
            {
                var examPapers = await _examPaperService.GetAllExamPapersAsync();
                
                ExamPapers.Clear();
                foreach (var paper in examPapers)
                {
                    ExamPapers.Add(paper);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载试卷列表时发生错误");
            }
        }

        private async Task QueryStatisticsAsync()
        {
            try
            {
                await LoadOverviewStatisticsAsync();
                await LoadQuestionAnalysisAsync();
                await LoadStudentRankingAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询统计数据时发生错误");
            }
        }

        private async Task LoadOverviewStatisticsAsync()
        {
            try
            {
                // TODO: 从统计服务获取真实数据
                // 模拟数据
                TotalStudents = 156;
                AverageScore = 78.5;
                PassRate = 85.2;
                ExcellentRate = 23.7;

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
                QuestionAnalysis.Clear();

                // TODO: 从统计服务获取真实数据
                // 模拟数据
                var analysisData = new[]
                {
                    new QuestionAnalysisModel
                    {
                        QuestionNumber = 1,
                        QuestionType = "单选题",
                        QuestionContent = "下列哪个选项是正确的？",
                        CorrectRate = 85.5,
                        DifficultyCoefficient = 0.855,
                        Discrimination = 0.42,
                        DifficultyLevel = "容易"
                    },
                    new QuestionAnalysisModel
                    {
                        QuestionNumber = 2,
                        QuestionType = "多选题",
                        QuestionContent = "以下选项中正确的有哪些？",
                        CorrectRate = 62.3,
                        DifficultyCoefficient = 0.623,
                        Discrimination = 0.38,
                        DifficultyLevel = "中等"
                    },
                    new QuestionAnalysisModel
                    {
                        QuestionNumber = 3,
                        QuestionType = "填空题",
                        QuestionContent = "请填写正确答案：______",
                        CorrectRate = 45.8,
                        DifficultyCoefficient = 0.458,
                        Discrimination = 0.51,
                        DifficultyLevel = "困难"
                    },
                    new QuestionAnalysisModel
                    {
                        QuestionNumber = 4,
                        QuestionType = "判断题",
                        QuestionContent = "这个说法是正确的。",
                        CorrectRate = 92.1,
                        DifficultyCoefficient = 0.921,
                        Discrimination = 0.25,
                        DifficultyLevel = "容易"
                    },
                    new QuestionAnalysisModel
                    {
                        QuestionNumber = 5,
                        QuestionType = "简答题",
                        QuestionContent = "请简述相关概念。",
                        CorrectRate = 58.7,
                        DifficultyCoefficient = 0.587,
                        Discrimination = 0.45,
                        DifficultyLevel = "中等"
                    }
                };

                foreach (var item in analysisData)
                {
                    QuestionAnalysis.Add(item);
                }

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
                StudentRanking.Clear();

                // TODO: 从统计服务获取真实数据
                // 模拟数据
                var rankingData = new[]
                {
                    new StudentRankingModel
                    {
                        Rank = 1,
                        StudentId = "2023001",
                        StudentName = "张三",
                        ClassName = "计算机1班",
                        TotalScore = 95.5,
                        ObjectiveScore = 48.0,
                        SubjectiveScore = 47.5,
                        ExamDuration = TimeSpan.FromMinutes(85),
                        SubmitTime = DateTime.Now.AddHours(-2),
                        Grade = "优秀"
                    },
                    new StudentRankingModel
                    {
                        Rank = 2,
                        StudentId = "2023002",
                        StudentName = "李四",
                        ClassName = "计算机1班",
                        TotalScore = 92.0,
                        ObjectiveScore = 46.5,
                        SubjectiveScore = 45.5,
                        ExamDuration = TimeSpan.FromMinutes(78),
                        SubmitTime = DateTime.Now.AddHours(-1.5),
                        Grade = "优秀"
                    },
                    new StudentRankingModel
                    {
                        Rank = 3,
                        StudentId = "2023003",
                        StudentName = "王五",
                        ClassName = "计算机2班",
                        TotalScore = 88.5,
                        ObjectiveScore = 44.0,
                        SubjectiveScore = 44.5,
                        ExamDuration = TimeSpan.FromMinutes(92),
                        SubmitTime = DateTime.Now.AddHours(-1),
                        Grade = "良好"
                    },
                    new StudentRankingModel
                    {
                        Rank = 4,
                        StudentId = "2023004",
                        StudentName = "赵六",
                        ClassName = "计算机1班",
                        TotalScore = 85.0,
                        ObjectiveScore = 42.5,
                        SubjectiveScore = 42.5,
                        ExamDuration = TimeSpan.FromMinutes(88),
                        SubmitTime = DateTime.Now.AddMinutes(-45),
                        Grade = "良好"
                    },
                    new StudentRankingModel
                    {
                        Rank = 5,
                        StudentId = "2023005",
                        StudentName = "孙七",
                        ClassName = "计算机2班",
                        TotalScore = 82.5,
                        ObjectiveScore = 41.0,
                        SubjectiveScore = 41.5,
                        ExamDuration = TimeSpan.FromMinutes(95),
                        SubmitTime = DateTime.Now.AddMinutes(-30),
                        Grade = "良好"
                    }
                };

                foreach (var item in rankingData)
                {
                    StudentRanking.Add(item);
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载学生排名时发生错误");
            }
        }

        private void ExportToExcel()
        {
            try
            {
                // TODO: 实现导出Excel功能
                _logger.LogInformation("导出Excel功能被调用");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出Excel时发生错误");
            }
        }

        private void ExportToPdf()
        {
            try
            {
                // TODO: 实现导出PDF功能
                _logger.LogInformation("导出PDF功能被调用");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出PDF时发生错误");
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }

    // 题目分析模型
    public class QuestionAnalysisModel
    {
        public int QuestionNumber { get; set; }
        public string QuestionType { get; set; }
        public string QuestionContent { get; set; }
        public double CorrectRate { get; set; }
        public double DifficultyCoefficient { get; set; }
        public double Discrimination { get; set; }
        public string DifficultyLevel { get; set; }
    }

    // 学生排名模型
    public class StudentRankingModel
    {
        public int Rank { get; set; }
        public string StudentId { get; set; }
        public string StudentName { get; set; }
        public string ClassName { get; set; }
        public double TotalScore { get; set; }
        public double ObjectiveScore { get; set; }
        public double SubjectiveScore { get; set; }
        public TimeSpan ExamDuration { get; set; }
        public DateTime SubmitTime { get; set; }
        public string Grade { get; set; }

        public string ExamDurationDisplay => $"{ExamDuration.Hours:D2}:{ExamDuration.Minutes:D2}:{ExamDuration.Seconds:D2}";
        public string SubmitTimeDisplay => SubmitTime.ToString("yyyy-MM-dd HH:mm:ss");
    }
}