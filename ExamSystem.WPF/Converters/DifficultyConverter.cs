using System;
using System.Globalization;
using System.Windows.Data;
using ExamSystem.Domain.Enums;

namespace ExamSystem.WPF.Converters
{
    /// <summary>
    /// 难度枚举到中文转换器
    /// </summary>
    public class DifficultyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Difficulty difficulty)
            {
                return difficulty switch
                {
                    Difficulty.Easy => "简单",
                    Difficulty.Medium => "中等",
                    Difficulty.Hard => "困难",
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
                    "简单" => Difficulty.Easy,
                    "中等" => Difficulty.Medium,
                    "困难" => Difficulty.Hard,
                    _ => Difficulty.Medium
                };
            }
            return Difficulty.Medium;
        }
    }
}