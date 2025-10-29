using System;
using System.Globalization;
using System.Windows.Data;

namespace ExamSystem.WPF.Views
{
    /// <summary>
    /// 数值比较转换器，判断值是否小于参数
    /// </summary>
    public class LessThanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double doubleValue && parameter is string paramString)
            {
                if (double.TryParse(paramString, out double threshold))
                {
                    return doubleValue < threshold;
                }
            }
            
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}