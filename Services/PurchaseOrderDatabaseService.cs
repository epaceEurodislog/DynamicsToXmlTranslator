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
    public class PurchaseOrderDatabaseService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<PurchaseOrderDatabaseService> _logger;
        private readonly string _connectionString;

        public PurchaseOrderDatabaseService(IConfiguration configuration, ILogger<PurchaseOrderDatabaseService> logger)
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
        /// Récupère tous les Purchase Orders de la base de données
        /// </summary>
        public virtual async Task<List<PurchaseOrder>> GetAllPurchaseOrdersAsync()
        {
            var purchaseOrders = new List<PurchaseOrder>();

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
                            AND JSON_FROM = 'data/BRINT32PurchOrderTables'
                            ORDER BY JSON_BKEY";

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                try
                                {
                                    var purchaseOrder = new PurchaseOrder
                                    {
                                        Id = reader.GetInt32(0),
                                        JsonData = reader.GetString(1),
                                        ContentHash = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                        ApiEndpoint = reader.IsDBNull(3) ? null : reader.GetString(3),
                                        PurchaseOrderId = ExtractPurchaseOrderIdFromBKey(reader.IsDBNull(4) ? null : reader.GetString(4)),
                                        FirstSeenAt = reader.GetDateTime(5),
                                        LastUpdatedAt = reader.GetDateTime(6),
                                        UpdateCount = reader.GetInt32(7)
                                    };

                                    if (!string.IsNullOrEmpty(purchaseOrder.JsonData))
                                    {
                                        purchaseOrder.DynamicsData = JsonConvert.DeserializeObject<DynamicsPurchaseOrder>(purchaseOrder.JsonData);
                                    }

                                    purchaseOrders.Add(purchaseOrder);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, $"Erreur lors de la lecture du Purchase Order ID: {reader.GetInt32(0)}");
                                }
                            }
                        }
                    }
                }

                _logger.LogInformation($"{purchaseOrders.Count} Purchase Orders récupérés de la base de données");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des Purchase Orders");
                throw;
            }

            return purchaseOrders;
        }

        /// <summary>
        /// Récupère uniquement les Purchase Orders non exportés
        /// </summary>
        public async Task<List<PurchaseOrder>> GetNonExportedPurchaseOrdersAsync()
        {
            var purchaseOrders = new List<PurchaseOrder>();

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
                            AND JSON_FROM = 'data/BRINT32PurchOrderTables'
                            AND (JSON_TRTP = 0 OR JSON_TRTP IS NULL)
                            ORDER BY JSON_BKEY";

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                try
                                {
                                    var purchaseOrder = new PurchaseOrder
                                    {
                                        Id = reader.GetInt32(0),
                                        JsonData = reader.GetString(1),
                                        ContentHash = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                        ApiEndpoint = reader.IsDBNull(3) ? null : reader.GetString(3),
                                        PurchaseOrderId = ExtractPurchaseOrderIdFromBKey(reader.IsDBNull(4) ? null : reader.GetString(4)),
                                        FirstSeenAt = reader.GetDateTime(5),
                                        LastUpdatedAt = reader.GetDateTime(6),
                                        UpdateCount = reader.GetInt32(7)
                                    };

                                    if (!string.IsNullOrEmpty(purchaseOrder.JsonData))
                                    {
                                        purchaseOrder.DynamicsData = JsonConvert.DeserializeObject<DynamicsPurchaseOrder>(purchaseOrder.JsonData);
                                    }

                                    purchaseOrders.Add(purchaseOrder);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, $"Erreur lors de la lecture du Purchase Order ID: {reader.GetInt32(0)}");
                                }
                            }
                        }
                    }
                }

                _logger.LogInformation($"{purchaseOrders.Count} Purchase Orders non exportés récupérés");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des Purchase Orders non exportés");
                throw;
            }

            return purchaseOrders;
        }

        /// <summary>
        /// Marque les Purchase Orders comme exportés
        /// </summary>
        public async Task MarkPurchaseOrdersAsExportedAsync(List<int> purchaseOrderIds, string batchName)
        {
            if (purchaseOrderIds == null || !purchaseOrderIds.Any())
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
                        var inClause = string.Join(",", purchaseOrderIds.Select(id => id.ToString()));

                        command.CommandText = $@"
                            UPDATE dbo.JSON_IN 
                            SET 
                                JSON_TRTP = 1,
                                JSON_TRDA = GETDATE(),
                                JSON_TREN = 'SPEED_PO',
                                JSON_SENT = 1
                            WHERE JSON_KEYU IN ({inClause})";

                        var updatedRows = await command.ExecuteNonQueryAsync();
                        _logger.LogInformation($"{updatedRows} Purchase Orders marqués comme exportés");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du marquage des Purchase Orders comme exportés");
                throw;
            }
        }

        /// <summary>
        /// Enregistre le log d'export des Purchase Orders
        /// </summary>
        public virtual async Task LogPurchaseOrderExportAsync(string fileName, int purchaseOrdersCount, string status, string? message = null)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'xml_purchase_export_logs')
                            BEGIN
                                CREATE TABLE xml_purchase_export_logs (
                                    id INT IDENTITY(1,1) PRIMARY KEY,
                                    file_name NVARCHAR(255),
                                    purchase_orders_count INT DEFAULT 0,
                                    status NVARCHAR(20) DEFAULT 'SUCCESS',
                                    message NVARCHAR(MAX),
                                    export_date DATETIME2 DEFAULT GETDATE()
                                )
                            END
                            
                            INSERT INTO xml_purchase_export_logs (
                                file_name,
                                purchase_orders_count,
                                status,
                                message,
                                export_date
                            ) VALUES (
                                @fileName,
                                @purchaseOrdersCount,
                                @status,
                                @message,
                                GETDATE()
                            )";

                        command.Parameters.AddWithValue("@fileName", fileName);
                        command.Parameters.AddWithValue("@purchaseOrdersCount", purchaseOrdersCount);
                        command.Parameters.AddWithValue("@status", status);
                        command.Parameters.AddWithValue("@message", message ?? "");

                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'enregistrement du log d'export Purchase Orders");
            }
        }

        /// <summary>
        /// Extrait l'ID du Purchase Order depuis le JSON_DATA
        /// </summary>
        private string? ExtractPurchaseOrderIdFromBKey(string? jsonBKey)
        {
            // Pour les Purchase Orders, on utilise directement le JSON_BKEY comme ID
            // ou on peut extraire le PurchId du JSON_DATA
            return jsonBKey;
        }
    }
}