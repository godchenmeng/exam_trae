using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;
using ExamSystem.Services.Interfaces;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace ExamSystem.Services.Services
{
    /// <summary>
    /// Excel导出服务实现
    /// </summary>
    public class ExcelExportService : IExcelExportService
    {
        private readonly IQuestionService _questionService;
        private readonly IQuestionBankService _questionBankService;
        private readonly ILogger<ExcelExportService> _logger;

        public ExcelExportService(
            IQuestionService questionService,
            IQuestionBankService questionBankService,
            ILogger<ExcelExportService> logger)
        {
            _questionService = questionService;
            _questionBankService = questionBankService;
            _logger = logger;
            
            // 设置EPPlus许可证（EPPlus 8及以上版本）
            ExcelPackage.License.SetNonCommercialPersonal("ExamSystem");
        }

        /// <summary>
        /// 导出题目到Excel文件
        /// </summary>
        public async Task<byte[]> ExportQuestionsToExcelAsync(IEnumerable<Question> questions, string questionBankName)
        {
            try
            {
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("题目列表");

                // 设置表头
                SetupQuestionHeaders(worksheet);

                // 填充数据
                var questionList = questions.ToList();
                for (int i = 0; i < questionList.Count; i++)
                {
                    var question = questionList[i];
                    var row = i + 2; // 从第2行开始（第1行是表头）

                    worksheet.Cells[row, 1].Value = question.Title;
                    worksheet.Cells[row, 3].Value = GetQuestionTypeDisplayName(question.QuestionType);
                    worksheet.Cells[row, 3].Value = FormatQuestionOptions(question);
                    worksheet.Cells[row, 4].Value = question.Answer;
                worksheet.Cells[row, 5].Value = question.Analysis ?? "";
                    worksheet.Cells[row, 6].Value = question.Score;
                    worksheet.Cells[row, 7].Value = GetDifficultyDisplayName(question.Difficulty);
                    worksheet.Cells[row, 8].Value = string.Join(", ", question.Tags?.Split(',') ?? Array.Empty<string>());
                    worksheet.Cells[row, 9].Value = question.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                }

                // 设置样式
                ApplyQuestionListStyling(worksheet, questionList.Count);

                // 添加统计信息工作表
                await AddStatisticsWorksheet(package, questionList, questionBankName);

                _logger.LogInformation($"成功导出 {questionList.Count} 道题目到Excel文件");
                return package.GetAsByteArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出题目到Excel时发生错误");
                throw;
            }
        }

        /// <summary>
        /// 导出题库统计信息到Excel
        /// </summary>
        public async Task<byte[]> ExportQuestionBankStatisticsAsync(int questionBankId)
        {
            try
            {
                var questionBank = await _questionBankService.GetQuestionBankByIdAsync(questionBankId);
                if (questionBank == null)
                {
                    throw new ArgumentException($"题库 {questionBankId} 不存在");
                }

                var questions = await _questionService.GetQuestionsByBankIdAsync(questionBankId);
                
                using var package = new ExcelPackage();
                
                // 添加统计概览
                await AddStatisticsOverviewWorksheet(package, questionBank, questions);
                
                // 添加题型分布
                AddQuestionTypeDistributionWorksheet(package, questions);
                
                // 添加难度分布
                AddDifficultyDistributionWorksheet(package, questions);
                
                // 添加标签统计
                AddTagStatisticsWorksheet(package, questions);

                _logger.LogInformation($"成功导出题库 {questionBank.Name} 的统计信息");
                return package.GetAsByteArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"导出题库 {questionBankId} 统计信息时发生错误");
                throw;
            }
        }

        /// <summary>
        /// 导出题目模板文件
        /// </summary>
        public byte[] ExportQuestionTemplate()
        {
            try
            {
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("题目导入模板");

                // 设置表头
                SetupQuestionHeaders(worksheet);

                // 添加示例数据
                AddTemplateExampleData(worksheet);

                // 添加说明工作表
                AddTemplateInstructionWorksheet(package);

                // 设置样式
                ApplyTemplateStyling(worksheet);

                _logger.LogInformation("成功生成题目导入模板");
                return package.GetAsByteArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成题目导入模板时发生错误");
                throw;
            }
        }

        /// <summary>
        /// 导出选定题目到Excel
        /// </summary>
        public async Task<byte[]> ExportSelectedQuestionsAsync(IEnumerable<int> questionIds, string questionBankName)
        {
            try
            {
                var questions = new List<Question>();
                foreach (var id in questionIds)
                {
                    var question = await _questionService.GetQuestionByIdAsync(id);
                    if (question != null)
                    {
                        questions.Add(question);
                    }
                }

                return await ExportQuestionsToExcelAsync(questions, questionBankName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出选定题目时发生错误");
                throw;
            }
        }

        #region 私有辅助方法

        /// <summary>
        /// 设置题目列表表头
        /// </summary>
        private void SetupQuestionHeaders(ExcelWorksheet worksheet)
        {
            var headers = new[]
            {
                "题目内容", "题型", "选项", "正确答案", "解析", "分值", "难度", "标签", "创建时间"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
            }
        }

        /// <summary>
        /// 格式化题目选项
        /// </summary>
        private string FormatQuestionOptions(Question question)
        {
            if (question.QuestionType == QuestionType.SingleChoice || question.QuestionType == QuestionType.MultipleChoice)
            {
                return string.Join("; ", question.Options?.Select(o => $"{o.Content}") ?? new List<string>());
            }
            return "";
        }

        /// <summary>
        /// 获取题型显示名称
        /// </summary>
        private string GetQuestionTypeDisplayName(QuestionType type)
        {
            return type switch
            {
                QuestionType.SingleChoice => "单选题",
                QuestionType.MultipleChoice => "多选题",
                QuestionType.TrueFalse => "判断题",
                QuestionType.FillInBlank => "填空题",
                QuestionType.Essay => "问答题",
                _ => "未知"
            };
        }

        /// <summary>
        /// 获取难度显示名称
        /// </summary>
        private string GetDifficultyDisplayName(Difficulty difficulty)
        {
            return difficulty switch
            {
                Difficulty.Easy => "简单",
                Difficulty.Medium => "中等",
                Difficulty.Hard => "困难",
                _ => "未知"
            };
        }

        /// <summary>
        /// 应用题目列表样式
        /// </summary>
        private void ApplyQuestionListStyling(ExcelWorksheet worksheet, int dataRowCount)
        {
            // 设置表头样式
            using (var headerRange = worksheet.Cells[1, 1, 1, 9])
            {
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerRange.Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                headerRange.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            // 设置数据区域边框
            if (dataRowCount > 0)
            {
                using (var dataRange = worksheet.Cells[1, 1, dataRowCount + 1, 9])
                {
                    dataRange.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    dataRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    dataRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    dataRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    dataRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }
            }

            // 自动调整列宽
            worksheet.Cells.AutoFitColumns();
            
            // 设置最大列宽
            for (int col = 1; col <= 9; col++)
            {
                if (worksheet.Column(col).Width > 50)
                {
                    worksheet.Column(col).Width = 50;
                }
            }
        }

        /// <summary>
        /// 添加统计信息工作表
        /// </summary>
        private async Task AddStatisticsWorksheet(ExcelPackage package, List<Question> questions, string questionBankName)
        {
            var statsWorksheet = package.Workbook.Worksheets.Add("统计信息");
            
            // 基本信息
            statsWorksheet.Cells[1, 1].Value = "题库名称";
            statsWorksheet.Cells[1, 2].Value = questionBankName;
            statsWorksheet.Cells[2, 1].Value = "题目总数";
            statsWorksheet.Cells[2, 2].Value = questions.Count;
            statsWorksheet.Cells[3, 1].Value = "导出时间";
            statsWorksheet.Cells[3, 2].Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // 题型统计
            var typeStats = questions.GroupBy(q => q.QuestionType)
                .Select(g => new { Type = GetQuestionTypeDisplayName(g.Key), Count = g.Count() })
                .ToList();

            statsWorksheet.Cells[5, 1].Value = "题型分布";
            for (int i = 0; i < typeStats.Count; i++)
            {
                statsWorksheet.Cells[6 + i, 1].Value = typeStats[i].Type;
                statsWorksheet.Cells[6 + i, 2].Value = typeStats[i].Count;
            }

            // 难度统计
            var difficultyStats = questions.GroupBy(q => q.Difficulty)
                .Select(g => new { Difficulty = GetDifficultyDisplayName(g.Key), Count = g.Count() })
                .ToList();

            int startRow = 6 + typeStats.Count + 2;
            statsWorksheet.Cells[startRow, 1].Value = "难度分布";
            for (int i = 0; i < difficultyStats.Count; i++)
            {
                statsWorksheet.Cells[startRow + 1 + i, 1].Value = difficultyStats[i].Difficulty;
                statsWorksheet.Cells[startRow + 1 + i, 2].Value = difficultyStats[i].Count;
            }
        }

        /// <summary>
        /// 添加统计概览工作表
        /// </summary>
        private async Task AddStatisticsOverviewWorksheet(ExcelPackage package, QuestionBank questionBank, IEnumerable<Question> questions)
        {
            var worksheet = package.Workbook.Worksheets.Add("统计概览");
            var questionList = questions.ToList();

            // 基本信息
            worksheet.Cells[1, 1].Value = "题库统计报告";
            worksheet.Cells[1, 1].Style.Font.Size = 16;
            worksheet.Cells[1, 1].Style.Font.Bold = true;

            worksheet.Cells[3, 1].Value = "题库名称：";
            worksheet.Cells[3, 2].Value = questionBank.Name;
            worksheet.Cells[4, 1].Value = "题库描述：";
            worksheet.Cells[4, 2].Value = questionBank.Description ?? "";
            worksheet.Cells[5, 1].Value = "题目总数：";
            worksheet.Cells[5, 2].Value = questionList.Count;
            worksheet.Cells[6, 1].Value = "创建时间：";
            worksheet.Cells[6, 2].Value = questionBank.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
            worksheet.Cells[7, 1].Value = "统计时间：";
            worksheet.Cells[7, 2].Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // 总分统计
            var totalScore = questionList.Sum(q => q.Score);
            var avgScore = questionList.Count > 0 ? questionList.Average(q => q.Score) : 0;
            
            worksheet.Cells[9, 1].Value = "分值统计：";
            worksheet.Cells[10, 1].Value = "总分值：";
            worksheet.Cells[10, 2].Value = totalScore;
            worksheet.Cells[11, 1].Value = "平均分值：";
            worksheet.Cells[11, 2].Value = Math.Round(avgScore, 2);
        }

        /// <summary>
        /// 添加题型分布工作表
        /// </summary>
        private void AddQuestionTypeDistributionWorksheet(ExcelPackage package, IEnumerable<Question> questions)
        {
            var worksheet = package.Workbook.Worksheets.Add("题型分布");
            var questionList = questions.ToList();

            worksheet.Cells[1, 1].Value = "题型";
            worksheet.Cells[1, 2].Value = "数量";
            worksheet.Cells[1, 3].Value = "占比";

            var typeStats = questionList.GroupBy(q => q.QuestionType)
                .Select(g => new { 
                    Type = GetQuestionTypeDisplayName(g.Key), 
                    Count = g.Count(),
                    Percentage = questionList.Count > 0 ? (double)g.Count() / questionList.Count * 100 : 0
                })
                .OrderByDescending(x => x.Count)
                .ToList();

            for (int i = 0; i < typeStats.Count; i++)
            {
                var row = i + 2;
                worksheet.Cells[row, 1].Value = typeStats[i].Type;
                worksheet.Cells[row, 2].Value = typeStats[i].Count;
                worksheet.Cells[row, 3].Value = $"{typeStats[i].Percentage:F1}%";
            }

            // 设置样式
            using (var headerRange = worksheet.Cells[1, 1, 1, 3])
            {
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerRange.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
            }

            worksheet.Cells.AutoFitColumns();
        }

        /// <summary>
        /// 添加难度分布工作表
        /// </summary>
        private void AddDifficultyDistributionWorksheet(ExcelPackage package, IEnumerable<Question> questions)
        {
            var worksheet = package.Workbook.Worksheets.Add("难度分布");
            var questionList = questions.ToList();

            worksheet.Cells[1, 1].Value = "难度";
            worksheet.Cells[1, 2].Value = "数量";
            worksheet.Cells[1, 3].Value = "占比";

            var difficultyStats = questionList.GroupBy(q => q.Difficulty)
                .Select(g => new { 
                    Difficulty = GetDifficultyDisplayName(g.Key), 
                    Count = g.Count(),
                    Percentage = questionList.Count > 0 ? (double)g.Count() / questionList.Count * 100 : 0
                })
                .OrderBy(x => x.Difficulty)
                .ToList();

            for (int i = 0; i < difficultyStats.Count; i++)
            {
                var row = i + 2;
                worksheet.Cells[row, 1].Value = difficultyStats[i].Difficulty;
                worksheet.Cells[row, 2].Value = difficultyStats[i].Count;
                worksheet.Cells[row, 3].Value = $"{difficultyStats[i].Percentage:F1}%";
            }

            // 设置样式
            using (var headerRange = worksheet.Cells[1, 1, 1, 3])
            {
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerRange.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
            }

            worksheet.Cells.AutoFitColumns();
        }

        /// <summary>
        /// 添加标签统计工作表
        /// </summary>
        private void AddTagStatisticsWorksheet(ExcelPackage package, IEnumerable<Question> questions)
        {
            var worksheet = package.Workbook.Worksheets.Add("标签统计");
            var questionList = questions.ToList();

            worksheet.Cells[1, 1].Value = "标签";
            worksheet.Cells[1, 2].Value = "使用次数";
            worksheet.Cells[1, 3].Value = "占比";

            // 统计标签使用情况
            var allTags = questionList
                .Where(q => !string.IsNullOrEmpty(q.Tags))
                .SelectMany(q => q.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(tag => tag.Trim())
                .Where(tag => !string.IsNullOrEmpty(tag))
                .ToList();

            var tagStats = allTags
                .GroupBy(tag => tag)
                .Select(g => new { 
                    Tag = g.Key, 
                    Count = g.Count(),
                    Percentage = allTags.Count > 0 ? (double)g.Count() / allTags.Count * 100 : 0
                })
                .OrderByDescending(x => x.Count)
                .ToList();

            for (int i = 0; i < tagStats.Count; i++)
            {
                var row = i + 2;
                worksheet.Cells[row, 1].Value = tagStats[i].Tag;
                worksheet.Cells[row, 2].Value = tagStats[i].Count;
                worksheet.Cells[row, 3].Value = $"{tagStats[i].Percentage:F1}%";
            }

            // 设置样式
            using (var headerRange = worksheet.Cells[1, 1, 1, 3])
            {
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerRange.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
            }

            worksheet.Cells.AutoFitColumns();
        }

        /// <summary>
        /// 添加模板示例数据
        /// </summary>
        private void AddTemplateExampleData(ExcelWorksheet worksheet)
        {
            // 单选题示例
            worksheet.Cells[2, 1].Value = "以下哪个是C#的关键字？";
            worksheet.Cells[2, 2].Value = "单选题";
            worksheet.Cells[2, 3].Value = "A.class|B.Class|C.CLASS|D.clazz";
            worksheet.Cells[2, 4].Value = "A";
            worksheet.Cells[2, 5].Value = "class是C#的关键字，用于定义类";
            worksheet.Cells[2, 6].Value = 2;
            worksheet.Cells[2, 7].Value = "简单";
            worksheet.Cells[2, 8].Value = "C#,基础语法";

            // 多选题示例
            worksheet.Cells[3, 1].Value = "以下哪些是面向对象的特征？";
            worksheet.Cells[3, 2].Value = "多选题";
            worksheet.Cells[3, 3].Value = "A.封装|B.继承|C.多态|D.编译";
            worksheet.Cells[3, 4].Value = "A,B,C";
            worksheet.Cells[3, 5].Value = "面向对象的三大特征是封装、继承和多态";
            worksheet.Cells[3, 6].Value = 3;
            worksheet.Cells[3, 7].Value = "中等";
            worksheet.Cells[3, 8].Value = "面向对象,理论";

            // 判断题示例
            worksheet.Cells[4, 1].Value = "C#是一种面向对象的编程语言";
            worksheet.Cells[4, 2].Value = "判断题";
            worksheet.Cells[4, 3].Value = "";
            worksheet.Cells[4, 4].Value = "正确";
            worksheet.Cells[4, 5].Value = "C#确实是一种面向对象的编程语言";
            worksheet.Cells[4, 6].Value = 1;
            worksheet.Cells[4, 7].Value = "简单";
            worksheet.Cells[4, 8].Value = "C#,基础";

            // 填空题示例
            worksheet.Cells[5, 1].Value = "在C#中，使用___关键字来定义一个类";
            worksheet.Cells[5, 2].Value = "填空题";
            worksheet.Cells[5, 3].Value = "";
            worksheet.Cells[5, 4].Value = "class";
            worksheet.Cells[5, 5].Value = "class关键字用于定义类";
            worksheet.Cells[5, 6].Value = 2;
            worksheet.Cells[5, 7].Value = "简单";
            worksheet.Cells[5, 8].Value = "C#,语法";
        }

        /// <summary>
        /// 添加模板说明工作表
        /// </summary>
        private void AddTemplateInstructionWorksheet(ExcelPackage package)
        {
            var worksheet = package.Workbook.Worksheets.Add("使用说明");

            worksheet.Cells[1, 1].Value = "题目导入模板使用说明";
            worksheet.Cells[1, 1].Style.Font.Size = 16;
            worksheet.Cells[1, 1].Style.Font.Bold = true;

            var instructions = new[]
            {
                "",
                "1. 题目内容：必填，题目的具体内容",
                "2. 题型：必填，支持：单选题、多选题、判断题、填空题、问答题",
                "3. 选项：选择题必填，格式为 A.选项1|B.选项2|C.选项3|D.选项4",
                "4. 正确答案：必填",
                "   - 单选题：填写选项字母，如 A",
                "   - 多选题：填写多个选项字母，用逗号分隔，如 A,B,C",
                "   - 判断题：填写 正确 或 错误",
                "   - 填空题：填写正确答案",
                "   - 问答题：填写参考答案",
                "5. 解析：选填，题目的解析说明",
                "6. 分值：必填，题目分值，必须为正数",
                "7. 难度：必填，支持：简单、中等、困难",
                "8. 标签：选填，多个标签用逗号分隔",
                "9. 创建时间：系统自动生成，无需填写",
                "",
                "注意事项：",
                "- 请严格按照模板格式填写",
                "- 题目内容不能为空",
                "- 选择题的选项格式必须正确",
                "- 分值必须为正数",
                "- 导入前请检查数据格式"
            };

            for (int i = 0; i < instructions.Length; i++)
            {
                worksheet.Cells[i + 2, 1].Value = instructions[i];
            }

            worksheet.Cells.AutoFitColumns();
            worksheet.Column(1).Width = 80;
        }

        /// <summary>
        /// 应用模板样式
        /// </summary>
        private void ApplyTemplateStyling(ExcelWorksheet worksheet)
        {
            // 设置表头样式
            using (var headerRange = worksheet.Cells[1, 1, 1, 9])
            {
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerRange.Style.Fill.BackgroundColor.SetColor(Color.LightGreen);
                headerRange.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            // 设置示例数据样式
            using (var exampleRange = worksheet.Cells[2, 1, 5, 9])
            {
                exampleRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                exampleRange.Style.Fill.BackgroundColor.SetColor(Color.LightYellow);
                exampleRange.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.Cells.AutoFitColumns();
        }

        #endregion
    }
}