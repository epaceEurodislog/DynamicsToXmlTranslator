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
        public async Task<string> ExportToXmlAsync(List<WinDevArticle> articles, string fileNamePrefix = "ARTICLE_GSCF")
        {
            if (articles == null || !articles.Any())
            {
                _logger.LogWarning("Aucun article à exporter");
                return null;
            }

            try
            {
                // Générer le nom du fichier avec timestamp
                string timestamp = DateTime.Now.ToString("dd-MM-yyyy_HHHmmMss", CultureInfo.InvariantCulture);
                string fileName = $"{fileNamePrefix}_{timestamp}_FICHIER TRAITE.XML";
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

                // Post-traitement pour formater les nombres décimaux
                await PostProcessXmlFile(filePath);

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
        /// Post-traite le fichier XML pour formater les nombres décimaux
        /// </summary>
        private async Task PostProcessXmlFile(string filePath)
        {
            try
            {
                // Lire le fichier
                string xmlContent = await File.ReadAllTextAsync(filePath, Encoding.GetEncoding("ISO-8859-1"));

                // Remplacer les virgules par des points pour les décimaux
                xmlContent = xmlContent.Replace("<ART_PRIX>", "<ART_PRIX>")
                                     .Replace(",", ".");

                // Réécrire le fichier
                await File.WriteAllTextAsync(filePath, xmlContent, Encoding.GetEncoding("ISO-8859-1"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du post-traitement du fichier XML");
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

                    string fileNamePrefix = $"{_configuration["XmlExport:FileNamePrefix"] ?? "ARTICLE_GSCF"}_LOT{batchNumber:D3}";
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
        /// Génère un fichier XML de test avec des données d'exemple
        /// </summary>
        public async Task<string> GenerateTestXmlAsync()
        {
            var testArticles = new List<WinDevArticle>
            {
                new WinDevArticle
                {
                    ActCode = "GSCF",
                    ArtCode = "TEST001",
                    TieCode = "1040",
                    ArtDesc = "ARTICLE TEST",
                    ArtDesl = "Article de test pour validation du format",
                    ArtEanu = "1234567890123",
                    ArtEanc = "",
                    ArtQtec = 1,
                    ArtQtep = 100,
                    ArtStat = 4,
                    ArtNval = 0,
                    ArtAlpha2 = "COLIS",
                    ArtDdlc = 1,
                    ArtAlpha8 = "TEST001",
                    ArtAlpha9 = "KGM",
                    ArtPoiu = 1000,
                    ArtPrix = 10.50m,
                    ArtAlpha14 = "SEC",
                    ArtUni = 1,
                    ArtNum19 = 365,
                    ArtNum21 = 0,
                    ArtAlpha18 = "1510@0@@@@",
                    ArtAlpha15 = "",
                    ArtTpve = 0,
                    ArtTpvs = 0,
                    ArtAlpha26 = "LTRF",
                    ArtAlpha25 = "",
                    ArtTop18 = 0,
                    ArtAlpha19 = ""
                }
            };

            return await ExportToXmlAsync(testArticles, "ARTICLE_TEST");
        }
    }
}