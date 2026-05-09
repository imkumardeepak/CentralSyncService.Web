-- =============================================
-- Author:      System
-- Description: Variance transfer report with production vs TO transfer matching
-- Time Range:  07:00 AM to 06:59 AM next day (production day / daily transfer logic)
-- =============================================

IF OBJECT_ID('dbo.sp_GetVarianceTransferReport', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetVarianceTransferReport;
GO

CREATE PROCEDURE dbo.sp_GetVarianceTransferReport
    @Date DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SET @Date = ISNULL(@Date, CAST(GETDATE() AS DATE));

    DECLARE @ShiftStart DATETIME2 = DATEADD(HOUR, 7, CAST(@Date AS DATETIME2));
    DECLARE @ShiftEnd DATETIME2 = DATEADD(DAY, 1, @ShiftStart);

    ;WITH ProducedRaw AS
    (
        SELECT
            SerialNo = ISNULL(bp.NewSerialNo, 0),
            Barcode = LTRIM(RTRIM(ISNULL(bp.NewBarcode, ''))),
            SapCode = LTRIM(RTRIM(ISNULL(bp.NewSAPCode, ''))),
            BatchNo = LTRIM(RTRIM(ISNULL(bp.NewBatchNo, ''))),
            OrderNo = CASE WHEN bp.OrderNo IS NULL THEN '' ELSE CONVERT(NVARCHAR(30), bp.OrderNo) END,
            PackDescription = LTRIM(RTRIM(ISNULL(bp.PackDes, ''))),
            ProductionTime = bp.EntryDate
        FROM dbo.BarcodePrint bp WITH(NOLOCK)
        WHERE UPPER(LTRIM(RTRIM(ISNULL(bp.NewPlant, '')))) = 'HF'
          AND bp.EntryDate >= @ShiftStart
          AND bp.EntryDate < @ShiftEnd
          AND LTRIM(RTRIM(ISNULL(bp.NewBarcode, ''))) <> ''
    ),
    TransferRaw AS
    (
        SELECT
            Barcode = LTRIM(RTRIM(ISNULL(ss.Barcode, ''))),
            TransferTime = ss.ScanDateTime
        FROM dbo.SorterScans_Sync ss WITH(NOLOCK)
        WHERE UPPER(LTRIM(RTRIM(ISNULL(ss.ScanType, '')))) = 'TO'
          AND ss.ScanDateTime >= @ShiftStart
          AND ss.ScanDateTime < @ShiftEnd
          AND LTRIM(RTRIM(ISNULL(ss.Barcode, ''))) <> ''
    ),
    ProducedDetails AS
    (
        SELECT
            pr.SerialNo,
            pr.Barcode,
            pr.SapCode,
            pr.BatchNo,
            pr.OrderNo,
            pr.PackDescription,
            pr.ProductionTime,
            TransferCount = ISNULL(tx.TransferCount, 0),
            FirstTransferTime = tx.FirstTransferTime,
            LastTransferTime = tx.LastTransferTime,
            IsMatched = CASE WHEN ISNULL(tx.TransferCount, 0) > 0 THEN 1 ELSE 0 END
        FROM ProducedRaw pr
        OUTER APPLY
        (
            SELECT
                TransferCount = COUNT(1),
                FirstTransferTime = MIN(tr.TransferTime),
                LastTransferTime = MAX(tr.TransferTime)
            FROM TransferRaw tr
            WHERE tr.Barcode = pr.Barcode
        ) tx
    ),
    TransferWithoutProduction AS
    (
        SELECT
            tr.Barcode,
            TransferCount = COUNT(1),
            FirstTransferTime = MIN(tr.TransferTime),
            LastTransferTime = MAX(tr.TransferTime)
        FROM TransferRaw tr
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM ProducedRaw pr
            WHERE pr.Barcode = tr.Barcode
        )
        GROUP BY tr.Barcode
    )
    SELECT
        ReportDate = CAST(@Date AS DATETIME2),
        ShiftStart = @ShiftStart,
        ShiftEnd = DATEADD(SECOND, -1, @ShiftEnd),
        TotalProduction = (SELECT COUNT(1) FROM ProducedRaw),
        TotalTransfer = (SELECT COUNT(1) FROM TransferRaw),
        MatchedProduction = (SELECT COUNT(1) FROM ProducedDetails WHERE IsMatched = 1),
        UnmatchedProduction = (SELECT COUNT(1) FROM ProducedDetails WHERE IsMatched = 0),
        TransferWithoutProduction = ISNULL((SELECT SUM(TransferCount) FROM TransferWithoutProduction), 0),
        Variance = (SELECT COUNT(1) FROM ProducedRaw) - (SELECT COUNT(1) FROM TransferRaw);

    SELECT
        SerialNo,
        Barcode,
        SapCode,
        BatchNo,
        OrderNo,
        PackDescription,
        ProductionTime,
        TransferCount,
        FirstTransferTime,
        LastTransferTime,
        IsMatched
    FROM ProducedDetails
    ORDER BY ProductionTime, Barcode;

    SELECT
        Barcode,
        TransferCount,
        FirstTransferTime,
        LastTransferTime
    FROM TransferWithoutProduction
    ORDER BY FirstTransferTime, Barcode;
END
GO

PRINT 'Procedure sp_GetVarianceTransferReport created';
GO
