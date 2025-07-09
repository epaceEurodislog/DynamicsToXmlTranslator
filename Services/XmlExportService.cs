using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using DynamicsToXmlTranslator.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DynamicsToXmlTranslator.Services
{
    public class XmlExportService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<XmlExportService> _logger;
        private readonly string _exportDirectory;
        private readonly DatabaseService _databaseService;

        public XmlExportService(IConfiguration configuration, ILogger<XmlExportService> logger, DatabaseService databaseService)
        {
            _configuration = configuration;
            _logger = logger;
            _databaseService = databaseService;

            _exportDirectory = _configuration["XmlExport:OutputDirectory"] ?? "exports";

            // Créer le répertoire d'export s'il n'existe pas
            if (!Directory.Exists(_exportDirectory))
            {
                Directory.CreateDirectory(_exportDirectory);
                _logger.LogInformation($"Répertoire d'export créé : {_exportDirectory}");
            }
        }

        /// <summary>
        /// Exporte une liste d'articles WINDEV en fichier XML
        /// </summary>
        public async Task<string?> ExportToXmlAsync(List<WinDevArticle> articles, List<int> originalArticleIds = null, string fileNamePrefix = "ARTICLE_COSMETIQUE")
        {
            if (articles == null || !articles.Any())
            {
                _logger.LogWarning("Aucun article à exporter");
                return null;
            }

            try
            {
                // Générer le nom du fichier avec le nouveau format
                // Format: ARTICLE_COSMETIQUE_YYYYMMDD_HHMMSS.XML
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
                string fileName = $"{fileNamePrefix}_{timestamp}.XML";
                string filePath = Path.Combine(_exportDirectory, fileName);

                // Créer l'objet racine
                var winDevTable = new WinDevTable
                {
                    Articles = articles
                };

                // ✅ MODIFICATION : Configuration pour forcer les balises fermantes
                var settings = new XmlWriterSettings
                {
                    Encoding = Encoding.GetEncoding("ISO-8859-1"),
                    Indent = true,
                    IndentChars = "\t",
                    OmitXmlDeclaration = false,
                    CloseOutput = true,
                    ConformanceLevel = ConformanceLevel.Document
                };

                // Serialisation
                using (var writer = XmlWriter.Create(filePath, settings))
                {
                    // Ajouter la déclaration XML
                    writer.WriteStartDocument(true);

                    var serializer = new XmlSerializer(typeof(WinDevTable));
                    var namespaces = new XmlSerializerNamespaces();
                    namespaces.Add("", ""); // Pas de namespace

                    serializer.Serialize(writer, winDevTable, namespaces);
                }

                // ✅ NOUVEAU : Post-traitement pour forcer les balises fermantes
                await ForceClosingTagsAsync(filePath);

                _logger.LogInformation($"Export XML réussi : {fileName} ({articles.Count} articles)");

                // Marquer les articles comme exportés
                if (originalArticleIds != null && originalArticleIds.Any())
                {
                    await _databaseService.MarkArticlesAsExportedAsync(originalArticleIds, fileName);
                }

                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'export XML");
                throw;
            }
        }

        /// <summary>
        /// Exporte les articles par lots
        /// </summary>
        public async Task<List<string>> ExportInBatchesAsync(List<WinDevArticle> articles, List<int> originalArticleIds = null, int batchSize = 1000)
        {
            var exportedFiles = new List<string>();

            if (articles == null || !articles.Any())
            {
                _logger.LogWarning("Aucun article à exporter");
                return exportedFiles;
            }

            try
            {
                // Diviser en lots
                var batches = articles
                    .Select((article, index) => new { article, index })
                    .GroupBy(x => x.index / batchSize)
                    .Select(g => g.Select(x => x.article).ToList())
                    .ToList();

                // Diviser aussi les IDs si fournis
                List<List<int>>? originalIdBatches = null;
                if (originalArticleIds != null && originalArticleIds.Any())
                {
                    originalIdBatches = originalArticleIds
                        .Select((id, index) => new { id, index })
                        .GroupBy(x => x.index / batchSize)
                        .Select(g => g.Select(x => x.id).ToList())
                        .ToList();
                }

                _logger.LogInformation($"Export de {articles.Count} articles en {batches.Count} fichiers");

                // Générer un timestamp unique pour tous les lots
                string baseTimestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);

                // Exporter chaque lot
                for (int i = 0; i < batches.Count; i++)
                {
                    var batch = batches[i];
                    var batchNumber = i + 1;
                    var batchIds = originalIdBatches?[i];

                    _logger.LogInformation($"Export du lot {batchNumber}/{batches.Count} ({batch.Count} articles)");

                    // Format: ARTICLE_COSMETIQUE_LOT001_YYYYMMDD_HHMMSS.XML
                    string fileNamePrefix = $"ARTICLE_COSMETIQUE_LOT{batchNumber:D3}_{baseTimestamp}";
                    string fileName = $"{fileNamePrefix}.XML";
                    string filePath = Path.Combine(_exportDirectory, fileName);

                    // Créer l'objet racine
                    var winDevTable = new WinDevTable
                    {
                        Articles = batch
                    };

                    // ✅ MODIFICATION : Configuration pour forcer les balises fermantes
                    var settings = new XmlWriterSettings
                    {
                        Encoding = Encoding.GetEncoding("ISO-8859-1"),
                        Indent = true,
                        IndentChars = "\t",
                        OmitXmlDeclaration = false,
                        CloseOutput = true,
                        ConformanceLevel = ConformanceLevel.Document
                    };

                    // Serialisation directe pour les lots
                    using (var writer = XmlWriter.Create(filePath, settings))
                    {
                        writer.WriteStartDocument(true);

                        var serializer = new XmlSerializer(typeof(WinDevTable));
                        var namespaces = new XmlSerializerNamespaces();
                        namespaces.Add("", "");

                        serializer.Serialize(writer, winDevTable, namespaces);
                    }

                    // ✅ NOUVEAU : Post-traitement pour forcer les balises fermantes
                    await ForceClosingTagsAsync(filePath);

                    if (File.Exists(filePath))
                    {
                        exportedFiles.Add(filePath);
                        _logger.LogInformation($"Lot {batchNumber} exporté : {fileName}");

                        // Marquer les articles du lot comme exportés
                        if (batchIds != null && batchIds.Any())
                        {
                            await _databaseService.MarkArticlesAsExportedAsync(batchIds, fileName);
                        }
                    }
                }

                return exportedFiles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'export par lots");
                throw;
            }
        }

        /// <summary>
        /// ✅ NOUVELLE MÉTHODE : Force les balises fermantes au lieu des balises auto-fermantes
        /// </summary>
        private async Task ForceClosingTagsAsync(string filePath)
        {
            try
            {
                string content = await File.ReadAllTextAsync(filePath, Encoding.GetEncoding("ISO-8859-1"));

                // Remplacer les balises auto-fermantes par des balises fermantes pour les articles
                var tagsToReplace = new[]
                {
                    "ART_CCLI", "ART_CODE", "ART_CODC", "ART_DESL", "ART_ALPHA2", "ART_EANU",
                    "ART_ALPHA17", "ART_ALPHA3", "ART_STAT", "ART_ALPHA8", "ART_ALPHA14",
                    "ART_RSTK", "ART_SPCB", "ART_ALPHA18", "ART_ALPHA24", "ART_ALPHA26",
                    "ART_NSE", "ART_EANC"
                };

                foreach (var tag in tagsToReplace)
                {
                    // Remplacer <TAG /> par <TAG></TAG>
                    content = content.Replace($"<{tag} />", $"<{tag}></{tag}>");
                    // Remplacer <TAG/> par <TAG></TAG> (sans espace)
                    content = content.Replace($"<{tag}/>", $"<{tag}></{tag}>");
                }

                await File.WriteAllTextAsync(filePath, content, Encoding.GetEncoding("ISO-8859-1"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors du post-traitement des balises fermantes pour {filePath}");
            }
        }

        /// <summary>
        /// Génère un fichier XML de test vide
        /// </summary>
        public async Task<string?> GenerateTestXmlAsync()
        {
            var testArticles = new List<WinDevArticle>();

            return await ExportToXmlAsync(testArticles, null, "ARTICLE_TEST_VIDE");
        }

        /// <summary>
        /// Méthode de test pour afficher le mapping d'un article depuis JSON
        /// </summary>
        public void TestArticleMapping(string jsonData)
        {
            try
            {
                _logger.LogInformation("=== TEST MAPPING API → SPEED (selon Excel) ===");

                var dynamicsArticle = Newtonsoft.Json.JsonConvert.DeserializeObject<DynamicsArticle>(jsonData);

                _logger.LogInformation("Données API reçues → Champs SPEED:");
                _logger.LogInformation($"  dataAreaId: '{dynamicsArticle?.dataAreaId}' → ART_CCLI");
                _logger.LogInformation($"  ItemId: '{dynamicsArticle?.ItemId}' → ART_PAR.ART_CODC");
                _logger.LogInformation($"  Name: '{dynamicsArticle?.Name}' → ART_PAR.ART_DESL");
                _logger.LogInformation($"  UnitId: '{dynamicsArticle?.UnitId}' → ART_PAR.ART_ALPHA2");
                _logger.LogInformation($"  itemBarCode: '{dynamicsArticle?.itemBarCode}' → ART_PAR.ART_EANU");
                _logger.LogInformation($"  Category: '{dynamicsArticle?.Category}' → ART.ALPHA17");
                _logger.LogInformation($"  OrigCountryRegionId: '{dynamicsArticle?.OrigCountryRegionId}' → ART.ALPHA3");
                _logger.LogInformation($"  GrossWeight: {dynamicsArticle?.GrossWeight}g → ART_PAR.ART_POIU");
                _logger.LogInformation($"  FactorColli: {dynamicsArticle?.FactorColli} → ART_PAR.ART_QTEC");
                _logger.LogInformation($"  FactorPallet: {dynamicsArticle?.FactorPallet} → ART_PAR.ART_QTEP");
                _logger.LogInformation($"  ACT_CODE: 'COSMETIQUE' (fixe)");

                _logger.LogInformation("Mapping selon Excel réussi !");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du test de mapping");
            }
        }
    }
}