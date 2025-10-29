using System;
using System.Globalization;
using System.Windows.Data;
using ExamSystem.Domain.Enums;

namespace ExamSystem.WPF.Converters
{
    /// <summary>
    /// 题型枚举到中文转换器
    /// </summary>
    public class QuestionTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is QuestionType questionType)
            {
                return questionType switch
                {
                    QuestionType.SingleChoice => "单选题",
                    QuestionType.MultipleChoice => "多选题",
                    QuestionType.TrueFalse => "判断题",
                    QuestionType.FillInBlank => "填空题",
                    QuestionType.ShortAnswer => "简答题",
                    QuestionType.Essay => "论述题",
                    QuestionType.MapDrawing => "地图绘制题",
                    _ => "未知"
                };
            }
            return "未知";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text)
            {
                return text switch
                {
                    "单选题" => QuestionType.SingleChoice,
                    "多选题" => QuestionType.MultipleChoice,
                    "判断题" => QuestionType.TrueFalse,
                    "填空题" => QuestionType.FillInBlank,
                    "简答题" => QuestionType.ShortAnswer,
                    "论述题" => QuestionType.Essay,
                    "地图绘制题" => QuestionType.MapDrawing,
                    _ => QuestionType.SingleChoice
                };
            }
            return QuestionType.SingleChoice;
        }
    }
}