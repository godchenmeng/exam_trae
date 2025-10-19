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

        private string _welcomeMessage;
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
                await LoadStatisticsAsync();
                
                // 加载最近活动
                await LoadRecentActivitiesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载仪表板数据时发生错误");
            }
        }

        private async Task LoadStatisticsAsync()
        {
            try
            {
                // 获取题库数量
                var questionBanks = await _questionBankService.GetAllQuestionBanksAsync();
                QuestionBankCount = questionBanks.Count;

                // 获取题目数量 - 通过获取所有题库的题目来统计
                var allQuestions = new List<Question>();
                foreach (var bank in questionBanks)
                {
                    var questions = await _questionService.GetQuestionsByBankIdAsync(bank.BankId);
                    allQuestions.AddRange(questions);
                }
                QuestionCount = allQuestions.Count;

                // 获取试卷数量
                var examPapers = await _examPaperService.GetAllExamPapersAsync();
                ExamPaperCount = examPapers.Count;

                // 获取考试记录数量 - 暂时设为0，因为没有获取所有记录的方法
                ExamRecordCount = 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载统计数据时发生错误");
            }
        }

        private async Task LoadRecentActivitiesAsync()
        {
            try
            {
                RecentActivities.Clear();

                // TODO: 从服务获取真实的最近活动数据
                // 模拟数据
                RecentActivities.Add(new RecentActivityModel
                {
                    Icon = "📚",
                    Title = "创建了新题库",
                    Description = "数学基础题库",
                    Time = "2小时前"
                });

                RecentActivities.Add(new RecentActivityModel
                {
                    Icon = "📝",
                    Title = "添加了新题目",
                    Description = "单选题：函数的定义域",
                    Time = "3小时前"
                });

                RecentActivities.Add(new RecentActivityModel
                {
                    Icon = "📄",
                    Title = "创建了新试卷",
                    Description = "期中考试试卷",
                    Time = "1天前"
                });

                RecentActivities.Add(new RecentActivityModel
                {
                    Icon = "👥",
                    Title = "学生完成考试",
                    Description = "张三完成了数学测试",
                    Time = "2天前"
                });

                RecentActivities.Add(new RecentActivityModel
                {
                    Icon = "📊",
                    Title = "生成了统计报表",
                    Description = "第一次月考成绩统计",
                    Time = "3天前"
                });

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载最近活动时发生错误");
            }
        }

        private void CreateQuestionBank()
        {
            // TODO: 实现创建题库功能
            _logger.LogInformation("创建题库功能被调用");
        }

        private void AddQuestion()
        {
            // TODO: 实现添加题目功能
            _logger.LogInformation("添加题目功能被调用");
        }

        private void CreateExamPaper()
        {
            // TODO: 实现创建试卷功能
            _logger.LogInformation("创建试卷功能被调用");
        }

        private void ViewStatistics()
        {
            // TODO: 实现查看统计功能
            _logger.LogInformation("查看统计功能被调用");
        }

        private void UserManagement()
        {
            // TODO: 实现用户管理功能
            _logger.LogInformation("用户管理功能被调用");
        }

        private void SystemSettings()
        {
            // TODO: 实现系统设置功能
            _logger.LogInformation("系统设置功能被调用");
        }

        private void DataBackup()
        {
            // TODO: 实现数据备份功能
            _logger.LogInformation("数据备份功能被调用");
        }

        private void HelpDocument()
        {
            // TODO: 实现帮助文档功能
            _logger.LogInformation("帮助文档功能被调用");
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

    // 最近活动模型
    public class RecentActivityModel
    {
        public string Icon { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Time { get; set; }
    }

}