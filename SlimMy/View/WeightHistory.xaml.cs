using ClosedXML.Excel;
using Microsoft.Win32;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using SlimMy.Model;
using SlimMy.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SlimMy.View
{
    /// <summary>
    /// WeightHistory.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class WeightHistory : Page
    {
        private bool isEventHooked = false;

        public WeightHistory()
        {
            InitializeComponent();

            Loaded += WeightHistory_Loaded;
        }

        private void ExportToPdf(FrameworkElement bmiChart, FrameworkElement weightChart, string filePath, IEnumerable<WeightRecordItem> records)
        {
            var doc = new PdfDocument();
            var page = doc.AddPage();
            var gfx = XGraphics.FromPdfPage(page);
            var font = new XFont("Arial", 12);

            // BMI 그래프
            gfx.DrawString("BMI 그래프", font, XBrushes.Black, new XPoint(40, 40));
            var bmiImage = RenderElementToImage(bmiChart);
            using (var msImg = new MemoryStream(bmiImage))
            {
                var img = XImage.FromStream(msImg);
                gfx.DrawImage(img, 40, 60, 280, 120);
            }

            // Weight 그래프
            gfx.DrawString("몸무게 그래프", font, XBrushes.Black, new XPoint(40, 280));
            var weightImage = RenderElementToImage(weightChart);
            using (var msImg = new MemoryStream(weightImage))
            {
                var img = XImage.FromStream(msImg);
                gfx.DrawImage(img, 40, 300, 350, 120);
            }

            // 기록 출력
            gfx.DrawString("기록", font, XBrushes.Black, new XPoint(40, 520));
            double y = 540;
            foreach (var r in records.OrderBy(r => r.Date))
            {
                gfx.DrawString($"{r.Date:yyyy-MM-dd} | {r.Weight}kg | BMI {r.BMI:F1}", font, XBrushes.Black, new XPoint(40, y));
                y += 20;
            }

            doc.Save(filePath);
            MessageBox.Show("PDF로 내보냈습니다.");
        }

        private void WeightHistory_Loaded(object sender, RoutedEventArgs e)
        {
            if (!isEventHooked && DataContext is WeightHistoryViewModel vm)
            {
                vm.ExportPdfRequested += () =>
                {
                    var dialog = new SaveFileDialog
                    {
                        Filter = "PDF 파일 (*.pdf)|*.pdf",
                        FileName = "몸무게기록.pdf"
                    };

                    if (dialog.ShowDialog() == true)
                    {
                        ExportToPdf(BmiChartElement, WeightChartElement, dialog.FileName, vm.WeightRecords);
                    }
                };

                vm.ExportExcelRequested += () =>
                {
                    var dialog = new SaveFileDialog
                    {
                        Filter = "Excel 파일 (*.xlsx)|*.xlsx",
                        FileName = "몸무게기록.xlsx"
                    };

                    if (dialog.ShowDialog() == true)
                    {
                        ExportToExcel(dialog.FileName);
                    }
                };

                isEventHooked = true;
            }
        }

        private byte[] RenderElementToImage(FrameworkElement element)
        {
            var bmp = new RenderTargetBitmap((int)element.ActualWidth, (int)element.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(element);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bmp));
            using (var ms = new MemoryStream())
            {
                encoder.Save(ms);
                return ms.ToArray();
            }
        }

        public void ExportToExcel(string filePath)
        {
            if (DataContext is not WeightHistoryViewModel vm) return;

            var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("몸무게 기록");

            sheet.Cell(1, 1).Value = "날짜";
            sheet.Cell(1, 2).Value = "몸무게 (kg)";
            sheet.Cell(1, 3).Value = "목표 몸무게 (kg)";
            sheet.Cell(1, 4).Value = "변화량";
            sheet.Cell(1, 5).Value = "BMI";

            int row = 2;

            foreach (var record in vm.WeightRecords.OrderBy(r => r.Date))
            {
                sheet.Cell(row, 1).Value = record.Date.ToString("yyyy-MM-dd");
                sheet.Cell(row, 2).Value = record.Weight;
                sheet.Cell(row, 3).Value = record.TargetWeight;
                sheet.Cell(row, 4).Value = record.WeightDiffFromPrevious;
                sheet.Cell(row, 5).Value = Math.Round(record.BMI, 1);
                row++;
            }

            sheet.Columns().AdjustToContents();
            workbook.SaveAs(filePath);

            MessageBox.Show("Excel로 내보냈습니다.");
        }
    }
}
