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

            // Row 4: Main Headers
            worksheet.Cell(4, 1).Value = "Date";
            worksheet.Cell(4, 2).Value = "Issue Line";
            worksheet.Cell(4, 3).Value = "Issue (FROM)";
            worksheet.Cell(4, 6).Value = "Receipt Line";
            worksheet.Cell(4, 7).Value = "Receipt (TO)";
            worksheet.Cell(4, 10).Value = "Deviation";

            // Style Row 4
            for (int i = 1; i <= 10; i++)
            {
                var cell = worksheet.Cell(4, i);
                cell.Style.Font.Bold = true;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Font.FontColor = XLColor.White;

                if (i == 1 || i == 2) cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#6B7280");
                else if (i >= 3 && i <= 5) cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#3B82F6");
                else if (i == 6) cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#6B7280");
                else if (i >= 7 && i <= 9) cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#10B981");
                else if (i == 10) cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F59E0B");
            }

            // Merges for Row 4
            worksheet.Range(4, 1, 5, 1).Merge(); // Date
            worksheet.Range(4, 2, 5, 2).Merge(); // Issue Line
            worksheet.Range(4, 3, 4, 5).Merge(); // Issue (FROM)
            worksheet.Range(4, 6, 5, 6).Merge(); // Receipt Line
            worksheet.Range(4, 7, 4, 9).Merge(); // Receipt (TO)
            worksheet.Range(4, 10, 5, 10).Merge(); // Deviation

            // Row 5: Sub Headers
            worksheet.Cell(5, 3).Value = "Total";
            worksheet.Cell(5, 4).Value = "Read";
            worksheet.Cell(5, 5).Value = "No Read";
            worksheet.Cell(5, 7).Value = "Read";
            worksheet.Cell(5, 8).Value = "No Read";
            worksheet.Cell(5, 9).Value = "Total";

            // Style Row 5
            for (int i = 1; i <= 10; i++)
            {
                if (i == 1 || i == 2 || i == 6 || i == 10) continue;
                var cell = worksheet.Cell(5, i);
                ApplyHeaderCellStyle(cell);
            }

            int dataRow = 6;
            foreach (var item in data)
            {
                worksheet.Cell(dataRow, 1).Value = FormatReportDate(item.ReportDate);
                worksheet.Cell(dataRow, 2).Value = FormatPlantName(item.FromPlant);
                worksheet.Cell(dataRow, 3).Value = item.IssueTotal;
                worksheet.Cell(dataRow, 4).Value = item.IssueRead;
                worksheet.Cell(dataRow, 5).Value = item.IssueNoRead;
                worksheet.Cell(dataRow, 6).Value = FormatPlantName(item.ToPlant);
                worksheet.Cell(dataRow, 7).Value = item.ReceiptRead;
                worksheet.Cell(dataRow, 8).Value = item.ReceiptNoRead;
                worksheet.Cell(dataRow, 9).Value = item.ReceiptTotal;
                
                var deviationText = item.Deviation >= 0 ? $"+{item.Deviation}" : item.Deviation.ToString();
                worksheet.Cell(dataRow, 10).Value = deviationText;

                // Center align numeric columns
                for (int c = 3; c <= 10; c++)
                {
                    if (c == 6) continue;
                    worksheet.Cell(dataRow, c).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }

                ApplyDataRowStyle(worksheet, dataRow, 10);
                dataRow++;
            }

            // Set column widths
            worksheet.Column(1).Width = 15;
            worksheet.Column(2).Width = 20;
            worksheet.Column(3).Width = 12;
            worksheet.Column(4).Width = 12;
            worksheet.Column(5).Width = 12;
            worksheet.Column(6).Width = 20;
            worksheet.Column(7).Width = 12;
            worksheet.Column(8).Width = 12;
            worksheet.Column(9).Width = 12;
            worksheet.Column(10).Width = 15;

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

            worksheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            worksheet.PageSetup.FitToPages(1, 1);

            var titleCell = worksheet.Cell(1, 1);
            titleCell.Value = "HALDIRAM SORTER SCAN SYSTEM";
            titleCell.Style.Font.Bold = true;
            titleCell.Style.Font.FontSize = 16;
            titleCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            var reportTitle = worksheet.Cell(2, 1);
            reportTitle.Value = "Overall Daily Transfer Report";
            reportTitle.Style.Font.Bold = true;
            reportTitle.Style.Font.FontSize = 14;
            reportTitle.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            var dateCell = worksheet.Cell(3, 1);
            dateCell.Value = $"Date Range: {fromDate:dd/MMM/yyyy} to {toDate:dd/MMM/yyyy}";
            dateCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            var generatedCell = worksheet.Cell(3, 8);
            generatedCell.Value = $"Generated: {DateTime.Now:dd/MMM/yyyy HH:mm}";
            generatedCell.Style.Font.FontSize = 9;
            generatedCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            worksheet.Range(1, 1, 1, 10).Merge();
            worksheet.Range(2, 1, 2, 10).Merge();
            worksheet.Range(3, 1, 3, 7).Merge();
            worksheet.Range(3, 8, 3, 10).Merge();

            // Row 4: Main Headers
            worksheet.Cell(4, 1).Value = "Date";
            worksheet.Cell(4, 2).Value = "Issue Line";
            worksheet.Cell(4, 3).Value = "Issue (FROM)";
            worksheet.Cell(4, 6).Value = "Receipt Line";
            worksheet.Cell(4, 7).Value = "Receipt (TO)";
            worksheet.Cell(4, 10).Value = "Deviation";

            // Style Row 4
            for (int i = 1; i <= 10; i++)
            {
                var cell = worksheet.Cell(4, i);
                cell.Style.Font.Bold = true;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Font.FontColor = XLColor.White;

                if (i == 1 || i == 2) cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#6B7280");
                else if (i >= 3 && i <= 5) cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#3B82F6");
                else if (i == 6) cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#6B7280");
                else if (i >= 7 && i <= 9) cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#10B981");
                else if (i == 10) cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F59E0B");
            }

            // Merges for Row 4
            worksheet.Range(4, 1, 5, 1).Merge(); // Date
            worksheet.Range(4, 2, 5, 2).Merge(); // Issue Line
            worksheet.Range(4, 3, 4, 5).Merge(); // Issue (FROM)
            worksheet.Range(4, 6, 5, 6).Merge(); // Receipt Line
            worksheet.Range(4, 7, 4, 9).Merge(); // Receipt (TO)
            worksheet.Range(4, 10, 5, 10).Merge(); // Deviation

            // Row 5: Sub Headers
            worksheet.Cell(5, 3).Value = "Total";
            worksheet.Cell(5, 4).Value = "Read";
            worksheet.Cell(5, 5).Value = "No Read";
            worksheet.Cell(5, 7).Value = "Read";
            worksheet.Cell(5, 8).Value = "No Read";
            worksheet.Cell(5, 9).Value = "Total";

            // Style Row 5
            for (int i = 1; i <= 10; i++)
            {
                if (i == 1 || i == 2 || i == 6 || i == 10) continue;
                var cell = worksheet.Cell(5, i);
                ApplyHeaderCellStyle(cell);
            }

            int dataRow = 6;
            foreach (var item in data)
            {
                worksheet.Cell(dataRow, 1).Value = FormatReportDate(item.ReportDate);
                worksheet.Cell(dataRow, 2).Value = FormatPlantName(item.FromPlant);
                worksheet.Cell(dataRow, 3).Value = item.IssueTotal;
                worksheet.Cell(dataRow, 4).Value = item.IssueRead;
                worksheet.Cell(dataRow, 5).Value = item.IssueNoRead;
                worksheet.Cell(dataRow, 6).Value = FormatPlantName(item.ToPlant);
                worksheet.Cell(dataRow, 7).Value = item.ReceiptRead;
                worksheet.Cell(dataRow, 8).Value = item.ReceiptNoRead;
                worksheet.Cell(dataRow, 9).Value = item.ReceiptTotal;
                
                var deviationText = item.Deviation >= 0 ? $"+{item.Deviation}" : item.Deviation.ToString();
                worksheet.Cell(dataRow, 10).Value = deviationText;

                // Center align numeric columns
                for (int c = 3; c <= 10; c++)
                {
                    if (c == 6) continue;
                    worksheet.Cell(dataRow, c).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }

                ApplyDataRowStyle(worksheet, dataRow, 10);
                dataRow++;
            }

            // Totals Row
            if (data.Count > 0)
            {
                worksheet.Cell(dataRow, 1).Value = "TOTAL";
                worksheet.Cell(dataRow, 3).Value = data.Sum(x => x.IssueTotal);
                worksheet.Cell(dataRow, 4).Value = data.Sum(x => x.IssueRead);
                worksheet.Cell(dataRow, 5).Value = data.Sum(x => x.IssueNoRead);
                worksheet.Cell(dataRow, 7).Value = data.Sum(x => x.ReceiptRead);
                worksheet.Cell(dataRow, 8).Value = data.Sum(x => x.ReceiptNoRead);
                worksheet.Cell(dataRow, 9).Value = data.Sum(x => x.ReceiptTotal);
                
                var totalDev = data.Sum(x => x.Deviation);
                worksheet.Cell(dataRow, 10).Value = totalDev >= 0 ? $"+{totalDev}" : totalDev.ToString();

                for (int i = 1; i <= 10; i++)
                {
                    var cell = worksheet.Cell(dataRow, i);
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E5E7EB");
                    if (i >= 3) cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }
            }

            // Set column widths
            worksheet.Column(1).Width = 15;
            worksheet.Column(2).Width = 20;
            worksheet.Column(3).Width = 12;
            worksheet.Column(4).Width = 12;
            worksheet.Column(5).Width = 12;
            worksheet.Column(6).Width = 20;
            worksheet.Column(7).Width = 12;
            worksheet.Column(8).Width = 12;
            worksheet.Column(9).Width = 12;
            worksheet.Column(10).Width = 15;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public byte[] ExportVarianceTransferReport(Core.DTOs.VarianceTransferReportResult data)
        {
            using var workbook = new XLWorkbook();

            var summarySheet = workbook.Worksheets.Add("Variance Summary");
            ApplyHeaderStyle(summarySheet, "Variance Transfer Report", data.SelectedDate);

            summarySheet.Cell(5, 1).Value = "Total Production";
            summarySheet.Cell(5, 2).Value = data.TotalProduction;
            summarySheet.Cell(6, 1).Value = "Total Transfer";
            summarySheet.Cell(6, 2).Value = data.TotalTransfer;
            summarySheet.Cell(7, 1).Value = "Matched";
            summarySheet.Cell(7, 2).Value = data.MatchedProduction;
            summarySheet.Cell(8, 1).Value = "Unmatched Production";
            summarySheet.Cell(8, 2).Value = data.UnmatchedProduction;
            summarySheet.Cell(9, 1).Value = "Extra Transfer";
            summarySheet.Cell(9, 2).Value = data.TransferWithoutProduction;
            summarySheet.Cell(10, 1).Value = "Variance";
            summarySheet.Cell(10, 2).Value = data.Variance;

            for (var row = 5; row <= 10; row++)
            {
                ApplyHeaderCellStyle(summarySheet.Cell(row, 1));
                ApplyDataRowStyle(summarySheet, row, 2);
                summarySheet.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                summarySheet.Cell(row, 2).Style.Font.Bold = true;
            }

            summarySheet.Column(1).Width = 24;
            summarySheet.Column(2).Width = 18;

            var producedSheet = workbook.Worksheets.Add("Produced Details");
            var producedHeaders = new[] { "S/N", "Barcode", "SAP Code", "Batch", "Order No", "Pack Description", "Production Time", "Sorter Scan Time", "Last Transfer", "Status" };
            for (int i = 0; i < producedHeaders.Length; i++)
            {
                var cell = producedSheet.Cell(1, i + 1);
                cell.Value = producedHeaders[i];
                ApplyHeaderCellStyle(cell);
            }

            var producedRow = 2;
            foreach (var item in data.ProducedDetails)
            {
                producedSheet.Cell(producedRow, 1).Value = item.SerialNo;
                producedSheet.Cell(producedRow, 2).Value = item.Barcode;
                producedSheet.Cell(producedRow, 3).Value = item.SapCode;
                producedSheet.Cell(producedRow, 4).Value = item.BatchNo;
                producedSheet.Cell(producedRow, 5).Value = item.OrderNo;
                producedSheet.Cell(producedRow, 6).Value = item.PackDescription;
                producedSheet.Cell(producedRow, 7).Value = item.ProductionTime?.ToString("dd/MMM/yyyy HH:mm:ss") ?? string.Empty;
                producedSheet.Cell(producedRow, 8).Value = item.FirstTransferTime?.ToString("dd/MMM/yyyy HH:mm:ss") ?? string.Empty;
                producedSheet.Cell(producedRow, 9).Value = item.LastTransferTime?.ToString("dd/MMM/yyyy HH:mm:ss") ?? string.Empty;
                producedSheet.Cell(producedRow, 10).Value = item.IsMatched ? "Correct" : "Unmatched";
                ApplyDataRowStyle(producedSheet, producedRow, 10);
                producedRow++;
            }

            producedSheet.Columns(1, 10).AdjustToContents();

            var extraSheet = workbook.Worksheets.Add("Extra Transfers");
            var extraHeaders = new[] { "Barcode", "Sorter Scan Time", "Last Transfer", "Status" };
            for (int i = 0; i < extraHeaders.Length; i++)
            {
                var cell = extraSheet.Cell(1, i + 1);
                cell.Value = extraHeaders[i];
                ApplyHeaderCellStyle(cell);
            }

            var extraRow = 2;
            foreach (var item in data.ExtraTransferDetails)
            {
                extraSheet.Cell(extraRow, 1).Value = item.Barcode;
                extraSheet.Cell(extraRow, 2).Value = item.FirstTransferTime?.ToString("dd/MMM/yyyy HH:mm:ss") ?? string.Empty;
                extraSheet.Cell(extraRow, 3).Value = item.LastTransferTime?.ToString("dd/MMM/yyyy HH:mm:ss") ?? string.Empty;
                extraSheet.Cell(extraRow, 4).Value = "Extra Transfer";
                ApplyDataRowStyle(extraSheet, extraRow, 4);
                extraRow++;
            }

            extraSheet.Columns(1, 4).AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public byte[] ExportNoReadKasanaReadKomalReport(Core.DTOs.NoReadKasanaReadKomalReportResult data)
        {
            using var workbook = new XLWorkbook();

            var summarySheet = workbook.Worksheets.Add("Summary");
            ApplyHeaderStyle(summarySheet, "No Read at Kasana and Read at Komal", data.SelectedDate);

            summarySheet.Cell(5, 1).Value = "Total Kasana Read";
            summarySheet.Cell(5, 2).Value = data.TotalKasanaRead;
            summarySheet.Cell(6, 1).Value = "Total Komal Read";
            summarySheet.Cell(6, 2).Value = data.TotalKomalRead;
            summarySheet.Cell(7, 1).Value = "No Read at Kasana / Read at Komal";
            summarySheet.Cell(7, 2).Value = data.TotalNoReadAtKasanaReadAtKomal;

            for (var row = 5; row <= 7; row++)
            {
                ApplyHeaderCellStyle(summarySheet.Cell(row, 1));
                ApplyDataRowStyle(summarySheet, row, 2);
                summarySheet.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                summarySheet.Cell(row, 2).Style.Font.Bold = true;
            }

            summarySheet.Column(1).Width = 34;
            summarySheet.Column(2).Width = 18;

            var detailSheet = workbook.Worksheets.Add("Details");
            var headers = new[] { "S/N", "Barcode", "Kasana Side", "Komal Side", "Sorter Scan Time", "Last Sorter Scan Time" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = detailSheet.Cell(1, i + 1);
                cell.Value = headers[i];
                ApplyHeaderCellStyle(cell);
            }

            var rowIndex = 2;
            foreach (var item in data.Details)
            {
                detailSheet.Cell(rowIndex, 1).Value = item.SerialNo;
                detailSheet.Cell(rowIndex, 2).Value = item.Barcode;
                detailSheet.Cell(rowIndex, 3).Value = item.KasanaStatus;
                detailSheet.Cell(rowIndex, 4).Value = item.KomalStatus;
                detailSheet.Cell(rowIndex, 5).Value = item.FirstKomalScanTime?.ToString("dd/MMM/yyyy HH:mm:ss") ?? string.Empty;
                detailSheet.Cell(rowIndex, 6).Value = item.LastKomalScanTime?.ToString("dd/MMM/yyyy HH:mm:ss") ?? string.Empty;
                ApplyDataRowStyle(detailSheet, rowIndex, 6);
                rowIndex++;
            }

            detailSheet.Columns(1, 6).AdjustToContents();

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
            dateCell.Value = $"Report Date: {date:dd/MMM/yyyy}";
            dateCell.Style.Font.FontSize = 10;
            dateCell.Style.Font.FontColor = XLColor.FromHtml("#6B7280");
            dateCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            var generatedCell = worksheet.Cell(3, 5);
            generatedCell.Value = $"Generated: {DateTime.Now:dd/MMM/yyyy HH:mm}";
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

        private string FormatReportDate(string reportDate)
        {
            if (DateTime.TryParse(reportDate, out var parsedDate))
            {
                return parsedDate.ToString("dd/MMM/yyyy");
            }

            return reportDate;
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
