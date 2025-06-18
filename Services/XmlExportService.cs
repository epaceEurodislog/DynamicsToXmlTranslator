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
        public async Task<string> ExportToXmlAsync(List<WinDevArticle> articles, string fileNamePrefix = "ARTICLE_WINDEV")
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
        /// Génère un fichier XML de test avec l'exemple SHSEBO500 réel
        /// </summary>
        public async Task<string> GenerateTestXmlAsync()
        {
            var testArticles = new List<WinDevArticle>
            {
                // Article basé sur l'exemple SHSEBO500 fourni
                new WinDevArticle
                {
                    ActCode = "BR", // dataAreaId: "br"
                    ArtCode = "SHSEBO500", // ItemId
                    ArtDesl = "SHAMPOOING TRAITANT SEBO-REEQUILIBRANT 500 ml", // Name
                    ArtEanu = "3700693203036", // itemBarCode
                    ArtPoiu = 566, // GrossWeight
                    ArtPoin = 0, // Weight
                    ArtHaut = 0, // Height
                    ArtLarg = 0, // Width
                    ArtProf = 0, // Depth
                    ArtHautb = 0, // grossHeight
                    ArtLargb = 0, // grossWidth
                    ArtProfb = 0, // grossDepth
                    ArtColi = 20, // FactorColli
                    ArtPal = 1000, // FactorPallet
                    ArtUnite = "U", // UnitId
                    ArtGroupe = "PF", // ItemGroupId
                    ArtCateg = "", // Category (vide dans l'exemple)
                    ArtStat3pl = "None", // INT3PLStatus
                    ArtHmim = 0, // HMIMIndicator
                    ArtLifecycle = "Non", // ProductLifecycleStateId
                    ArtVersion = "No", // ProducVersionAttribute
                    ArtDdlc = 1440, // PdsShelfLife
                    ArtLot1 = 1, // TrackingLot1
                    ArtLot2 = 0, // TrackingLot2
                    ArtDluo = 1, // TrackingDLCDDLUO
                    ArtProof = 1, // TrackingProoftag
                    ArtExtid = "", // ExternalItemId (vide)
                    ArtIntrastat = "", // IntrastatCommodity (vide)
                    ArtOrigine = "FRA", // OrigCountryRegionId
                    ArtVolume = 0 // Calculé (dimensions nulles)
                },
                
                // Article de test supplémentaire
                new WinDevArticle
                {
                    ActCode = "BR",
                    ArtCode = "TEST001",
                    ArtDesl = "ARTICLE TEST AVEC DIMENSIONS",
                    ArtEanu = "1234567890123",
                    ArtPoiu = 250,
                    ArtPoin = 200,
                    ArtHaut = 10,
                    ArtLarg = 5,
                    ArtProf = 3,
                    ArtHautb = 12,
                    ArtLargb = 6,
                    ArtProfb = 4,
                    ArtColi = 12,
                    ArtPal = 144,
                    ArtUnite = "PCE",
                    ArtGroupe = "TEST",
                    ArtCateg = "Produit Test",
                    ArtStat3pl = "Active",
                    ArtHmim = 0,
                    ArtLifecycle = "Actif",
                    ArtVersion = "V1",
                    ArtDdlc = 720,
                    ArtLot1 = 1,
                    ArtLot2 = 1,
                    ArtDluo = 1,
                    ArtProof = 0,
                    ArtExtid = "EXT001",
                    ArtIntrastat = "12345678",
                    ArtOrigine = "FRA",
                    ArtVolume = 150 // 10×5×3
                }
            };

            return await ExportToXmlAsync(testArticles, "ARTICLE_TEST_API_MAPPING");
        }

        /// <summary>
        /// Méthode de test pour afficher le mapping d'un article depuis JSON
        /// </summary>
        public void TestArticleMapping(string jsonData)
        {
            try
            {
                _logger.LogInformation("=== TEST MAPPING API → WINDEV ===");

                var dynamicsArticle = Newtonsoft.Json.JsonConvert.DeserializeObject<DynamicsArticle>(jsonData);

                _logger.LogInformation("Données API reçues:");
                _logger.LogInformation($"  ItemId: '{dynamicsArticle?.ItemId}' → ART_CODE");
                _logger.LogInformation($"  Name: '{dynamicsArticle?.Name}' → ART_DESL");
                _logger.LogInformation($"  dataAreaId: '{dynamicsArticle?.dataAreaId}' → ACT_CODE");
                _logger.LogInformation($"  itemBarCode: '{dynamicsArticle?.itemBarCode}' → ART_EANU");
                _logger.LogInformation($"  GrossWeight: {dynamicsArticle?.GrossWeight}g → ART_POIU");
                _logger.LogInformation($"  FactorColli: {dynamicsArticle?.FactorColli} → ART_COLI");
                _logger.LogInformation($"  FactorPallet: {dynamicsArticle?.FactorPallet} → ART_PAL");
                _logger.LogInformation($"  Category: '{dynamicsArticle?.Category}' → ART_CATEG");
                _logger.LogInformation($"  ItemGroupId: '{dynamicsArticle?.ItemGroupId}' → ART_GROUPE");

                _logger.LogInformation("Mapping réussi !");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du test de mapping");
            }
        }
    }
}