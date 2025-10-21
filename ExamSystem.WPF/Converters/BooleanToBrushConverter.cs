using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ExamSystem.WPF.Converters
{
    /// <summary>
    /// 将布尔值转换为画刷：
    /// true => 已读 => 灰色；false => 未读 => 主题色(蓝色)
    /// </summary>
    public class BooleanToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var isRead = value is bool b && b;
            // 可通过 parameter 指定颜色，如 parameter="Green" 或 "#FF0000"
            if (parameter is string colorParam && TryParseColor(colorParam, out var overrideBrush))
            {
                return overrideBrush;
            }
            return isRead ? new SolidColorBrush(Color.FromRgb(189, 189, 189)) // 灰色
                          : new SolidColorBrush(Color.FromRgb(25, 118, 210)); // 蓝色 #1976D2
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 单向转换
            return Binding.DoNothing;
        }

        private static bool TryParseColor(string input, out SolidColorBrush brush)
        {
            try
            {
                if (input.StartsWith("#", StringComparison.Ordinal))
                {
                    var color = (Color)ColorConverter.ConvertFromString(input);
                    brush = new SolidColorBrush(color);
                    return true;
                }
                var prop = typeof(Colors).GetProperty(input);
                if (prop != null)
                {
                    var color = (Color)prop.GetValue(null);
                    brush = new SolidColorBrush(color);
                    return true;
                }
            }
            catch { }
            brush = new SolidColorBrush(Color.FromRgb(0, 0, 0));
            return false;
        }
    }
}