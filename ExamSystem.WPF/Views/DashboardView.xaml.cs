using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ExamSystem.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace ExamSystem.WPF.Views
{
    public partial class DashboardView : UserControl
    {
        private readonly IQuestionBankService _questionBankService;
        private readonly IQuestionService _questionService;
        private readonly IExamPaperService _examPaperService;
        private readonly IExamService _examService;
        private readonly ILogger<DashboardView> _logger;

        public ObservableCollection<RecentActivity> RecentActivities { get; set; }

        public DashboardView(
            IQuestionBankService questionBankService,
            IQuestionService questionService,
            IExamPaperService examPaperService,
            IExamService examService,
            ILogger<DashboardView> logger)
        {
            InitializeComponent();
            
            _questionBankService = questionBankService;
            _questionService = questionService;
            _examPaperService = examPaperService;
            _examService = examService;
            _logger = logger;

            RecentActivities = new ObservableCollection<RecentActivity>();
            RecentActivitiesListView.ItemsSource = RecentActivities;

            Loaded += DashboardView_Loaded;
        }

        private async void DashboardView_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDashboardDataAsync();
        }

        private async Task LoadDashboardDataAsync()
        {
            try
            {
                // 加载统计数据
                await LoadStatisticsAsync();
                
                // 加载最近活动
                await LoadRecentActivitiesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载仪表板数据时发生错误");
                MessageBox.Show("加载数据时发生错误，请稍后重试。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadStatisticsAsync()
        {
            try
            {
                // 获取题库数量
                var questionBanks = await _questionBankService.GetAllQuestionBanksAsync();
                QuestionBankCountTextBlock.Text = questionBanks.Count.ToString();

                // 获取题目数量
                var questions = await _questionService.GetAllQuestionsAsync();
                QuestionCountTextBlock.Text = questions.Count.ToString();

                // 获取试卷数量
                var examPapers = await _examPaperService.GetAllExamPapersAsync();
                ExamPaperCountTextBlock.Text = examPapers.Count.ToString();

                // 获取考试记录数量
                var examRecords = await _examService.GetAllExamRecordsAsync();
                ExamRecordCountTextBlock.Text = examRecords.Count.ToString();
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

                // 模拟最近活动数据
                RecentActivities.Add(new RecentActivity
                {
                    Icon = "📚",
                    Title = "创建了新题库",
                    Description = "数学基础题库",
                    Time = "2小时前"
                });

                RecentActivities.Add(new RecentActivity
                {
                    Icon = "📝",
                    Title = "添加了新题目",
                    Description = "单选题：函数的定义域",
                    Time = "3小时前"
                });

                RecentActivities.Add(new RecentActivity
                {
                    Icon = "📄",
                    Title = "创建了新试卷",
                    Description = "期中考试试卷",
                    Time = "1天前"
                });

                RecentActivities.Add(new RecentActivity
                {
                    Icon = "👥",
                    Title = "学生完成考试",
                    Description = "张三完成了数学测试",
                    Time = "2天前"
                });

                RecentActivities.Add(new RecentActivity
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

        // 快速操作按钮事件处理
        private void CreateQuestionBankButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: 打开创建题库对话框
            MessageBox.Show("创建题库功能", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AddQuestionButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: 打开添加题目对话框
            MessageBox.Show("添加题目功能", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CreateExamPaperButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: 打开创建试卷页面
            MessageBox.Show("创建试卷功能", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ViewStatisticsButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: 打开统计页面
            MessageBox.Show("查看统计功能", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void UserManagementButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: 打开用户管理页面
            MessageBox.Show("用户管理功能", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SystemSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: 打开系统设置页面
            MessageBox.Show("系统设置功能", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void DataBackupButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: 执行数据备份
            MessageBox.Show("数据备份功能", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void HelpDocumentButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: 打开帮助文档
            MessageBox.Show("帮助文档功能", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    // 最近活动数据模型
    public class RecentActivity
    {
        public string Icon { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Time { get; set; }
    }
}