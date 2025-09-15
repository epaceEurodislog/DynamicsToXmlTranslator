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
    public class ReturnOrderDatabaseService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ReturnOrderDatabaseService> _logger;
        private readonly string _connectionString;

        public ReturnOrderDatabaseService(IConfiguration configuration, ILogger<ReturnOrderDatabaseService> logger)
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
        /// Récupère tous les Return Orders de la base de données
        /// </summary>
        public virtual async Task<List<ReturnOrder>> GetAllReturnOrdersAsync()
        {
            var returnOrders = new List<ReturnOrder>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = connection.CreateCommand())
                    {
                        // ✅ CORRIGÉ : Utilise le bon endpoint selon vos données
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
                            AND JSON_FROM = 'data/BRINT32ReturnOrderTables'
                            ORDER BY JSON_BKEY";

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                try
                                {
                                    var returnOrder = new ReturnOrder
                                    {
                                        Id = reader.GetInt32(0),
                                        JsonData = reader.GetString(1),
                                        ContentHash = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                        ApiEndpoint = reader.IsDBNull(3) ? null : reader.GetString(3),
                                        ReturnOrderId = ExtractReturnOrderIdFromBKey(reader.IsDBNull(4) ? null : reader.GetString(4)),
                                        FirstSeenAt = reader.GetDateTime(5),
                                        LastUpdatedAt = reader.GetDateTime(6),
                                        UpdateCount = reader.GetInt32(7)
                                    };

                                    if (!string.IsNullOrEmpty(returnOrder.JsonData))
                                    {
                                        returnOrder.DynamicsData = JsonConvert.DeserializeObject<DynamicsReturnOrder>(returnOrder.JsonData);
                                    }

                                    returnOrders.Add(returnOrder);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, $"Erreur lors de la lecture du Return Order ID: {reader.GetInt32(0)}");
                                }
                            }
                        }
                    }
                }

                _logger.LogInformation($"{returnOrders.Count} Return Orders récupérés de la base de données");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des Return Orders");
                throw;
            }

            return returnOrders;
        }

        /// <summary>
        /// Récupère uniquement les Return Orders non exportés
        /// </summary>
        public async Task<List<ReturnOrder>> GetNonExportedReturnOrdersAsync()
        {
            var returnOrders = new List<ReturnOrder>();

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
                            AND JSON_FROM = 'data/BRINT32ReturnOrderTables'
                            AND (JSON_TRTP = 0 OR JSON_TRTP IS NULL)
                            ORDER BY JSON_BKEY";

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                try
                                {
                                    var returnOrder = new ReturnOrder
                                    {
                                        Id = reader.GetInt32(0),
                                        JsonData = reader.GetString(1),
                                        ContentHash = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                        ApiEndpoint = reader.IsDBNull(3) ? null : reader.GetString(3),
                                        ReturnOrderId = ExtractReturnOrderIdFromBKey(reader.IsDBNull(4) ? null : reader.GetString(4)),
                                        FirstSeenAt = reader.GetDateTime(5),
                                        LastUpdatedAt = reader.GetDateTime(6),
                                        UpdateCount = reader.GetInt32(7)
                                    };

                                    if (!string.IsNullOrEmpty(returnOrder.JsonData))
                                    {
                                        returnOrder.DynamicsData = JsonConvert.DeserializeObject<DynamicsReturnOrder>(returnOrder.JsonData);
                                    }

                                    returnOrders.Add(returnOrder);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, $"Erreur lors de la lecture du Return Order ID: {reader.GetInt32(0)}");
                                }
                            }
                        }
                    }
                }

                _logger.LogInformation($"{returnOrders.Count} Return Orders non exportés récupérés");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des Return Orders non exportés");
                throw;
            }

            return returnOrders;
        }

        /// <summary>
        /// Marque les Return Orders comme exportés
        /// </summary>
        public async Task MarkReturnOrdersAsExportedAsync(List<int> returnOrderIds, string batchName)
        {
            if (returnOrderIds == null || !returnOrderIds.Any())
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
                        var inClause = string.Join(",", returnOrderIds.Select(id => id.ToString()));

                        command.CommandText = $@"
                            UPDATE dbo.JSON_IN 
                            SET 
                                JSON_TRTP = 1,
                                JSON_TRDA = GETDATE(),
                                JSON_TREN = 'SPEED_RO',
                                JSON_SENT = 1
                            WHERE JSON_KEYU IN ({inClause})
                            AND JSON_FROM = 'data/BRINT32ReturnOrderTables'";

                        var updatedRows = await command.ExecuteNonQueryAsync();
                        _logger.LogInformation($"{updatedRows} Return Orders marqués comme exportés");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du marquage des Return Orders comme exportés");
                throw;
            }
        }

        /// <summary>
        /// Enregistre le log d'export des Return Orders
        /// </summary>
        public virtual async Task LogReturnOrderExportAsync(string fileName, int returnOrdersCount, string status, string? message = null)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'xml_return_export_logs')
                            BEGIN
                                CREATE TABLE xml_return_export_logs (
                                    id INT IDENTITY(1,1) PRIMARY KEY,
                                    file_name NVARCHAR(255),
                                    return_orders_count INT DEFAULT 0,
                                    status NVARCHAR(20) DEFAULT 'SUCCESS',
                                    message NVARCHAR(MAX),
                                    export_date DATETIME2 DEFAULT GETDATE()
                                )
                            END
                            
                            INSERT INTO xml_return_export_logs (
                                file_name,
                                return_orders_count,
                                status,
                                message,
                                export_date
                            ) VALUES (
                                @fileName,
                                @returnOrdersCount,
                                @status,
                                @message,
                                GETDATE()
                            )";

                        command.Parameters.AddWithValue("@fileName", fileName);
                        command.Parameters.AddWithValue("@returnOrdersCount", returnOrdersCount);
                        command.Parameters.AddWithValue("@status", status);
                        command.Parameters.AddWithValue("@message", message ?? "");

                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'enregistrement du log d'export Return Orders");
            }
        }

        /// <summary>
        /// Extrait l'ID du Return Order depuis le JSON_BKEY
        /// Selon votre exemple : "HASH_ECE1F4FBFB0A364D_1751977706"
        /// </summary>
        private string? ExtractReturnOrderIdFromBKey(string? jsonBKey)
        {
            if (string.IsNullOrEmpty(jsonBKey))
                return null;

            // Pour les Return Orders, utiliser le JSON_BKEY tel quel
            // ou extraire une partie si nécessaire
            return jsonBKey;
        }

        /// <summary>
        /// Crée les tables nécessaires pour les Return Orders si elles n'existent pas
        /// </summary>
        public virtual async Task CreateReturnOrderTablesIfNotExistsAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            -- Table de logs d'export XML Return Orders
                            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'xml_return_export_logs')
                            BEGIN
                                CREATE TABLE xml_return_export_logs (
                                    id INT IDENTITY(1,1) PRIMARY KEY,
                                    file_name NVARCHAR(255),
                                    return_orders_count INT DEFAULT 0,
                                    status NVARCHAR(20) DEFAULT 'SUCCESS',
                                    message NVARCHAR(MAX),
                                    export_date DATETIME2 DEFAULT GETDATE()
                                )
                            END";

                        await command.ExecuteNonQueryAsync();
                    }
                }

                _logger.LogInformation("Tables Return Orders vérifiées/créées avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification des tables Return Orders");
                throw;
            }
        }

        /// <summary>
        /// Récupère les Return Orders depuis une date donnée
        /// </summary>
        public async Task<List<ReturnOrder>> GetReturnOrdersSinceDateAsync(DateTime sinceDate)
        {
            var returnOrders = new List<ReturnOrder>();

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
                            AND JSON_FROM = 'data/BRINT32ReturnOrderTables'
                            AND JSON_CRDA >= @sinceDate
                            ORDER BY JSON_BKEY";

                        command.Parameters.AddWithValue("@sinceDate", sinceDate);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                try
                                {
                                    var returnOrder = new ReturnOrder
                                    {
                                        Id = reader.GetInt32(0),
                                        JsonData = reader.GetString(1),
                                        ContentHash = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                        ApiEndpoint = reader.IsDBNull(3) ? null : reader.GetString(3),
                                        ReturnOrderId = ExtractReturnOrderIdFromBKey(reader.IsDBNull(4) ? null : reader.GetString(4)),
                                        FirstSeenAt = reader.GetDateTime(5),
                                        LastUpdatedAt = reader.GetDateTime(6),
                                        UpdateCount = reader.GetInt32(7)
                                    };

                                    if (!string.IsNullOrEmpty(returnOrder.JsonData))
                                    {
                                        returnOrder.DynamicsData = JsonConvert.DeserializeObject<DynamicsReturnOrder>(returnOrder.JsonData);
                                    }

                                    returnOrders.Add(returnOrder);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, $"Erreur lors de la lecture du Return Order ID: {reader.GetInt32(0)}");
                                }
                            }
                        }
                    }
                }

                _logger.LogInformation($"{returnOrders.Count} Return Orders récupérés depuis {sinceDate:yyyy-MM-dd HH:mm:ss}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des Return Orders par date");
                throw;
            }

            return returnOrders;
        }
    }
}