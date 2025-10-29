using System;
using System.Globalization;
using System.Windows.Data;

namespace ExamSystem.WPF.Views
{
    /// <summary>
    /// 数值乘法转换器，将输入值乘以参数
    /// </summary>
    public class MultiplyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double doubleValue && parameter is string paramString)
            {
                if (double.TryParse(paramString, out double multiplier))
                {
                    return doubleValue * multiplier;
                }
            }
            
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double doubleValue && parameter is string paramString)
            {
                if (double.TryParse(paramString, out double multiplier) && multiplier != 0)
                {
                    return doubleValue / multiplier;
                }
            }
            
            return value;
        }
    }
}