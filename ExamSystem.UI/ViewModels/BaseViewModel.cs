using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;

namespace ExamSystem.UI.ViewModels;

/// <summary>
/// ViewModel基类
/// </summary>
public abstract class BaseViewModel : ObservableObject
{
    private bool _isBusy;
    private string _busyMessage = "正在处理...";

    /// <summary>
    /// 是否正在忙碌
    /// </summary>
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(IsNotBusy));
            }
        }
    }

    /// <summary>
    /// 是否不忙碌
    /// </summary>
    public bool IsNotBusy => !IsBusy;

    /// <summary>
    /// 忙碌消息
    /// </summary>
    public string BusyMessage
    {
        get => _busyMessage;
        set => SetProperty(ref _busyMessage, value);
    }

    /// <summary>
    /// 执行异步操作
    /// </summary>
    protected async Task ExecuteAsync(Func<Task> operation, string? busyMessage = null)
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            if (!string.IsNullOrEmpty(busyMessage))
            {
                BusyMessage = busyMessage;
            }

            await operation();
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// 执行异步操作并返回结果
    /// </summary>
    protected async Task<T?> ExecuteAsync<T>(Func<Task<T>> operation, string? busyMessage = null)
    {
        if (IsBusy) return default;

        try
        {
            IsBusy = true;
            if (!string.IsNullOrEmpty(busyMessage))
            {
                BusyMessage = busyMessage;
            }

            return await operation();
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// 显示消息
    /// </summary>
    protected virtual void ShowMessage(string message, MessageType type = MessageType.Information)
    {
        // 子类可以重写此方法来实现具体的消息显示逻辑
        System.Windows.MessageBox.Show(message, GetMessageTitle(type), 
            System.Windows.MessageBoxButton.OK, GetMessageBoxImage(type));
    }

    /// <summary>
    /// 显示确认对话框
    /// </summary>
    protected virtual bool ShowConfirmation(string message, string title = "确认")
    {
        var result = System.Windows.MessageBox.Show(message, title, 
            System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
        return result == System.Windows.MessageBoxResult.Yes;
    }

    private static string GetMessageTitle(MessageType type)
    {
        return type switch
        {
            MessageType.Information => "信息",
            MessageType.Warning => "警告",
            MessageType.Error => "错误",
            MessageType.Success => "成功",
            _ => "提示"
        };
    }

    private static System.Windows.MessageBoxImage GetMessageBoxImage(MessageType type)
    {
        return type switch
        {
            MessageType.Information => System.Windows.MessageBoxImage.Information,
            MessageType.Warning => System.Windows.MessageBoxImage.Warning,
            MessageType.Error => System.Windows.MessageBoxImage.Error,
            MessageType.Success => System.Windows.MessageBoxImage.Information,
            _ => System.Windows.MessageBoxImage.None
        };
    }
}

/// <summary>
/// 消息类型
/// </summary>
public enum MessageType
{
    Information,
    Warning,
    Error,
    Success
}