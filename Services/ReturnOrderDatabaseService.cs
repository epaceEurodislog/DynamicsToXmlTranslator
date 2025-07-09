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
                        // TODO: Adapter le JSON_FROM selon votre endpoint API pour les Return Orders
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
                            AND JSON_FROM = 'data/BRReturnOrders'
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
                            AND JSON_FROM = 'data/BRReturnOrders'
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