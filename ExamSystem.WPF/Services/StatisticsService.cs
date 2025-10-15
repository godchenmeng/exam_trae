using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExamSystem.WPF.Services
{
    public class StatisticsService : IStatisticsService
    {
        public async Task<int> GetTotalStudentsAsync()
        {
            // 模拟异步操作
            await Task.Delay(100);
            return 150;
        }

        public async Task<double> GetAverageScoreAsync()
        {
            await Task.Delay(100);
            return 78.5;
        }

        public async Task<double> GetPassRateAsync()
        {
            await Task.Delay(100);
            return 85.2;
        }

        public async Task<double> GetExcellentRateAsync()
        {
            await Task.Delay(100);
            return 32.8;
        }

        public async Task<List<QuestionAnalysisModel>> GetQuestionAnalysisAsync()
        {
            await Task.Delay(100);
            return new List<QuestionAnalysisModel>
            {
                new QuestionAnalysisModel
                {
                    QuestionNumber = 1,
                    QuestionType = "单选题",
                    Content = "以下哪个是正确的？",
                    CorrectRate = 85.5,
                    DifficultyCoefficient = 0.85,
                    Discrimination = 0.45,
                    DifficultyLevel = "容易"
                },
                new QuestionAnalysisModel
                {
                    QuestionNumber = 2,
                    QuestionType = "多选题",
                    Content = "选择所有正确答案",
                    CorrectRate = 62.3,
                    DifficultyCoefficient = 0.62,
                    Discrimination = 0.38,
                    DifficultyLevel = "中等"
                }
            };
        }

        public async Task<List<StudentRankingModel>> GetStudentRankingAsync()
        {
            await Task.Delay(100);
            return new List<StudentRankingModel>
            {
                new StudentRankingModel
                {
                    Rank = 1,
                    StudentId = "2021001",
                    Name = "张三",
                    Class = "计算机1班",
                    TotalScore = 95.5,
                    ObjectiveScore = 48.5,
                    SubjectiveScore = 47.0,
                    ExamDuration = "45分钟",
                    SubmitTime = "2024-01-15 10:45",
                    Grade = "优秀"
                },
                new StudentRankingModel
                {
                    Rank = 2,
                    StudentId = "2021002",
                    Name = "李四",
                    Class = "计算机1班",
                    TotalScore = 88.0,
                    ObjectiveScore = 44.0,
                    SubjectiveScore = 44.0,
                    ExamDuration = "52分钟",
                    SubmitTime = "2024-01-15 10:52",
                    Grade = "良好"
                }
            };
        }
    }
}