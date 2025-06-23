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

        public XmlExportService(IConfiguration configuration, ILogger<XmlExportService> logger)
        {
            _configuration = configuration;
            _logger = logger;

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
        public async Task<string?> ExportToXmlAsync(List<WinDevArticle> articles, string fileNamePrefix = "ARTICLE_WINDEV")
        {
            if (articles == null || !articles.Any())
            {
                _logger.LogWarning("Aucun article à exporter");
                return null;
            }

            try
            {
                // Générer le nom du fichier avec timestamp
                string timestamp = DateTime.Now.ToString("dd-MM-yyyy_HHmmss", CultureInfo.InvariantCulture);
                string fileName = $"{fileNamePrefix}_{timestamp}_FICHIER_TRAITE.XML";
                string filePath = Path.Combine(_exportDirectory, fileName);

                // Créer l'objet racine
                var winDevTable = new WinDevTable
                {
                    Articles = articles
                };

                // Configuration du serializer XML
                var settings = new XmlWriterSettings
                {
                    Encoding = Encoding.GetEncoding("ISO-8859-1"),
                    Indent = true,
                    IndentChars = "\t"
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

                _logger.LogInformation($"Export XML réussi : {fileName} ({articles.Count} articles)");
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
        public async Task<List<string>> ExportInBatchesAsync(List<WinDevArticle> articles, int batchSize = 1000)
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

                _logger.LogInformation($"Export de {articles.Count} articles en {batches.Count} fichiers");

                // Exporter chaque lot
                for (int i = 0; i < batches.Count; i++)
                {
                    var batch = batches[i];
                    var batchNumber = i + 1;

                    _logger.LogInformation($"Export du lot {batchNumber}/{batches.Count} ({batch.Count} articles)");

                    string fileNamePrefix = $"{_configuration["XmlExport:FileNamePrefix"] ?? "ARTICLE_WINDEV"}_LOT{batchNumber:D3}";
                    string filePath = await ExportToXmlAsync(batch, fileNamePrefix);

                    if (!string.IsNullOrEmpty(filePath))
                    {
                        exportedFiles.Add(filePath);
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
        /// Génère un fichier XML de test vide (plus d'articles de test nécessaires)
        /// </summary>
        public async Task<string?> GenerateTestXmlAsync()
        {
            var testArticles = new List<WinDevArticle>();

            return await ExportToXmlAsync(testArticles, "ARTICLE_TEST_VIDE");
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