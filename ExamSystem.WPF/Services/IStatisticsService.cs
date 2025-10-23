using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExamSystem.WPF.Services
{
    public interface IStatisticsService
    {
        Task<int> GetTotalStudentsAsync();
        Task<double> GetAverageScoreAsync();
        Task<double> GetPassRateAsync();
        Task<double> GetExcellentRateAsync();
        Task<List<QuestionAnalysisModel>> GetQuestionAnalysisAsync();
        Task<List<StudentRankingModel>> GetStudentRankingAsync();
    }

    public class QuestionAnalysisModel
    {
        public int QuestionNumber { get; set; }
        public string QuestionType { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public double CorrectRate { get; set; }
        public double DifficultyCoefficient { get; set; }
        public double Discrimination { get; set; }
        public string DifficultyLevel { get; set; } = string.Empty;
    }

    public class StudentRankingModel
    {
        public int Rank { get; set; }
        public string StudentId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Class { get; set; } = string.Empty;
        public double TotalScore { get; set; }
        public double ObjectiveScore { get; set; }
        public double SubjectiveScore { get; set; }
        public string ExamDuration { get; set; } = string.Empty;
        public string SubmitTime { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
    }
}