using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Web.Core.Entities;
using Web.Core.Interfaces;

namespace Web.Infrastructure.Repositories
{
    public class PlantRepository : IPlantRepository
    {
        private readonly string _connectionString;

        public PlantRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("CentralDb")
                ?? configuration["CentralDbConnectionString"]
                ?? throw new InvalidOperationException("Central DB connection string is not configured.");
        }

        public async Task<List<PlantConfiguration>> GetAllAsync(string searchTerm, string plantType, bool? isActive)
        {
            var plants = new List<PlantConfiguration>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                
                var sql = @"
                    SELECT Id, PlantCode, PlantName, PlantType, ServerIP, DatabaseName, 
                           Username, Password, Port, IsActive, Description, Location, 
                           ContactPerson, ContactPhone, 
                           CreatedDate, CreatedBy, ModifiedDate, ModifiedBy,
                           LastSyncSuccess, LastSyncStatus
                    FROM PlantConfiguration
                    WHERE 1=1";

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    sql += " AND (PlantCode LIKE @SearchTerm OR PlantName LIKE @SearchTerm OR Location LIKE @SearchTerm)";
                }

                if (!string.IsNullOrEmpty(plantType))
                {
                    sql += " AND PlantType = @PlantType";
                }

                if (isActive.HasValue)
                {
                    sql += " AND IsActive = @IsActive";
                }

                sql += " ORDER BY PlantType, PlantCode";

                using (var command = new SqlCommand(sql, connection))
                {
                    if (!string.IsNullOrEmpty(searchTerm))
                        command.Parameters.AddWithValue("@SearchTerm", $"%{searchTerm}%");
                    
                    if (!string.IsNullOrEmpty(plantType))
                        command.Parameters.AddWithValue("@PlantType", plantType);
                    
                    if (isActive.HasValue)
                        command.Parameters.AddWithValue("@IsActive", isActive.Value);

                    using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                    {
                        while (await reader.ReadAsync().ConfigureAwait(false))
                        {
                            plants.Add(MapPlantFromReader(reader));
                        }
                    }
                }
            }

            return plants;
        }

        public async Task<PlantConfiguration?> GetByIdAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                
                var sql = @"
                    SELECT Id, PlantCode, PlantName, PlantType, ServerIP, DatabaseName, 
                           Username, Password, Port, IsActive, Description, Location, 
                           ContactPerson, ContactPhone, 
                           CreatedDate, CreatedBy, ModifiedDate, ModifiedBy,
                           LastSyncSuccess, LastSyncStatus
                    FROM PlantConfiguration
                    WHERE Id = @Id";

                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);

                    using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                    {
                        if (await reader.ReadAsync().ConfigureAwait(false))
                        {
                            return MapPlantFromReader(reader);
                        }
                    }
                }
            }

            return null;
        }

        public async Task AddAsync(PlantConfiguration plant)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                
                var sql = @"
                    INSERT INTO PlantConfiguration 
                        (PlantCode, PlantName, PlantType, ServerIP, DatabaseName, 
                         Username, Password, Port, IsActive, Description, Location, 
                         ContactPerson, ContactPhone, CreatedDate, CreatedBy)
                    VALUES 
                        (@PlantCode, @PlantName, @PlantType, @ServerIP, @DatabaseName, 
                         @Username, @Password, @Port, @IsActive, @Description, @Location, 
                         @ContactPerson, @ContactPhone, GETDATE(), @CreatedBy);
                    SELECT SCOPE_IDENTITY();";

                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@PlantCode", plant.PlantCode);
                    command.Parameters.AddWithValue("@PlantName", plant.PlantName);
                    command.Parameters.AddWithValue("@PlantType", plant.PlantType);
                    command.Parameters.AddWithValue("@ServerIP", plant.ServerIP);
                    command.Parameters.AddWithValue("@DatabaseName", plant.DatabaseName);
                    command.Parameters.AddWithValue("@Username", (object?)plant.Username ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Password", (object?)plant.Password ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Port", plant.Port);
                    command.Parameters.AddWithValue("@IsActive", plant.IsActive);
                    command.Parameters.AddWithValue("@Description", (object?)plant.Description ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Location", (object?)plant.Location ?? DBNull.Value);
                    command.Parameters.AddWithValue("@ContactPerson", (object?)plant.ContactPerson ?? DBNull.Value);
                    command.Parameters.AddWithValue("@ContactPhone", (object?)plant.ContactPhone ?? DBNull.Value);
                    command.Parameters.AddWithValue("@CreatedBy", (object?)plant.CreatedBy ?? DBNull.Value);

                    var result = await command.ExecuteScalarAsync().ConfigureAwait(false);
                    if (result != null && result != DBNull.Value)
                    {
                        plant.Id = Convert.ToInt32(result);
                    }
                }
            }
        }

        public async Task UpdateAsync(PlantConfiguration plant)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                
                var sql = @"
                    UPDATE PlantConfiguration SET
                        PlantCode = @PlantCode,
                        PlantName = @PlantName,
                        PlantType = @PlantType,
                        ServerIP = @ServerIP,
                        DatabaseName = @DatabaseName,
                        Username = @Username,
                        Password = CASE WHEN @Password IS NULL OR @Password = '' THEN Password ELSE @Password END,
                        Port = @Port,
                        IsActive = @IsActive,
                        Description = @Description,
                        Location = @Location,
                        ContactPerson = @ContactPerson,
                        ContactPhone = @ContactPhone,
                        ModifiedDate = GETDATE(),
                        ModifiedBy = @ModifiedBy
                    WHERE Id = @Id";

                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Id", plant.Id);
                    command.Parameters.AddWithValue("@PlantCode", plant.PlantCode);
                    command.Parameters.AddWithValue("@PlantName", plant.PlantName);
                    command.Parameters.AddWithValue("@PlantType", plant.PlantType);
                    command.Parameters.AddWithValue("@ServerIP", plant.ServerIP);
                    command.Parameters.AddWithValue("@DatabaseName", plant.DatabaseName);
                    command.Parameters.AddWithValue("@Username", (object?)plant.Username ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Password", (object?)plant.Password ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Port", plant.Port);
                    command.Parameters.AddWithValue("@IsActive", plant.IsActive);
                    command.Parameters.AddWithValue("@Description", (object?)plant.Description ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Location", (object?)plant.Location ?? DBNull.Value);
                    command.Parameters.AddWithValue("@ContactPerson", (object?)plant.ContactPerson ?? DBNull.Value);
                    command.Parameters.AddWithValue("@ContactPhone", (object?)plant.ContactPhone ?? DBNull.Value);
                    command.Parameters.AddWithValue("@ModifiedBy", (object?)plant.ModifiedBy ?? DBNull.Value);

                    await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            }
        }

        public async Task DeleteAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                var sql = "DELETE FROM PlantConfiguration WHERE Id = @Id";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            }
        }

        public async Task ToggleStatusAsync(int id, bool isActive)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                var sql = "UPDATE PlantConfiguration SET IsActive = @IsActive, ModifiedDate = GETDATE() WHERE Id = @Id";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    command.Parameters.AddWithValue("@IsActive", isActive);
                    await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            }
        }

        private PlantConfiguration MapPlantFromReader(SqlDataReader reader)
        {
            return new PlantConfiguration
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                PlantCode = reader.GetString(reader.GetOrdinal("PlantCode")),
                PlantName = reader.GetString(reader.GetOrdinal("PlantName")),
                PlantType = reader.GetString(reader.GetOrdinal("PlantType")),
                ServerIP = reader.GetString(reader.GetOrdinal("ServerIP")),
                DatabaseName = reader.GetString(reader.GetOrdinal("DatabaseName")),
                Username = GetNullableString(reader, "Username"),
                Password = GetNullableString(reader, "Password"),
                Port = GetNullableInt(reader, "Port") ?? 1433,
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                Description = GetNullableString(reader, "Description"),
                Location = GetNullableString(reader, "Location"),
                ContactPerson = GetNullableString(reader, "ContactPerson"),
                ContactPhone = GetNullableString(reader, "ContactPhone"),
                CreatedDate = GetNullableDateTime(reader, "CreatedDate") ?? DateTime.Now,
                CreatedBy = GetNullableString(reader, "CreatedBy"),
                ModifiedDate = GetNullableDateTime(reader, "ModifiedDate"),
                ModifiedBy = GetNullableString(reader, "ModifiedBy"),
                LastSyncSuccess = GetNullableDateTime(reader, "LastSyncSuccess"),
                LastSyncStatus = GetNullableString(reader, "LastSyncStatus")
            };
        }

        private string? GetNullableString(SqlDataReader reader, string columnName)
        {
            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
            }
            catch
            {
                return null;
            }
        }

        private int? GetNullableInt(SqlDataReader reader, string columnName)
        {
            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
            }
            catch
            {
                return null;
            }
        }

        private DateTime? GetNullableDateTime(SqlDataReader reader, string columnName)
        {
            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
            }
            catch
            {
                return null;
            }
        }
    }
}
