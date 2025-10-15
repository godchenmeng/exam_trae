using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ExamSystem.WPF.Views
{
    /// <summary>
    /// 转换器类，包含常用的WPF转换器
    /// </summary>
    public static class Converters
    {
        /// <summary>
        /// 布尔值到可见性转换器
        /// </summary>
        public static readonly BooleanToVisibilityConverter BooleanToVisibilityConverter = new();

        /// <summary>
        /// 字符串到可见性转换器
        /// </summary>
        public static readonly StringToVisibilityConverter StringToVisibilityConverter = new();
    }

    /// <summary>
    /// 布尔值到可见性转换器
    /// 支持反转参数
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not bool boolValue)
                return Visibility.Collapsed;

            // 检查是否需要反转
            bool invert = parameter?.ToString()?.Equals("Invert", StringComparison.OrdinalIgnoreCase) == true;
            
            if (invert)
                boolValue = !boolValue;

            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not Visibility visibility)
                return false;

            bool result = visibility == Visibility.Visible;
            
            // 检查是否需要反转
            bool invert = parameter?.ToString()?.Equals("Invert", StringComparison.OrdinalIgnoreCase) == true;
            
            if (invert)
                result = !result;

            return result;
        }
    }

    /// <summary>
    /// 字符串到可见性转换器
    /// 空字符串或null时隐藏，否则显示
    /// </summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                return string.IsNullOrWhiteSpace(str) ? Visibility.Collapsed : Visibility.Visible;
            }

            return value == null ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("StringToVisibilityConverter does not support ConvertBack");
        }
    }
}