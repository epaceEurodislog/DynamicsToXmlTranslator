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
    public class TransferOrderDatabaseService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<TransferOrderDatabaseService> _logger;
        private readonly string _connectionString;

        public TransferOrderDatabaseService(IConfiguration configuration, ILogger<TransferOrderDatabaseService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            // Construction de la chaîne de connexion SQL Server
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
        /// Récupère tous les Transfer Orders de la base de données
        /// </summary>
        public virtual async Task<List<TransferOrder>> GetAllTransferOrdersAsync()
        {
            var transferOrders = new List<TransferOrder>();

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
                            AND JSON_FROM = 'data/BRINT32TransferOrderTables'
                            ORDER BY JSON_BKEY";

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                try
                                {
                                    var transferOrder = new TransferOrder
                                    {
                                        Id = reader.GetInt32(0),
                                        JsonData = reader.GetString(1),
                                        ContentHash = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                        ApiEndpoint = reader.IsDBNull(3) ? null : reader.GetString(3),
                                        TransferOrderId = ExtractTransferOrderIdFromBKey(reader.IsDBNull(4) ? null : reader.GetString(4)),
                                        FirstSeenAt = reader.GetDateTime(5),
                                        LastUpdatedAt = reader.GetDateTime(6),
                                        UpdateCount = reader.GetInt32(7)
                                    };

                                    if (!string.IsNullOrEmpty(transferOrder.JsonData))
                                    {
                                        transferOrder.DynamicsData = JsonConvert.DeserializeObject<DynamicsTransferOrder>(transferOrder.JsonData);
                                    }

                                    transferOrders.Add(transferOrder);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, $"Erreur lors de la lecture du Transfer Order ID: {reader.GetInt32(0)}");
                                }
                            }
                        }
                    }
                }

                _logger.LogInformation($"{transferOrders.Count} Transfer Orders récupérés de la base de données");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des Transfer Orders");
                throw;
            }

            return transferOrders;
        }

        /// <summary>
        /// Récupère uniquement les Transfer Orders non exportés
        /// </summary>
        public async Task<List<TransferOrder>> GetNonExportedTransferOrdersAsync()
        {
            var transferOrders = new List<TransferOrder>();

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
                            AND JSON_FROM = 'data/BRINT32TransferOrderTables'
                            AND (JSON_TRTP = 0 OR JSON_TRTP IS NULL)
                            ORDER BY JSON_BKEY";

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                try
                                {
                                    var transferOrder = new TransferOrder
                                    {
                                        Id = reader.GetInt32(0),
                                        JsonData = reader.GetString(1),
                                        ContentHash = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                        ApiEndpoint = reader.IsDBNull(3) ? null : reader.GetString(3),
                                        TransferOrderId = ExtractTransferOrderIdFromBKey(reader.IsDBNull(4) ? null : reader.GetString(4)),
                                        FirstSeenAt = reader.GetDateTime(5),
                                        LastUpdatedAt = reader.GetDateTime(6),
                                        UpdateCount = reader.GetInt32(7)
                                    };

                                    if (!string.IsNullOrEmpty(transferOrder.JsonData))
                                    {
                                        transferOrder.DynamicsData = JsonConvert.DeserializeObject<DynamicsTransferOrder>(transferOrder.JsonData);
                                    }

                                    transferOrders.Add(transferOrder);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, $"Erreur lors de la lecture du Transfer Order ID: {reader.GetInt32(0)}");
                                }
                            }
                        }
                    }
                }

                _logger.LogInformation($"{transferOrders.Count} Transfer Orders non exportés récupérés");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des Transfer Orders non exportés");
                throw;
            }

            return transferOrders;
        }

        /// <summary>
        /// Marque les Transfer Orders comme exportés
        /// </summary>
        public async Task MarkTransferOrdersAsExportedAsync(List<int> transferOrderIds, string batchName)
        {
            if (transferOrderIds == null || !transferOrderIds.Any())
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
                        var inClause = string.Join(",", transferOrderIds.Select(id => id.ToString()));

                        command.CommandText = $@"
                            UPDATE dbo.JSON_IN 
                            SET 
                                JSON_TRTP = 1,
                                JSON_TRDA = GETDATE(),
                                JSON_TREN = 'SPEED_TO',
                                JSON_SENT = 1
                            WHERE JSON_KEYU IN ({inClause})
                            AND JSON_FROM = 'data/BRINT32TransferOrderTables'";

                        var updatedRows = await command.ExecuteNonQueryAsync();
                        _logger.LogInformation($"{updatedRows} Transfer Orders marqués comme exportés");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du marquage des Transfer Orders comme exportés");
                throw;
            }
        }

        /// <summary>
        /// Enregistre le log d'export des Transfer Orders
        /// </summary>
        public virtual async Task LogTransferOrderExportAsync(string fileName, int transferOrdersCount, string status, string? message = null)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'xml_transfer_export_logs')
                            BEGIN
                                CREATE TABLE xml_transfer_export_logs (
                                    id INT IDENTITY(1,1) PRIMARY KEY,
                                    file_name NVARCHAR(255),
                                    transfer_orders_count INT DEFAULT 0,
                                    status NVARCHAR(20) DEFAULT 'SUCCESS',
                                    message NVARCHAR(MAX),
                                    export_date DATETIME2 DEFAULT GETDATE()
                                )
                            END
                            
                            INSERT INTO xml_transfer_export_logs (
                                file_name,
                                transfer_orders_count,
                                status,
                                message,
                                export_date
                            ) VALUES (
                                @fileName,
                                @transferOrdersCount,
                                @status,
                                @message,
                                GETDATE()
                            )";

                        command.Parameters.AddWithValue("@fileName", fileName);
                        command.Parameters.AddWithValue("@transferOrdersCount", transferOrdersCount);
                        command.Parameters.AddWithValue("@status", status);
                        command.Parameters.AddWithValue("@message", message ?? "");

                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'enregistrement du log d'export Transfer Orders");
            }
        }

        /// <summary>
        /// Extrait l'ID du Transfer Order depuis le JSON_BKEY
        /// </summary>
        private string? ExtractTransferOrderIdFromBKey(string? jsonBKey)
        {
            if (string.IsNullOrEmpty(jsonBKey))
                return null;

            // Pour les Transfer Orders, utiliser le JSON_BKEY tel quel
            return jsonBKey;
        }

        /// <summary>
        /// Crée les tables nécessaires pour les Transfer Orders si elles n'existent pas
        /// </summary>
        public virtual async Task CreateTransferOrderTablesIfNotExistsAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            -- Table de logs d'export XML Transfer Orders
                            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'xml_transfer_export_logs')
                            BEGIN
                                CREATE TABLE xml_transfer_export_logs (
                                    id INT IDENTITY(1,1) PRIMARY KEY,
                                    file_name NVARCHAR(255),
                                    transfer_orders_count INT DEFAULT 0,
                                    status NVARCHAR(20) DEFAULT 'SUCCESS',
                                    message NVARCHAR(MAX),
                                    export_date DATETIME2 DEFAULT GETDATE()
                                )
                            END";

                        await command.ExecuteNonQueryAsync();
                    }
                }

                _logger.LogInformation("Tables Transfer Orders vérifiées/créées avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification des tables Transfer Orders");
                throw;
            }
        }

        /// <summary>
        /// Récupère les Transfer Orders depuis une date donnée
        /// </summary>
        public async Task<List<TransferOrder>> GetTransferOrdersSinceDateAsync(DateTime sinceDate)
        {
            var transferOrders = new List<TransferOrder>();

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
                            AND JSON_FROM = 'data/BRINT32TransferOrderTables'
                            AND JSON_CRDA >= @sinceDate
                            ORDER BY JSON_BKEY";

                        command.Parameters.AddWithValue("@sinceDate", sinceDate);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                try
                                {
                                    var transferOrder = new TransferOrder
                                    {
                                        Id = reader.GetInt32(0),
                                        JsonData = reader.GetString(1),
                                        ContentHash = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                        ApiEndpoint = reader.IsDBNull(3) ? null : reader.GetString(3),
                                        TransferOrderId = ExtractTransferOrderIdFromBKey(reader.IsDBNull(4) ? null : reader.GetString(4)),
                                        FirstSeenAt = reader.GetDateTime(5),
                                        LastUpdatedAt = reader.GetDateTime(6),
                                        UpdateCount = reader.GetInt32(7)
                                    };

                                    if (!string.IsNullOrEmpty(transferOrder.JsonData))
                                    {
                                        transferOrder.DynamicsData = JsonConvert.DeserializeObject<DynamicsTransferOrder>(transferOrder.JsonData);
                                    }

                                    transferOrders.Add(transferOrder);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, $"Erreur lors de la lecture du Transfer Order ID: {reader.GetInt32(0)}");
                                }
                            }
                        }
                    }
                }

                _logger.LogInformation($"{transferOrders.Count} Transfer Orders récupérés depuis {sinceDate:yyyy-MM-dd HH:mm:ss}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des Transfer Orders par date");
                throw;
            }

            return transferOrders;
        }
    }
}