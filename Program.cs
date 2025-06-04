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

namespace DynamicsToXmlTranslator
{
    class Program
    {
        private static ILogger<Program> _logger;
        private static IConfiguration _configuration;
        private static DatabaseService _databaseService;
        private static XmlExportService _xmlExportService;
        private static ArticleMapper _articleMapper;

        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Traducteur Dynamics vers XML WINDEV ===");
            Console.WriteLine($"Démarrage : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            try
            {
                // Configuration et initialisation
                SetupConfiguration();
                SetupLogging();
                SetupServices();

                // Vérifier/créer les tables nécessaires
                await _databaseService.CreateTablesIfNotExistsAsync();

                // Menu principal
                bool continuer = true;
                while (continuer)
                {
                    Console.WriteLine("\n--- Menu Principal ---");
                    Console.WriteLine("1. Exporter tous les articles");
                    Console.WriteLine("2. Exporter les articles modifiés aujourd'hui");
                    Console.WriteLine("3. Exporter les articles modifiés depuis une date");
                    Console.WriteLine("4. Générer un fichier XML de test");
                    Console.WriteLine("5. Afficher les statistiques");
                    Console.WriteLine("0. Quitter");
                    Console.Write("\nVotre choix : ");

                    var choix = Console.ReadLine();

                    switch (choix)
                    {
                        case "1":
                            await ExportAllArticles();
                            break;
                        case "2":
                            await ExportTodayArticles();
                            break;
                        case "3":
                            await ExportArticlesSinceDate();
                            break;
                        case "4":
                            await GenerateTestXml();
                            break;
                        case "5":
                            await ShowStatistics();
                            break;
                        case "0":
                            continuer = false;
                            break;
                        default:
                            Console.WriteLine("Choix invalide !");
                            break;
                    }
                }

                Console.WriteLine("\nAu revoir !");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur fatale dans le programme principal");
                Console.WriteLine($"\nERREUR FATALE : {ex.Message}");
                Console.WriteLine("Consultez les logs pour plus de détails.");
                Console.ReadKey();
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
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddConsole()
                    .AddFile("logs/translator.log");
            });

            _logger = loggerFactory.CreateLogger<Program>();
        }

        private static void SetupServices()
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole().AddFile("logs/translator.log");
            });

            _databaseService = new DatabaseService(_configuration, loggerFactory.CreateLogger<DatabaseService>());
            _xmlExportService = new XmlExportService(_configuration, loggerFactory.CreateLogger<XmlExportService>());
            _articleMapper = new ArticleMapper(_configuration, loggerFactory.CreateLogger<ArticleMapper>());
        }

        private static async Task ExportAllArticles()
        {
            Console.WriteLine("\n--- Export de tous les articles ---");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Récupérer tous les articles
                Console.WriteLine("Récupération des articles depuis la base de données...");
                var articles = await _databaseService.GetAllArticlesAsync();
                Console.WriteLine($"✓ {articles.Count} articles trouvés");

                if (articles.Count == 0)
                {
                    Console.WriteLine("Aucun article à exporter !");
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
                    Console.WriteLine($"⚠ {erreurs} articles n'ont pas pu être convertis");
                }

                // Exporter en XML
                Console.WriteLine("Export en fichier(s) XML...");
                var batchSize = _configuration.GetValue<int>("XmlExport:BatchSize", 1000);

                if (winDevArticles.Count > batchSize)
                {
                    // Export par lots
                    var files = await _xmlExportService.ExportInBatchesAsync(winDevArticles, batchSize);
                    Console.WriteLine($"✓ Export terminé : {files.Count} fichiers créés");

                    foreach (var file in files)
                    {
                        Console.WriteLine($"  - {Path.GetFileName(file)}");
                    }

                    // Enregistrer le log d'export
                    await _databaseService.LogExportAsync(
                        $"Export complet ({files.Count} fichiers)",
                        winDevArticles.Count,
                        "SUCCESS",
                        $"{files.Count} fichiers générés"
                    );
                }
                else
                {
                    // Export en un seul fichier
                    var filePath = await _xmlExportService.ExportToXmlAsync(winDevArticles);
                    Console.WriteLine($"✓ Export terminé : {Path.GetFileName(filePath)}");

                    // Enregistrer le log d'export
                    await _databaseService.LogExportAsync(
                        Path.GetFileName(filePath),
                        winDevArticles.Count,
                        "SUCCESS"
                    );
                }

                stopwatch.Stop();
                Console.WriteLine($"\n⏱ Temps total : {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'export complet");
                Console.WriteLine($"❌ Erreur : {ex.Message}");

                await _databaseService.LogExportAsync(
                    "Export échoué",
                    0,
                    "ERROR",
                    ex.Message
                );
            }
        }

        private static async Task ExportTodayArticles()
        {
            var today = DateTime.Today;
            await ExportArticlesSince(today, "articles modifiés aujourd'hui");
        }

        private static async Task ExportArticlesSinceDate()
        {
            Console.Write("\nEntrez la date (YYYY-MM-DD) : ");
            var dateStr = Console.ReadLine();

            if (DateTime.TryParse(dateStr, out var date))
            {
                await ExportArticlesSince(date, $"articles modifiés depuis le {date:yyyy-MM-dd}");
            }
            else
            {
                Console.WriteLine("Date invalide !");
            }
        }

        private static async Task ExportArticlesSince(DateTime sinceDate, string description)
        {
            Console.WriteLine($"\n--- Export des {description} ---");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Récupérer les articles modifiés
                Console.WriteLine($"Récupération des articles modifiés depuis {sinceDate:yyyy-MM-dd HH:mm:ss}...");
                var articles = await _databaseService.GetArticlesSinceDateAsync(sinceDate);
                Console.WriteLine($"✓ {articles.Count} articles trouvés");

                if (articles.Count == 0)
                {
                    Console.WriteLine("Aucun article à exporter !");
                    return;
                }

                // Convertir et exporter
                var winDevArticles = articles
                    .Select(a => _articleMapper.MapToWinDev(a))
                    .Where(a => a != null)
                    .ToList();

                var filePath = await _xmlExportService.ExportToXmlAsync(winDevArticles);
                Console.WriteLine($"✓ Export terminé : {Path.GetFileName(filePath)}");

                await _databaseService.LogExportAsync(
                    Path.GetFileName(filePath),
                    winDevArticles.Count,
                    "SUCCESS",
                    description
                );

                stopwatch.Stop();
                Console.WriteLine($"\n⏱ Temps total : {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'export par date");
                Console.WriteLine($"❌ Erreur : {ex.Message}");
            }
        }

        private static async Task GenerateTestXml()
        {
            Console.WriteLine("\n--- Génération d'un fichier XML de test ---");

            try
            {
                var filePath = await _xmlExportService.GenerateTestXmlAsync();
                Console.WriteLine($"✓ Fichier de test créé : {Path.GetFileName(filePath)}");
                Console.WriteLine($"  Chemin complet : {filePath}");

                // Afficher le contenu
                Console.Write("\nVoulez-vous afficher le contenu ? (O/N) : ");
                if (Console.ReadLine()?.ToUpper() == "O")
                {
                    var content = await File.ReadAllTextAsync(filePath);
                    Console.WriteLine("\n--- Contenu du fichier ---");
                    Console.WriteLine(content);
                    Console.WriteLine("--- Fin du fichier ---");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la génération du test");
                Console.WriteLine($"❌ Erreur : {ex.Message}");
            }
        }

        private static async Task ShowStatistics()
        {
            Console.WriteLine("\n--- Statistiques ---");

            try
            {
                var articles = await _databaseService.GetAllArticlesAsync();
                Console.WriteLine($"Total d'articles en base : {articles.Count}");

                // Grouper par date de mise à jour
                var parJour = articles
                    .GroupBy(a => a.LastUpdatedAt.Date)
                    .OrderByDescending(g => g.Key)
                    .Take(7)
                    .ToList();

                Console.WriteLine("\nArticles mis à jour (7 derniers jours) :");
                foreach (var groupe in parJour)
                {
                    Console.WriteLine($"  {groupe.Key:yyyy-MM-dd} : {groupe.Count()} articles");
                }

                // Afficher les exports récents
                Console.WriteLine("\nDerniers exports (à implémenter avec requête SQL)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'affichage des statistiques");
                Console.WriteLine($"❌ Erreur : {ex.Message}");
            }
        }
    }
}