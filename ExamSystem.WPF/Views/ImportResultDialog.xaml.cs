using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ExamSystem.WPF.ViewModels;
using Microsoft.Win32;
using OfficeOpenXml;

namespace ExamSystem.WPF.Views
{
    /// <summary>
    /// ImportResultDialog.xaml 的交互逻辑
    /// </summary>
    public partial class ImportResultDialog : Window
    {
        public ImportResultViewModel ViewModel { get; private set; }

        public ImportResultDialog(ImportResultViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = viewModel;
            
            // 根据结果设置默认选中的标签页
            if (viewModel.FailureCount > 0)
            {
                FailureTab.IsSelected = true;
            }
            else
            {
                SuccessTab.IsSelected = true;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void ExportReportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Title = "导出导入结果报告",
                    Filter = "Excel文件 (*.xlsx)|*.xlsx",
                    FileName = $"题目导入结果报告_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    ExportToExcel(saveFileDialog.FileName);
                    MessageBox.Show($"报告已成功导出到：\n{saveFileDialog.FileName}", 
                                  "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出报告时发生错误：\n{ex.Message}", 
                              "导出失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportToExcel(string filePath)
        {
            // 设置EPPlus许可证（EPPlus 8及以上版本）
            ExcelPackage.License.SetNonCommercialPersonal("ExamSystem");
            
            using (var package = new ExcelPackage())
            {
                // 创建统计信息工作表
                var summarySheet = package.Workbook.Worksheets.Add("导入统计");
                CreateSummarySheet(summarySheet);

                // 创建成功导入工作表
                if (ViewModel.SuccessfulQuestions?.Any() == true)
                {
                    var successSheet = package.Workbook.Worksheets.Add("成功导入");
                    CreateSuccessSheet(successSheet);
                }

                // 创建失败导入工作表
                if (ViewModel.FailedQuestions?.Any() == true)
                {
                    var failureSheet = package.Workbook.Worksheets.Add("导入失败");
                    CreateFailureSheet(failureSheet);
                }

                package.SaveAs(new FileInfo(filePath));
            }
        }

        private void CreateSummarySheet(OfficeOpenXml.ExcelWorksheet worksheet)
        {
            worksheet.Cells[1, 1].Value = "题目导入结果统计报告";
            worksheet.Cells[1, 1].Style.Font.Size = 16;
            worksheet.Cells[1, 1].Style.Font.Bold = true;

            worksheet.Cells[3, 1].Value = "导入时间：";
            worksheet.Cells[3, 2].Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            worksheet.Cells[4, 1].Value = "总题目数：";
            worksheet.Cells[4, 2].Value = ViewModel.TotalCount;

            worksheet.Cells[5, 1].Value = "成功导入：";
            worksheet.Cells[5, 2].Value = ViewModel.SuccessCount;
            worksheet.Cells[5, 2].Style.Font.Color.SetColor(System.Drawing.Color.Green);

            worksheet.Cells[6, 1].Value = "导入失败：";
            worksheet.Cells[6, 2].Value = ViewModel.FailureCount;
            worksheet.Cells[6, 2].Style.Font.Color.SetColor(System.Drawing.Color.Red);

            worksheet.Cells[7, 1].Value = "成功率：";
            var successRate = ViewModel.TotalCount > 0 ? 
                (double)ViewModel.SuccessCount / ViewModel.TotalCount * 100 : 0;
            worksheet.Cells[7, 2].Value = $"{successRate:F1}%";

            // 设置列宽
            worksheet.Column(1).Width = 15;
            worksheet.Column(2).Width = 20;
        }

        private void CreateSuccessSheet(OfficeOpenXml.ExcelWorksheet worksheet)
        {
            // 设置标题
            worksheet.Cells[1, 1].Value = "行号";
            worksheet.Cells[1, 2].Value = "题目标题";
            worksheet.Cells[1, 3].Value = "题目类型";
            worksheet.Cells[1, 4].Value = "难度";
            worksheet.Cells[1, 5].Value = "分值";

            // 设置标题样式
            using (var range = worksheet.Cells[1, 1, 1, 5])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
            }

            // 填充数据
            var row = 2;
            foreach (var question in ViewModel.SuccessfulQuestions)
            {
                worksheet.Cells[row, 1].Value = question.RowNumber;
                worksheet.Cells[row, 2].Value = question.Title;
                worksheet.Cells[row, 3].Value = question.QuestionType;
                worksheet.Cells[row, 4].Value = question.Difficulty;
                worksheet.Cells[row, 5].Value = question.Score;
                row++;
            }

            // 设置列宽
            worksheet.Column(1).Width = 8;
            worksheet.Column(2).Width = 30;
            worksheet.Column(3).Width = 12;
            worksheet.Column(4).Width = 8;
            worksheet.Column(5).Width = 8;
        }

        private void CreateFailureSheet(OfficeOpenXml.ExcelWorksheet worksheet)
        {
            // 设置标题
            worksheet.Cells[1, 1].Value = "行号";
            worksheet.Cells[1, 2].Value = "题目标题";
            worksheet.Cells[1, 3].Value = "错误信息";
            worksheet.Cells[1, 4].Value = "原始数据";

            // 设置标题样式
            using (var range = worksheet.Cells[1, 1, 1, 4])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightCoral);
            }

            // 填充数据
            var row = 2;
            foreach (var failure in ViewModel.FailedQuestions)
            {
                worksheet.Cells[row, 1].Value = failure.RowNumber;
                worksheet.Cells[row, 2].Value = failure.Title;
                worksheet.Cells[row, 3].Value = failure.ErrorMessage;
                worksheet.Cells[row, 4].Value = failure.RawData;
                row++;
            }

            // 设置列宽
            worksheet.Column(1).Width = 8;
            worksheet.Column(2).Width = 25;
            worksheet.Column(3).Width = 40;
            worksheet.Column(4).Width = 30;
        }
    }
}