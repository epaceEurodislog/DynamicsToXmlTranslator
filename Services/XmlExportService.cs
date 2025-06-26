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

                // Configuration du serializer XML
                var settings = new XmlWriterSettings
                {
                    Encoding = Encoding.GetEncoding("ISO-8859-1"),
                    Indent = true,
                    IndentChars = "\t",
                    OmitXmlDeclaration = false,
                    CloseOutput = true
                };

                // Serialisation
                using (var writer = XmlWriter.Create(filePath, settings))
                {
                    // Ajouter la déclaration XML
                    writer.WriteStartDocument(true);

                    var serializer = new XmlSerializer(typeof(WinDevTable));
                    var namespaces = new XmlSerializerNamespaces();
                    namespaces.Add("", ""); // Pas de namespace

                    // Créer un custom XmlWriter pour forcer les balises fermées
                    var customWriter = new ForceCloseTagXmlWriter(writer);
                    serializer.Serialize(customWriter, winDevTable, namespaces);
                }

                _logger.LogInformation($"Export XML réussi : {fileName} ({articles.Count} articles)");

                // NOUVEAU : Marquer les articles comme exportés
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

                    // Pour les lots, on passe un nom complet sans timestamp supplémentaire
                    string fileName = $"{fileNamePrefix}.XML";
                    string filePath = Path.Combine(_exportDirectory, fileName);

                    // Créer l'objet racine
                    var winDevTable = new WinDevTable
                    {
                        Articles = batch
                    };

                    // Configuration du serializer XML
                    var settings = new XmlWriterSettings
                    {
                        Encoding = Encoding.GetEncoding("ISO-8859-1"),
                        Indent = true,
                        IndentChars = "\t",
                        OmitXmlDeclaration = false,
                        CloseOutput = true
                    };

                    // Serialisation directe pour les lots
                    using (var writer = XmlWriter.Create(filePath, settings))
                    {
                        writer.WriteStartDocument(true);

                        var serializer = new XmlSerializer(typeof(WinDevTable));
                        var namespaces = new XmlSerializerNamespaces();
                        namespaces.Add("", "");

                        // Créer un custom XmlWriter pour forcer les balises fermées
                        var customWriter = new ForceCloseTagXmlWriter(writer);
                        serializer.Serialize(customWriter, winDevTable, namespaces);
                    }

                    if (File.Exists(filePath))
                    {
                        exportedFiles.Add(filePath);
                        _logger.LogInformation($"Lot {batchNumber} exporté : {fileName}");

                        // NOUVEAU : Marquer les articles du lot comme exportés
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

    /// <summary>
    /// Custom XmlWriter qui force la fermeture des balises vides
    /// au lieu d'utiliser la syntaxe auto-fermante <tag />
    /// </summary>
    public class ForceCloseTagXmlWriter : XmlWriter
    {
        private readonly XmlWriter _writer;

        public ForceCloseTagXmlWriter(XmlWriter writer)
        {
            _writer = writer;
        }

        public override void WriteStartElement(string prefix, string localName, string ns)
        {
            _writer.WriteStartElement(prefix, localName, ns);
        }

        public override void WriteEndElement()
        {
            _writer.WriteFullEndElement(); // Force l'écriture complète même pour les éléments vides
        }

        public override void WriteFullEndElement()
        {
            _writer.WriteFullEndElement();
        }

        public override void WriteString(string text)
        {
            _writer.WriteString(text);
        }

        // Déléguer toutes les autres méthodes au writer sous-jacent
        public override void Close() => _writer.Close();
        public override void Flush() => _writer.Flush();
        public override string LookupPrefix(string ns) => _writer.LookupPrefix(ns);
        public override void WriteBase64(byte[] buffer, int index, int count) => _writer.WriteBase64(buffer, index, count);
        public override void WriteCData(string text) => _writer.WriteCData(text);
        public override void WriteCharEntity(char ch) => _writer.WriteCharEntity(ch);
        public override void WriteChars(char[] buffer, int index, int count) => _writer.WriteChars(buffer, index, count);
        public override void WriteComment(string text) => _writer.WriteComment(text);
        public override void WriteDocType(string name, string pubid, string sysid, string subset) => _writer.WriteDocType(name, pubid, sysid, subset);
        public override void WriteEndAttribute() => _writer.WriteEndAttribute();
        public override void WriteEndDocument() => _writer.WriteEndDocument();
        public override void WriteEntityRef(string name) => _writer.WriteEntityRef(name);
        public override void WriteProcessingInstruction(string name, string text) => _writer.WriteProcessingInstruction(name, text);
        public override void WriteRaw(string data) => _writer.WriteRaw(data);
        public override void WriteRaw(char[] buffer, int index, int count) => _writer.WriteRaw(buffer, index, count);
        public override void WriteStartAttribute(string prefix, string localName, string ns) => _writer.WriteStartAttribute(prefix, localName, ns);
        public override void WriteStartDocument() => _writer.WriteStartDocument();
        public override void WriteStartDocument(bool standalone) => _writer.WriteStartDocument(standalone);
        public override void WriteSurrogateCharEntity(char lowChar, char highChar) => _writer.WriteSurrogateCharEntity(lowChar, highChar);
        public override void WriteWhitespace(string ws) => _writer.WriteWhitespace(ws);

        public override WriteState WriteState => _writer.WriteState;
    }
}