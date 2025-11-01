using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using ExamSystem.Domain.Entities;
using ExamSystem.WPF.ViewModels;

namespace ExamSystem.WPF.Views
{
    /// <summary>
    /// BuildingEditDialog.xaml 的交互逻辑
    /// </summary>
    public partial class BuildingEditDialog : Window
    {
        private BuildingEditViewModel? _viewModel;

        public Building? Result { get; private set; }

        public BuildingEditDialog()
        {
            InitializeComponent();
        }

        public BuildingEditDialog(BuildingEditViewModel viewModel) : this()
        {
            _viewModel = viewModel;
            DataContext = viewModel;

            // 订阅事件
            viewModel.BuildingSaved += OnBuildingSaved;
            viewModel.EditCancelled += OnEditCancelled;
        }

        private void OnBuildingSaved(object? sender, Building building)
        {
            Result = building;
            DialogResult = true;
            Close();
        }

        private void OnEditCancelled(object? sender, EventArgs e)
        {
            DialogResult = false;
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            // 取消订阅事件
            if (_viewModel != null)
            {
                _viewModel.BuildingSaved -= OnBuildingSaved;
                _viewModel.EditCancelled -= OnEditCancelled;
            }
            base.OnClosed(e);
        }
    }

    /// <summary>
    /// 布尔值到字符串转换器
    /// </summary>
    public class BooleanToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && parameter is string paramString)
            {
                var parts = paramString.Split('|');
                if (parts.Length == 2)
                {
                    return boolValue ? parts[0] : parts[1];
                }
            }
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 字符串到可见性转换器
    /// </summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.IsNullOrEmpty(value?.ToString()) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 反向布尔转换器
    /// </summary>
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool boolValue ? !boolValue : true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool boolValue ? !boolValue : false;
        }
    }
}