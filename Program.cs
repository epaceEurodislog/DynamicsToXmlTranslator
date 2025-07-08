using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DynamicsToXmlTranslator.Mappers;
using DynamicsToXmlTranslator.Models;
using DynamicsToXmlTranslator.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;

namespace DynamicsToXmlTranslator
{
    class Program
    {
        private static Microsoft.Extensions.Logging.ILogger<Program> _logger;
        private static IConfiguration _configuration;
        private static DatabaseService _databaseService;
        private static XmlExportService _xmlExportService;
        private static ArticleMapper _articleMapper;

        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Traducteur Dynamics vers XML WINDEV ===");
            Console.WriteLine($"Démarrage automatique : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            try
            {
                // Configuration et initialisation
                SetupConfiguration();
                SetupLogging();
                SetupServices();

                // Vérifier/créer les tables nécessaires
                await _databaseService.CreateTablesIfNotExistsAsync();

                // Déterminer le mode d'exécution
                bool isTestMode = IsTestMode(args);

                if (isTestMode)
                {
                    Console.WriteLine("🧪 MODE TEST ACTIVÉ - Récupération de TOUS les articles");
                    _logger.LogInformation("Mode test activé - export de tous les articles sans marquage");
                    await ExportAllArticlesTestMode();
                }
                else
                {
                    Console.WriteLine("🔄 MODE PRODUCTION - Récupération des nouveaux articles uniquement");
                    _logger.LogInformation("Mode production - export des nouveaux articles uniquement");
                    await ExportNewArticlesOnly();
                }

                Console.WriteLine("\n✅ Export terminé avec succès");
                _logger.LogInformation("Export automatique terminé avec succès");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erreur fatale dans le programme principal");
                Console.WriteLine($"\n❌ ERREUR FATALE : {ex.Message}");
                Console.WriteLine("Consultez les logs pour plus de détails.");
                throw; // Relancer l'exception pour que le programme appelant puisse la gérer
            }
            finally
            {
                // Fermer les logs proprement
                Log.CloseAndFlush();
            }
        }

        /// <summary>
        /// Détermine si le mode test est activé via les arguments ou la configuration
        /// </summary>
        private static bool IsTestMode(string[] args)
        {
            // Vérifier les arguments de ligne de commande
            if (args.Length > 0)
            {
                var arg = args[0].ToLower();
                if (arg == "test" || arg == "--test" || arg == "-t" || arg == "all")
                {
                    return true;
                }
            }

            // Vérifier la configuration
            var testModeConfig = _configuration?.GetValue<bool>("Export:TestMode", false) ?? false;
            return testModeConfig;
        }

        /// <summary>
        /// Mode test : Export de tous les articles SANS les marquer comme exportés
        /// </summary>
        private static async Task ExportAllArticlesTestMode()
        {
            Console.WriteLine("\n🧪 MODE TEST - Export de tous les articles");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Récupérer TOUS les articles (même ceux déjà exportés)
                Console.WriteLine("Récupération de TOUS les articles depuis la base de données...");
                var articles = await _databaseService.GetAllArticlesAsync();
                Console.WriteLine($"✓ {articles.Count} articles trouvés (incluant ceux déjà exportés)");

                if (articles.Count == 0)
                {
                    Console.WriteLine("ℹ️ Aucun article trouvé dans la base de données");
                    return;
                }

                // Convertir les articles
                Console.WriteLine("Conversion des articles au format WINDEV...");
                var winDevArticles = new List<WinDevArticle>();
                int erreurs = 0;

                foreach (var article in articles)
                {
                    var winDevArticle = _articleMapper.MapToWinDev(article);
                    if (winDevArticle != null)
                    {
                        winDevArticles.Add(winDevArticle);
                    }
                    else
                    {
                        erreurs++;
                    }
                }

                Console.WriteLine($"✓ {winDevArticles.Count} articles convertis avec succès");
                if (erreurs > 0)
                {
                    Console.WriteLine($"⚠️ {erreurs} articles n'ont pas pu être convertis");
                }

                // Export en XML SANS marquer les articles comme exportés (mode test)
                Console.WriteLine("Export en fichier(s) XML (MODE TEST - pas de marquage)...");
                var batchSize = _configuration.GetValue<int>("XmlExport:BatchSize", 1000);

                if (winDevArticles.Count > batchSize)
                {
                    // Export par lots SANS marquage
                    var files = await _xmlExportService.ExportInBatchesAsync(winDevArticles, null, batchSize);
                    Console.WriteLine($"✓ Export terminé : {files.Count} fichiers créés");

                    foreach (var file in files)
                    {
                        Console.WriteLine($"  📁 {Path.GetFileName(file)} (TEST)");
                    }

                    // Enregistrer le log d'export
                    await _databaseService.LogExportAsync(
                        $"Export TEST complet ({files.Count} fichiers)",
                        winDevArticles.Count,
                        "SUCCESS",
                        $"MODE TEST - {files.Count} fichiers générés SANS marquage"
                    );
                }
                else
                {
                    // Export en un seul fichier SANS marquage
                    var filePath = await _xmlExportService.ExportToXmlAsync(winDevArticles, null, "ARTICLE_TEST_COMPLET");
                    if (filePath != null)
                    {
                        Console.WriteLine($"✓ Export terminé : {Path.GetFileName(filePath)} (TEST)");
                        Console.WriteLine($"  📁 Chemin complet : {filePath}");

                        // Enregistrer le log d'export
                        await _databaseService.LogExportAsync(
                            Path.GetFileName(filePath),
                            winDevArticles.Count,
                            "SUCCESS",
                            "MODE TEST - Export complet SANS marquage"
                        );
                    }
                }

                Console.WriteLine($"🧪 MODE TEST : {winDevArticles.Count} articles exportés SANS marquage");
                Console.WriteLine("⚠️ Les articles ne sont PAS marqués comme exportés en mode test");

                stopwatch.Stop();
                Console.WriteLine($"\n⏱️ Temps total : {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur lors de l'export en mode test : {ex.Message}");
                _logger.LogError(ex, "Erreur lors de l'export en mode test");

                await _databaseService.LogExportAsync(
                    "Export TEST échoué",
                    0,
                    "ERROR",
                    ex.Message
                );

                throw;
            }
        }

        /// <summary>
        /// Mode production : Export des nouveaux articles uniquement avec marquage
        /// </summary>
        private static async Task ExportNewArticlesOnly()
        {
            Console.WriteLine("\n🆕 Export des nouveaux articles uniquement");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Récupérer uniquement les articles non exportés
                Console.WriteLine("Récupération des nouveaux articles depuis la base de données...");
                var articles = await _databaseService.GetNonExportedArticlesAsync();
                Console.WriteLine($"✓ {articles.Count} nouveaux articles trouvés");

                if (articles.Count == 0)
                {
                    Console.WriteLine("ℹ️ Aucun nouvel article à exporter");
                    return;
                }

                // Convertir les articles
                Console.WriteLine("Conversion des articles au format WINDEV...");
                var winDevArticles = new List<WinDevArticle>();
                var originalIds = new List<int>();
                int erreurs = 0;

                foreach (var article in articles)
                {
                    var winDevArticle = _articleMapper.MapToWinDev(article);
                    if (winDevArticle != null)
                    {
                        winDevArticles.Add(winDevArticle);
                        originalIds.Add(article.Id);
                    }
                    else
                    {
                        erreurs++;
                    }
                }

                Console.WriteLine($"✓ {winDevArticles.Count} articles convertis avec succès");
                if (erreurs > 0)
                {
                    Console.WriteLine($"⚠️ {erreurs} articles n'ont pas pu être convertis");
                }

                // Exporter en XML avec marquage automatique
                Console.WriteLine("Export en fichier(s) XML...");
                var batchSize = _configuration.GetValue<int>("XmlExport:BatchSize", 1000);

                if (winDevArticles.Count > batchSize)
                {
                    // Export par lots
                    var files = await _xmlExportService.ExportInBatchesAsync(winDevArticles, originalIds, batchSize);
                    Console.WriteLine($"✓ Export terminé : {files.Count} fichiers créés");

                    foreach (var file in files)
                    {
                        Console.WriteLine($"  📁 {Path.GetFileName(file)}");
                    }

                    // Enregistrer le log d'export
                    await _databaseService.LogExportAsync(
                        $"Export automatique ({files.Count} fichiers)",
                        winDevArticles.Count,
                        "SUCCESS",
                        $"{files.Count} fichiers générés automatiquement"
                    );
                }
                else
                {
                    // Export en un seul fichier
                    var filePath = await _xmlExportService.ExportToXmlAsync(winDevArticles, originalIds);
                    if (filePath != null)
                    {
                        Console.WriteLine($"✓ Export terminé : {Path.GetFileName(filePath)}");
                        Console.WriteLine($"  📁 Chemin complet : {filePath}");

                        // Enregistrer le log d'export
                        await _databaseService.LogExportAsync(
                            Path.GetFileName(filePath),
                            winDevArticles.Count,
                            "SUCCESS",
                            "Export automatique"
                        );
                    }
                }

                Console.WriteLine($"🎯 {winDevArticles.Count} articles marqués comme exportés");
                stopwatch.Stop();
                Console.WriteLine($"\n⏱️ Temps total : {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur lors de l'export des nouveaux articles : {ex.Message}");
                _logger.LogError(ex, "Erreur lors de l'export des nouveaux articles");

                await _databaseService.LogExportAsync(
                    "Export automatique échoué",
                    0,
                    "ERROR",
                    ex.Message
                );

                throw; // Relancer pour que le programme appelant sache qu'il y a eu une erreur
            }
        }

        private static void SetupConfiguration()
        {
            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
        }

        private static void SetupLogging()
        {
            // Créer le répertoire de logs s'il n'existe pas
            var logDirectory = "logs";
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            // Configuration Serilog simplifiée pour .NET 8
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    path: Path.Combine(logDirectory, "translator.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    fileSizeLimitBytes: 10 * 1024 * 1024, // 10MB
                    rollOnFileSizeLimit: true,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddSerilog(dispose: true);
            });

            _logger = loggerFactory.CreateLogger<Program>();

            _logger.LogInformation("=== Traducteur Dynamics vers XML WINDEV - Démarrage ===");
            _logger.LogInformation("Version .NET: {DotNetVersion}", Environment.Version);
        }

        private static void SetupServices()
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddSerilog(dispose: true);
            });

            _databaseService = new DatabaseService(_configuration, loggerFactory.CreateLogger<DatabaseService>());
            _xmlExportService = new XmlExportService(_configuration, loggerFactory.CreateLogger<XmlExportService>(), _databaseService);
            _articleMapper = new ArticleMapper(_configuration, loggerFactory.CreateLogger<ArticleMapper>());
        }
    }
}