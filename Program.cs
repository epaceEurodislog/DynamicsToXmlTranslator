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

        // ✅ NOUVEAU : Service de traitement UTF-8
        private static Utf8TextProcessor _textProcessor;

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

        // Services Transfer Orders
        private static TransferOrderDatabaseService _transferOrderDatabaseService;
        private static TransferOrderXmlExportService _transferOrderXmlExportService;
        private static TransferOrderMapper _transferOrderMapper;

        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Traducteur Dynamics vers XML WINDEV (avec UTF-8) ===");
            Console.WriteLine($"Démarrage automatique : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            try
            {
                // Configuration et initialisation
                SetupConfiguration();
                SetupLogging();
                SetupServices();

                // ✅ NOUVEAU : Log des informations UTF-8
                _logger.LogInformation("✅ Service de traitement UTF-8 initialisé");
                _logger.LogInformation("Tous les champs texte seront normalisés pour la compatibilité XML");

                // Vérifier/créer les tables nécessaires
                await _databaseService.CreateTablesIfNotExistsAsync();
                await _returnOrderDatabaseService.CreateReturnOrderTablesIfNotExistsAsync();
                await _transferOrderDatabaseService.CreateTransferOrderTablesIfNotExistsAsync();

                // Déterminer le mode d'exécution
                bool isTestMode = IsTestMode(args);
                string exportType = GetExportType(args);

                Console.WriteLine($"Mode: {(isTestMode ? "TEST" : "PRODUCTION")}");
                Console.WriteLine($"Export: {exportType.ToUpper()}");
                Console.WriteLine("✅ Traitement UTF-8 activé pour tous les champs texte");

                if (isTestMode)
                {
                    Console.WriteLine("🧪 MODE TEST ACTIVÉ");
                    _logger.LogInformation("Mode test activé - export sans marquage avec traitement UTF-8");

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

                    if (exportType == "transferorders" || exportType == "to" || exportType == "all")
                    {
                        await ExportAllTransferOrdersTestMode();
                    }
                }
                else
                {
                    Console.WriteLine("🔄 MODE PRODUCTION - Nouveaux éléments uniquement");
                    _logger.LogInformation("Mode production - export des nouveaux éléments uniquement avec traitement UTF-8");

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

                    if (exportType == "transferorders" || exportType == "to" || exportType == "all")
                    {
                        await ExportNewTransferOrdersOnly();
                    }
                }

                Console.WriteLine("\n✅ Export terminé avec succès (avec traitement UTF-8)");
                _logger.LogInformation("Export automatique terminé avec succès - tous les caractères spéciaux traités");
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
                if (exportType == "articles" || exportType == "purchaseorders" || exportType == "po" ||
                    exportType == "returnorders" || exportType == "ro" || exportType == "transferorders" || exportType == "to")
                {
                    return exportType switch
                    {
                        "po" => "purchaseorders",
                        "ro" => "returnorders",
                        "to" => "transferorders",
                        _ => exportType
                    };
                }
            }

            // Si un seul argument et que c'est pas "test", considérer comme type d'export
            if (args.Length == 1 && !IsTestMode(args))
            {
                var exportType = args[0].ToLower();
                if (exportType == "articles" || exportType == "purchaseorders" || exportType == "po" ||
                    exportType == "returnorders" || exportType == "ro" || exportType == "transferorders" || exportType == "to")
                {
                    return exportType switch
                    {
                        "po" => "purchaseorders",
                        "ro" => "returnorders",
                        "to" => "transferorders",
                        _ => exportType
                    };
                }
            }

            // Par défaut, exporter tout
            return "all";
        }

        /// <summary>
        /// Mode test : Export de tous les articles SANS les marquer comme exportés
        /// AVEC traitement UTF-8
        /// </summary>
        private static async Task ExportAllArticlesTestMode()
        {
            Console.WriteLine("\n🧪 MODE TEST - Export de tous les articles (avec UTF-8)");
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

                // Convertir les articles avec traitement UTF-8
                Console.WriteLine("Conversion des articles au format WINDEV (avec traitement UTF-8)...");
                var winDevArticles = new List<WinDevArticle>();
                int erreurs = 0;
                int utf8Transformations = 0;

                foreach (var article in articles)
                {
                    var winDevArticle = _articleMapper.MapToWinDev(article);
                    if (winDevArticle != null)
                    {
                        winDevArticles.Add(winDevArticle);

                        // Compter les transformations UTF-8
                        if (HasUtf8Transformations(article.DynamicsData))
                        {
                            utf8Transformations++;
                        }
                    }
                    else
                    {
                        erreurs++;
                    }
                }

                Console.WriteLine($"✓ {winDevArticles.Count} articles convertis avec succès");
                Console.WriteLine($"✅ {utf8Transformations} articles avec transformations UTF-8 appliquées");
                if (erreurs > 0)
                {
                    Console.WriteLine($"⚠️ {erreurs} articles n'ont pas pu être convertis");
                }

                // Export en XML SANS marquer les articles comme exportés
                await ExportArticlesInBatches(winDevArticles, null, "ARTICLE_TEST_COMPLET_UTF8");

                Console.WriteLine($"🧪 MODE TEST : {winDevArticles.Count} articles exportés SANS marquage (UTF-8 traité)");
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
        /// Mode production : Export des nouveaux articles uniquement avec marquage
        /// AVEC traitement UTF-8
        /// </summary>
        private static async Task ExportNewArticlesOnly()
        {
            Console.WriteLine("\n🆕 Export des nouveaux articles uniquement (avec UTF-8)");
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

                // Convertir les articles avec traitement UTF-8
                Console.WriteLine("Conversion des articles au format WINDEV (avec traitement UTF-8)...");
                var winDevArticles = new List<WinDevArticle>();
                var originalIds = new List<int>();
                int erreurs = 0;
                int utf8Transformations = 0;

                foreach (var article in articles)
                {
                    var winDevArticle = _articleMapper.MapToWinDev(article);
                    if (winDevArticle != null)
                    {
                        winDevArticles.Add(winDevArticle);
                        originalIds.Add(article.Id);

                        // Compter les transformations UTF-8
                        if (HasUtf8Transformations(article.DynamicsData))
                        {
                            utf8Transformations++;
                        }
                    }
                    else
                    {
                        erreurs++;
                    }
                }

                Console.WriteLine($"✓ {winDevArticles.Count} articles convertis avec succès");
                Console.WriteLine($"✅ {utf8Transformations} articles avec transformations UTF-8 appliquées");
                if (erreurs > 0)
                {
                    Console.WriteLine($"⚠️ {erreurs} articles n'ont pas pu être convertis");
                }

                // Exporter en XML avec marquage automatique
                await ExportArticlesInBatches(winDevArticles, originalIds, "ARTICLE_COSMETIQUE_UTF8");

                Console.WriteLine($"🎯 {winDevArticles.Count} articles marqués comme exportés (UTF-8 traité)");
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

        // ========== MÉTHODES POUR PURCHASE ORDERS ==========

        /// <summary>
        /// Mode test : Export de tous les Purchase Orders AVEC UTF-8
        /// </summary>
        private static async Task ExportAllPurchaseOrdersTestMode()
        {
            Console.WriteLine("\n🧪 MODE TEST - Export de tous les Purchase Orders (avec UTF-8)");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var purchaseOrders = await _purchaseOrderDatabaseService.GetAllPurchaseOrdersAsync();
                Console.WriteLine($"✓ {purchaseOrders.Count} Purchase Orders trouvés");

                if (purchaseOrders.Count == 0)
                {
                    Console.WriteLine("ℹ️ Aucun Purchase Order trouvé");
                    return;
                }

                var winDevPurchaseOrders = new List<WinDevPurchaseOrder>();
                int erreurs = 0;
                int utf8Transformations = 0;

                foreach (var purchaseOrder in purchaseOrders)
                {
                    var winDevPurchaseOrder = _purchaseOrderMapper.MapToWinDev(purchaseOrder);
                    if (winDevPurchaseOrder != null)
                    {
                        winDevPurchaseOrders.Add(winDevPurchaseOrder);
                        if (HasUtf8TransformationsPO(purchaseOrder.DynamicsData))
                        {
                            utf8Transformations++;
                        }
                    }
                    else
                    {
                        erreurs++;
                    }
                }

                Console.WriteLine($"✓ {winDevPurchaseOrders.Count} Purchase Orders convertis");
                Console.WriteLine($"✅ {utf8Transformations} Purchase Orders avec transformations UTF-8");

                await ExportPurchaseOrdersInBatches(winDevPurchaseOrders, null, "PURCHASE_ORDERS_TEST_UTF8");

                stopwatch.Stop();
                Console.WriteLine($"⏱️ Temps Purchase Orders : {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur Purchase Orders mode test : {ex.Message}");
                _logger.LogError(ex, "Erreur Purchase Orders mode test");
                throw;
            }
        }

        /// <summary>
        /// Mode production : Export des nouveaux Purchase Orders AVEC UTF-8
        /// </summary>
        private static async Task ExportNewPurchaseOrdersOnly()
        {
            Console.WriteLine("\n🆕 Export des nouveaux Purchase Orders (avec UTF-8)");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var purchaseOrders = await _purchaseOrderDatabaseService.GetNonExportedPurchaseOrdersAsync();
                Console.WriteLine($"✓ {purchaseOrders.Count} nouveaux Purchase Orders trouvés");

                if (purchaseOrders.Count == 0)
                {
                    Console.WriteLine("ℹ️ Aucun nouveau Purchase Order à exporter");
                    return;
                }

                var winDevPurchaseOrders = new List<WinDevPurchaseOrder>();
                var originalIds = new List<int>();
                int erreurs = 0;
                int utf8Transformations = 0;

                foreach (var purchaseOrder in purchaseOrders)
                {
                    var winDevPurchaseOrder = _purchaseOrderMapper.MapToWinDev(purchaseOrder);
                    if (winDevPurchaseOrder != null)
                    {
                        winDevPurchaseOrders.Add(winDevPurchaseOrder);
                        originalIds.Add(purchaseOrder.Id);
                        if (HasUtf8TransformationsPO(purchaseOrder.DynamicsData))
                        {
                            utf8Transformations++;
                        }
                    }
                    else
                    {
                        erreurs++;
                    }
                }

                Console.WriteLine($"✓ {winDevPurchaseOrders.Count} Purchase Orders convertis");
                Console.WriteLine($"✅ {utf8Transformations} Purchase Orders avec transformations UTF-8");

                await ExportPurchaseOrdersInBatches(winDevPurchaseOrders, originalIds, "PURCHASE_ORDERS_UTF8");

                stopwatch.Stop();
                Console.WriteLine($"⏱️ Temps Purchase Orders : {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur nouveaux Purchase Orders : {ex.Message}");
                _logger.LogError(ex, "Erreur nouveaux Purchase Orders");
                throw;
            }
        }

        // ========== MÉTHODES POUR RETURN ORDERS ==========

        /// <summary>
        /// Mode test : Export de tous les Return Orders AVEC UTF-8
        /// </summary>
        private static async Task ExportAllReturnOrdersTestMode()
        {
            Console.WriteLine("\n🧪 MODE TEST - Export de tous les Return Orders (avec UTF-8)");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var returnOrders = await _returnOrderDatabaseService.GetAllReturnOrdersAsync();
                Console.WriteLine($"✓ {returnOrders.Count} Return Orders trouvés");

                if (returnOrders.Count == 0)
                {
                    Console.WriteLine("ℹ️ Aucun Return Order trouvé");
                    return;
                }

                var winDevReturnOrders = new List<WinDevReturnOrder>();
                int erreurs = 0;
                int utf8Transformations = 0;

                foreach (var returnOrder in returnOrders)
                {
                    var winDevReturnOrder = _returnOrderMapper.MapToWinDev(returnOrder);
                    if (winDevReturnOrder != null)
                    {
                        winDevReturnOrders.Add(winDevReturnOrder);
                        if (HasUtf8TransformationsRO(returnOrder.DynamicsData))
                        {
                            utf8Transformations++;
                        }
                    }
                    else
                    {
                        erreurs++;
                    }
                }

                Console.WriteLine($"✓ {winDevReturnOrders.Count} Return Orders convertis");
                Console.WriteLine($"✅ {utf8Transformations} Return Orders avec transformations UTF-8");

                await ExportReturnOrdersInBatches(winDevReturnOrders, null, "RETURN_ORDERS_TEST_UTF8");

                stopwatch.Stop();
                Console.WriteLine($"⏱️ Temps Return Orders : {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur Return Orders mode test : {ex.Message}");
                _logger.LogError(ex, "Erreur Return Orders mode test");
                throw;
            }
        }

        /// <summary>
        /// Mode production : Export des nouveaux Return Orders AVEC UTF-8
        /// </summary>
        private static async Task ExportNewReturnOrdersOnly()
        {
            Console.WriteLine("\n🆕 Export des nouveaux Return Orders (avec UTF-8)");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var returnOrders = await _returnOrderDatabaseService.GetNonExportedReturnOrdersAsync();
                Console.WriteLine($"✓ {returnOrders.Count} nouveaux Return Orders trouvés");

                if (returnOrders.Count == 0)
                {
                    Console.WriteLine("ℹ️ Aucun nouveau Return Order à exporter");
                    return;
                }

                var winDevReturnOrders = new List<WinDevReturnOrder>();
                var originalIds = new List<int>();
                int erreurs = 0;
                int utf8Transformations = 0;

                foreach (var returnOrder in returnOrders)
                {
                    var winDevReturnOrder = _returnOrderMapper.MapToWinDev(returnOrder);
                    if (winDevReturnOrder != null)
                    {
                        winDevReturnOrders.Add(winDevReturnOrder);
                        originalIds.Add(returnOrder.Id);
                        if (HasUtf8TransformationsRO(returnOrder.DynamicsData))
                        {
                            utf8Transformations++;
                        }
                    }
                    else
                    {
                        erreurs++;
                    }
                }

                Console.WriteLine($"✓ {winDevReturnOrders.Count} Return Orders convertis");
                Console.WriteLine($"✅ {utf8Transformations} Return Orders avec transformations UTF-8");

                await ExportReturnOrdersInBatches(winDevReturnOrders, originalIds, "RETURN_ORDERS_UTF8");

                stopwatch.Stop();
                Console.WriteLine($"⏱️ Temps Return Orders : {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur nouveaux Return Orders : {ex.Message}");
                _logger.LogError(ex, "Erreur nouveaux Return Orders");
                throw;
            }
        }

        // ========== MÉTHODES POUR TRANSFER ORDERS ==========

        /// <summary>
        /// Mode test : Export de tous les Transfer Orders AVEC UTF-8
        /// </summary>
        private static async Task ExportAllTransferOrdersTestMode()
        {
            Console.WriteLine("\n🧪 MODE TEST - Export de tous les Transfer Orders (avec UTF-8)");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var transferOrders = await _transferOrderDatabaseService.GetAllTransferOrdersAsync();
                Console.WriteLine($"✓ {transferOrders.Count} Transfer Orders trouvés");

                if (transferOrders.Count == 0)
                {
                    Console.WriteLine("ℹ️ Aucun Transfer Order trouvé");
                    return;
                }

                var winDevTransferOrders = new List<WinDevTransferOrder>();
                int erreurs = 0;
                int utf8Transformations = 0;

                foreach (var transferOrder in transferOrders)
                {
                    var winDevTransferOrder = _transferOrderMapper.MapToWinDev(transferOrder);
                    if (winDevTransferOrder != null)
                    {
                        winDevTransferOrders.Add(winDevTransferOrder);
                        if (HasUtf8TransformationsTO(transferOrder.DynamicsData))
                        {
                            utf8Transformations++;
                        }
                    }
                    else
                    {
                        erreurs++;
                    }
                }

                Console.WriteLine($"✓ {winDevTransferOrders.Count} Transfer Orders convertis");
                Console.WriteLine($"✅ {utf8Transformations} Transfer Orders avec transformations UTF-8");

                await ExportTransferOrdersInBatches(winDevTransferOrders, null, "TRANSFER_ORDERS_TEST_UTF8");

                stopwatch.Stop();
                Console.WriteLine($"⏱️ Temps Transfer Orders : {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur Transfer Orders mode test : {ex.Message}");
                _logger.LogError(ex, "Erreur Transfer Orders mode test");
                throw;
            }
        }

        /// <summary>
        /// Mode production : Export des nouveaux Transfer Orders AVEC UTF-8
        /// </summary>
        private static async Task ExportNewTransferOrdersOnly()
        {
            Console.WriteLine("\n🆕 Export des nouveaux Transfer Orders (avec UTF-8)");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var transferOrders = await _transferOrderDatabaseService.GetNonExportedTransferOrdersAsync();
                Console.WriteLine($"✓ {transferOrders.Count} nouveaux Transfer Orders trouvés");

                if (transferOrders.Count == 0)
                {
                    Console.WriteLine("ℹ️ Aucun nouveau Transfer Order à exporter");
                    return;
                }

                var winDevTransferOrders = new List<WinDevTransferOrder>();
                var originalIds = new List<int>();
                int erreurs = 0;
                int utf8Transformations = 0;

                foreach (var transferOrder in transferOrders)
                {
                    var winDevTransferOrder = _transferOrderMapper.MapToWinDev(transferOrder);
                    if (winDevTransferOrder != null)
                    {
                        winDevTransferOrders.Add(winDevTransferOrder);
                        originalIds.Add(transferOrder.Id);
                        if (HasUtf8TransformationsTO(transferOrder.DynamicsData))
                        {
                            utf8Transformations++;
                        }
                    }
                    else
                    {
                        erreurs++;
                    }
                }

                Console.WriteLine($"✓ {winDevTransferOrders.Count} Transfer Orders convertis");
                Console.WriteLine($"✅ {utf8Transformations} Transfer Orders avec transformations UTF-8");

                await ExportTransferOrdersInBatches(winDevTransferOrders, originalIds, "TRANSFER_ORDERS_UTF8");

                stopwatch.Stop();
                Console.WriteLine($"⏱️ Temps Transfer Orders : {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur nouveaux Transfer Orders : {ex.Message}");
                _logger.LogError(ex, "Erreur nouveaux Transfer Orders");
                throw;
            }
        }

        // ========== MÉTHODES UTILITAIRES UTF-8 ==========

        /// <summary>
        /// Vérifie si un article a des transformations UTF-8
        /// </summary>
        private static bool HasUtf8Transformations(DynamicsArticle? dynamics)
        {
            if (dynamics == null) return false;

            // Vérifier les champs texte principaux
            var originalName = dynamics.Name ?? "";
            var originalItemId = dynamics.ItemId ?? "";
            var originalCategory = dynamics.Category ?? "";

            var processedName = _textProcessor.ProcessName(originalName);
            var processedItemId = _textProcessor.ProcessCode(originalItemId);
            var processedCategory = _textProcessor.ProcessText(originalCategory);

            return originalName != processedName ||
                   originalItemId != processedItemId ||
                   originalCategory != processedCategory;
        }

        /// <summary>
        /// Vérifie si un Purchase Order a des transformations UTF-8
        /// </summary>
        private static bool HasUtf8TransformationsPO(DynamicsPurchaseOrder? dynamics)
        {
            if (dynamics == null) return false;

            var originalPurchName = dynamics.PurchName ?? "";
            var originalPurchId = dynamics.PurchId ?? "";
            var originalNotes = dynamics.Notes ?? "";

            var processedPurchName = _textProcessor.ProcessName(originalPurchName);
            var processedPurchId = _textProcessor.ProcessCode(originalPurchId);
            var processedNotes = _textProcessor.ProcessName(originalNotes);

            return originalPurchName != processedPurchName ||
                   originalPurchId != processedPurchId ||
                   originalNotes != processedNotes;
        }

        /// <summary>
        /// Vérifie si un Return Order a des transformations UTF-8
        /// </summary>
        private static bool HasUtf8TransformationsRO(DynamicsReturnOrder? dynamics)
        {
            if (dynamics == null) return false;

            var originalSalesName = dynamics.SalesName ?? "";
            var originalSalesId = dynamics.SalesId ?? "";
            var originalNotes = dynamics.Notes ?? "";

            var processedSalesName = _textProcessor.ProcessName(originalSalesName);
            var processedSalesId = _textProcessor.ProcessCode(originalSalesId);
            var processedNotes = _textProcessor.ProcessName(originalNotes);

            return originalSalesName != processedSalesName ||
                   originalSalesId != processedSalesId ||
                   originalNotes != processedNotes;
        }

        /// <summary>
        /// Vérifie si un Transfer Order a des transformations UTF-8
        /// </summary>
        private static bool HasUtf8TransformationsTO(DynamicsTransferOrder? dynamics)
        {
            if (dynamics == null) return false;

            var originalTransferId = dynamics.TransferId ?? "";
            var originalNotes = dynamics.Notes ?? "";
            var originalInventTransId = dynamics.InventTransId ?? "";

            var processedTransferId = _textProcessor.ProcessCode(originalTransferId);
            var processedNotes = _textProcessor.ProcessName(originalNotes);
            var processedInventTransId = _textProcessor.ProcessCode(originalInventTransId);

            return originalTransferId != processedTransferId ||
                   originalNotes != processedNotes ||
                   originalInventTransId != processedInventTransId;
        }

        // ========== MÉTHODES D'EXPORT EN LOTS ==========

        /// <summary>
        /// Méthode utilitaire pour exporter les articles en gérant les lots
        /// </summary>
        private static async Task ExportArticlesInBatches(List<WinDevArticle> winDevArticles, List<int>? originalIds, string filePrefix)
        {
            Console.WriteLine("Export des articles en fichier(s) XML...");
            var batchSize = _configuration.GetValue<int>("XmlExport:BatchSize", 1000);

            if (winDevArticles.Count > batchSize)
            {
                var files = await _xmlExportService.ExportInBatchesAsync(winDevArticles, originalIds, batchSize);
                Console.WriteLine($"✓ Export terminé : {files.Count} fichiers créés");

                foreach (var file in files)
                {
                    Console.WriteLine($"  📁 {Path.GetFileName(file)}");
                }

                await _databaseService.LogExportAsync(
                    $"Export articles ({files.Count} fichiers)",
                    winDevArticles.Count,
                    "SUCCESS",
                    $"{files.Count} fichiers générés avec UTF-8"
                );
            }
            else
            {
                var filePath = await _xmlExportService.ExportToXmlAsync(winDevArticles, originalIds, filePrefix);
                if (filePath != null)
                {
                    Console.WriteLine($"✓ Export terminé : {Path.GetFileName(filePath)}");
                    Console.WriteLine($"  📁 Chemin complet : {filePath}");

                    await _databaseService.LogExportAsync(
                        Path.GetFileName(filePath),
                        winDevArticles.Count,
                        "SUCCESS",
                        "Export articles avec UTF-8"
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
                var files = await _purchaseOrderXmlExportService.ExportInBatchesAsync(winDevPurchaseOrders, originalIds, batchSize);
                Console.WriteLine($"✓ Export terminé : {files.Count} fichiers créés");

                foreach (var file in files)
                {
                    Console.WriteLine($"  📁 {Path.GetFileName(file)}");
                }

                await _purchaseOrderDatabaseService.LogPurchaseOrderExportAsync(
                    $"Export Purchase Orders ({files.Count} fichiers)",
                    winDevPurchaseOrders.Count,
                    "SUCCESS",
                    $"{files.Count} fichiers générés avec UTF-8"
                );
            }
            else
            {
                var filePath = await _purchaseOrderXmlExportService.ExportToXmlAsync(winDevPurchaseOrders, originalIds, filePrefix);
                if (filePath != null)
                {
                    Console.WriteLine($"✓ Export terminé : {Path.GetFileName(filePath)}");
                    Console.WriteLine($"  📁 Chemin complet : {filePath}");

                    await _purchaseOrderDatabaseService.LogPurchaseOrderExportAsync(
                        Path.GetFileName(filePath),
                        winDevPurchaseOrders.Count,
                        "SUCCESS",
                        "Export Purchase Orders avec UTF-8"
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
                var files = await _returnOrderXmlExportService.ExportInBatchesAsync(winDevReturnOrders, originalIds, batchSize);
                Console.WriteLine($"✓ Export terminé : {files.Count} fichiers créés");

                foreach (var file in files)
                {
                    Console.WriteLine($"  📁 {Path.GetFileName(file)}");
                }

                await _returnOrderDatabaseService.LogReturnOrderExportAsync(
                    $"Export Return Orders ({files.Count} fichiers)",
                    winDevReturnOrders.Count,
                    "SUCCESS",
                    $"{files.Count} fichiers générés avec UTF-8"
                );
            }
            else
            {
                var filePath = await _returnOrderXmlExportService.ExportToXmlAsync(winDevReturnOrders, originalIds, filePrefix);
                if (filePath != null)
                {
                    Console.WriteLine($"✓ Export terminé : {Path.GetFileName(filePath)}");
                    Console.WriteLine($"  📁 Chemin complet : {filePath}");

                    await _returnOrderDatabaseService.LogReturnOrderExportAsync(
                        Path.GetFileName(filePath),
                        winDevReturnOrders.Count,
                        "SUCCESS",
                        "Export Return Orders avec UTF-8"
                    );
                }
            }
        }

        /// <summary>
        /// Méthode utilitaire pour exporter les Transfer Orders en gérant les lots
        /// </summary>
        private static async Task ExportTransferOrdersInBatches(List<WinDevTransferOrder> winDevTransferOrders, List<int>? originalIds, string filePrefix)
        {
            Console.WriteLine("Export des Transfer Orders en fichier(s) XML...");
            var batchSize = _configuration.GetValue<int>("XmlExport:BatchSize", 1000);

            if (winDevTransferOrders.Count > batchSize)
            {
                var files = await _transferOrderXmlExportService.ExportInBatchesAsync(winDevTransferOrders, originalIds, batchSize);
                Console.WriteLine($"✓ Export terminé : {files.Count} fichiers créés");

                foreach (var file in files)
                {
                    Console.WriteLine($"  📁 {Path.GetFileName(file)}");
                }

                await _transferOrderDatabaseService.LogTransferOrderExportAsync(
                    $"Export Transfer Orders ({files.Count} fichiers)",
                    winDevTransferOrders.Count,
                    "SUCCESS",
                    $"{files.Count} fichiers générés avec UTF-8"
                );
            }
            else
            {
                var filePath = await _transferOrderXmlExportService.ExportToXmlAsync(winDevTransferOrders, originalIds, filePrefix);
                if (filePath != null)
                {
                    Console.WriteLine($"✓ Export terminé : {Path.GetFileName(filePath)}");
                    Console.WriteLine($"  📁 Chemin complet : {filePath}");

                    await _transferOrderDatabaseService.LogTransferOrderExportAsync(
                        Path.GetFileName(filePath),
                        winDevTransferOrders.Count,
                        "SUCCESS",
                        "Export Transfer Orders avec UTF-8"
                    );
                }
            }
        }

        // ========== CONFIGURATION ET SERVICES ==========

        private static void SetupConfiguration()
        {
            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
        }

        private static void SetupLogging()
        {
            var logDirectory = "logs";
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    path: Path.Combine(logDirectory, "translator.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    fileSizeLimitBytes: 10 * 1024 * 1024,
                    rollOnFileSizeLimit: true,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddSerilog(dispose: true);
            });

            _logger = loggerFactory.CreateLogger<Program>();

            _logger.LogInformation("=== Traducteur Dynamics vers XML WINDEV avec UTF-8 - Démarrage ===");
            _logger.LogInformation("Version .NET: {DotNetVersion}", Environment.Version);
        }

        private static void SetupServices()
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddSerilog(dispose: true);
            });

            // ✅ NOUVEAU : Service de traitement UTF-8 (PREMIER à initialiser)
            _textProcessor = new Utf8TextProcessor(loggerFactory.CreateLogger<Utf8TextProcessor>());

            // ✅ NOUVEAU : Log des exemples de transformation au démarrage
            _textProcessor.LogTransformationExamples(_logger);

            // Services Articles (MODIFIÉS pour inclure UTF-8)
            _databaseService = new DatabaseService(_configuration, loggerFactory.CreateLogger<DatabaseService>());
            _xmlExportService = new XmlExportService(_configuration, loggerFactory.CreateLogger<XmlExportService>(), _databaseService);
            _articleMapper = new ArticleMapper(_configuration, loggerFactory.CreateLogger<ArticleMapper>(), _textProcessor);

            // Services Purchase Orders (MODIFIÉS pour inclure UTF-8)
            _purchaseOrderDatabaseService = new PurchaseOrderDatabaseService(_configuration, loggerFactory.CreateLogger<PurchaseOrderDatabaseService>());
            _purchaseOrderXmlExportService = new PurchaseOrderXmlExportService(_configuration, loggerFactory.CreateLogger<PurchaseOrderXmlExportService>(), _purchaseOrderDatabaseService);
            _purchaseOrderMapper = new PurchaseOrderMapper(_configuration, loggerFactory.CreateLogger<PurchaseOrderMapper>(), _textProcessor);

            // Services Return Orders (MODIFIÉS pour inclure UTF-8)
            _returnOrderDatabaseService = new ReturnOrderDatabaseService(_configuration, loggerFactory.CreateLogger<ReturnOrderDatabaseService>());
            _returnOrderXmlExportService = new ReturnOrderXmlExportService(_configuration, loggerFactory.CreateLogger<ReturnOrderXmlExportService>(), _returnOrderDatabaseService);
            _returnOrderMapper = new ReturnOrderMapper(_configuration, loggerFactory.CreateLogger<ReturnOrderMapper>(), _textProcessor);

            // Services Transfer Orders (MODIFIÉS pour inclure UTF-8)
            _transferOrderDatabaseService = new TransferOrderDatabaseService(_configuration, loggerFactory.CreateLogger<TransferOrderDatabaseService>());
            _transferOrderXmlExportService = new TransferOrderXmlExportService(_configuration, loggerFactory.CreateLogger<TransferOrderXmlExportService>(), _transferOrderDatabaseService);
            _transferOrderMapper = new TransferOrderMapper(_configuration, loggerFactory.CreateLogger<TransferOrderMapper>(), _textProcessor);
        }
    }
}