using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using DynamicsToXmlTranslator.Models;

namespace DynamicsToXmlTranslator.Services
{
    public class DatabaseService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DatabaseService> _logger;
        private readonly string _connectionString;

        public DatabaseService(IConfiguration configuration, ILogger<DatabaseService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            _connectionString = new MySqlConnectionStringBuilder
            {
                Server = _configuration["Database:Host"],
                Port = (uint)_configuration.GetValue<int>("Database:Port", 3306),
                UserID = _configuration["Database:User"],
                Password = _configuration["Database:Password"],
                Database = _configuration["Database:Name"]
            }.ConnectionString;
        }

        /// <summary>
        /// Récupère tous les articles de la base de données
        /// </summary>
        public virtual async Task<List<Article>> GetAllArticlesAsync()
        {
            var articles = new List<Article>();

            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            SELECT 
                                id,
                                json_data,
                                content_hash,
                                api_endpoint,
                                item_id,
                                first_seen_at,
                                last_updated_at,
                                update_count
                            FROM articles_raw
                            ORDER BY item_id";

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                try
                                {
                                    var article = new Article
                                    {
                                        Id = reader.GetInt32(0),                                    // id
                                        JsonData = reader.GetString(1),                            // json_data
                                        ContentHash = reader.GetString(2),                         // content_hash
                                        ApiEndpoint = reader.IsDBNull(3) ? null : reader.GetString(3), // api_endpoint
                                        ItemId = reader.IsDBNull(4) ? null : reader.GetString(4),     // item_id
                                        FirstSeenAt = reader.GetDateTime(5),                       // first_seen_at
                                        LastUpdatedAt = reader.GetDateTime(6),                     // last_updated_at
                                        UpdateCount = reader.GetInt32(7)                           // update_count
                                    };

                                    // Désérialiser le JSON Dynamics
                                    if (!string.IsNullOrEmpty(article.JsonData))
                                    {
                                        article.DynamicsData = JsonConvert.DeserializeObject<DynamicsArticle>(article.JsonData);
                                    }

                                    articles.Add(article);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, $"Erreur lors de la lecture de l'article ID: {reader.GetInt32(0)}");
                                }
                            }
                        }
                    }
                }

                _logger.LogInformation($"{articles.Count} articles récupérés de la base de données");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des articles");
                throw;
            }

            return articles;
        }

        /// <summary>
        /// Récupère les articles modifiés depuis une date donnée
        /// </summary>
        public async Task<List<Article>> GetArticlesSinceDateAsync(DateTime sinceDate)
        {
            var articles = new List<Article>();

            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            SELECT 
                                id,
                                json_data,
                                content_hash,
                                api_endpoint,
                                item_id,
                                first_seen_at,
                                last_updated_at,
                                update_count
                            FROM articles_raw
                            WHERE last_updated_at >= @sinceDate
                            ORDER BY item_id";

                        command.Parameters.AddWithValue("@sinceDate", sinceDate);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                try
                                {
                                    var article = new Article
                                    {
                                        Id = reader.GetInt32(0),                                    // id
                                        JsonData = reader.GetString(1),                            // json_data
                                        ContentHash = reader.GetString(2),                         // content_hash
                                        ApiEndpoint = reader.IsDBNull(3) ? null : reader.GetString(3), // api_endpoint
                                        ItemId = reader.IsDBNull(4) ? null : reader.GetString(4),     // item_id
                                        FirstSeenAt = reader.GetDateTime(5),                       // first_seen_at
                                        LastUpdatedAt = reader.GetDateTime(6),                     // last_updated_at
                                        UpdateCount = reader.GetInt32(7)                           // update_count
                                    };

                                    // Désérialiser le JSON Dynamics
                                    if (!string.IsNullOrEmpty(article.JsonData))
                                    {
                                        article.DynamicsData = JsonConvert.DeserializeObject<DynamicsArticle>(article.JsonData);
                                    }

                                    articles.Add(article);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, $"Erreur lors de la lecture de l'article ID: {reader.GetInt32(0)}");
                                }
                            }
                        }
                    }
                }

                _logger.LogInformation($"{articles.Count} articles récupérés depuis {sinceDate:yyyy-MM-dd HH:mm:ss}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des articles par date");
                throw;
            }

            return articles;
        }

        /// <summary>
        /// Enregistre le log d'export
        /// </summary>
        public virtual async Task LogExportAsync(string fileName, int articlesCount, string status, string? message = null)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            INSERT INTO xml_export_logs (
                                file_name,
                                articles_count,
                                status,
                                message,
                                export_date
                            ) VALUES (
                                @fileName,
                                @articlesCount,
                                @status,
                                @message,
                                NOW()
                            )";

                        command.Parameters.AddWithValue("@fileName", fileName);
                        command.Parameters.AddWithValue("@articlesCount", articlesCount);
                        command.Parameters.AddWithValue("@status", status);
                        command.Parameters.AddWithValue("@message", message ?? "");

                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'enregistrement du log d'export");
            }
        }

        /// <summary>
        /// Crée les tables nécessaires si elles n'existent pas
        /// </summary>
        public virtual async Task CreateTablesIfNotExistsAsync()
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Table de logs d'export XML
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            CREATE TABLE IF NOT EXISTS xml_export_logs (
                                id INT AUTO_INCREMENT PRIMARY KEY,
                                file_name VARCHAR(255),
                                articles_count INT DEFAULT 0,
                                status ENUM('SUCCESS', 'ERROR', 'WARNING') DEFAULT 'SUCCESS',
                                message TEXT,
                                export_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                                INDEX idx_export_date (export_date)
                            )";

                        await command.ExecuteNonQueryAsync();
                    }
                }

                _logger.LogInformation("Tables vérifiées/créées avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création des tables");
                throw;
            }
        }
    }
}