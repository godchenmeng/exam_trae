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
        public string QuestionType { get; set; }
        public string Content { get; set; }
        public double CorrectRate { get; set; }
        public double DifficultyCoefficient { get; set; }
        public double Discrimination { get; set; }
        public string DifficultyLevel { get; set; }
    }

    public class StudentRankingModel
    {
        public int Rank { get; set; }
        public string StudentId { get; set; }
        public string Name { get; set; }
        public string Class { get; set; }
        public double TotalScore { get; set; }
        public double ObjectiveScore { get; set; }
        public double SubjectiveScore { get; set; }
        public string ExamDuration { get; set; }
        public string SubmitTime { get; set; }
        public string Grade { get; set; }
    }
}