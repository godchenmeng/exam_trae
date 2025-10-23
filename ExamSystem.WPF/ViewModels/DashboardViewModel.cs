using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using ExamSystem.WPF.Commands;
using ExamSystem.Services.Interfaces;
using ExamSystem.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace ExamSystem.WPF.ViewModels
{
    public class DashboardViewModel : INotifyPropertyChanged
    {
        private readonly IQuestionBankService _questionBankService;
        private readonly IQuestionService _questionService;
        private readonly IExamPaperService _examPaperService;
        private readonly IExamService _examService;
        private readonly ILogger<DashboardViewModel> _logger;

        public DashboardViewModel(
            IQuestionBankService questionBankService,
            IQuestionService questionService,
            IExamPaperService examPaperService,
            IExamService examService,
            ILogger<DashboardViewModel> logger)
        {
            _questionBankService = questionBankService;
            _questionService = questionService;
            _examPaperService = examPaperService;
            _examService = examService;
            _logger = logger;

            RecentActivities = new ObservableCollection<RecentActivityModel>();
            
            LoadDataCommand = new RelayCommand(async () => await LoadDashboardDataAsync());
            CreateQuestionBankCommand = new RelayCommand(CreateQuestionBank);
            AddQuestionCommand = new RelayCommand(AddQuestion);
            CreateExamPaperCommand = new RelayCommand(CreateExamPaper);
            ViewStatisticsCommand = new RelayCommand(ViewStatistics);
            UserManagementCommand = new RelayCommand(UserManagement);
            SystemSettingsCommand = new RelayCommand(SystemSettings);
            DataBackupCommand = new RelayCommand(DataBackup);
            HelpDocumentCommand = new RelayCommand(HelpDocument);
        }

        #region Properties

        private int _questionBankCount;
        public int QuestionBankCount
        {
            get => _questionBankCount;
            set => SetProperty(ref _questionBankCount, value);
        }

        private int _questionCount;
        public int QuestionCount
        {
            get => _questionCount;
            set => SetProperty(ref _questionCount, value);
        }

        private int _examPaperCount;
        public int ExamPaperCount
        {
            get => _examPaperCount;
            set => SetProperty(ref _examPaperCount, value);
        }

        private int _examRecordCount;
        public int ExamRecordCount
        {
            get => _examRecordCount;
            set => SetProperty(ref _examRecordCount, value);
        }

        private string _welcomeMessage = string.Empty;
        public string WelcomeMessage
        {
            get => _welcomeMessage;
            set => SetProperty(ref _welcomeMessage, value);
        }

        public ObservableCollection<RecentActivityModel> RecentActivities { get; }

        #endregion

        #region Commands

        public ICommand LoadDataCommand { get; }
        public ICommand CreateQuestionBankCommand { get; }
        public ICommand AddQuestionCommand { get; }
        public ICommand CreateExamPaperCommand { get; }
        public ICommand ViewStatisticsCommand { get; }
        public ICommand UserManagementCommand { get; }
        public ICommand SystemSettingsCommand { get; }
        public ICommand DataBackupCommand { get; }
        public ICommand HelpDocumentCommand { get; }

        #endregion

        #region Methods

        public async Task LoadDashboardDataAsync()
        {
            try
            {
                // 设置欢迎消息
                var currentHour = DateTime.Now.Hour;
                var greeting = currentHour switch
                {
                    >= 6 and < 12 => "早上好",
                    >= 12 and < 18 => "下午好",
                    _ => "晚上好"
                };
                WelcomeMessage = $"{greeting}，欢迎使用在线考试系统！";

                // 加载统计数据
                var questionBanks = await _questionBankService.GetAllQuestionBanksAsync();
                QuestionBankCount = questionBanks.Count;
                
                var allQuestions = new List<Question>();
                foreach (var bank in questionBanks)
                {
                    var questions = await _questionService.GetQuestionsByBankIdAsync(bank.BankId);
                    allQuestions.AddRange(questions);
                }
                QuestionCount = allQuestions.Count;
                
                var examPapers = await _examPaperService.GetAllExamPapersAsync();
                ExamPaperCount = examPapers.Count;
                
                // 暂时没有获取所有考试记录的方法，设为 0
                ExamRecordCount = 0;
                
                // 加载最近活动（示例数据）
                RecentActivities.Clear();
                RecentActivities.Add(new RecentActivityModel { Icon = "📚", Title = "创建题库", Description = "创建了新的数学题库", Time = "2024-05-01 10:00" });
                RecentActivities.Add(new RecentActivityModel { Icon = "📝", Title = "新增试题", Description = "添加了5道选择题", Time = "2024-05-02 14:30" });
                RecentActivities.Add(new RecentActivityModel { Icon = "📄", Title = "生成试卷", Description = "生成了期中考试试卷", Time = "2024-05-03 09:15" });
                RecentActivities.Add(new RecentActivityModel { Icon = "📊", Title = "查看统计", Description = "查看了最近考试统计数据", Time = "2024-05-04 16:45" });

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载仪表盘数据时发生错误");
            }
        }

        private void CreateQuestionBank()
        {
            // TODO: 跳转到题库管理页面
            _logger.LogInformation("跳转到题库管理页面");
        }

        private void AddQuestion()
        {
            // TODO: 跳转到添加试题页面
            _logger.LogInformation("跳转到添加试题页面");
        }

        private void CreateExamPaper()
        {
            // TODO: 跳转到试卷创建页面
            _logger.LogInformation("跳转到试卷创建页面");
        }

        private void ViewStatistics()
        {
            // TODO: 跳转到统计分析页面
            _logger.LogInformation("跳转到统计分析页面");
        }

        private void UserManagement()
        {
            // TODO: 跳转到用户管理页面
            _logger.LogInformation("跳转到用户管理页面");
        }

        private void SystemSettings()
        {
            // TODO: 跳转到系统设置页面
            _logger.LogInformation("跳转到系统设置页面");
        }

        private void DataBackup()
        {
            // TODO: 执行数据备份
            _logger.LogInformation("执行数据备份");
        }

        private void HelpDocument()
        {
            // TODO: 打开帮助文档
            _logger.LogInformation("打开帮助文档");
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

    // 最近活动模型
    public class RecentActivityModel
    {
        public string Icon { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
    }

}