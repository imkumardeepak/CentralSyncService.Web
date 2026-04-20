using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;

namespace Web.Services
{
    public class ExcelExportService
    {
        public byte[] ExportShiftReport(List<Core.DTOs.ShiftReportRecord> data, DateTime selectedDate, bool consolidated = false)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Shift Report");

            var reportTitle = consolidated ? "Shift Report - Consolidated" : "Shift Report";
            ApplyHeaderStyle(worksheet, reportTitle, selectedDate);

            var headers = consolidated
                ? new[] { "Material Code", "Batch", "Material", "Material Description", "Total Qty" }
                : new[] { "Material Code", "Batch", "Material", "Material Description", "Shift", "Total Qty" };
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
                worksheet.Cell(row, 1).Value = item.MaterialCode;
                worksheet.Cell(row, 2).Value = item.Batch;
                worksheet.Cell(row, 3).Value = item.Material;
                worksheet.Cell(row, 4).Value = item.MaterialDescription;
                
                if (consolidated)
                {
                    worksheet.Cell(row, 5).Value = item.TotalQty;
                    ApplyDataRowStyle(worksheet, row, 5);
                }
                else
                {
                    worksheet.Cell(row, 5).Value = item.Shift;
                    worksheet.Cell(row, 6).Value = item.TotalQty;
                    ApplyDataRowStyle(worksheet, row, 6);
                }
                row++;
            }

            worksheet.Column(1).Width = 15;
            worksheet.Column(2).Width = 15;
            worksheet.Column(3).Width = 20;
            worksheet.Column(4).Width = 35;
            if (consolidated)
            {
                worksheet.Column(5).Width = 15;
            }
            else
            {
                worksheet.Column(5).Width = 10;
                worksheet.Column(6).Width = 15;
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public byte[] ExportDailyTransfer(List<Core.DTOs.OverallDailyTransferRecord> data, DateTime fromDate, DateTime toDate)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Daily Transfer");

            var title = $"Daily Transfer Summary ({fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd})";
            ApplyHeaderStyle(worksheet, title, fromDate);

            // Main headers
            var headers1 = new[] { "Issue (FROM)", "", "", "Receipt (TO)", "", "Deviation" };
            var headerRow1 = worksheet.Row(4);
            for (int i = 0; i < headers1.Length; i++)
            {
                var cell = headerRow1.Cell(i + 1);
                cell.Value = headers1[i];
                if (i == 0 || i == 1 || i == 2)
                {
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#3B82F6");
                    cell.Style.Font.FontColor = XLColor.White;
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }
                else if (i == 3 || i == 4)
                {
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#10B981");
                    cell.Style.Font.FontColor = XLColor.White;
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }
                else if (i == 5)
                {
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F59E0B");
                    cell.Style.Font.FontColor = XLColor.White;
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                }
            }
            
            // Merge cells for grouped headers
            worksheet.Range(4, 1, 4, 3).Merge();  // Issue (FROM)
            worksheet.Range(4, 4, 4, 5).Merge();  // Receipt (TO)

            // Sub-headers
            var subHeaders = new[] { "Total", "Read", "No Read", "Total", "Read", "No Read", "Deviation" };
            var subHeaderRow = worksheet.Row(5);
            for (int i = 0; i < subHeaders.Length; i++)
            {
                var cell = subHeaderRow.Cell(i + 1);
                cell.Value = subHeaders[i];
                if (i == 6)
                {
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F59E0B");
                    cell.Style.Font.FontColor = XLColor.White;
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }
                else
                {
                    ApplyHeaderCellStyle(cell);
                }
            }

            int dataRow = 6;
            foreach (var item in data)
            {
                // Issue Total
                worksheet.Cell(dataRow, 1).Value = item.IssueTotal;
                worksheet.Cell(dataRow, 1).Style.Font.Bold = true;
                worksheet.Cell(dataRow, 1).Style.Font.FontSize = 14;
                worksheet.Cell(dataRow, 1).Style.Font.FontColor = XLColor.FromHtml("#1D4ED8");
                worksheet.Cell(dataRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                
                // Issue Read
                worksheet.Cell(dataRow, 2).Value = item.IssueRead;
                worksheet.Cell(dataRow, 2).Style.Font.FontSize = 12;
                worksheet.Cell(dataRow, 2).Style.Font.FontColor = XLColor.FromHtml("#059669");
                worksheet.Cell(dataRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                
                // Issue No Read
                worksheet.Cell(dataRow, 3).Value = item.IssueNoRead;
                worksheet.Cell(dataRow, 3).Style.Font.FontSize = 12;
                worksheet.Cell(dataRow, 3).Style.Font.FontColor = XLColor.FromHtml("#DC2626");
                worksheet.Cell(dataRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                
                // Receipt Total
                worksheet.Cell(dataRow, 4).Value = item.ReceiptTotal;
                worksheet.Cell(dataRow, 4).Style.Font.Bold = true;
                worksheet.Cell(dataRow, 4).Style.Font.FontSize = 14;
                worksheet.Cell(dataRow, 4).Style.Font.FontColor = XLColor.FromHtml("#047857");
                worksheet.Cell(dataRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                
                // Receipt Read
                worksheet.Cell(dataRow, 5).Value = item.ReceiptRead;
                worksheet.Cell(dataRow, 5).Style.Font.FontSize = 12;
                worksheet.Cell(dataRow, 5).Style.Font.FontColor = XLColor.FromHtml("#059669");
                worksheet.Cell(dataRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                
                // Receipt No Read
                worksheet.Cell(dataRow, 6).Value = item.ReceiptNoRead;
                worksheet.Cell(dataRow, 6).Style.Font.FontSize = 12;
                worksheet.Cell(dataRow, 6).Style.Font.FontColor = XLColor.FromHtml("#DC2626");
                worksheet.Cell(dataRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                
                // Deviation
                var deviationText = item.Deviation > 0 ? $"+{item.Deviation}" : item.Deviation.ToString();
                worksheet.Cell(dataRow, 7).Value = deviationText;
                worksheet.Cell(dataRow, 7).Style.Font.Bold = true;
                worksheet.Cell(dataRow, 7).Style.Font.FontSize = 12;
                worksheet.Cell(dataRow, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                
                if (item.Deviation > 0)
                    worksheet.Cell(dataRow, 7).Style.Font.FontColor = XLColor.FromHtml("#059669");
                else if (item.Deviation < 0)
                    worksheet.Cell(dataRow, 7).Style.Font.FontColor = XLColor.FromHtml("#DC2626");
                else
                    worksheet.Cell(dataRow, 7).Style.Font.FontColor = XLColor.FromHtml("#374151");

                ApplyDataRowStyle(worksheet, dataRow, 7);
                worksheet.Row(dataRow).Height = 25;

                dataRow++;
            }

            // Set column widths
            worksheet.Column(1).Width = 25;
            worksheet.Column(2).Width = 15;
            worksheet.Column(3).Width = 15;
            worksheet.Column(1).Width = 15;
            worksheet.Column(2).Width = 15;
            worksheet.Column(3).Width = 15;
            worksheet.Column(4).Width = 15;
            worksheet.Column(5).Width = 15;
            worksheet.Column(6).Width = 15;
            worksheet.Column(7).Width = 15;

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

        public byte[] ExportOverallDailyTransfer(List<Core.DTOs.OverallDailyTransferRecord> data, DateTime fromDate, DateTime toDate)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Overall Daily Transfer");

            // Apply custom header for date range
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
            reportTitle.Value = "Overall Daily Transfer Report";
            reportTitle.Style.Font.Bold = true;
            reportTitle.Style.Font.FontSize = 14;
            reportTitle.Style.Font.FontColor = XLColor.FromHtml("#374151");
            reportTitle.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            var dateCell = worksheet.Cell(3, 1);
            dateCell.Value = $"Date Range: {fromDate:dd MMMM yyyy} to {toDate:dd MMMM yyyy}";
            dateCell.Style.Font.FontSize = 10;
            dateCell.Style.Font.FontColor = XLColor.FromHtml("#6B7280");
            dateCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            var generatedCell = worksheet.Cell(3, 5);
            generatedCell.Value = $"Generated: {DateTime.Now:dd MMM yyyy HH:mm}";
            generatedCell.Style.Font.FontSize = 9;
            generatedCell.Style.Font.FontColor = XLColor.FromHtml("#9CA3AF");
            generatedCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            worksheet.Range(1, 1, 1, 9).Merge();
            worksheet.Range(2, 1, 2, 9).Merge();
            worksheet.Range(3, 1, 3, 4).Merge();
            worksheet.Range(3, 5, 3, 9).Merge();

            worksheet.Row(1).Height = 25;
            worksheet.Row(2).Height = 20;
            worksheet.Row(3).Height = 18;

            // Main headers
            var headers1 = new[] { "FROM Plant (Issue)", "", "", "", "TO Plant (Receipt)", "", "", "", "Deviation" };
            var headerRow1 = worksheet.Row(4);
            for (int i = 0; i < headers1.Length; i++)
            {
                var cell = headerRow1.Cell(i + 1);
                cell.Value = headers1[i];
                if (i <= 3)
                {
                    cell.Style.Font.Bold = true;
                    cell.Style.Font.FontSize = 10;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#3B82F6");
                    cell.Style.Font.FontColor = XLColor.White;
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }
                else if (i <= 7)
                {
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#10B981");
                    cell.Style.Font.FontColor = XLColor.White;
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }
                else
                {
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F59E0B");
                    cell.Style.Font.FontColor = XLColor.White;
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                }
            }

            // Merge cells for grouped headers
            worksheet.Range(4, 1, 4, 4).Merge();  // FROM Plant (Issue)
            worksheet.Range(4, 5, 4, 8).Merge();  // TO Plant (Receipt)

            // Sub-headers
            var subHeaders = new[] { "Plant", "Total", "Read", "No Read", "Plant", "Total", "Read", "No Read", "" };
            var subHeaderRow = worksheet.Row(5);
            for (int i = 0; i < subHeaders.Length; i++)
            {
                var cell = subHeaderRow.Cell(i + 1);
                cell.Value = subHeaders[i];
                if (i == 8)
                {
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F59E0B");
                    cell.Style.Font.FontColor = XLColor.White;
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                }
                else
                {
                    ApplyHeaderCellStyle(cell);
                }
            }

            int dataRow = 6;
            foreach (var item in data)
            {
                // FROM Plant - format like UI
                var fromPlantFormatted = FormatPlantName(item.FromPlant);
                worksheet.Cell(dataRow, 1).Value = string.IsNullOrWhiteSpace(fromPlantFormatted) ? "-" : fromPlantFormatted;
                worksheet.Cell(dataRow, 1).Style.Font.Bold = true;
                worksheet.Cell(dataRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                worksheet.Cell(dataRow, 2).Value = item.IssueTotal;
                worksheet.Cell(dataRow, 2).Style.Font.FontSize = 10;
                worksheet.Cell(dataRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                worksheet.Cell(dataRow, 3).Value = item.IssueRead;
                worksheet.Cell(dataRow, 3).Style.Font.FontSize = 10;
                worksheet.Cell(dataRow, 3).Style.Font.FontColor = XLColor.FromHtml("#059669");
                worksheet.Cell(dataRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                worksheet.Cell(dataRow, 4).Value = item.IssueNoRead;
                worksheet.Cell(dataRow, 4).Style.Font.FontSize = 10;
                worksheet.Cell(dataRow, 4).Style.Font.FontColor = XLColor.FromHtml("#DC2626");
                worksheet.Cell(dataRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // TO Plant
                worksheet.Cell(dataRow, 5).Value = string.IsNullOrWhiteSpace(item.ToPlant) ? "-" : FormatPlantName(item.ToPlant);
                worksheet.Cell(dataRow, 5).Style.Font.Bold = true;
                worksheet.Cell(dataRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                worksheet.Cell(dataRow, 6).Value = item.ReceiptTotal;
                worksheet.Cell(dataRow, 6).Style.Font.FontSize = 10;
                worksheet.Cell(dataRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                worksheet.Cell(dataRow, 7).Value = item.ReceiptRead;
                worksheet.Cell(dataRow, 7).Style.Font.FontSize = 10;
                worksheet.Cell(dataRow, 7).Style.Font.FontColor = XLColor.FromHtml("#059669");
                worksheet.Cell(dataRow, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                worksheet.Cell(dataRow, 8).Value = item.ReceiptNoRead;
                worksheet.Cell(dataRow, 8).Style.Font.FontSize = 10;
                worksheet.Cell(dataRow, 8).Style.Font.FontColor = XLColor.FromHtml("#DC2626");
                worksheet.Cell(dataRow, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Deviation
                worksheet.Cell(dataRow, 9).Value = item.Deviation;
                worksheet.Cell(dataRow, 9).Style.Font.Bold = true;
                worksheet.Cell(dataRow, 9).Style.Font.FontSize = 10;
                worksheet.Cell(dataRow, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                if (item.Deviation > 0)
                    worksheet.Cell(dataRow, 9).Style.Font.FontColor = XLColor.FromHtml("#D97706");
                else if (item.Deviation < 0)
                    worksheet.Cell(dataRow, 9).Style.Font.FontColor = XLColor.FromHtml("#DC2626");
                else
                    worksheet.Cell(dataRow, 9).Style.Font.FontColor = XLColor.FromHtml("#374151");

                ApplyDataRowStyle(worksheet, dataRow, 9);
                worksheet.Row(dataRow).Height = 18;

                dataRow++;
            }

            // Add totals row
            if (data.Count > 0)
            {
                var totalsRow = dataRow;
                worksheet.Cell(totalsRow, 1).Value = "TOTAL";
                worksheet.Cell(totalsRow, 1).Style.Font.Bold = true;
                worksheet.Cell(totalsRow, 1).Style.Font.FontSize = 10;
                worksheet.Cell(totalsRow, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#E5E7EB");
                worksheet.Cell(totalsRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                worksheet.Cell(totalsRow, 2).Value = data.Sum(x => x.IssueTotal);
                worksheet.Cell(totalsRow, 2).Style.Font.Bold = true;
                worksheet.Cell(totalsRow, 2).Style.Font.FontSize = 10;
                worksheet.Cell(totalsRow, 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#E5E7EB");
                worksheet.Cell(totalsRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                worksheet.Cell(totalsRow, 3).Value = data.Sum(x => x.IssueRead);
                worksheet.Cell(totalsRow, 3).Style.Font.Bold = true;
                worksheet.Cell(totalsRow, 3).Style.Font.FontSize = 10;
                worksheet.Cell(totalsRow, 3).Style.Fill.BackgroundColor = XLColor.FromHtml("#E5E7EB");
                worksheet.Cell(totalsRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                worksheet.Cell(totalsRow, 4).Value = data.Sum(x => x.IssueNoRead);
                worksheet.Cell(totalsRow, 4).Style.Font.Bold = true;
                worksheet.Cell(totalsRow, 4).Style.Font.FontSize = 10;
                worksheet.Cell(totalsRow, 4).Style.Fill.BackgroundColor = XLColor.FromHtml("#E5E7EB");
                worksheet.Cell(totalsRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                worksheet.Cell(totalsRow, 5).Value = "TOTAL";
                worksheet.Cell(totalsRow, 5).Style.Font.Bold = true;
                worksheet.Cell(totalsRow, 5).Style.Font.FontSize = 10;
                worksheet.Cell(totalsRow, 5).Style.Fill.BackgroundColor = XLColor.FromHtml("#E5E7EB");
                worksheet.Cell(totalsRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                worksheet.Cell(totalsRow, 6).Value = data.Sum(x => x.ReceiptTotal);
                worksheet.Cell(totalsRow, 6).Style.Font.Bold = true;
                worksheet.Cell(totalsRow, 6).Style.Font.FontSize = 10;
                worksheet.Cell(totalsRow, 6).Style.Fill.BackgroundColor = XLColor.FromHtml("#E5E7EB");
                worksheet.Cell(totalsRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                worksheet.Cell(totalsRow, 7).Value = data.Sum(x => x.ReceiptRead);
                worksheet.Cell(totalsRow, 7).Style.Font.Bold = true;
                worksheet.Cell(totalsRow, 7).Style.Font.FontSize = 10;
                worksheet.Cell(totalsRow, 7).Style.Fill.BackgroundColor = XLColor.FromHtml("#E5E7EB");
                worksheet.Cell(totalsRow, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                worksheet.Cell(totalsRow, 8).Value = data.Sum(x => x.ReceiptNoRead);
                worksheet.Cell(totalsRow, 8).Style.Font.Bold = true;
                worksheet.Cell(totalsRow, 8).Style.Font.FontSize = 10;
                worksheet.Cell(totalsRow, 8).Style.Fill.BackgroundColor = XLColor.FromHtml("#E5E7EB");
                worksheet.Cell(totalsRow, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                var totalDeviation = data.Sum(x => x.Deviation);
                worksheet.Cell(totalsRow, 9).Value = totalDeviation;
                worksheet.Cell(totalsRow, 9).Style.Font.Bold = true;
                worksheet.Cell(totalsRow, 9).Style.Font.FontSize = 10;
                worksheet.Cell(totalsRow, 9).Style.Fill.BackgroundColor = XLColor.FromHtml("#E5E7EB");
                worksheet.Cell(totalsRow, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                if (totalDeviation > 0)
                    worksheet.Cell(totalsRow, 9).Style.Font.FontColor = XLColor.FromHtml("#D97706");
                else if (totalDeviation < 0)
                    worksheet.Cell(totalsRow, 9).Style.Font.FontColor = XLColor.FromHtml("#DC2626");
                else
                    worksheet.Cell(totalsRow, 9).Style.Font.FontColor = XLColor.FromHtml("#374151");

                worksheet.Row(totalsRow).Height = 20;
            }

            // Set column widths
            worksheet.Column(1).Width = 24;
            worksheet.Column(2).Width = 12;
            worksheet.Column(3).Width = 12;
            worksheet.Column(4).Width = 12;
            worksheet.Column(5).Width = 24;
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

        private string FormatPlantName(string plant)
        {
            if (string.IsNullOrWhiteSpace(plant)) return plant;
            return plant.ToUpper() switch
            {
                "KASANA BELOW" => "Kasana Grnd Flr",
                "KASANA TOP" => "Kasana 1st Flr",
                "KOMAL BELOW" => "Komal S1",
                "KOMAL TOP" => "Komal S2",
                _ => plant
            };
        }
    }
}
