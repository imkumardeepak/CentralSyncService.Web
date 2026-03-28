using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;

namespace Web.Services
{
    public class ExcelExportService
    {
        public byte[] ExportShiftReport(List<Core.DTOs.ShiftReportRecord> data, DateTime selectedDate)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Shift Report");

            ApplyHeaderStyle(worksheet, "Shift Report", selectedDate);

            var headers = new[] { "SAP Code", "Product Name", "Batch No", "Date", "Shift", "Total Qty (Cs)", "Total Qty (MT)" };
            var headerRow = worksheet.Row(4);
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = headerRow.Cell(i + 1);
                cell.Value = headers[i];
                ApplyHeaderCellStyle(cell);
            }

            int row = 5;
            foreach (var item in data)
            {
                worksheet.Cell(row, 1).Value = item.SAPCode;
                worksheet.Cell(row, 2).Value = item.ProductName;
                worksheet.Cell(row, 3).Value = item.BatchNo;
                worksheet.Cell(row, 4).Value = item.ReportDate.ToString("dd-MM-yyyy");
                worksheet.Cell(row, 5).Value = item.Shift;
                worksheet.Cell(row, 6).Value = item.TotalQtyInCs;
                worksheet.Cell(row, 7).Value = item.TotalQtyInMT;

                ApplyDataRowStyle(worksheet, row, 7);
                row++;
            }

            worksheet.Column(1).Width = 15;
            worksheet.Column(2).Width = 35;
            worksheet.Column(3).Width = 15;
            worksheet.Column(4).Width = 12;
            worksheet.Column(5).Width = 10;
            worksheet.Column(6).Width = 15;
            worksheet.Column(7).Width = 15;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public byte[] ExportDailyTransfer(List<Core.DTOs.DailyTransferReportRecord> data, DateTime selectedDate)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Daily Transfer");

            ApplyHeaderStyle(worksheet, "Daily Transfer Report", selectedDate);

            var headers1 = new[] { "", "", "FROM Plant (Issue)", "", "", "", "", "TO Plant (Receipt)", "", "", "", "Deviation" };
            var headerRow1 = worksheet.Row(4);
            for (int i = 0; i < headers1.Length; i++)
            {
                var cell = headerRow1.Cell(i + 1);
                cell.Value = headers1[i];
                if (i >= 2 && i <= 5)
                {
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#3B82F6");
                    cell.Style.Font.FontColor = XLColor.White;
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }
                else if (i >= 7 && i <= 10)
                {
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#10B981");
                    cell.Style.Font.FontColor = XLColor.White;
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }
                else if (i == 11)
                {
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F59E0B");
                    cell.Style.Font.FontColor = XLColor.White;
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }
            }
            worksheet.Range(4, 11, 4, 12).Merge();

            var subHeaders = new[] { "From Plant", "Total", "Read", "No Read", "", "To Plant", "Total", "Read", "No Read", "", "Deviation" };
            var subHeaderRow = worksheet.Row(5);
            for (int i = 0; i < subHeaders.Length; i++)
            {
                var cell = subHeaderRow.Cell(i + 1);
                cell.Value = subHeaders[i];
                ApplyHeaderCellStyle(cell);
            }

            int dataRow = 6;
            foreach (var item in data)
            {
                worksheet.Cell(dataRow, 1).Value = item.FromPlant;
                worksheet.Cell(dataRow, 2).Value = item.IssueTotal;
                worksheet.Cell(dataRow, 3).Value = item.IssueRead;
                worksheet.Cell(dataRow, 4).Value = item.IssueNoRead;
                worksheet.Cell(dataRow, 5).Value = "";
                worksheet.Cell(dataRow, 6).Value = item.ToPlant;
                worksheet.Cell(dataRow, 7).Value = item.ReceiptTotal;
                worksheet.Cell(dataRow, 8).Value = item.ReceiptRead;
                worksheet.Cell(dataRow, 9).Value = item.ReceiptNoRead;
                worksheet.Cell(dataRow, 10).Value = "";
                worksheet.Cell(dataRow, 11).Value = item.Deviation;

                ApplyDataRowStyle(worksheet, dataRow, 11);

                worksheet.Cell(dataRow, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#EFF6FF");
                worksheet.Cell(dataRow, 6).Style.Fill.BackgroundColor = XLColor.FromHtml("#ECFDF5");

                if (item.Deviation > 0)
                    worksheet.Cell(dataRow, 11).Style.Font.FontColor = XLColor.FromHtml("#059669");
                else if (item.Deviation < 0)
                    worksheet.Cell(dataRow, 11).Style.Font.FontColor = XLColor.FromHtml("#DC2626");

                dataRow++;
            }

            worksheet.Column(1).Width = 25;
            worksheet.Column(2).Width = 10;
            worksheet.Column(3).Width = 10;
            worksheet.Column(4).Width = 10;
            worksheet.Column(5).Width = 3;
            worksheet.Column(6).Width = 25;
            worksheet.Column(7).Width = 10;
            worksheet.Column(8).Width = 10;
            worksheet.Column(9).Width = 10;
            worksheet.Column(10).Width = 3;
            worksheet.Column(11).Width = 12;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public byte[] ExportOverallTransferByOrder(List<Core.DTOs.OverallTransferByProductionOrderRecord> data, DateTime selectedDate)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Transfer by Order");

            ApplyHeaderStyle(worksheet, "Overall Transfer By Production Order", selectedDate);

            var headers = new[] { "Order No", "SAP Code", "Product Name", "Batch", "Order Qty", "Prod. Qty", "Issue Count", "Receipt Count", "Deviation" };
            var headerRow = worksheet.Row(4);
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = headerRow.Cell(i + 1);
                cell.Value = headers[i];
                ApplyHeaderCellStyle(cell);
            }

            int row = 5;
            foreach (var item in data)
            {
                worksheet.Cell(row, 1).Value = item.OrderNo;
                worksheet.Cell(row, 2).Value = item.MaterialNumber;
                worksheet.Cell(row, 3).Value = item.MaterialDescription;
                worksheet.Cell(row, 4).Value = item.Batch;
                worksheet.Cell(row, 5).Value = item.OrderQty;
                worksheet.Cell(row, 6).Value = item.CurQTY;
                worksheet.Cell(row, 7).Value = item.IssueCount;
                worksheet.Cell(row, 8).Value = item.ReceiptCount;
                worksheet.Cell(row, 9).Value = item.Deviation;

                ApplyDataRowStyle(worksheet, row, 9);

                if (item.Deviation > 0)
                    worksheet.Cell(row, 9).Style.Font.FontColor = XLColor.FromHtml("#059669");
                else if (item.Deviation < 0)
                    worksheet.Cell(row, 9).Style.Font.FontColor = XLColor.FromHtml("#DC2626");

                row++;
            }

            worksheet.Column(1).Width = 15;
            worksheet.Column(2).Width = 15;
            worksheet.Column(3).Width = 35;
            worksheet.Column(4).Width = 15;
            worksheet.Column(5).Width = 12;
            worksheet.Column(6).Width = 12;
            worksheet.Column(7).Width = 12;
            worksheet.Column(8).Width = 12;
            worksheet.Column(9).Width = 12;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private void ApplyHeaderStyle(IXLWorksheet worksheet, string title, DateTime date)
        {
            worksheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            worksheet.PageSetup.FitToPages(1, 1);
            worksheet.PageSetup.Margins.Left = 0.5;
            worksheet.PageSetup.Margins.Right = 0.5;
            worksheet.PageSetup.Margins.Top = 0.75;
            worksheet.PageSetup.Margins.Bottom = 0.75;

            var titleCell = worksheet.Cell(1, 1);
            titleCell.Value = "HALDIRAM SORTER SCAN SYSTEM";
            titleCell.Style.Font.Bold = true;
            titleCell.Style.Font.FontSize = 16;
            titleCell.Style.Font.FontColor = XLColor.FromHtml("#1F2937");
            titleCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            var reportTitle = worksheet.Cell(2, 1);
            reportTitle.Value = title;
            reportTitle.Style.Font.Bold = true;
            reportTitle.Style.Font.FontSize = 14;
            reportTitle.Style.Font.FontColor = XLColor.FromHtml("#374151");
            reportTitle.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            var dateCell = worksheet.Cell(3, 1);
            dateCell.Value = $"Report Date: {date:dd MMMM yyyy}";
            dateCell.Style.Font.FontSize = 10;
            dateCell.Style.Font.FontColor = XLColor.FromHtml("#6B7280");
            dateCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            var generatedCell = worksheet.Cell(3, 5);
            generatedCell.Value = $"Generated: {DateTime.Now:dd MMM yyyy HH:mm}";
            generatedCell.Style.Font.FontSize = 9;
            generatedCell.Style.Font.FontColor = XLColor.FromHtml("#9CA3AF");
            generatedCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            worksheet.Range(1, 1, 1, 10).Merge();
            worksheet.Range(2, 1, 2, 10).Merge();
            worksheet.Range(3, 1, 3, 4).Merge();
            worksheet.Range(3, 5, 3, 10).Merge();

            worksheet.Row(1).Height = 25;
            worksheet.Row(2).Height = 20;
            worksheet.Row(3).Height = 18;
        }

        private void ApplyHeaderCellStyle(IXLCell cell)
        {
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontSize = 10;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#374151");
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cell.Style.Border.SetBottomBorder(XLBorderStyleValues.Thin);
            cell.Style.Border.SetBottomBorderColor(XLColor.FromHtml("#D1D5DB"));
        }

        private void ApplyDataRowStyle(IXLWorksheet worksheet, int row, int lastColumn)
        {
            var rowBg = row % 2 == 0 ? XLColor.FromHtml("#F9FAFB") : XLColor.White;
            for (int i = 1; i <= lastColumn; i++)
            {
                var cell = worksheet.Cell(row, i);
                cell.Style.Fill.BackgroundColor = rowBg;
                cell.Style.Font.FontSize = 10;
                cell.Style.Font.FontColor = XLColor.FromHtml("#374151");
                cell.Style.Border.SetBottomBorder(XLBorderStyleValues.Thin);
                cell.Style.Border.SetBottomBorderColor(XLColor.FromHtml("#E5E7EB"));
            }

            worksheet.Row(row).Height = 18;
        }
    }
}
