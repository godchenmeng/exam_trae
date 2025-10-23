using System;
using System.Globalization;
using System.Windows.Data;

namespace ExamSystem.WPF.Converters
{
    /// <summary>
    /// 返回第一个绑定的集合值，仅当第二个绑定的布尔值为 true 时；否则返回 Binding.DoNothing。
    /// 用于按条件提供 ItemsControl 的 ItemsSource，从而避免隐藏控件被实例化并参与逻辑。
    /// </summary>
    public class ItemsSourceWhenTrueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return Binding.DoNothing;

            var items = values[0];
            var flag = values[1] is bool b && b;
            return flag ? items : Binding.DoNothing;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            // 单向转换，返回 Binding.DoNothing 以避免回传。
            return new object[] { Binding.DoNothing, Binding.DoNothing };
        }
    }
}