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

    // 新增：布尔/字符串 true/false 到中文“正确/错误”文本转换器
    public class TrueFalseToChineseTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;

            if (value is bool b)
            {
                return b ? "正确" : "错误";
            }

            if (value is string s)
            {
                var v = s.Trim();
                if (string.Equals(v, "true", StringComparison.OrdinalIgnoreCase) || v == "1")
                    return "正确";
                if (string.Equals(v, "false", StringComparison.OrdinalIgnoreCase) || v == "0")
                    return "错误";
                return v; // 其它字符串原样返回
            }

            return value.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s)
            {
                var v = s.Trim();
                if (v == "正确") return true;
                if (v == "错误") return false;
            }
            return Binding.DoNothing;
        }
    }

    // 新增：分数百分比颜色转换器（>=80% 绿 #52c41a，60-79% 蓝 #1890ff，<60% 红 #f5222d）
    public class ScorePercentageBrushConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2) return new SolidColorBrush(Colors.Gray);
            if (values[0] == null || values[1] == null) return new SolidColorBrush(Colors.Gray);
    
            try
            {
                var score = System.Convert.ToDecimal(values[0]);
                var max = System.Convert.ToDecimal(values[1]);
                if (max <= 0) return new SolidColorBrush(Colors.Gray);
                var percent = (double)(score / max * 100m);
    
                // 颜色：#52c41a (82,196,26), #1890ff (24,144,255), #f5222d (245,34,45)
                if (percent >= 80.0) return new SolidColorBrush(Color.FromRgb(82, 196, 26));
                if (percent >= 60.0) return new SolidColorBrush(Color.FromRgb(24, 144, 255));
                return new SolidColorBrush(Color.FromRgb(245, 34, 45));
            }
            catch
            {
                return new SolidColorBrush(Colors.Gray);
            }
        }
    
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return new object[] { Binding.DoNothing, Binding.DoNothing };
        }
    }
    
    // 新增：分数显示为 “X/Y” 的转换器
    public class ScoreFractionTextConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2) return "-/-";
            if (values[0] == null || values[1] == null) return "-/-";
            try
            {
                var score = System.Convert.ToDecimal(values[0]);
                var max = System.Convert.ToDecimal(values[1]);
                return string.Format(culture, "{0:0.##}/{1:0.##}", score, max);
            }
            catch
            {
                return "-/-";
            }
        }
    
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return new object[] { Binding.DoNothing, Binding.DoNothing };
        }
    }

    // 新增：选择题可见性转换器
    public class ChoiceQuestionVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ExamSystem.Domain.Enums.QuestionType questionType)
            {
                return questionType == ExamSystem.Domain.Enums.QuestionType.SingleChoice || 
                       questionType == ExamSystem.Domain.Enums.QuestionType.MultipleChoice
                    ? Visibility.Visible 
                    : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}