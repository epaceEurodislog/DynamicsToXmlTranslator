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
using static DynamicsToXmlTranslator.Models.SpeedPackingSlipHeader;

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

        // Services Packing Slips
        private static PackingSlipDatabaseService _packingSlipDatabaseService;
        private static PackingSlipTxtExportService _packingSlipTxtExportService;
        private static PackingSlipMapper _packingSlipMapper;

        //AJOUT RD 31/07/2025
        public static string Truncate(string value, int maxLength)
<<<<<<< HEAD
=======
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
        //FIN AJOUT

        static async Task Main(string[] args)
>>>>>>> 64d8ba17659add20872df6f622ca79ff25e05502
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
        //FIN AJOUT

        public static async Task Main(string[] args)
        {
            Console.WriteLine("🔄 === DYNAMICS TO XML TRANSLATOR === 🔄");

            try
            {
                // Configuration et initialisation (OBLIGATOIRE)
                SetupConfiguration();
                SetupLogging();
                SetupServices();

                // ✅ OBLIGATOIRE : Un argument doit être fourni
                if (args.Length == 0)
                {
                    Console.WriteLine("❌ ERREUR: Un type de traitement doit être spécifié");
                    Console.WriteLine("📖 Types disponibles : articles, purchase, return, transfer, sales");
                    Environment.Exit(1);
                }

                var filterType = args[0].ToLower();

                // ✅ AJOUT: Log de débogage pour voir l'argument reçu
                Console.WriteLine($"🐛 DEBUG: Argument reçu = '{args[0]}'");
                Console.WriteLine($"🐛 DEBUG: filterType après ToLower() = '{filterType}'");

                Console.WriteLine($"🎯 Mode spécialisé : {filterType.ToUpper()} uniquement");
                _logger.LogInformation($"Traitement spécialisé activé : {filterType}");

                // ✅ VALIDATION: Vérifier que le type est valide avant de continuer
                var validTypes = new[] { "articles", "purchase", "return", "transfer", "sales" };
                if (!validTypes.Contains(filterType))
                {
                    Console.WriteLine($"❌ ERREUR: Type '{filterType}' non reconnu");
                    Console.WriteLine($"📖 Types valides : {string.Join(", ", validTypes)}");
                    Environment.Exit(1);
                }

                // ✅ TRAITEMENT SPÉCIALISÉ UNIQUEMENT
                await ProcessSpecificType(filterType);

                Console.WriteLine("✅ === TRAITEMENT TERMINÉ === ✅");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERREUR: {ex.Message}");
                _logger?.LogError(ex, "Erreur fatale dans le programme principal");
                Environment.Exit(1);
            }
            finally
            {
                // Fermer les logs proprement
                Log.CloseAndFlush();
            }

            Console.WriteLine("🚨🚨🚨 VERSION MODIFIEE SEPTEMBRE 2025 - ARG: " + (args.Length > 0 ? args[0] : "AUCUN") + " 🚨🚨🚨");
        }

        /// <summary>
        /// Traite seulement un type spécifique de données - VERSION CORRIGÉE
        /// </summary>
        private static async Task ProcessSpecificType(string filterType)
        {
            Console.WriteLine($"🔍 Traitement des données {filterType.ToUpper()}...");

            try
            {
                await _databaseService.CreateTablesIfNotExistsAsync();

                _logger.LogInformation($"Démarrage traitement filtré : {filterType}");

                // ✅ AJOUT: Log de débogage pour vérifier le type reçu
                Console.WriteLine($"🐛 DEBUG: filterType reçu = '{filterType}'");
                Console.WriteLine($"🐛 DEBUG: filterType.ToLower() = '{filterType.ToLower()}'");

                // Traiter selon le type demandé avec les bonnes méthodes
                switch (filterType.ToLower())
                {
                    case "articles":
                        Console.WriteLine("🧬 Traitement spécialisé des articles...");
                        _logger.LogInformation("Traitement articles sélectionné");
                        // Pas besoin de table spécifique - utilise xml_export_logs du DatabaseService
                        await ExportNewArticlesOnly();
                        break;

                    case "purchase":
                        Console.WriteLine("💰 Traitement spécialisé des purchase orders...");
                        _logger.LogInformation("Traitement purchase orders sélectionné");
                        // ✅ CORRECTION: Vérifier si les services Purchase Order sont initialisés
                        if (_purchaseOrderDatabaseService == null)
                        {
                            throw new InvalidOperationException("Service PurchaseOrderDatabaseService non initialisé");
                        }
                        if (_purchaseOrderXmlExportService == null)
                        {
                            throw new InvalidOperationException("Service PurchaseOrderXmlExportService non initialisé");
                        }

                        // Pas de méthode CreatePurchaseOrderTablesIfNotExistsAsync() - elle n'existe pas
                        await ExportNewPurchaseOrdersOnly();
                        break;

                    case "return":
                        Console.WriteLine("↩️ Traitement spécialisé des return orders...");
                        _logger.LogInformation("Traitement return orders sélectionné");
                        // ✅ GARDER : Cette méthode existe et crée xml_return_export_logs
                        await _returnOrderDatabaseService.CreateReturnOrderTablesIfNotExistsAsync();
                        await ExportNewReturnOrdersOnly();
                        break;

                    case "transfer":
                        Console.WriteLine("🔄 Traitement spécialisé des transfer orders...");
                        _logger.LogInformation("Traitement transfer orders sélectionné");
                        // ✅ GARDER : Cette méthode existe et crée xml_transfer_export_logs
                        await _transferOrderDatabaseService.CreateTransferOrderTablesIfNotExistsAsync();
                        await ExportNewTransferOrdersOnly();
                        break;

                    case "sales":
                        Console.WriteLine("🛒 Traitement spécialisé des packing slips (sales orders)...");
                        _logger.LogInformation("Traitement sales orders (packing slips) sélectionné");
                        // ✅ GARDER : Cette méthode existe et crée txt_packingslip_export_logs
                        await _packingSlipDatabaseService.CreatePackingSlipTablesIfNotExistsAsync();
                        await ExportNewPackingSlipsOnly();
                        break;

                    default:
                        var errorMsg = $"Type non reconnu: {filterType}. Types valides: articles, purchase, return, transfer, sales";
                        Console.WriteLine($"❌ {errorMsg}");
                        _logger.LogError(errorMsg);
                        throw new ArgumentException(errorMsg);
                }

                Console.WriteLine($"✅ Traitement {filterType.ToUpper()} terminé avec succès");
                _logger.LogInformation($"Traitement {filterType} terminé avec succès");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur lors du traitement {filterType}: {ex.Message}");
                _logger.LogError(ex, $"Erreur lors du traitement {filterType}");
                throw;
            }
        }

        /// <summary>
        /// ✅ NOUVELLE MÉTHODE : Affiche des statistiques détaillées sur les articles
        /// </summary>
        private static async Task DisplayArticleStatistics()
        {
            try
            {
                Console.WriteLine("\n📊 Analyse des articles dans la base de données...");
                var statistics = await _databaseService.GetArticleStatisticsAsync();

                Console.WriteLine("=== STATISTIQUES ARTICLES ===");
                Console.WriteLine($"📋 Total articles : {statistics["Total"]}");
                Console.WriteLine($"✅ Articles ART_STAT=2 (exportables) : {statistics["ART_STAT_2"]}");
                Console.WriteLine($"🚫 Articles ART_STAT=3 (exclus) : {statistics["ART_STAT_3"]}");
                Console.WriteLine($"❓ Articles statut inconnu : {statistics["ART_STAT_Unknown"]}");

                if (statistics["Total"] > 0)
                {
                    double excludedPercentage = (double)statistics["ART_STAT_3"] / statistics["Total"] * 100;
                    Console.WriteLine($"📈 Pourcentage d'exclusion : {excludedPercentage:F1}%");
                }

                Console.WriteLine("==============================\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Impossible d'afficher les statistiques : {ex.Message}");
                _logger.LogWarning(ex, "Erreur lors de l'affichage des statistiques");
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
        /// Détermine le type d'export à effectuer - CORRIGÉ avec arguments courts
        /// </summary>
        private static string GetExportType(string[] args)
        {
            // Vérifier les arguments de ligne de commande
            if (args.Length > 1)
            {
                var exportType = args[1].ToLower();
                if (exportType == "articles" ||
                    exportType == "purchase" || exportType == "purchaseorders" || exportType == "po" ||
                    exportType == "return" || exportType == "returnorders" || exportType == "ro" ||
                    exportType == "transfer" || exportType == "transferorders" || exportType == "to" ||
                    exportType == "sales" || exportType == "packingslips" || exportType == "ps")
                {
                    return exportType switch
                    {
                        "purchase" => "purchaseorders",    // ✅ AJOUT
                        "po" => "purchaseorders",
                        "return" => "returnorders",        // ✅ AJOUT
                        "ro" => "returnorders",
                        "transfer" => "transferorders",    // ✅ AJOUT
                        "to" => "transferorders",
                        "sales" => "packingslips",         // ✅ AJOUT (sales -> packingslips)
                        "ps" => "packingslips",
                        _ => exportType
                    };
                }
            }

            // Si un seul argument et que c'est pas "test", considérer comme type d'export
            if (args.Length == 1 && !IsTestMode(args))
            {
                var exportType = args[0].ToLower();
                if (exportType == "articles" ||
                    exportType == "purchase" || exportType == "purchaseorders" || exportType == "po" ||
                    exportType == "return" || exportType == "returnorders" || exportType == "ro" ||
                    exportType == "transfer" || exportType == "transferorders" || exportType == "to" ||
                    exportType == "sales" || exportType == "packingslips" || exportType == "ps")
                {
                    return exportType switch
                    {
                        "purchase" => "purchaseorders",    // ✅ AJOUT
                        "po" => "purchaseorders",
                        "return" => "returnorders",        // ✅ AJOUT
                        "ro" => "returnorders",
                        "transfer" => "transferorders",    // ✅ AJOUT
                        "to" => "transferorders",
                        "sales" => "packingslips",         // ✅ AJOUT
                        "ps" => "packingslips",
                        _ => exportType
                    };
                }
            }

            // Par défaut, exporter tout
            return "all";
        }

        // ========== MÉTHODES POUR PACKING SLIPS ==========

        /// <summary>
        /// Mode test : Export de tous les Packing Slips AVEC UTF-8 (2 fichiers)
        /// </summary>
        private static async Task ExportAllPackingSlipsTestMode()
        {
            Console.WriteLine("\n🧪 MODE TEST - Export de tous les Packing Slips (avec UTF-8, 2 fichiers)");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var packingSlips = await _packingSlipDatabaseService.GetAllPackingSlipsAsync();
                Console.WriteLine($"✓ {packingSlips.Count} Packing Slips trouvés");

                if (packingSlips.Count == 0)
                {
                    Console.WriteLine("ℹ️ Aucun Packing Slip trouvé");
                    return;
                }

                // ✅ NOUVEAU : Grouper par commande (transRefId) pour créer les structures complètes
                var commandGroups = packingSlips
                    .Where(ps => ps.DynamicsData != null)
                    .GroupBy(ps => ps.DynamicsData.transRefId)
                    .ToList();

                Console.WriteLine($"✓ {commandGroups.Count} commandes distinctes trouvées");

                var speedPackingSlips = new List<SpeedPackingSlipComplete>();
                var allOriginalIds = new List<int>();
                int erreurs = 0;
                int utf8Transformations = 0;

                foreach (var commandGroup in commandGroups)
                {
                    var packingSlipsList = commandGroup.ToList();
                    var speedPackingSlip = _packingSlipMapper.MapToSpeedComplete(packingSlipsList);

                    if (speedPackingSlip != null)
                    {
                        speedPackingSlips.Add(speedPackingSlip);
                        allOriginalIds.AddRange(speedPackingSlip.OriginalPackingSlipIds);

                        // Vérifier les transformations UTF-8
                        if (packingSlipsList.Any(ps => HasUtf8TransformationsPS(ps.DynamicsData)))
                        {
                            utf8Transformations++;
                        }
                    }
                    else
                    {
                        erreurs++;
                    }
                }

                Console.WriteLine($"✓ {speedPackingSlips.Count} commandes converties");
                Console.WriteLine($"✓ {speedPackingSlips.Sum(ps => ps.Lines.Count)} lignes d'articles");
                Console.WriteLine($"✅ {utf8Transformations} commandes avec transformations UTF-8");

                if (erreurs > 0)
                {
                    Console.WriteLine($"⚠️ {erreurs} commandes n'ont pas pu être converties");
                }

                // ✅ NOUVEAU : Export avec la nouvelle méthode (2 fichiers)
                await ExportPackingSlipsInBatches(speedPackingSlips, null, "COSMETIQUE_TEST");

                stopwatch.Stop();
                Console.WriteLine($"⏱️ Temps Packing Slips : {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur Packing Slips mode test : {ex.Message}");
                _logger.LogError(ex, "Erreur Packing Slips mode test");
                throw;
            }
        }

        /// <summary>
        /// Mode production : Export des nouveaux Packing Slips AVEC UTF-8 (2 fichiers)
        /// </summary>
        private static async Task ExportNewPackingSlipsOnly()
        {
            Console.WriteLine("\n🆕 Export des nouveaux Packing Slips (avec UTF-8, 2 fichiers)");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var packingSlips = await _packingSlipDatabaseService.GetNonExportedPackingSlipsAsync();
                Console.WriteLine($"✓ {packingSlips.Count} nouveaux Packing Slips trouvés");

                if (packingSlips.Count == 0)
                {
                    Console.WriteLine("ℹ️ Aucun nouveau Packing Slip à exporter");
                    return;
                }

                // ✅ NOUVEAU : Grouper par commande (transRefId) pour créer les structures complètes
                var commandGroups = packingSlips
                    .Where(ps => ps.DynamicsData != null)
                    .GroupBy(ps => ps.DynamicsData.transRefId)
                    .ToList();

                Console.WriteLine($"✓ {commandGroups.Count} commandes distinctes trouvées");

                var speedPackingSlips = new List<SpeedPackingSlipComplete>();
                var allOriginalIds = new List<int>();
                int erreurs = 0;
                int utf8Transformations = 0;

                foreach (var commandGroup in commandGroups)
                {
                    var packingSlipsList = commandGroup.ToList();
                    var speedPackingSlip = _packingSlipMapper.MapToSpeedComplete(packingSlipsList);

                    if (speedPackingSlip != null)
                    {
                        speedPackingSlips.Add(speedPackingSlip);
                        allOriginalIds.AddRange(speedPackingSlip.OriginalPackingSlipIds);

                        // Vérifier les transformations UTF-8
                        if (packingSlipsList.Any(ps => HasUtf8TransformationsPS(ps.DynamicsData)))
                        {
                            utf8Transformations++;
                        }
                    }
                    else
                    {
                        erreurs++;
                    }
                }

                Console.WriteLine($"✓ {speedPackingSlips.Count} commandes converties");
                Console.WriteLine($"✓ {speedPackingSlips.Sum(ps => ps.Lines.Count)} lignes d'articles");
                Console.WriteLine($"✅ {utf8Transformations} commandes avec transformations UTF-8");

                if (erreurs > 0)
                {
                    Console.WriteLine($"⚠️ {erreurs} commandes n'ont pas pu être converties");
                }

                // ✅ NOUVEAU : Export avec marquage automatique (2 fichiers)
                await ExportPackingSlipsInBatches(speedPackingSlips, allOriginalIds, "COSMETIQUE");

                stopwatch.Stop();
                Console.WriteLine($"⏱️ Temps Packing Slips : {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur nouveaux Packing Slips : {ex.Message}");
                _logger.LogError(ex, "Erreur nouveaux Packing Slips");
                throw;
            }
        }

        /// <summary>
        /// Vérifie si un Packing Slip a des transformations UTF-8
        /// </summary>
        private static bool HasUtf8TransformationsPS(DynamicsPackingSlip? dynamics)
        {
            if (dynamics == null) return false;

            var originalDeliveryName = dynamics.DeliveryName ?? "";
            var originalStreet = dynamics.Street ?? "";
            var originalCommentPreparation = dynamics.CommentPreparation ?? "";

            var processedDeliveryName = _textProcessor.ProcessName(originalDeliveryName);
            var processedStreet = _textProcessor.ProcessName(originalStreet);
            var processedCommentPreparation = _textProcessor.ProcessName(originalCommentPreparation);

            return originalDeliveryName != processedDeliveryName ||
                   originalStreet != processedStreet ||
                   originalCommentPreparation != processedCommentPreparation;
        }

        /// <summary>
        /// Méthode utilitaire pour exporter les Packing Slips en gérant les lots (2 fichiers)
        /// </summary>
        private static async Task ExportPackingSlipsInBatches(List<SpeedPackingSlipComplete> speedPackingSlips, List<int>? originalIds, string filePrefix)
        {
            Console.WriteLine("Export des Packing Slips en 2 fichiers TXT (OPE + OPL)...");
            var batchSize = _configuration.GetValue<int>("XmlExport:BatchSize", 1000);

            if (speedPackingSlips.Count > batchSize)
            {
                Console.WriteLine($"📦 Export en {Math.Ceiling((double)speedPackingSlips.Count / batchSize)} lots...");

                var results = await _packingSlipTxtExportService.ExportInBatchesAsync(speedPackingSlips, originalIds, batchSize);

                Console.WriteLine($"✓ Export terminé : {results.Count} lots créés");

                int totalHeaders = 0;
                int totalLines = 0;

                foreach (var result in results)
                {
                    Console.WriteLine($"  📁 En-têtes: {Path.GetFileName(result.HeaderFilePath)} ({result.HeaderCount} commandes)");
                    Console.WriteLine($"  📁 Lignes: {Path.GetFileName(result.LinesFilePath)} ({result.LinesCount} lignes)");

                    totalHeaders += result.HeaderCount;
                    totalLines += result.LinesCount;
                }

                Console.WriteLine($"📊 Total: {totalHeaders} en-têtes, {totalLines} lignes");

                await _packingSlipDatabaseService.LogPackingSlipExportAsync(
                    $"Export Packing Slips ({results.Count} lots)",
                    totalHeaders,
                    "SUCCESS",
                    $"{results.Count} lots générés avec UTF-8: {totalHeaders} en-têtes, {totalLines} lignes"
                );
            }
            else
            {
                var result = await _packingSlipTxtExportService.ExportToTxtAsync(speedPackingSlips, originalIds, "COSMETIQUE");

                if (result != null)
                {
                    Console.WriteLine($"✓ Export terminé :");
                    Console.WriteLine($"  📁 En-têtes: {Path.GetFileName(result.HeaderFilePath)} ({result.HeaderCount} commandes)");
                    Console.WriteLine($"  📁 Lignes: {Path.GetFileName(result.LinesFilePath)} ({result.LinesCount} lignes)");

                    Console.WriteLine($"📊 Résumé: {result.HeaderCount} en-têtes, {result.LinesCount} lignes");

                    await _packingSlipDatabaseService.LogPackingSlipExportAsync(
                        $"{Path.GetFileName(result.HeaderFilePath)}+{Path.GetFileName(result.LinesFilePath)}",
                        result.HeaderCount,
                        "SUCCESS",
                        $"Export 2 fichiers TXT avec UTF-8: {result.HeaderCount} en-têtes, {result.LinesCount} lignes"
                    );
                }
            }
        }

        /// <summary>
        /// Mode test : Export de tous les articles SANS les marquer comme exportés
        /// AVEC traitement UTF-8 ET exclusion des articles ART_STAT=3
        /// </summary>
        private static async Task ExportAllArticlesTestMode()
        {
            Console.WriteLine("\n🧪 MODE TEST - Export de tous les articles (avec UTF-8 + exclusion ART_STAT=3)");
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

                // Convertir les articles avec traitement UTF-8 ET exclusion ART_STAT=3
                Console.WriteLine("Conversion des articles au format WINDEV (avec UTF-8 + exclusion ART_STAT=3)...");
                var winDevArticles = new List<WinDevArticle>();
                int erreurs = 0;
                int utf8Transformations = 0;
                int articlesExclus = 0; // ✅ NOUVEAU : Compteur d'exclusions

                foreach (var article in articles)
                {
                    // ✅ NOUVELLE RÈGLE : Vérifier si l'article doit être exclu
                    if (_articleMapper.ShouldExcludeArticle(article))
                    {
                        articlesExclus++;
                        continue; // Passer au suivant sans traiter
                    }

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
                Console.WriteLine($"🚫 {articlesExclus} articles exclus (ART_STAT=3)"); // ✅ NOUVEAU : Affichage exclusions
                if (erreurs > 0)
                {
                    Console.WriteLine($"⚠️ {erreurs} articles n'ont pas pu être convertis");
                }

                // Export en XML SANS marquer les articles comme exportés
                await ExportArticlesInBatches(winDevArticles, null, "ARTICLE_TEST_COMPLET", articlesExclus);

                Console.WriteLine($"🧪 MODE TEST : {winDevArticles.Count} articles exportés SANS marquage (UTF-8 traité, ART_STAT=3 exclus)");
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
        /// AVEC traitement UTF-8 ET exclusion des articles ART_STAT=3
        /// </summary>
        private static async Task ExportNewArticlesOnly()
        {
            Console.WriteLine("\n🆕 Export des nouveaux articles uniquement (avec UTF-8 + exclusion ART_STAT=3)");
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

                // Convertir les articles avec traitement UTF-8 ET exclusion ART_STAT=3
                Console.WriteLine("Conversion des articles au format WINDEV (avec UTF-8 + exclusion ART_STAT=3)...");
                var winDevArticles = new List<WinDevArticle>();
                var originalIds = new List<int>();
                var excludedIds = new List<int>(); // ✅ NOUVEAU : IDs des articles exclus
                int erreurs = 0;
                int utf8Transformations = 0;
                int articlesExclus = 0;

                foreach (var article in articles)
                {
                    // ✅ NOUVELLE RÈGLE : Vérifier si l'article doit être exclu
                    if (_articleMapper.ShouldExcludeArticle(article))
                    {
                        articlesExclus++;
                        excludedIds.Add(article.Id); // Conserver l'ID pour marquage potentiel
                        continue;
                    }

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
                Console.WriteLine($"🚫 {articlesExclus} articles exclus (ART_STAT=3)");
                if (erreurs > 0)
                {
                    Console.WriteLine($"⚠️ {erreurs} articles n'ont pas pu être convertis");
                }

                // Exporter en XML avec marquage automatique (seulement les articles traités)
                if (winDevArticles.Count > 0)
                {
                    await ExportArticlesInBatches(winDevArticles, originalIds, "ARTICLE_COSMETIQUE");
                    Console.WriteLine($"🎯 {winDevArticles.Count} articles marqués comme exportés (UTF-8 traité, ART_STAT=3 exclus)");
                }

                // ✅ NOUVEAU : Marquer également les articles exclus pour éviter qu'ils soient retraités
                if (excludedIds.Count > 0)
                {
                    await _databaseService.MarkArticlesAsExportedAsync(excludedIds, "EXCLUDED_ART_STAT_3");
                    Console.WriteLine($"🚫 {excludedIds.Count} articles exclus marqués comme traités");
                    _logger.LogInformation($"{excludedIds.Count} articles avec ART_STAT=3 marqués comme exclus");
                }

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

                await ExportPurchaseOrdersInBatches(winDevPurchaseOrders, null, "PURCHASE_ORDERS_TEST");

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

                await ExportPurchaseOrdersInBatches(winDevPurchaseOrders, originalIds, "RECAT_COSMETIQUE_PURCHASE_ORDERS");

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

                await ExportReturnOrdersInBatches(winDevReturnOrders, null, "RETURN_ORDERS_TEST");

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

                await ExportReturnOrdersInBatches(winDevReturnOrders, originalIds, "RECAT_COSMETIQUE_RETURN_ORDERS");

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

                await ExportTransferOrdersInBatches(winDevTransferOrders, null, "TRANSFER_ORDERS_TEST");

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

                await ExportTransferOrdersInBatches(winDevTransferOrders, originalIds, "RECAT_COSMETIQUE_TRANSFER_ORDERS");

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
        /// ✅ MODIFIÉ : Méthode utilitaire pour exporter les articles en gérant les lots ET les exclusions ART_STAT=3
        /// </summary>
        private static async Task ExportArticlesInBatches(List<WinDevArticle> winDevArticles, List<int>? originalIds, string filePrefix, int excludedCount = 0)
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

                // ✅ NOUVEAU : Affichage du résumé avec exclusions
                if (excludedCount > 0)
                {
                    Console.WriteLine($"  🚫 {excludedCount} articles exclus (ART_STAT=3)");
                }

                // ✅ MODIFIÉ : Utilisation de la nouvelle méthode avec exclusions
                await _databaseService.LogExportWithExclusionsAsync(
                    $"Export articles ({files.Count} fichiers)",
                    winDevArticles.Count,
                    excludedCount,
                    "SUCCESS",
                    $"{files.Count} fichiers générés avec UTF-8, {excludedCount} articles exclus (ART_STAT=3)"
                );
            }
            else
            {
                // ✅ MODIFIÉ : Passage du paramètre excludedCount
                var filePath = await _xmlExportService.ExportToXmlAsync(winDevArticles, originalIds, filePrefix, excludedCount);
                if (filePath != null)
                {
                    Console.WriteLine($"✓ Export terminé : {Path.GetFileName(filePath)}");
                    Console.WriteLine($"  📁 Chemin complet : {filePath}");

                    // ✅ NOUVEAU : Affichage du résumé avec exclusions
                    if (excludedCount > 0)
                    {
                        Console.WriteLine($"  🚫 {excludedCount} articles exclus (ART_STAT=3)");
                    }

                    // ✅ NOTE : Le log est maintenant fait automatiquement dans ExportToXmlAsync
                    // avec la nouvelle méthode LogExportWithExclusionsAsync
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
        /// ✅ NOUVEAU : Test spécifique de suppression des entités
        /// </summary>
        private static void TestEntityRemoval()
        {
            Console.WriteLine("=== TEST SUPPRESSION ENTITÉS ===");

            var testCases = new Dictionary<string, string>
    {
        {"L&apos;Oréal & Co", "LOreal  Co"},
        {"Société &amp; Associés", "Societe  Associes"},
        {"Produit &quot;Premium&quot;", "Produit Premium"},
        {"Prix &lt;100&gt; euros", "Prix 100 euros"},
        {"Beauté &amp; Santé", "Beaute  Sante"},
        {"L&apos;Occitane &amp; Cie", "LOccitane  Cie"},
        {"Code &#39;spécial&#39;", "Code special"},
        {"Marque &#x26; Distribution", "Marque  Distribution"}
    };

            int passed = 0;
            foreach (var test in testCases)
            {
                var processed = _textProcessor.ProcessText(test.Key);
                var expectedWithoutSpaces = test.Value.Replace("  ", " "); // Normaliser espaces
                var success = processed.Replace("  ", " ") == expectedWithoutSpaces;

                var status = success ? "✅" : "❌";
                Console.WriteLine($"{status} '{test.Key}' → '{processed}'");

                if (!success)
                {
                    Console.WriteLine($"   Attendu: '{expectedWithoutSpaces}'");
                }
                else
                {
                    passed++;
                }
            }

            Console.WriteLine($"=== RÉSULTAT: {passed}/{testCases.Count} tests passés ===\n");
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
            Console.WriteLine("✅ Service Utf8TextProcessor initialisé");

            // ✅ NOUVEAU : Log des exemples de transformation au démarrage
            _textProcessor.LogTransformationExamples(_logger);

            // ✅ NOUVEAU : Test des entités imbriquées
            _textProcessor.LogNestedEntityExamples(_logger);

            // Services Articles (MODIFIÉS pour inclure UTF-8)
            _databaseService = new DatabaseService(_configuration, loggerFactory.CreateLogger<DatabaseService>());
            _xmlExportService = new XmlExportService(_configuration, loggerFactory.CreateLogger<XmlExportService>(), _databaseService);
            _articleMapper = new ArticleMapper(_configuration, loggerFactory.CreateLogger<ArticleMapper>(), _textProcessor);
            Console.WriteLine("✅ Services Articles initialisés");

            // ✅ VÉRIFICATION: Services Purchase Orders (MODIFIÉS pour inclure UTF-8)
            try
            {
                _purchaseOrderDatabaseService = new PurchaseOrderDatabaseService(_configuration, loggerFactory.CreateLogger<PurchaseOrderDatabaseService>());
                _purchaseOrderXmlExportService = new PurchaseOrderXmlExportService(_configuration, loggerFactory.CreateLogger<PurchaseOrderXmlExportService>(), _purchaseOrderDatabaseService);
                _purchaseOrderMapper = new PurchaseOrderMapper(_configuration, loggerFactory.CreateLogger<PurchaseOrderMapper>(), _textProcessor);
                Console.WriteLine("✅ Services Purchase Orders initialisés");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERREUR lors de l'initialisation des services Purchase Orders: {ex.Message}");
                _logger?.LogError(ex, "Erreur initialisation services Purchase Orders");
                throw;
            }

            // Services Return Orders (MODIFIÉS pour inclure UTF-8)
            _returnOrderDatabaseService = new ReturnOrderDatabaseService(_configuration, loggerFactory.CreateLogger<ReturnOrderDatabaseService>());
            _returnOrderXmlExportService = new ReturnOrderXmlExportService(_configuration, loggerFactory.CreateLogger<ReturnOrderXmlExportService>(), _returnOrderDatabaseService);
            _returnOrderMapper = new ReturnOrderMapper(_configuration, loggerFactory.CreateLogger<ReturnOrderMapper>(), _textProcessor);
            Console.WriteLine("✅ Services Return Orders initialisés");

            // Services Transfer Orders (MODIFIÉS pour inclure UTF-8)
            _transferOrderDatabaseService = new TransferOrderDatabaseService(_configuration, loggerFactory.CreateLogger<TransferOrderDatabaseService>());
            _transferOrderXmlExportService = new TransferOrderXmlExportService(_configuration, loggerFactory.CreateLogger<TransferOrderXmlExportService>(), _transferOrderDatabaseService);
            _transferOrderMapper = new TransferOrderMapper(_configuration, loggerFactory.CreateLogger<TransferOrderMapper>(), _textProcessor);
            Console.WriteLine("✅ Services Transfer Orders initialisés");

            // ✅ Services Packing Slips (NOUVEAUX avec UTF-8)
            _packingSlipDatabaseService = new PackingSlipDatabaseService(_configuration, loggerFactory.CreateLogger<PackingSlipDatabaseService>());
            _packingSlipTxtExportService = new PackingSlipTxtExportService(_configuration, loggerFactory.CreateLogger<PackingSlipTxtExportService>(), _packingSlipDatabaseService);
            _packingSlipMapper = new PackingSlipMapper(_configuration, loggerFactory.CreateLogger<PackingSlipMapper>(), _textProcessor);
            Console.WriteLine("✅ Services Packing Slips initialisés");

            Console.WriteLine("✅ Tous les services ont été initialisés avec succès");
        }
    }
}