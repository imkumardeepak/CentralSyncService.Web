-- =============================================
-- Author:      System
-- Description: Barcodes read at Komal side (TO) but not read at Kasana side (FROM)
-- Time Range:  07:00 AM to 06:59 AM next day (production day / daily transfer logic)
-- =============================================

IF OBJECT_ID('dbo.sp_GetNoReadKasanaReadKomalReport', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetNoReadKasanaReadKomalReport;
GO

CREATE PROCEDURE dbo.sp_GetNoReadKasanaReadKomalReport
    @Date DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SET @Date = ISNULL(@Date, CAST(GETDATE() AS DATE));

    DECLARE @ShiftStart DATETIME2 = DATEADD(HOUR, 7, CAST(@Date AS DATETIME2));
    DECLARE @ShiftEnd DATETIME2 = DATEADD(DAY, 1, @ShiftStart);

    IF OBJECT_ID('tempdb..#KasanaReads') IS NOT NULL DROP TABLE #KasanaReads;
    IF OBJECT_ID('tempdb..#KomalReads') IS NOT NULL DROP TABLE #KomalReads;
    IF OBJECT_ID('tempdb..#Result') IS NOT NULL DROP TABLE #Result;

    CREATE TABLE #KasanaReads
    (
        Barcode NVARCHAR(100) NOT NULL,
        FirstKasanaScanTime DATETIME NULL,
        LastKasanaScanTime DATETIME NULL
    );

    CREATE TABLE #KomalReads
    (
        Barcode NVARCHAR(100) NOT NULL,
        FirstKomalScanTime DATETIME NULL,
        LastKomalScanTime DATETIME NULL
    );

    INSERT INTO #KasanaReads (Barcode, FirstKasanaScanTime, LastKasanaScanTime)
    SELECT
        Barcode = LTRIM(RTRIM(ISNULL(ss.Barcode, ''))),
        FirstKasanaScanTime = MIN(ss.ScanDateTime),
        LastKasanaScanTime = MAX(ss.ScanDateTime)
    FROM dbo.SorterScans_Sync ss WITH(NOLOCK)
    WHERE UPPER(ISNULL(ss.ScanType, '')) = 'FROM'
      AND ss.ScanDateTime >= @ShiftStart
      AND ss.ScanDateTime < @ShiftEnd
      AND ISNULL(ss.Barcode, '') <> ''
    GROUP BY LTRIM(RTRIM(ISNULL(ss.Barcode, '')));

    INSERT INTO #KomalReads (Barcode, FirstKomalScanTime, LastKomalScanTime)
    SELECT
        Barcode = LTRIM(RTRIM(ISNULL(ss.Barcode, ''))),
        FirstKomalScanTime = MIN(ss.ScanDateTime),
        LastKomalScanTime = MAX(ss.ScanDateTime)
    FROM dbo.SorterScans_Sync ss WITH(NOLOCK)
    WHERE UPPER(ISNULL(ss.ScanType, '')) = 'TO'
      AND ss.ScanDateTime >= @ShiftStart
      AND ss.ScanDateTime < @ShiftEnd
      AND ISNULL(ss.Barcode, '') <> ''
    GROUP BY LTRIM(RTRIM(ISNULL(ss.Barcode, '')));

    CREATE CLUSTERED INDEX IX_KasanaReads_Barcode ON #KasanaReads (Barcode);
    CREATE CLUSTERED INDEX IX_KomalReads_Barcode ON #KomalReads (Barcode);

    SELECT
        SerialNo = ROW_NUMBER() OVER (ORDER BY kr.FirstKomalScanTime, kr.Barcode),
        kr.Barcode,
        KasanaStatus = CAST('No Read' AS NVARCHAR(20)),
        KomalStatus = CAST('Read' AS NVARCHAR(20)),
        kr.FirstKomalScanTime,
        kr.LastKomalScanTime
    INTO #Result
    FROM #KomalReads kr
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM #KasanaReads ks
        WHERE ks.Barcode = kr.Barcode
    );

    SELECT
        ReportDate = CAST(@Date AS DATETIME2),
        ShiftStart = @ShiftStart,
        ShiftEnd = DATEADD(SECOND, -1, @ShiftEnd),
        TotalKasanaRead = (SELECT COUNT(1) FROM #KasanaReads),
        TotalKomalRead = (SELECT COUNT(1) FROM #KomalReads),
        TotalNoReadAtKasanaReadAtKomal = (SELECT COUNT(1) FROM #Result);

    SELECT
        SerialNo,
        Barcode,
        KasanaStatus,
        KomalStatus,
        FirstKomalScanTime,
        LastKomalScanTime
    FROM #Result
    ORDER BY SerialNo;
END
GO

PRINT 'Procedure sp_GetNoReadKasanaReadKomalReport created';
GO
