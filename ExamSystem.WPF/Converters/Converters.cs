using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ExamSystem.WPF.Converters
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

        /// <summary>
        /// 布尔值到True转换器
        /// </summary>
        public static readonly BooleanToTrueConverter BooleanToTrueConverter = new();

        /// <summary>
        /// 布尔值到False转换器
        /// </summary>
        public static readonly BooleanToFalseConverter BooleanToFalseConverter = new();

        /// <summary>
        /// 反向布尔值到可见性转换器
        /// </summary>
        public static readonly InverseBooleanToVisibilityConverter InverseBooleanToVisibilityConverter = new();

        /// <summary>
        /// 通过/失败样式转换器
        /// </summary>
        public static readonly PassedFailedStyleConverter PassedFailedStyleConverter = new();

        /// <summary>
        /// 布尔值到背景色转换器
        /// </summary>
        public static readonly BooleanToBackgroundConverter BooleanToBackgroundConverter = new();

        /// <summary>
        /// 判断题答案转换器
        /// </summary>
        public static readonly TrueFalseAnswerConverter TrueFalseAnswerConverter = new();
    }

    /// <summary>
    /// 字符串到可见性转换器
    /// </summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 布尔值到True转换器
    /// </summary>
    public class BooleanToTrueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool boolValue && boolValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 当“正确”单选按钮被勾选时，写回 True；未勾选时不改变源值（避免在切换到“错误”时写回 False 两次）
            if (value is bool isChecked)
            {
                return isChecked ? true : Binding.DoNothing;
            }
            return Binding.DoNothing;
        }
    }

    /// <summary>
    /// 布尔值到False转换器
    /// </summary>
    public class BooleanToFalseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool boolValue && !boolValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 当“错误”单选按钮被勾选时，写回 False；未勾选时不改变源值
            if (value is bool isChecked)
            {
                return isChecked ? false : Binding.DoNothing;
            }
            return Binding.DoNothing;
        }
    }

    /// <summary>
    /// 反向布尔值到可见性转换器
    /// </summary>
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Collapsed;
            }
            return false;
        }
    }

    /// <summary>
    /// 通过/失败样式转换器
    /// </summary>
    public class PassedFailedStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool passed)
            {
                return passed ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 布尔值到背景色转换器
    /// </summary>
    public class BooleanToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isReadOnly && isReadOnly)
            {
                return new SolidColorBrush(Color.FromRgb(245, 245, 245)); // 浅灰色背景
            }
            return new SolidColorBrush(Colors.White);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 判断题答案转换器，用于 RadioButton 绑定
    /// </summary>
    public class TrueFalseAnswerConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && parameter is string paramStr)
            {
                if (string.Equals(paramStr, "true", StringComparison.OrdinalIgnoreCase))
                    return boolValue == true;
                if (string.Equals(paramStr, "false", StringComparison.OrdinalIgnoreCase))
                    return boolValue == false;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isChecked && isChecked && parameter is string paramStr)
            {
                if (string.Equals(paramStr, "true", StringComparison.OrdinalIgnoreCase))
                    return true;
                if (string.Equals(paramStr, "false", StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            return Binding.DoNothing;
        }
    }

    // 新增：布尔值到可见性转换器（自定义实现）
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var isTrue = value is bool b && b;
            var invert = parameter is string p && p.Equals("Invert", StringComparison.OrdinalIgnoreCase);
            if (invert) isTrue = !isTrue;
            return isTrue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility v)
            {
                var result = v == Visibility.Visible;
                var invert = parameter is string p && p.Equals("Invert", StringComparison.OrdinalIgnoreCase);
                return invert ? !result : result;
            }
            return false;
        }
    }
}