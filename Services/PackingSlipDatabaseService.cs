using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using DynamicsToXmlTranslator.Models;
using System.Linq;

namespace DynamicsToXmlTranslator.Services
{
    public class PackingSlipDatabaseService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<PackingSlipDatabaseService> _logger;
        private readonly string _connectionString;

        public PackingSlipDatabaseService(IConfiguration configuration, ILogger<PackingSlipDatabaseService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            var builder = new SqlConnectionStringBuilder
            {
                DataSource = _configuration["Database:Host"],
                InitialCatalog = _configuration["Database:Name"],
                UserID = _configuration["Database:User"],
                Password = _configuration["Database:Password"],
                TrustServerCertificate = true,
                ConnectTimeout = 30
            };
            _connectionString = builder.ConnectionString;
        }

        /// <summary>
        /// Récupère tous les Packing Slips de la base de données
        /// </summary>
        public virtual async Task<List<PackingSlip>> GetAllPackingSlipsAsync()
        {
            var packingSlips = new List<PackingSlip>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            SELECT 
                                JSON_KEYU,
                                JSON_DATA,
                                JSON_HASH,
                                JSON_FROM,
                                JSON_BKEY,
                                JSON_CRDA,
                                JSON_CRDA as last_updated_at,
                                0 as update_count
                            FROM dbo.JSON_IN
                            WHERE (JSON_STAT = 'ACTIVE' OR JSON_STAT IS NULL OR JSON_STAT = 'DELETED')
                            AND (JSON_CCLI = 'BR' OR JSON_CCLI IS NULL)
                            AND JSON_FROM = 'data/BRPackingSlipInterfaces'
                            ORDER BY JSON_BKEY";

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                try
                                {
                                    var packingSlip = new PackingSlip
                                    {
                                        Id = reader.GetInt32(0),
                                        JsonData = reader.GetString(1),
                                        ContentHash = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                        ApiEndpoint = reader.IsDBNull(3) ? null : reader.GetString(3),
                                        PackingSlipId = ExtractPackingSlipIdFromBKey(reader.IsDBNull(4) ? null : reader.GetString(4)),
                                        FirstSeenAt = reader.GetDateTime(5),
                                        LastUpdatedAt = reader.GetDateTime(6),
                                        UpdateCount = reader.GetInt32(7)
                                    };

                                    if (!string.IsNullOrEmpty(packingSlip.JsonData))
                                    {
                                        packingSlip.DynamicsData = JsonConvert.DeserializeObject<DynamicsPackingSlip>(packingSlip.JsonData);
                                    }

                                    packingSlips.Add(packingSlip);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, $"Erreur lors de la lecture du Packing Slip ID: {reader.GetInt32(0)}");
                                }
                            }
                        }
                    }
                }

                _logger.LogInformation($"{packingSlips.Count} Packing Slips récupérés de la base de données");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des Packing Slips");
                throw;
            }

            return packingSlips;
        }

        /// <summary>
        /// Récupère uniquement les Packing Slips non exportés
        /// </summary>
        public async Task<List<PackingSlip>> GetNonExportedPackingSlipsAsync()
        {
            var packingSlips = new List<PackingSlip>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            SELECT 
                                JSON_KEYU,
                                JSON_DATA,
                                JSON_HASH,
                                JSON_FROM,
                                JSON_BKEY,
                                JSON_CRDA,
                                JSON_CRDA as last_updated_at,
                                0 as update_count
                            FROM dbo.JSON_IN
                            WHERE (JSON_STAT = 'ACTIVE' OR JSON_STAT IS NULL OR JSON_STAT = 'DELETED')
                            AND (JSON_CCLI = 'BR' OR JSON_CCLI IS NULL)
                            AND JSON_FROM = 'data/BRPackingSlipInterfaces'
                            AND (JSON_TRTP = 0 OR JSON_TRTP IS NULL)
                            ORDER BY JSON_BKEY";

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                try
                                {
                                    var packingSlip = new PackingSlip
                                    {
                                        Id = reader.GetInt32(0),
                                        JsonData = reader.GetString(1),
                                        ContentHash = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                        ApiEndpoint = reader.IsDBNull(3) ? null : reader.GetString(3),
                                        PackingSlipId = ExtractPackingSlipIdFromBKey(reader.IsDBNull(4) ? null : reader.GetString(4)),
                                        FirstSeenAt = reader.GetDateTime(5),
                                        LastUpdatedAt = reader.GetDateTime(6),
                                        UpdateCount = reader.GetInt32(7)
                                    };

                                    if (!string.IsNullOrEmpty(packingSlip.JsonData))
                                    {
                                        packingSlip.DynamicsData = JsonConvert.DeserializeObject<DynamicsPackingSlip>(packingSlip.JsonData);
                                    }

                                    packingSlips.Add(packingSlip);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, $"Erreur lors de la lecture du Packing Slip ID: {reader.GetInt32(0)}");
                                }
                            }
                        }
                    }
                }

                _logger.LogInformation($"{packingSlips.Count} Packing Slips non exportés récupérés");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des Packing Slips non exportés");
                throw;
            }

            return packingSlips;
        }

        /// <summary>
        /// Marque les Packing Slips comme exportés
        /// </summary>
        public async Task MarkPackingSlipsAsExportedAsync(List<int> packingSlipIds, string batchName)
        {
            if (packingSlipIds == null || !packingSlipIds.Any())
            {
                return;
            }

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = connection.CreateCommand())
                    {
                        var inClause = string.Join(",", packingSlipIds.Select(id => id.ToString()));

                        command.CommandText = $@"
                            UPDATE dbo.JSON_IN 
                            SET 
                                JSON_TRTP = 1,
                                JSON_TRDA = GETDATE(),
                                JSON_TREN = 'SPEED_PS',
                                JSON_SENT = 1
                            WHERE JSON_KEYU IN ({inClause})
                            AND JSON_FROM = 'data/BRPackingSlipInterfaces'";

                        var updatedRows = await command.ExecuteNonQueryAsync();
                        _logger.LogInformation($"{updatedRows} Packing Slips marqués comme exportés");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du marquage des Packing Slips comme exportés");
                throw;
            }
        }

        /// <summary>
        /// Enregistre le log d'export des Packing Slips
        /// </summary>
        public virtual async Task LogPackingSlipExportAsync(string fileName, int packingSlipsCount, string status, string? message = null)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'txt_packingslip_export_logs')
                            BEGIN
                                CREATE TABLE txt_packingslip_export_logs (
                                    id INT IDENTITY(1,1) PRIMARY KEY,
                                    file_name NVARCHAR(255),
                                    packingslips_count INT DEFAULT 0,
                                    status NVARCHAR(20) DEFAULT 'SUCCESS',
                                    message NVARCHAR(MAX),
                                    export_date DATETIME2 DEFAULT GETDATE()
                                )
                            END
                            
                            INSERT INTO txt_packingslip_export_logs (
                                file_name,
                                packingslips_count,
                                status,
                                message,
                                export_date
                            ) VALUES (
                                @fileName,
                                @packingSlipsCount,
                                @status,
                                @message,
                                GETDATE()
                            )";

                        command.Parameters.AddWithValue("@fileName", fileName);
                        command.Parameters.AddWithValue("@packingSlipsCount", packingSlipsCount);
                        command.Parameters.AddWithValue("@status", status);
                        command.Parameters.AddWithValue("@message", message ?? "");

                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'enregistrement du log d'export Packing Slips");
            }
        }

        /// <summary>
        /// Extrait l'ID du Packing Slip depuis le JSON_BKEY
        /// </summary>
        private string? ExtractPackingSlipIdFromBKey(string? jsonBKey)
        {
            if (string.IsNullOrEmpty(jsonBKey))
                return null;

            // Pour les Packing Slips, utiliser le JSON_BKEY tel quel
            return jsonBKey;
        }

        /// <summary>
        /// Crée les tables nécessaires pour les Packing Slips si elles n'existent pas
        /// </summary>
        public virtual async Task CreatePackingSlipTablesIfNotExistsAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            -- Table de logs d'export TXT Packing Slips
                            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'txt_packingslip_export_logs')
                            BEGIN
                                CREATE TABLE txt_packingslip_export_logs (
                                    id INT IDENTITY(1,1) PRIMARY KEY,
                                    file_name NVARCHAR(255),
                                    packingslips_count INT DEFAULT 0,
                                    status NVARCHAR(20) DEFAULT 'SUCCESS',
                                    message NVARCHAR(MAX),
                                    export_date DATETIME2 DEFAULT GETDATE()
                                )
                            END";

                        await command.ExecuteNonQueryAsync();
                    }
                }

                _logger.LogInformation("Tables Packing Slips vérifiées/créées avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification des tables Packing Slips");
                throw;
            }
        }
    }
}