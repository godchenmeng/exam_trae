using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ExamSystem.WPF.Converters
{
    /// <summary>
    /// 将布尔值转换为画刷：
    /// true => 已读 => 灰色；false => 未读 => 主题色（消防红）
    /// 可通过 parameter 覆盖返回颜色，如 parameter="Green" 或 "#FF0000"
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

            if (isRead)
            {
                return new SolidColorBrush(Color.FromRgb(189, 189, 189)); // 灰色
            }

            // 未读 => 尝试使用全局主题 PrimaryBrush；失败时回退为消防红 #FF3B30
            var primary = TryGetBrushResource("PrimaryBrush") ?? new SolidColorBrush(Color.FromRgb(255, 59, 48));
            return primary;
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

        private static SolidColorBrush? TryGetBrushResource(string key)
        {
            try
            {
                if (Application.Current?.Resources.Contains(key) == true)
                {
                    var brush = Application.Current.Resources[key] as SolidColorBrush;
                    if (brush != null)
                        return brush;
                }
            }
            catch { }
            return null;
        }
    }
}