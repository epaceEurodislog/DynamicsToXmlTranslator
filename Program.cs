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

        // Services Articles
        private static DatabaseService _databaseService;
        private static XmlExportService _xmlExportService;
        private static ArticleMapper _articleMapper;

        // Services Purchase Orders
        private static PurchaseOrderDatabaseService _purchaseOrderDatabaseService;
        private static PurchaseOrderXmlExportService _purchaseOrderXmlExportService;
        private static PurchaseOrderMapper _purchaseOrderMapper;

        // Services Return Orders
        private static ReturnOrderDatabaseService _returnOrderDatabaseService;
        private static ReturnOrderXmlExportService _returnOrderXmlExportService;
        private static ReturnOrderMapper _returnOrderMapper;

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
                await _returnOrderDatabaseService.CreateReturnOrderTablesIfNotExistsAsync();

                // Déterminer le mode d'exécution
                bool isTestMode = IsTestMode(args);
                string exportType = GetExportType(args);

                Console.WriteLine($"Mode: {(isTestMode ? "TEST" : "PRODUCTION")}");
                Console.WriteLine($"Export: {exportType.ToUpper()}");

                if (isTestMode)
                {
                    Console.WriteLine("🧪 MODE TEST ACTIVÉ");
                    _logger.LogInformation("Mode test activé - export sans marquage");

                    if (exportType == "articles" || exportType == "all")
                    {
                        await ExportAllArticlesTestMode();
                    }

                    if (exportType == "purchaseorders" || exportType == "all")
                    {
                        await ExportAllPurchaseOrdersTestMode();
                    }

                    if (exportType == "returnorders" || exportType == "all")
                    {
                        await ExportAllReturnOrdersTestMode();
                    }
                }
                else
                {
                    Console.WriteLine("🔄 MODE PRODUCTION - Nouveaux éléments uniquement");
                    _logger.LogInformation("Mode production - export des nouveaux éléments uniquement");

                    if (exportType == "articles" || exportType == "all")
                    {
                        await ExportNewArticlesOnly();
                    }

                    if (exportType == "purchaseorders" || exportType == "all")
                    {
                        await ExportNewPurchaseOrdersOnly();
                    }

                    if (exportType == "returnorders" || exportType == "all")
                    {
                        await ExportNewReturnOrdersOnly();
                    }
                }

                Console.WriteLine("\n✅ Export terminé avec succès");
                _logger.LogInformation("Export automatique terminé avec succès");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erreur fatale dans le programme principal");
                Console.WriteLine($"\n❌ ERREUR FATALE : {ex.Message}");
                Console.WriteLine("Consultez les logs pour plus de détails.");
                throw;
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
                if (arg == "test" || arg == "--test" || arg == "-t")
                {
                    return true;
                }
            }

            // Vérifier la configuration
            var testModeConfig = _configuration?.GetValue<bool>("Export:TestMode", false) ?? false;
            return testModeConfig;
        }

        /// <summary>
        /// Détermine le type d'export à effectuer
        /// </summary>
        private static string GetExportType(string[] args)
        {
            // Vérifier les arguments de ligne de commande
            if (args.Length > 1)
            {
                var exportType = args[1].ToLower();
                if (exportType == "articles" || exportType == "purchaseorders" || exportType == "po" || exportType == "returnorders" || exportType == "ro")
                {
                    return exportType switch
                    {
                        "po" => "purchaseorders",
                        "ro" => "returnorders",
                        _ => exportType
                    };
                }
            }

            // Si un seul argument et que c'est pas "test", considérer comme type d'export
            if (args.Length == 1 && !IsTestMode(args))
            {
                var exportType = args[0].ToLower();
                if (exportType == "articles" || exportType == "purchaseorders" || exportType == "po" || exportType == "returnorders" || exportType == "ro")
                {
                    return exportType switch
                    {
                        "po" => "purchaseorders",
                        "ro" => "returnorders",
                        _ => exportType
                    };
                }
            }

            // Par défaut, exporter tout
            return "all";
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
                // Récupérer TOUS les articles
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

                // Export en XML SANS marquer les articles comme exportés
                await ExportArticlesInBatches(winDevArticles, null, "ARTICLE_TEST_COMPLET");

                Console.WriteLine($"🧪 MODE TEST : {winDevArticles.Count} articles exportés SANS marquage");
                Console.WriteLine("⚠️ Les articles ne sont PAS marqués comme exportés en mode test");

                stopwatch.Stop();
                Console.WriteLine($"⏱️ Temps articles : {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur lors de l'export des articles en mode test : {ex.Message}");
                _logger.LogError(ex, "Erreur lors de l'export des articles en mode test");
                throw;
            }
        }

        /// <summary>
        /// Mode test : Export de tous les Purchase Orders SANS les marquer comme exportés
        /// </summary>
        private static async Task ExportAllPurchaseOrdersTestMode()
        {
            Console.WriteLine("\n🧪 MODE TEST - Export de tous les Purchase Orders");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Récupérer TOUS les Purchase Orders
                Console.WriteLine("Récupération de TOUS les Purchase Orders depuis la base de données...");
                var purchaseOrders = await _purchaseOrderDatabaseService.GetAllPurchaseOrdersAsync();
                Console.WriteLine($"✓ {purchaseOrders.Count} Purchase Orders trouvés (incluant ceux déjà exportés)");

                if (purchaseOrders.Count == 0)
                {
                    Console.WriteLine("ℹ️ Aucun Purchase Order trouvé dans la base de données");
                    return;
                }

                // Convertir les Purchase Orders
                Console.WriteLine("Conversion des Purchase Orders au format WINDEV...");
                var winDevPurchaseOrders = new List<WinDevPurchaseOrder>();
                int erreurs = 0;

                foreach (var purchaseOrder in purchaseOrders)
                {
                    var winDevPurchaseOrder = _purchaseOrderMapper.MapToWinDev(purchaseOrder);
                    if (winDevPurchaseOrder != null)
                    {
                        winDevPurchaseOrders.Add(winDevPurchaseOrder);
                    }
                    else
                    {
                        erreurs++;
                    }
                }

                Console.WriteLine($"✓ {winDevPurchaseOrders.Count} Purchase Orders convertis avec succès");
                if (erreurs > 0)
                {
                    Console.WriteLine($"⚠️ {erreurs} Purchase Orders n'ont pas pu être convertis");
                }

                // Export en XML SANS marquer les Purchase Orders comme exportés
                await ExportPurchaseOrdersInBatches(winDevPurchaseOrders, null, "PURCHASE_ORDERS_TEST_COMPLET");

                Console.WriteLine($"🧪 MODE TEST : {winDevPurchaseOrders.Count} Purchase Orders exportés SANS marquage");
                Console.WriteLine("⚠️ Les Purchase Orders ne sont PAS marqués comme exportés en mode test");

                stopwatch.Stop();
                Console.WriteLine($"⏱️ Temps Purchase Orders : {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur lors de l'export des Purchase Orders en mode test : {ex.Message}");
                _logger.LogError(ex, "Erreur lors de l'export des Purchase Orders en mode test");
                throw;
            }
        }

        /// <summary>
        /// Mode test : Export de tous les Return Orders SANS les marquer comme exportés
        /// </summary>
        private static async Task ExportAllReturnOrdersTestMode()
        {
            Console.WriteLine("\n🧪 MODE TEST - Export de tous les Return Orders");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Récupérer TOUS les Return Orders
                Console.WriteLine("Récupération de TOUS les Return Orders depuis la base de données...");
                var returnOrders = await _returnOrderDatabaseService.GetAllReturnOrdersAsync();
                Console.WriteLine($"✓ {returnOrders.Count} Return Orders trouvés (incluant ceux déjà exportés)");

                if (returnOrders.Count == 0)
                {
                    Console.WriteLine("ℹ️ Aucun Return Order trouvé dans la base de données");
                    return;
                }

                // Convertir les Return Orders
                Console.WriteLine("Conversion des Return Orders au format WINDEV...");
                var winDevReturnOrders = new List<WinDevReturnOrder>();
                int erreurs = 0;

                foreach (var returnOrder in returnOrders)
                {
                    var winDevReturnOrder = _returnOrderMapper.MapToWinDev(returnOrder);
                    if (winDevReturnOrder != null)
                    {
                        winDevReturnOrders.Add(winDevReturnOrder);
                    }
                    else
                    {
                        erreurs++;
                    }
                }

                Console.WriteLine($"✓ {winDevReturnOrders.Count} Return Orders convertis avec succès");
                if (erreurs > 0)
                {
                    Console.WriteLine($"⚠️ {erreurs} Return Orders n'ont pas pu être convertis");
                }

                // Export en XML SANS marquer les Return Orders comme exportés
                await ExportReturnOrdersInBatches(winDevReturnOrders, null, "RETURN_ORDERS_TEST_COMPLET");

                Console.WriteLine($"🧪 MODE TEST : {winDevReturnOrders.Count} Return Orders exportés SANS marquage");
                Console.WriteLine("⚠️ Les Return Orders ne sont PAS marqués comme exportés en mode test");

                stopwatch.Stop();
                Console.WriteLine($"⏱️ Temps Return Orders : {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur lors de l'export des Return Orders en mode test : {ex.Message}");
                _logger.LogError(ex, "Erreur lors de l'export des Return Orders en mode test");
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
                await ExportArticlesInBatches(winDevArticles, originalIds, "ARTICLE_COSMETIQUE");

                Console.WriteLine($"🎯 {winDevArticles.Count} articles marqués comme exportés");
                stopwatch.Stop();
                Console.WriteLine($"⏱️ Temps articles : {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur lors de l'export des nouveaux articles : {ex.Message}");
                _logger.LogError(ex, "Erreur lors de l'export des nouveaux articles");
                throw;
            }
        }

        /// <summary>
        /// Mode production : Export des nouveaux Purchase Orders uniquement avec marquage
        /// </summary>
        private static async Task ExportNewPurchaseOrdersOnly()
        {
            Console.WriteLine("\n🆕 Export des nouveaux Purchase Orders uniquement");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Récupérer uniquement les Purchase Orders non exportés
                Console.WriteLine("Récupération des nouveaux Purchase Orders depuis la base de données...");
                var purchaseOrders = await _purchaseOrderDatabaseService.GetNonExportedPurchaseOrdersAsync();
                Console.WriteLine($"✓ {purchaseOrders.Count} nouveaux Purchase Orders trouvés");

                if (purchaseOrders.Count == 0)
                {
                    Console.WriteLine("ℹ️ Aucun nouveau Purchase Order à exporter");
                    return;
                }

                // Convertir les Purchase Orders
                Console.WriteLine("Conversion des Purchase Orders au format WINDEV...");
                var winDevPurchaseOrders = new List<WinDevPurchaseOrder>();
                var originalIds = new List<int>();
                int erreurs = 0;

                foreach (var purchaseOrder in purchaseOrders)
                {
                    var winDevPurchaseOrder = _purchaseOrderMapper.MapToWinDev(purchaseOrder);
                    if (winDevPurchaseOrder != null)
                    {
                        winDevPurchaseOrders.Add(winDevPurchaseOrder);
                        originalIds.Add(purchaseOrder.Id);
                    }
                    else
                    {
                        erreurs++;
                    }
                }

                Console.WriteLine($"✓ {winDevPurchaseOrders.Count} Purchase Orders convertis avec succès");
                if (erreurs > 0)
                {
                    Console.WriteLine($"⚠️ {erreurs} Purchase Orders n'ont pas pu être convertis");
                }

                // Exporter en XML avec marquage automatique
                await ExportPurchaseOrdersInBatches(winDevPurchaseOrders, originalIds, "PURCHASE_ORDERS_COSMETIQUE");

                Console.WriteLine($"🎯 {winDevPurchaseOrders.Count} Purchase Orders marqués comme exportés");
                stopwatch.Stop();
                Console.WriteLine($"⏱️ Temps Purchase Orders : {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur lors de l'export des nouveaux Purchase Orders : {ex.Message}");
                _logger.LogError(ex, "Erreur lors de l'export des nouveaux Purchase Orders");
                throw;
            }
        }

        /// <summary>
        /// Mode production : Export des nouveaux Return Orders uniquement avec marquage
        /// </summary>
        private static async Task ExportNewReturnOrdersOnly()
        {
            Console.WriteLine("\n🆕 Export des nouveaux Return Orders uniquement");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Récupérer uniquement les Return Orders non exportés
                Console.WriteLine("Récupération des nouveaux Return Orders depuis la base de données...");
                var returnOrders = await _returnOrderDatabaseService.GetNonExportedReturnOrdersAsync();
                Console.WriteLine($"✓ {returnOrders.Count} nouveaux Return Orders trouvés");

                if (returnOrders.Count == 0)
                {
                    Console.WriteLine("ℹ️ Aucun nouveau Return Order à exporter");
                    return;
                }

                // Convertir les Return Orders
                Console.WriteLine("Conversion des Return Orders au format WINDEV...");
                var winDevReturnOrders = new List<WinDevReturnOrder>();
                var originalIds = new List<int>();
                int erreurs = 0;

                foreach (var returnOrder in returnOrders)
                {
                    var winDevReturnOrder = _returnOrderMapper.MapToWinDev(returnOrder);
                    if (winDevReturnOrder != null)
                    {
                        winDevReturnOrders.Add(winDevReturnOrder);
                        originalIds.Add(returnOrder.Id);
                    }
                    else
                    {
                        erreurs++;
                    }
                }

                Console.WriteLine($"✓ {winDevReturnOrders.Count} Return Orders convertis avec succès");
                if (erreurs > 0)
                {
                    Console.WriteLine($"⚠️ {erreurs} Return Orders n'ont pas pu être convertis");
                }

                // Exporter en XML avec marquage automatique
                await ExportReturnOrdersInBatches(winDevReturnOrders, originalIds, "RETURN_ORDERS_COSMETIQUE");

                Console.WriteLine($"🎯 {winDevReturnOrders.Count} Return Orders marqués comme exportés");
                stopwatch.Stop();
                Console.WriteLine($"⏱️ Temps Return Orders : {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur lors de l'export des nouveaux Return Orders : {ex.Message}");
                _logger.LogError(ex, "Erreur lors de l'export des nouveaux Return Orders");
                throw;
            }
        }

        /// <summary>
        /// Méthode utilitaire pour exporter les articles en gérant les lots
        /// </summary>
        private static async Task ExportArticlesInBatches(List<WinDevArticle> winDevArticles, List<int>? originalIds, string filePrefix)
        {
            Console.WriteLine("Export des articles en fichier(s) XML...");
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
                    $"Export articles ({files.Count} fichiers)",
                    winDevArticles.Count,
                    "SUCCESS",
                    $"{files.Count} fichiers générés"
                );
            }
            else
            {
                // Export en un seul fichier
                var filePath = await _xmlExportService.ExportToXmlAsync(winDevArticles, originalIds, filePrefix);
                if (filePath != null)
                {
                    Console.WriteLine($"✓ Export terminé : {Path.GetFileName(filePath)}");
                    Console.WriteLine($"  📁 Chemin complet : {filePath}");

                    // Enregistrer le log d'export
                    await _databaseService.LogExportAsync(
                        Path.GetFileName(filePath),
                        winDevArticles.Count,
                        "SUCCESS",
                        "Export articles"
                    );
                }
            }
        }

        /// <summary>
        /// Méthode utilitaire pour exporter les Purchase Orders en gérant les lots
        /// </summary>
        private static async Task ExportPurchaseOrdersInBatches(List<WinDevPurchaseOrder> winDevPurchaseOrders, List<int>? originalIds, string filePrefix)
        {
            Console.WriteLine("Export des Purchase Orders en fichier(s) XML...");
            var batchSize = _configuration.GetValue<int>("XmlExport:BatchSize", 1000);

            if (winDevPurchaseOrders.Count > batchSize)
            {
                // Export par lots
                var files = await _purchaseOrderXmlExportService.ExportInBatchesAsync(winDevPurchaseOrders, originalIds, batchSize);
                Console.WriteLine($"✓ Export terminé : {files.Count} fichiers créés");

                foreach (var file in files)
                {
                    Console.WriteLine($"  📁 {Path.GetFileName(file)}");
                }

                // Enregistrer le log d'export
                await _purchaseOrderDatabaseService.LogPurchaseOrderExportAsync(
                    $"Export Purchase Orders ({files.Count} fichiers)",
                    winDevPurchaseOrders.Count,
                    "SUCCESS",
                    $"{files.Count} fichiers générés"
                );
            }
            else
            {
                // Export en un seul fichier
                var filePath = await _purchaseOrderXmlExportService.ExportToXmlAsync(winDevPurchaseOrders, originalIds, filePrefix);
                if (filePath != null)
                {
                    Console.WriteLine($"✓ Export terminé : {Path.GetFileName(filePath)}");
                    Console.WriteLine($"  📁 Chemin complet : {filePath}");

                    // Enregistrer le log d'export
                    await _purchaseOrderDatabaseService.LogPurchaseOrderExportAsync(
                        Path.GetFileName(filePath),
                        winDevPurchaseOrders.Count,
                        "SUCCESS",
                        "Export Purchase Orders"
                    );
                }
            }
        }

        /// <summary>
        /// Méthode utilitaire pour exporter les Return Orders en gérant les lots
        /// </summary>
        private static async Task ExportReturnOrdersInBatches(List<WinDevReturnOrder> winDevReturnOrders, List<int>? originalIds, string filePrefix)
        {
            Console.WriteLine("Export des Return Orders en fichier(s) XML...");
            var batchSize = _configuration.GetValue<int>("XmlExport:BatchSize", 1000);

            if (winDevReturnOrders.Count > batchSize)
            {
                // Export par lots
                var files = await _returnOrderXmlExportService.ExportInBatchesAsync(winDevReturnOrders, originalIds, batchSize);
                Console.WriteLine($"✓ Export terminé : {files.Count} fichiers créés");

                foreach (var file in files)
                {
                    Console.WriteLine($"  📁 {Path.GetFileName(file)}");
                }

                // Enregistrer le log d'export
                await _returnOrderDatabaseService.LogReturnOrderExportAsync(
                    $"Export Return Orders ({files.Count} fichiers)",
                    winDevReturnOrders.Count,
                    "SUCCESS",
                    $"{files.Count} fichiers générés"
                );
            }
            else
            {
                // Export en un seul fichier
                var filePath = await _returnOrderXmlExportService.ExportToXmlAsync(winDevReturnOrders, originalIds, filePrefix);
                if (filePath != null)
                {
                    Console.WriteLine($"✓ Export terminé : {Path.GetFileName(filePath)}");
                    Console.WriteLine($"  📁 Chemin complet : {filePath}");

                    // Enregistrer le log d'export
                    await _returnOrderDatabaseService.LogReturnOrderExportAsync(
                        Path.GetFileName(filePath),
                        winDevReturnOrders.Count,
                        "SUCCESS",
                        "Export Return Orders"
                    );
                }
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

            // Configuration Serilog
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

            // Services Articles
            _databaseService = new DatabaseService(_configuration, loggerFactory.CreateLogger<DatabaseService>());
            _xmlExportService = new XmlExportService(_configuration, loggerFactory.CreateLogger<XmlExportService>(), _databaseService);
            _articleMapper = new ArticleMapper(_configuration, loggerFactory.CreateLogger<ArticleMapper>());

            // Services Purchase Orders
            _purchaseOrderDatabaseService = new PurchaseOrderDatabaseService(_configuration, loggerFactory.CreateLogger<PurchaseOrderDatabaseService>());
            _purchaseOrderXmlExportService = new PurchaseOrderXmlExportService(_configuration, loggerFactory.CreateLogger<PurchaseOrderXmlExportService>(), _purchaseOrderDatabaseService);
            _purchaseOrderMapper = new PurchaseOrderMapper(_configuration, loggerFactory.CreateLogger<PurchaseOrderMapper>());

            // Services Return Orders
            _returnOrderDatabaseService = new ReturnOrderDatabaseService(_configuration, loggerFactory.CreateLogger<ReturnOrderDatabaseService>());
            _returnOrderXmlExportService = new ReturnOrderXmlExportService(_configuration, loggerFactory.CreateLogger<ReturnOrderXmlExportService>(), _returnOrderDatabaseService);
            _returnOrderMapper = new ReturnOrderMapper(_configuration, loggerFactory.CreateLogger<ReturnOrderMapper>());
        }
    }
}