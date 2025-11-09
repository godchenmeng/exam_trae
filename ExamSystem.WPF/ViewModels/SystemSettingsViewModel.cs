using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ExamSystem.WPF.Commands;
using ExamSystem.Services;

namespace ExamSystem.WPF.ViewModels
{
  public class SystemSettingsViewModel : INotifyPropertyChanged
  {
    private readonly ConfigurationService _configService;
    private readonly BaiduAkValidator _akValidator;
    private readonly BaiduAkProvider _akProvider;
    private readonly DatabaseBackupService _backupService;
    private readonly DatabaseImportService _importService;

    public SystemSettingsViewModel()
    {
      _configService = new ConfigurationService();
      _akValidator = new BaiduAkValidator();
      _akProvider = new BaiduAkProvider(_configService);
      _backupService = new DatabaseBackupService(_configService);
      _importService = new DatabaseImportService(_backupService);

      _akInput = _akProvider.GetAk() ?? string.Empty;

      // 验证改为可选；保存不再依赖验证结果
      ValidateAkCommand = new RelayCommand(async () => await ValidateAkAsync(), () => !string.IsNullOrWhiteSpace(AkInput));
      SaveAkCommand = new RelayCommand(async () => await SaveAkAsync(), () => !string.IsNullOrWhiteSpace(AkInput));
      BackupNowCommand = new RelayCommand(() => BackupNow(), () => true);
      OpenBackupFolderCommand = new RelayCommand(() => OpenBackupFolder(), () => true);
      ImportSqlCommand = new RelayCommand(async () => await ImportSqlAsync(), () => true);

      LoadBackupList();
    }

    // AK管理
    private string _akInput;
    public string AkInput { get => _akInput; set { _akInput = value; OnPropertyChanged(); AkValid = null; AkMessage = null; } }
    private bool? _akValid;
    public bool? AkValid { get => _akValid; set { _akValid = value; OnPropertyChanged(); } }
    private string? _akMessage;
    public string? AkMessage { get => _akMessage; set { _akMessage = value; OnPropertyChanged(); } }

    public ICommand ValidateAkCommand { get; }
    public ICommand SaveAkCommand { get; }

    private async Task ValidateAkAsync()
    {
      AkMessage = "正在验证...";
      var (ok, msg) = await _akValidator.ValidateAsync(AkInput);
      AkValid = ok;
      AkMessage = msg;
    }

    private Task SaveAkAsync()
    {
      // 不强制验证，直接保存
      try
      {
        _configService.SetConfig(ConfigurationService.Keys.BaiduMapAk, AkInput, operatorName: CurrentOperator ?? "Admin", encryptSensitive: true, detail: "保存百度地图AK");
        AkMessage = "保存成功";
        _akProvider.GetAk(forceReload: true);
      }
      catch (Exception ex)
      {
        AkMessage = "保存失败: " + ex.Message;
        MessageBox.Show(AkMessage, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
      }
      return Task.CompletedTask;
    }

    // 备份与导入
    public ObservableCollection<string> BackupFiles { get; } = new ObservableCollection<string>();
    public ICommand BackupNowCommand { get; }
    public ICommand OpenBackupFolderCommand { get; }
    public ICommand ImportSqlCommand { get; }

    private void LoadBackupList()
    {
      try
      {
        var dir = _configService.GetConfig(ConfigurationService.Keys.BackupDir, decryptSensitive: false) ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
        Directory.CreateDirectory(dir);
        BackupFiles.Clear();
        foreach (var f in Directory.GetFiles(dir, "*.sql"))
        {
          BackupFiles.Add(Path.GetFileName(f));
        }
      }
      catch { }
    }

    private void BackupNow()
    {
      try
      {
        var path = _backupService.BackupNow(CurrentOperator ?? "Admin", mode: "manual");
        MessageBox.Show("备份成功：" + path, "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        LoadBackupList();
      }
      catch (Exception ex)
      {
        MessageBox.Show("备份失败：" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void OpenBackupFolder()
    {
      var dir = _configService.GetConfig(ConfigurationService.Keys.BackupDir, decryptSensitive: false) ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
      try
      {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
          FileName = dir,
          UseShellExecute = true
        });
      }
      catch (Exception ex)
      {
        MessageBox.Show("打开目录失败：" + ex.Message);
      }
    }

    private async Task ImportSqlAsync()
    {
      var dlg = new Microsoft.Win32.OpenFileDialog
      {
        Title = "选择SQL备份文件",
        Filter = "SQL 文件 (*.sql)|*.sql",
        Multiselect = false
      };
      if (dlg.ShowDialog() == true)
      {
        var file = dlg.FileName;
        if (!File.Exists(file)) { MessageBox.Show("文件不存在"); return; }
        var confirm = MessageBox.Show("导入将覆盖当前数据库，是否继续？", "确认导入", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes) return;

        var progress = new Progress<int>(p => ImportProgress = p);
        try
        {
          ImportProgress = 0;
          await _importService.ImportAsync(file, CurrentOperator ?? "Admin", progress);
          ImportProgress = 100;
          MessageBox.Show("导入完成", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
          MessageBox.Show("导入失败：" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
      }
    }

    private int _importProgress;
    public int ImportProgress { get => _importProgress; set { _importProgress = value; OnPropertyChanged(); } }

    // 当前操作人
    private string? _currentOperator;
    public string? CurrentOperator { get => _currentOperator; set { _currentOperator = value; OnPropertyChanged(); } }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
      => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
  }

  // 使用统一的 RelayCommand 实现（ExamSystem.WPF.Commands）
}