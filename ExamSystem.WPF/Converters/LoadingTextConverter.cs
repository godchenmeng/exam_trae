using System;
using System.Globalization;
using System.Windows.Data;

namespace ExamSystem.WPF.Converters
{
    public class LoadingTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isLoading)
            {
                return isLoading ? "登录中..." : "登录";
            }
            return "登录";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}