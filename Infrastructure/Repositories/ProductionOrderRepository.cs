using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Web.Core.Entities;
using Web.Core.Interfaces;

namespace Web.Infrastructure.Repositories
{
    public class ProductionOrderRepository : IProductionOrderRepository
    {
        private readonly string _connectionString;

        public ProductionOrderRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("CentralDb")
                ?? throw new InvalidOperationException("Central DB connection string is not configured.");
        }

        public async Task<List<ProductionOrder>> GetAllAsync(int page = 1, int pageSize = 50)
        {
            var list = new List<ProductionOrder>();
            var offset = (page - 1) * pageSize;

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                SELECT Id, OrderNo, PlantCode, Material, MaterialDescription, 
                       OrderQty, UOM, Batch, BsDate, CurQTY, BalQTY, ComFlag,
                       UploadDate, UploadTime, UpdateDate, UpdateTime, Mrp,
                       OverDelivery, UnderDelivery, PlantName, PackLine, PackLine2
                FROM ProductionOrder
                ORDER BY BsDate DESC, Id DESC
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

        public async Task<ProductionOrder?> GetByIdAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = "SELECT * FROM ProductionOrder WHERE Id = @Id";
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapFromReader(reader);
            }

            return null;
        }

        public async Task<List<ProductionOrder>> GetByPlantCodeAsync(string plantCode)
        {
            var list = new List<ProductionOrder>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                SELECT Id, OrderNo, PlantCode, Material, MaterialDescription, 
                       OrderQty, UOM, Batch, BsDate, CurQTY, BalQTY, ComFlag,
                       UploadDate, UploadTime, UpdateDate, UpdateTime, Mrp,
                       OverDelivery, UnderDelivery, PlantName, PackLine, PackLine2
                FROM ProductionOrder
                WHERE PlantCode = @PlantCode
                ORDER BY BsDate DESC";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@PlantCode", plantCode);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(MapFromReader(reader));
            }

            return list;
        }

        public async Task<List<ProductionOrder>> GetByOrderNoAsync(int orderNo)
        {
            var list = new List<ProductionOrder>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                SELECT Id, OrderNo, PlantCode, Material, MaterialDescription, 
                       OrderQty, UOM, Batch, BsDate, CurQTY, BalQTY, ComFlag,
                       UploadDate, UploadTime, UpdateDate, UpdateTime, Mrp,
                       OverDelivery, UnderDelivery, PlantName, PackLine, PackLine2
                FROM ProductionOrder
                WHERE OrderNo = @OrderNo
                ORDER BY BsDate DESC";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@OrderNo", orderNo);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(MapFromReader(reader));
            }

            return list;
        }

        public async Task<List<ProductionOrder>> GetPendingOrdersAsync()
        {
            var list = new List<ProductionOrder>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                SELECT Id, OrderNo, PlantCode, Material, MaterialDescription, 
                       OrderQty, UOM, Batch, BsDate, CurQTY, BalQTY, ComFlag,
                       UploadDate, UploadTime, UpdateDate, UpdateTime, Mrp,
                       OverDelivery, UnderDelivery, PlantName, PackLine, PackLine2
                FROM ProductionOrder
                WHERE ComFlag = 'N' OR ComFlag IS NULL OR CurQTY < OrderQty
                ORDER BY BsDate DESC";

            using var command = new SqlCommand(sql, connection);

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

            var sql = "SELECT COUNT(*) FROM ProductionOrder";
            using var command = new SqlCommand(sql, connection);
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        private ProductionOrder MapFromReader(SqlDataReader reader)
        {
            return new ProductionOrder
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                OrderNo = GetNullableInt(reader, "OrderNo"),
                PlantCode = GetNullableString(reader, "PlantCode") ?? string.Empty,
                Material = GetNullableString(reader, "Material") ?? string.Empty,
                MaterialDescription = GetNullableString(reader, "MaterialDescription") ?? string.Empty,
                OrderQty = GetNullableInt(reader, "OrderQty"),
                UOM = GetNullableString(reader, "UOM") ?? string.Empty,
                Batch = GetNullableString(reader, "Batch") ?? string.Empty,
                BsDate = GetNullableDateTime(reader, "BsDate"),
                CurQTY = GetNullableInt(reader, "CurQTY"),
                BalQTY = GetNullableInt(reader, "BalQTY"),
                ComFlag = GetNullableString(reader, "ComFlag") ?? string.Empty,
                UploadDate = GetNullableDateTime(reader, "UploadDate"),
                UploadTime = GetNullableString(reader, "UploadTime"),
                UpdateDate = GetNullableDateTime(reader, "UpdateDate"),
                UpdateTime = GetNullableString(reader, "UpdateTime"),
                Mrp = GetNullableDecimal(reader, "Mrp"),
                OverDelivery = GetNullableDecimal(reader, "OverDelivery"),
                UnderDelivery = GetNullableDecimal(reader, "UnderDelivery"),
                PlantName = GetNullableString(reader, "PlantName") ?? string.Empty,
                PackLine = GetNullableString(reader, "PackLine") ?? string.Empty,
                PackLine2 = GetNullableString(reader, "PackLine2")
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

        private decimal? GetNullableDecimal(SqlDataReader reader, string columnName)
        {
            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);
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
