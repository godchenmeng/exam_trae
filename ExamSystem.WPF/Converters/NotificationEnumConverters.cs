using System;
using System.Globalization;
using System.Windows.Data;
using ExamSystem.Domain.Enums;

namespace ExamSystem.WPF.Converters
{
    /// <summary>
    /// 将 NotificationPriority 枚举值转换为中文文本
    /// </summary>
    public class NotificationPriorityToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is NotificationPriority p)
            {
                return p switch
                {
                    NotificationPriority.Low => "低",
                    NotificationPriority.Normal => "普通",
                    NotificationPriority.High => "高",
                    _ => p.ToString()
                };
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s)
            {
                return s.Trim() switch
                {
                    "低" => NotificationPriority.Low,
                    "普通" => NotificationPriority.Normal,
                    "高" => NotificationPriority.High,
                    _ => Binding.DoNothing
                };
            }
            return Binding.DoNothing;
        }
    }

    /// <summary>
    /// 将 NotificationScope 枚举值转换为中文文本
    /// </summary>
    public class NotificationScopeToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is NotificationScope s)
            {
                return s switch
                {
                    NotificationScope.AllStudents => "全部考生",
                    NotificationScope.SpecificUsers => "指定用户",
                    NotificationScope.AllUsers => "全部用户",
                    _ => s.ToString()
                };
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                return str.Trim() switch
                {
                    "全部考生" => NotificationScope.AllStudents,
                    "指定用户" => NotificationScope.SpecificUsers,
                    "全部用户" => NotificationScope.AllUsers,
                    _ => Binding.DoNothing
                };
            }
            return Binding.DoNothing;
        }
    }
}