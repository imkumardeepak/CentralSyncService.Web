using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Web.Core.Entities;
using Web.Core.Interfaces;

namespace Web.Infrastructure.Repositories
{
    public class BarcodePrintRepository : IBarcodePrintRepository
    {
        private readonly string _connectionString;

        public BarcodePrintRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("CentralDb")
                ?? throw new InvalidOperationException("Central DB connection string is not configured.");
        }

        public async Task<List<BarcodePrint>> GetAllAsync(int page = 1, int pageSize = 50)
        {
            var list = new List<BarcodePrint>();
            var offset = (page - 1) * pageSize;

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                SELECT NewPlant, EANCode, NewSAPCode, NewBatchNo, NewSerialNo, 
                       EntryDate, NewBarcode, OldSapCode, PackDes, PackDes1, 
                       Shift, SapFlag, Username, RptFlag, OrderNo, Unit
                FROM BarcodePrint
                ORDER BY EntryDate DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Offset", offset);
            command.Parameters.AddWithValue("@PageSize", pageSize);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(MapFromReader(reader));
            }

            return list;
        }

        public async Task<BarcodePrint?> GetByIdAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = "SELECT * FROM BarcodePrint WHERE NewSerialNo = @Id";
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapFromReader(reader);
            }

            return null;
        }

        public async Task<List<BarcodePrint>> GetByBarcodeAsync(string barcode)
        {
            var list = new List<BarcodePrint>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                SELECT NewPlant, EANCode, NewSAPCode, NewBatchNo, NewSerialNo, 
                       EntryDate, NewBarcode, OldSapCode, PackDes, PackDes1, 
                       Shift, SapFlag, Username, RptFlag, OrderNo, Unit
                FROM BarcodePrint
                WHERE NewBarcode LIKE @Barcode OR EANCode LIKE @Barcode
                ORDER BY EntryDate DESC";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Barcode", $"%{barcode}%");

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(MapFromReader(reader));
            }

            return list;
        }

        public async Task<List<BarcodePrint>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            var list = new List<BarcodePrint>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                SELECT NewPlant, EANCode, NewSAPCode, NewBatchNo, NewSerialNo, 
                       EntryDate, NewBarcode, OldSapCode, PackDes, PackDes1, 
                       Shift, SapFlag, Username, RptFlag, OrderNo, Unit
                FROM BarcodePrint
                WHERE EntryDate BETWEEN @StartDate AND @EndDate
                ORDER BY EntryDate DESC";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@StartDate", startDate);
            command.Parameters.AddWithValue("@EndDate", endDate);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(MapFromReader(reader));
            }

            return list;
        }

        public async Task<int> GetTotalCountAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = "SELECT COUNT(*) FROM BarcodePrint";
            using var command = new SqlCommand(sql, connection);
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        private BarcodePrint MapFromReader(SqlDataReader reader)
        {
            return new BarcodePrint
            {
                NewPlant = GetNullableString(reader, "NewPlant"),
                EANCode = GetNullableString(reader, "EANCode"),
                NewSAPCode = GetNullableString(reader, "NewSAPCode"),
                NewBatchNo = GetNullableString(reader, "NewBatchNo"),
                NewSerialNo = GetNullableInt(reader, "NewSerialNo"),
                EntryDate = GetNullableDateTime(reader, "EntryDate"),
                NewBarcode = GetNullableString(reader, "NewBarcode"),
                OldSapCode = GetNullableString(reader, "OldSapCode"),
                PackDes = GetNullableString(reader, "PackDes"),
                PackDes1 = GetNullableString(reader, "PackDes1"),
                Shift = GetNullableString(reader, "Shift"),
                SapFlag = GetNullableString(reader, "SapFlag"),
                Username = GetNullableString(reader, "Username"),
                RptFlag = GetNullableString(reader, "RptFlag"),
                OrderNo = GetNullableInt(reader, "OrderNo"),
                Unit = GetNullableString(reader, "Unit")
            };
        }

        private string? GetNullableString(SqlDataReader reader, string columnName)
        {
            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
            }
            catch { return null; }
        }

        private int? GetNullableInt(SqlDataReader reader, string columnName)
        {
            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
            }
            catch { return null; }
        }

        private DateTime? GetNullableDateTime(SqlDataReader reader, string columnName)
        {
            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
            }
            catch { return null; }
        }
    }
}
