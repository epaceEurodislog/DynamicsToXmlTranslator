using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient; // CHANGEMENT : Utilisation de SQL Server
using Newtonsoft.Json;
using DynamicsToXmlTranslator.Models;
using System.Linq;

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

            // CHANGEMENT : Construction de la chaîne de connexion SQL Server
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = _configuration["Database:Host"],
                InitialCatalog = _configuration["Database:Name"],
                UserID = _configuration["Database:User"],
                Password = _configuration["Database:Password"],
                TrustServerCertificate = true, // Pour éviter les erreurs de certificat
                ConnectTimeout = 30
            };
            _connectionString = builder.ConnectionString;
        }

        /// <summary>
        /// Récupère tous les articles de la base de données
        /// CORRIGÉ : Utilise les vrais noms de colonnes découverts par le diagnostic
        /// </summary>
        public virtual async Task<List<Article>> GetAllArticlesAsync()
        {
            var articles = new List<Article>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = connection.CreateCommand())
                    {
                        // CORRIGÉ : Requête avec les vrais noms de colonnes
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
                            WHERE (JSON_STAT = 'ACTIVE' OR JSON_STAT IS NULL)
                            AND (JSON_CCLI = 'BR' OR JSON_CCLI IS NULL)
                            ORDER BY JSON_BKEY";

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                try
                                {
                                    var article = new Article
                                    {
                                        Id = reader.GetInt32(0),                                    // JSON_KEYU
                                        JsonData = reader.GetString(1),                            // JSON_DATA
                                        ContentHash = reader.IsDBNull(2) ? "" : reader.GetString(2), // JSON_HASH
                                        ApiEndpoint = reader.IsDBNull(3) ? null : reader.GetString(3), // JSON_FROM
                                        ItemId = ExtractItemIdFromBKey(reader.IsDBNull(4) ? null : reader.GetString(4)), // JSON_BKEY
                                        FirstSeenAt = reader.GetDateTime(5),                       // JSON_CRDA
                                        LastUpdatedAt = reader.GetDateTime(6),                     // JSON_CRDA
                                        UpdateCount = reader.GetInt32(7)                           // 0 (calculé)
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
        /// CORRIGÉ : Utilise JSON_CRDA au lieu de JSON_CRD
        /// </summary>
        public async Task<List<Article>> GetArticlesSinceDateAsync(DateTime sinceDate)
        {
            var articles = new List<Article>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = connection.CreateCommand())
                    {
                        // CORRIGÉ : Requête avec les vrais noms de colonnes
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
                            WHERE (JSON_STAT = 'ACTIVE' OR JSON_STAT IS NULL)
                            AND (JSON_CCLI = 'BR' OR JSON_CCLI IS NULL)
                            AND JSON_CRDA >= @sinceDate
                            ORDER BY JSON_BKEY";

                        command.Parameters.AddWithValue("@sinceDate", sinceDate);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                try
                                {
                                    var article = new Article
                                    {
                                        Id = reader.GetInt32(0),                                    // JSON_KEYU
                                        JsonData = reader.GetString(1),                            // JSON_DATA
                                        ContentHash = reader.IsDBNull(2) ? "" : reader.GetString(2), // JSON_HASH
                                        ApiEndpoint = reader.IsDBNull(3) ? null : reader.GetString(3), // JSON_FROM
                                        ItemId = ExtractItemIdFromBKey(reader.IsDBNull(4) ? null : reader.GetString(4)), // JSON_BKEY
                                        FirstSeenAt = reader.GetDateTime(5),                       // JSON_CRDA
                                        LastUpdatedAt = reader.GetDateTime(6),                     // JSON_CRDA
                                        UpdateCount = reader.GetInt32(7)                           // 0 (calculé)
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
        /// Récupère uniquement les articles non exportés en XML
        /// CORRIGÉ : Utilisation de JSON_TRTP = 0 pour identifier les articles non exportés
        /// </summary>
        public async Task<List<Article>> GetNonExportedArticlesAsync()
        {
            var articles = new List<Article>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = connection.CreateCommand())
                    {
                        // CORRIGÉ : JSON_TRTP = 0 signifie "en attente d'export"
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
                            WHERE (JSON_STAT = 'ACTIVE' OR JSON_STAT IS NULL)
                            AND (JSON_CCLI = 'BR' OR JSON_CCLI IS NULL)
                            AND (JSON_TRTP = 0 OR JSON_TRTP IS NULL)
                            ORDER BY JSON_BKEY";

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                try
                                {
                                    var article = new Article
                                    {
                                        Id = reader.GetInt32(0),                                    // JSON_KEYU
                                        JsonData = reader.GetString(1),                            // JSON_DATA
                                        ContentHash = reader.IsDBNull(2) ? "" : reader.GetString(2), // JSON_HASH
                                        ApiEndpoint = reader.IsDBNull(3) ? null : reader.GetString(3), // JSON_FROM
                                        ItemId = ExtractItemIdFromBKey(reader.IsDBNull(4) ? null : reader.GetString(4)), // JSON_BKEY
                                        FirstSeenAt = reader.GetDateTime(5),                       // JSON_CRDA
                                        LastUpdatedAt = reader.GetDateTime(6),                     // JSON_CRDA
                                        UpdateCount = reader.GetInt32(7)                           // 0 (calculé)
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

                _logger.LogInformation($"{articles.Count} articles non exportés récupérés");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des articles non exportés");
                throw;
            }

            return articles;
        }

        /// <summary>
        /// Marque les articles comme exportés en XML
        /// CORRIGÉ : Utilisation de JSON_TRTP = 1 et JSON_TRDA pour marquer comme exporté
        /// </summary>
        public async Task MarkArticlesAsExportedAsync(List<int> articleIds, string batchName)
        {
            if (articleIds == null || !articleIds.Any())
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
                        // CORRIGÉ : Mise à jour de JSON_TRTP et JSON_TRDA
                        var inClause = string.Join(",", articleIds.Select(id => id.ToString()));

                        command.CommandText = $@"
                            UPDATE dbo.JSON_IN 
                            SET 
                                JSON_TRTP = 1,
                                JSON_TRDA = GETDATE(),
                                JSON_TREN = 'SPEED'
                            WHERE JSON_KEYU IN ({inClause})";

                        var updatedRows = await command.ExecuteNonQueryAsync();
                        _logger.LogInformation($"{updatedRows} articles marqués comme exportés");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du marquage des articles comme exportés");
                throw;
            }
        }

        /// <summary>
        /// Enregistre le log d'export
        /// </summary>
        public virtual async Task LogExportAsync(string fileName, int articlesCount, string status, string? message = null)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = connection.CreateCommand())
                    {
                        // Table de logs séparée (optionnelle)
                        command.CommandText = @"
                            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'xml_export_logs')
                            BEGIN
                                CREATE TABLE xml_export_logs (
                                    id INT IDENTITY(1,1) PRIMARY KEY,
                                    file_name NVARCHAR(255),
                                    articles_count INT DEFAULT 0,
                                    status NVARCHAR(20) DEFAULT 'SUCCESS',
                                    message NVARCHAR(MAX),
                                    export_date DATETIME2 DEFAULT GETDATE()
                                )
                            END
                            
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
                                GETDATE()
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
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Vérification que la table JSON_IN existe
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'JSON_IN')
                            BEGIN
                                RAISERROR('Table JSON_IN n''existe pas dans la base Middleware', 16, 1)
                            END
                            
                            -- Table de logs d'export XML (optionnelle)
                            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'xml_export_logs')
                            BEGIN
                                CREATE TABLE xml_export_logs (
                                    id INT IDENTITY(1,1) PRIMARY KEY,
                                    file_name NVARCHAR(255),
                                    articles_count INT DEFAULT 0,
                                    status NVARCHAR(20) DEFAULT 'SUCCESS',
                                    message NVARCHAR(MAX),
                                    export_date DATETIME2 DEFAULT GETDATE()
                                )
                            END";

                        await command.ExecuteNonQueryAsync();
                    }
                }

                _logger.LogInformation("Tables vérifiées/créées avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification des tables");
                throw;
            }
        }

        /// <summary>
        /// Extrait l'ItemId depuis le JSON_BKEY 
        /// Format attendu : "ART_XXXXXX" → renvoie "XXXXXX"
        /// </summary>
        private string? ExtractItemIdFromBKey(string? jsonBKey)
        {
            if (string.IsNullOrEmpty(jsonBKey))
                return null;

            // Si le format est "ART_XXXXXX", on extrait "XXXXXX"
            if (jsonBKey.StartsWith("ART_"))
            {
                return jsonBKey.Substring(4);
            }

            // Sinon on retourne tel quel
            return jsonBKey;
        }
    }
}