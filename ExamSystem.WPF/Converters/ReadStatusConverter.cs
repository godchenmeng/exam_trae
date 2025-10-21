using System;
using System.Globalization;
using System.Windows.Data;

namespace ExamSystem.WPF.Converters
{
    /// <summary>
    /// 将布尔值转换为“已读/未读”的显示文本
    /// </summary>
    public class ReadStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var isRead = value is bool b && b;
            return isRead ? "已读" : "未读";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s)
            {
                if (string.Equals(s, "已读", StringComparison.OrdinalIgnoreCase)) return true;
                if (string.Equals(s, "未读", StringComparison.OrdinalIgnoreCase)) return false;
            }
            return Binding.DoNothing;
        }
    }
}