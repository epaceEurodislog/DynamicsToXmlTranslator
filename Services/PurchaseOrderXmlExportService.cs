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
    public class PurchaseOrderXmlExportService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<PurchaseOrderXmlExportService> _logger;
        private readonly string _exportDirectory;
        private readonly PurchaseOrderDatabaseService _purchaseOrderDatabaseService;

        public PurchaseOrderXmlExportService(
            IConfiguration configuration,
            ILogger<PurchaseOrderXmlExportService> logger,
            PurchaseOrderDatabaseService purchaseOrderDatabaseService)
        {
            _configuration = configuration;
            _logger = logger;
            _purchaseOrderDatabaseService = purchaseOrderDatabaseService;

            _exportDirectory = _configuration["XmlExport:OutputDirectory"] ?? "exports";

            if (!Directory.Exists(_exportDirectory))
            {
                Directory.CreateDirectory(_exportDirectory);
                _logger.LogInformation($"Répertoire d'export créé : {_exportDirectory}");
            }
        }

        /// <summary>
        /// Exporte une liste de Purchase Orders WINDEV en fichier XML
        /// </summary>
        public async Task<string?> ExportToXmlAsync(List<WinDevPurchaseOrder> purchaseOrders, List<int> originalPurchaseOrderIds = null, string fileNamePrefix = "RECAT_COSMETIQUE_PURCHASE_ORDERS")
        {
            if (purchaseOrders == null || !purchaseOrders.Any())
            {
                _logger.LogWarning("Aucun Purchase Order à exporter");
                return null;
            }

            try
            {
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
                string fileName = $"{fileNamePrefix}_{timestamp}.XML";
                string filePath = Path.Combine(_exportDirectory, fileName);

                var winDevPurchaseTable = new WinDevPurchaseTable
                {
                    PurchaseOrders = purchaseOrders
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

                using (var writer = XmlWriter.Create(filePath, settings))
                {
                    writer.WriteStartDocument(true);

                    var serializer = new XmlSerializer(typeof(WinDevPurchaseTable));
                    var namespaces = new XmlSerializerNamespaces();
                    namespaces.Add("", "");

                    serializer.Serialize(writer, winDevPurchaseTable, namespaces);
                }

                // ✅ NOUVEAU : Post-traitement pour forcer les balises fermantes
                await ForceClosingTagsAsync(filePath);

                _logger.LogInformation($"Export XML Purchase Orders réussi : {fileName} ({purchaseOrders.Count} Purchase Orders)");

                if (originalPurchaseOrderIds != null && originalPurchaseOrderIds.Any())
                {
                    await _purchaseOrderDatabaseService.MarkPurchaseOrdersAsExportedAsync(originalPurchaseOrderIds, fileName);
                }

                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'export XML Purchase Orders");
                throw;
            }
        }

        /// <summary>
        /// Exporte les Purchase Orders par lots
        /// </summary>
        public async Task<List<string>> ExportInBatchesAsync(List<WinDevPurchaseOrder> purchaseOrders, List<int> originalPurchaseOrderIds = null, int batchSize = 1000)
        {
            var exportedFiles = new List<string>();

            if (purchaseOrders == null || !purchaseOrders.Any())
            {
                _logger.LogWarning("Aucun Purchase Order à exporter");
                return exportedFiles;
            }

            try
            {
                var batches = purchaseOrders
                    .Select((purchaseOrder, index) => new { purchaseOrder, index })
                    .GroupBy(x => x.index / batchSize)
                    .Select(g => g.Select(x => x.purchaseOrder).ToList())
                    .ToList();

                List<List<int>>? originalIdBatches = null;
                if (originalPurchaseOrderIds != null && originalPurchaseOrderIds.Any())
                {
                    originalIdBatches = originalPurchaseOrderIds
                        .Select((id, index) => new { id, index })
                        .GroupBy(x => x.index / batchSize)
                        .Select(g => g.Select(x => x.id).ToList())
                        .ToList();
                }

                _logger.LogInformation($"Export de {purchaseOrders.Count} Purchase Orders en {batches.Count} fichiers");

                string baseTimestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);

                for (int i = 0; i < batches.Count; i++)
                {
                    var batch = batches[i];
                    var batchNumber = i + 1;
                    var batchIds = originalIdBatches?[i];

                    _logger.LogInformation($"Export du lot {batchNumber}/{batches.Count} ({batch.Count} Purchase Orders)");

                    string fileNamePrefix = $"RECAT_COSMETIQUE_PURCHASE_ORDERS_LOT{batchNumber:D3}_{baseTimestamp}";
                    string fileName = $"{fileNamePrefix}.XML";
                    string filePath = Path.Combine(_exportDirectory, fileName);

                    var winDevPurchaseTable = new WinDevPurchaseTable
                    {
                        PurchaseOrders = batch
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

                    using (var writer = XmlWriter.Create(filePath, settings))
                    {
                        writer.WriteStartDocument(true);

                        var serializer = new XmlSerializer(typeof(WinDevPurchaseTable));
                        var namespaces = new XmlSerializerNamespaces();
                        namespaces.Add("", "");

                        serializer.Serialize(writer, winDevPurchaseTable, namespaces);
                    }

                    // ✅ NOUVEAU : Post-traitement pour forcer les balises fermantes
                    await ForceClosingTagsAsync(filePath);

                    if (File.Exists(filePath))
                    {
                        exportedFiles.Add(filePath);
                        _logger.LogInformation($"Lot {batchNumber} exporté : {fileName}");

                        if (batchIds != null && batchIds.Any())
                        {
                            await _purchaseOrderDatabaseService.MarkPurchaseOrdersAsExportedAsync(batchIds, fileName);
                        }
                    }
                }

                return exportedFiles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'export Purchase Orders par lots");
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

                // Remplacer les balises auto-fermantes par des balises fermantes
                var tagsToReplace = new[]
                {
                    "REA_LOT1", "REA_LOT2", "REA_DLUO", "REA_NoSU", "REA_COM", "REA_RFAF",
                    "PurchTableVersion", "INT3PLStatus", "ConfirmedDlv", "DeliveryDate",
                    "InventLocationId", "DocumentState", "PurchName"
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
            var testPurchaseOrders = new List<WinDevPurchaseOrder>();
            return await ExportToXmlAsync(testPurchaseOrders, null, "RECAT_COSMETIQUE_PURCHASE_ORDERS_TEST_VIDE");
        }
    }
}