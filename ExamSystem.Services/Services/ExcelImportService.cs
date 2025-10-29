using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;
using ExamSystem.Services.Interfaces;
using ExamSystem.Services.Models;
using System.IO.Compression;

namespace ExamSystem.Services.Services
{
    /// <summary>
    /// Excel导入服务实现
    /// </summary>
    public class ExcelImportService : IExcelImportService
    {
        private readonly IQuestionService _questionService;
        private readonly ILogger<ExcelImportService> _logger;

        public ExcelImportService(IQuestionService questionService, ILogger<ExcelImportService> logger)
        {
            _questionService = questionService;
            _logger = logger;
            
            // 设置EPPlus许可证（EPPlus 8及以上版本）
            ExcelPackage.License.SetNonCommercialPersonal("ExamSystem");
        }

        /// <summary>
        /// 导入题目从Excel文件
        /// </summary>
        public async Task<ImportResult> ImportQuestionsFromExcelAsync(Stream fileStream, int questionBankId)
        {
            var result = new ImportResult();
            
            try
            {
                _logger.LogInformation("开始导入题目，题库ID: {QuestionBankId}", questionBankId);

                // 预检测：文件是否为有效的 .xlsx（Open XML Zip 包）
                if (!IsOpenXmlZip(fileStream))
                {
                    result.ErrorMessages.Add("提供的文件不是有效的 Excel 2007+ 工作簿 (.xlsx)。请确保使用 .xlsx 格式，或在 .xls/.csv 通过 Excel 另存为 .xlsx 后再导入。");
                    _logger.LogWarning("Excel文件不是有效的Open XML包，终止导入");
                    return result;
                }

                using var package = new ExcelPackage(fileStream);
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                
                if (worksheet == null)
                {
                    result.ErrorMessages.Add("Excel文件中没有找到工作表");
                    return result;
                }

                // 验证表头
                var headerValidation = ValidateHeaders(worksheet);
                if (!headerValidation.IsSuccess)
                {
                    result.ErrorMessages.AddRange(headerValidation.ErrorMessages);
                    return result;
                }

                // 读取数据行
                var questions = ReadQuestionsFromWorksheet(worksheet);
                result.TotalCount = questions.Count;

                // 验证和导入每个题目
                foreach (var questionDto in questions)
                {
                    try
                    {
                        var validationResult = ValidateQuestionData(questionDto);
                        if (!validationResult.IsSuccess)
                        {
                            result.DetailedErrors[questionDto.RowNumber] = validationResult.ErrorMessages;
                            result.FailedQuestions.Add(new ImportFailureInfo
                            {
                                RowNumber = questionDto.RowNumber,
                                Title = questionDto.Content?.Length > 50 ? questionDto.Content.Substring(0, 50) + "..." : (questionDto.Content ?? string.Empty),
                                ErrorMessage = string.Join("; ", validationResult.ErrorMessages),
                                RawData = $"题型:{questionDto.QuestionType}, 内容:{(questionDto.Content ?? string.Empty)}"
                            });
                            result.FailureCount++;
                            continue;
                        }

                        var question = ConvertToQuestion(questionDto, questionBankId);
                        var success = await _questionService.CreateQuestionAsync(question);
                        
                        if (success)
                        {
                            result.SuccessCount++;
                            result.SuccessfulQuestions.Add(new ImportedQuestionInfo
                            {
                                RowNumber = questionDto.RowNumber,
                                Title = questionDto.Content?.Length > 50 ? questionDto.Content.Substring(0, 50) + "..." : (questionDto.Content ?? string.Empty),
                                QuestionType = questionDto.QuestionType,
                                Difficulty = questionDto.Difficulty,
                                Score = int.TryParse(questionDto.Points, out var score) ? score : 0,
                                Tags = questionDto.Tags ?? string.Empty
                            });
                            _logger.LogDebug("成功导入题目，行号: {RowNumber}", questionDto.RowNumber);
                        }
                        else
                        {
                            result.DetailedErrors[questionDto.RowNumber] = new List<string> { "保存题目到数据库失败" };
                            result.FailedQuestions.Add(new ImportFailureInfo
                            {
                                RowNumber = questionDto.RowNumber,
                                Title = questionDto.Content?.Length > 50 ? questionDto.Content.Substring(0, 50) + "..." : (questionDto.Content ?? string.Empty),
                                ErrorMessage = "保存题目到数据库失败",
                                RawData = $"题型:{questionDto.QuestionType}, 内容:{(questionDto.Content ?? string.Empty)}"
                            });
                            result.FailureCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "导入题目失败，行号: {RowNumber}", questionDto.RowNumber);
                        result.DetailedErrors[questionDto.RowNumber] = new List<string> { $"导入异常: {ex.Message}" };
                        result.FailedQuestions.Add(new ImportFailureInfo
                        {
                            RowNumber = questionDto.RowNumber,
                            Title = questionDto.Content?.Length > 50 ? questionDto.Content.Substring(0, 50) + "..." : (questionDto.Content ?? string.Empty),
                            ErrorMessage = $"导入异常: {ex.Message}",
                            RawData = $"题型:{questionDto.QuestionType}, 内容:{(questionDto.Content ?? string.Empty)}"
                        });
                        result.FailureCount++;
                    }
                }

                result.IsSuccess = result.SuccessCount > 0;
                _logger.LogInformation("题目导入完成，成功: {SuccessCount}，失败: {FailureCount}", 
                    result.SuccessCount, result.FailureCount);
            }
            catch (InvalidDataException ex)
            {
                _logger.LogError(ex, "Excel文件不是有效的Open XML包或可能受保护");
                result.ErrorMessages.Add("Excel文件不是有效的 .xlsx 包（或受密码保护）。请使用未加密的 .xlsx 文件，或在导入前取消保护/提供密码。");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excel导入过程中发生异常");
                result.ErrorMessages.Add($"导入过程异常: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// 验证Excel文件格式
        /// </summary>
        public async Task<ImportResult> ValidateExcelFormatAsync(Stream fileStream)
        {
            var result = new ImportResult();
            
            try
            {
                using var package = new ExcelPackage(fileStream);
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                
                if (worksheet == null)
                {
                    result.ErrorMessages.Add("Excel文件中没有找到工作表");
                    return result;
                }

                // 验证表头
                var headerValidation = ValidateHeaders(worksheet);
                if (!headerValidation.IsSuccess)
                {
                    result.ErrorMessages.AddRange(headerValidation.ErrorMessages);
                    return result;
                }

                // 读取并验证数据
                var questions = ReadQuestionsFromWorksheet(worksheet);
                result.TotalCount = questions.Count;

                foreach (var questionDto in questions)
                {
                    var validationResult = ValidateQuestionData(questionDto);
                    if (!validationResult.IsSuccess)
                    {
                        result.DetailedErrors[questionDto.RowNumber] = validationResult.ErrorMessages;
                        result.FailureCount++;
                    }
                    else
                    {
                        result.SuccessCount++;
                    }
                }

                result.IsSuccess = result.FailureCount == 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证Excel格式时发生异常");
                result.ErrorMessages.Add($"验证异常: {ex.Message}");
            }

            await Task.CompletedTask;
            return result;
        }

        /// <summary>
        /// 获取Excel模板文件
        /// </summary>
        public byte[] GetExcelTemplate()
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("题目导入模板");

            // 设置表头
            var headers = new[]
            {
                "题型", "题目内容", "选项A", "选项B", "选项C", "选项D", "选项E", "选项F",
                "正确答案", "题目解析", "分值", "难度", "标签"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
                worksheet.Cells[1, i + 1].Style.Font.Bold = true;
            }

            // 添加示例数据
            worksheet.Cells[2, 1].Value = "单选";
            worksheet.Cells[2, 2].Value = "以下哪个是C#的关键字？";
            worksheet.Cells[2, 3].Value = "class";
            worksheet.Cells[2, 4].Value = "Class";
            worksheet.Cells[2, 5].Value = "CLASS";
            worksheet.Cells[2, 6].Value = "clazz";
            worksheet.Cells[2, 9].Value = "A";
            worksheet.Cells[2, 10].Value = "class是C#的关键字，用于定义类";
            worksheet.Cells[2, 11].Value = "5";
            worksheet.Cells[2, 12].Value = "简单";
            worksheet.Cells[2, 13].Value = "C#,基础语法";

            worksheet.Cells[3, 1].Value = "多选";
            worksheet.Cells[3, 2].Value = "以下哪些是面向对象的特征？";
            worksheet.Cells[3, 3].Value = "封装";
            worksheet.Cells[3, 4].Value = "继承";
            worksheet.Cells[3, 5].Value = "多态";
            worksheet.Cells[3, 6].Value = "抽象";
            worksheet.Cells[3, 9].Value = "ABCD";
            worksheet.Cells[3, 10].Value = "封装、继承、多态、抽象都是面向对象的基本特征";
            worksheet.Cells[3, 11].Value = "10";
            worksheet.Cells[3, 12].Value = "中等";
            worksheet.Cells[3, 13].Value = "面向对象,基础概念";

            worksheet.Cells[4, 1].Value = "判断";
            worksheet.Cells[4, 2].Value = "C#是一种面向对象的编程语言";
            worksheet.Cells[4, 9].Value = "正确";
            worksheet.Cells[4, 10].Value = "C#确实是一种面向对象的编程语言";
            worksheet.Cells[4, 11].Value = "3";
            worksheet.Cells[4, 12].Value = "简单";
            worksheet.Cells[4, 13].Value = "C#,基础概念";

            worksheet.Cells[5, 1].Value = "填空";
            worksheet.Cells[5, 2].Value = "C#中用于定义类的关键字是____";
            worksheet.Cells[5, 9].Value = "class";
            worksheet.Cells[5, 10].Value = "class关键字用于定义类";
            worksheet.Cells[5, 11].Value = "5";
            worksheet.Cells[5, 12].Value = "简单";
            worksheet.Cells[5, 13].Value = "C#,关键字";

            worksheet.Cells[6, 1].Value = "主观";
            worksheet.Cells[6, 2].Value = "请简述面向对象编程的优点";
            worksheet.Cells[6, 9].Value = "面向对象编程具有封装性、继承性、多态性等特点，能够提高代码的可重用性、可维护性和可扩展性";
            worksheet.Cells[6, 10].Value = "参考答案要点：封装、继承、多态、代码重用、维护性等";
            worksheet.Cells[6, 11].Value = "15";
            worksheet.Cells[6, 12].Value = "困难";
            worksheet.Cells[6, 13].Value = "面向对象,理论";

            // 自动调整列宽
            worksheet.Cells.AutoFitColumns();

            return package.GetAsByteArray();
        }

        /// <summary>
        /// 验证表头
        /// </summary>
        private ImportResult ValidateHeaders(ExcelWorksheet worksheet)
        {
            var result = new ImportResult();
            var expectedHeaders = new[]
            {
                "题型", "题目内容", "选项A", "选项B", "选项C", "选项D", "选项E", "选项F",
                "正确答案", "题目解析", "分值", "难度", "标签"
            };

            for (int i = 0; i < expectedHeaders.Length; i++)
            {
                var cellValue = worksheet.Cells[1, i + 1].Value?.ToString()?.Trim();
                if (cellValue != expectedHeaders[i])
                {
                    result.ErrorMessages.Add($"第{i + 1}列表头应为'{expectedHeaders[i]}'，实际为'{cellValue}'");
                }
            }

            result.IsSuccess = result.ErrorMessages.Count == 0;
            return result;
        }

        /// <summary>
        /// 从工作表读取题目数据
        /// </summary>
        private List<QuestionImportDto> ReadQuestionsFromWorksheet(ExcelWorksheet worksheet)
        {
            var questions = new List<QuestionImportDto>();
            var rowCount = worksheet.Dimension?.Rows ?? 0;

            for (int row = 2; row <= rowCount; row++)
            {
                // 检查是否为空行
                if (IsEmptyRow(worksheet, row))
                    continue;

                var question = new QuestionImportDto
                {
                    RowNumber = row,
                    QuestionType = GetCellValue(worksheet, row, 1),
                    Content = GetCellValue(worksheet, row, 2),
                    OptionA = GetCellValue(worksheet, row, 3),
                    OptionB = GetCellValue(worksheet, row, 4),
                    OptionC = GetCellValue(worksheet, row, 5),
                    OptionD = GetCellValue(worksheet, row, 6),
                    OptionE = GetCellValue(worksheet, row, 7),
                    OptionF = GetCellValue(worksheet, row, 8),
                    CorrectAnswer = GetCellValue(worksheet, row, 9),
                    Explanation = GetCellValue(worksheet, row, 10),
                    Points = GetCellValue(worksheet, row, 11),
                    Difficulty = GetCellValue(worksheet, row, 12),
                    Tags = GetCellValue(worksheet, row, 13)
                };

                questions.Add(question);
            }

            return questions;
        }

        /// <summary>
        /// 检查是否为空行
        /// </summary>
        private bool IsEmptyRow(ExcelWorksheet worksheet, int row)
        {
            for (int col = 1; col <= 13; col++)
            {
                if (!string.IsNullOrWhiteSpace(GetCellValue(worksheet, row, col)))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 获取单元格值
        /// </summary>
        private string GetCellValue(ExcelWorksheet worksheet, int row, int col)
        {
            return worksheet.Cells[row, col].Value?.ToString()?.Trim() ?? string.Empty;
        }

        /// <summary>
        /// 验证题目数据
        /// </summary>
        private ImportResult ValidateQuestionData(QuestionImportDto dto)
        {
            var result = new ImportResult();

            // 验证题型
            if (string.IsNullOrWhiteSpace(dto.QuestionType))
            {
                result.ErrorMessages.Add("题型不能为空");
            }
            else if (!IsValidQuestionType(dto.QuestionType))
            {
                result.ErrorMessages.Add($"无效的题型: {dto.QuestionType}，支持的题型: 单选、多选、判断、填空、主观");
            }

            // 验证题目内容
            if (string.IsNullOrWhiteSpace(dto.Content))
            {
                result.ErrorMessages.Add("题目内容不能为空");
            }
            else if (dto.Content.Length > 5000)
            {
                result.ErrorMessages.Add("题目内容长度不能超过5000字符");
            }

            // 验证选择题选项
            if (dto.QuestionType == "单选" || dto.QuestionType == "多选")
            {
                if (string.IsNullOrWhiteSpace(dto.OptionA) || string.IsNullOrWhiteSpace(dto.OptionB))
                {
                    result.ErrorMessages.Add("选择题至少需要选项A和选项B");
                }
            }

            // 验证正确答案
            if (string.IsNullOrWhiteSpace(dto.CorrectAnswer))
            {
                result.ErrorMessages.Add("正确答案不能为空");
            }
            else
            {
                var answerValidation = ValidateAnswer(dto.QuestionType, dto.CorrectAnswer, dto);
                if (!answerValidation.IsSuccess)
                {
                    result.ErrorMessages.AddRange(answerValidation.ErrorMessages);
                }
            }

            // 验证分值
            if (string.IsNullOrWhiteSpace(dto.Points))
            {
                result.ErrorMessages.Add("分值不能为空");
            }
            else if (!decimal.TryParse(dto.Points, out var points) || points <= 0 || points > 100)
            {
                result.ErrorMessages.Add("分值必须是0-100之间的数字");
            }

            // 验证难度
            if (string.IsNullOrWhiteSpace(dto.Difficulty))
            {
                result.ErrorMessages.Add("难度不能为空");
            }
            else if (!IsValidDifficulty(dto.Difficulty))
            {
                result.ErrorMessages.Add($"无效的难度: {dto.Difficulty}，支持的难度: 简单、中等、困难");
            }

            result.IsSuccess = result.ErrorMessages.Count == 0;
            return result;
        }

        /// <summary>
        /// 验证题型是否有效
        /// </summary>
        private bool IsValidQuestionType(string questionType)
        {
            return questionType switch
            {
                "单选" or "多选" or "判断" or "填空" or "主观" or "地图绘制" => true,
                _ => false
            };
        }

        /// <summary>
        /// 验证难度是否有效
        /// </summary>
        private bool IsValidDifficulty(string difficulty)
        {
            return difficulty switch
            {
                "简单" or "中等" or "困难" => true,
                _ => false
            };
        }

        /// <summary>
        /// 验证答案格式
        /// </summary>
        private ImportResult ValidateAnswer(string questionType, string answer, QuestionImportDto dto)
        {
            var result = new ImportResult();

            switch (questionType)
            {
                case "单选":
                    if (!answer.All(c => "ABCDEF".Contains(c)) || answer.Length != 1)
                    {
                        result.ErrorMessages.Add("单选题答案必须是A、B、C、D、E、F中的一个");
                    }
                    break;

                case "多选":
                    if (!answer.All(c => "ABCDEF".Contains(c)) || answer.Length < 2)
                    {
                        result.ErrorMessages.Add("多选题答案必须是A、B、C、D、E、F中的多个字母组合");
                    }
                    break;

                case "判断":
                    if (answer != "正确" && answer != "错误")
                    {
                        result.ErrorMessages.Add("判断题答案必须是'正确'或'错误'");
                    }
                    break;

                case "填空":
                case "主观":
                    if (answer.Length > 10000)
                    {
                        result.ErrorMessages.Add("答案长度不能超过10000字符");
                    }
                    break;
            }

            result.IsSuccess = result.ErrorMessages.Count == 0;
            return result;
        }

        /// <summary>
        /// 转换为Question实体
        /// </summary>
        private Question ConvertToQuestion(QuestionImportDto dto, int questionBankId)
        {
            // 将Excel中的答案转换为服务层校验所需的格式
            var formattedAnswer = ConvertAnswerToServiceFormat(dto.QuestionType, dto.CorrectAnswer);

            var question = new Question
            {
                BankId = questionBankId,
                Title = BuildTitleFromContent(dto.Content),
                Content = dto.Content,
                QuestionType = ConvertQuestionType(dto.QuestionType),
                Answer = formattedAnswer,
                Analysis = dto.Explanation,
                Score = decimal.Parse(dto.Points),
                Difficulty = ConvertDifficulty(dto.Difficulty),
                Tags = dto.Tags,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                Options = new List<QuestionOption>()
            };

            // 添加选项，并标记正确选项
            if (dto.QuestionType == "单选" || dto.QuestionType == "多选")
            {
                var options = new List<(string key, string value)>
                {
                    ("A", dto.OptionA),
                    ("B", dto.OptionB),
                    ("C", dto.OptionC),
                    ("D", dto.OptionD),
                    ("E", dto.OptionE),
                    ("F", dto.OptionF)
                };

                var correctSet = new HashSet<string>(formattedAnswer
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim().ToUpper()));

                foreach (var (key, value) in options)
                {
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        question.Options.Add(new QuestionOption
                        {
                            OptionLabel = key,
                            Content = value,
                            IsCorrect = correctSet.Contains(key),
                            Question = question
                        });
                    }
                }
            }

            return question;
        }

        // 将Excel答案转换为服务层需要的规范格式
        private string ConvertAnswerToServiceFormat(string questionType, string excelAnswer)
        {
            if (string.IsNullOrWhiteSpace(excelAnswer)) return string.Empty;

            switch (questionType)
            {
                case "判断":
                    // 将中文“正确/错误”转换为"True"/"False"
                    return excelAnswer.Trim() == "正确" ? "True" : "False";
                case "多选":
                    // 支持 "ABCD" 或 "A,B,C,D" 两种格式，统一转换为 "A,B,C,D"
                    var lettersMulti = excelAnswer.ToUpper().Where(c => "ABCDEF".Contains(c)).Distinct().ToArray();
                    return string.Join(",", lettersMulti);
                case "单选":
                    // 取第一个合法字母作为答案
                    var letter = excelAnswer.ToUpper().FirstOrDefault(c => "ABCDEF".Contains(c));
                    return letter == default(char) ? string.Empty : letter.ToString();
                default:
                    // 填空、主观题直接使用原答案
                    return excelAnswer;
            }
        }

        // 从内容生成标题（去除换行，截断至200字符）
        private string BuildTitleFromContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return string.Empty;
            var title = content.Replace("\r", " ").Replace("\n", " ").Trim();
            if (title.Length > 200) title = title.Substring(0, 200);
            return title;
        }

        /// <summary>
        /// 转换题型
        /// </summary>
        private QuestionType ConvertQuestionType(string questionType)
        {
            return questionType switch
            {
                "单选" => QuestionType.SingleChoice,
                "多选" => QuestionType.MultipleChoice,
                "判断" => QuestionType.TrueFalse,
                "填空" => QuestionType.FillInBlank,
                "主观" => QuestionType.Essay,
                "地图绘制" => QuestionType.MapDrawing,
                _ => QuestionType.SingleChoice
            };
        }

        /// <summary>
        /// 转换难度
        /// </summary>
        private Difficulty ConvertDifficulty(string difficulty)
        {
            return difficulty switch
            {
                "简单" => Difficulty.Easy,
                "中等" => Difficulty.Medium,
                "困难" => Difficulty.Hard,
                _ => Difficulty.Medium
            };
        }

        // 验证文件是否为有效的 .xlsx（Open XML Zip 包）
        private bool IsOpenXmlZip(Stream fileStream)
        {
            long? originalPos = null;
            try
            {
                if (fileStream.CanSeek)
                {
                    originalPos = fileStream.Position;
                }
                using var zip = new ZipArchive(fileStream, ZipArchiveMode.Read, true);
                // .xlsx 至少应包含 [Content_Types].xml 文件
                return zip.Entries.Any(e => string.Equals(e.FullName, "[Content_Types].xml", StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return false;
            }
            finally
            {
                if (originalPos.HasValue && fileStream.CanSeek)
                {
                    fileStream.Position = originalPos.Value;
                }
            }
        }
    }
}