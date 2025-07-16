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
    public class ReturnOrderXmlExportService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ReturnOrderXmlExportService> _logger;
        private readonly string _exportDirectory;
        private readonly ReturnOrderDatabaseService _returnOrderDatabaseService;

        public ReturnOrderXmlExportService(
            IConfiguration configuration,
            ILogger<ReturnOrderXmlExportService> logger,
            ReturnOrderDatabaseService returnOrderDatabaseService)
        {
            _configuration = configuration;
            _logger = logger;
            _returnOrderDatabaseService = returnOrderDatabaseService;

            _exportDirectory = _configuration["XmlExport:OutputDirectory"] ?? "exports";

            if (!Directory.Exists(_exportDirectory))
            {
                Directory.CreateDirectory(_exportDirectory);
                _logger.LogInformation($"Répertoire d'export créé : {_exportDirectory}");
            }
        }

        /// <summary>
        /// Exporte une liste de Return Orders WINDEV en fichier XML
        /// </summary>
        public async Task<string?> ExportToXmlAsync(List<WinDevReturnOrder> returnOrders, List<int>? originalReturnOrderIds = null, string fileNamePrefix = "RECAT_COSMETIQUE_RETURN_ORDERS")
        {
            if (returnOrders == null || !returnOrders.Any())
            {
                _logger.LogWarning("Aucun Return Order à exporter");
                return null;
            }

            try
            {
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
                string fileName = $"{fileNamePrefix}_{timestamp}.XML";
                string filePath = Path.Combine(_exportDirectory, fileName);

                var winDevReturnTable = new WinDevReturnTable
                {
                    ReturnOrders = returnOrders
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

                    var serializer = new XmlSerializer(typeof(WinDevReturnTable));
                    var namespaces = new XmlSerializerNamespaces();
                    namespaces.Add("", "");

                    serializer.Serialize(writer, winDevReturnTable, namespaces);
                }

                // ✅ NOUVEAU : Post-traitement pour forcer les balises fermantes
                await ForceClosingTagsAsync(filePath);

                _logger.LogInformation($"Export XML Return Orders réussi : {fileName} ({returnOrders.Count} Return Orders)");

                if (originalReturnOrderIds != null && originalReturnOrderIds.Any())
                {
                    await _returnOrderDatabaseService.MarkReturnOrdersAsExportedAsync(originalReturnOrderIds, fileName);
                }

                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'export XML Return Orders");
                throw;
            }
        }

        /// <summary>
        /// Exporte les Return Orders par lots
        /// </summary>
        public async Task<List<string>> ExportInBatchesAsync(List<WinDevReturnOrder> returnOrders, List<int>? originalReturnOrderIds = null, int batchSize = 1000)
        {
            var exportedFiles = new List<string>();

            if (returnOrders == null || !returnOrders.Any())
            {
                _logger.LogWarning("Aucun Return Order à exporter");
                return exportedFiles;
            }

            try
            {
                var batches = returnOrders
                    .Select((returnOrder, index) => new { returnOrder, index })
                    .GroupBy(x => x.index / batchSize)
                    .Select(g => g.Select(x => x.returnOrder).ToList())
                    .ToList();

                List<List<int>>? originalIdBatches = null;
                if (originalReturnOrderIds != null && originalReturnOrderIds.Any())
                {
                    originalIdBatches = originalReturnOrderIds
                        .Select((id, index) => new { id, index })
                        .GroupBy(x => x.index / batchSize)
                        .Select(g => g.Select(x => x.id).ToList())
                        .ToList();
                }

                _logger.LogInformation($"Export de {returnOrders.Count} Return Orders en {batches.Count} fichiers");

                string baseTimestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);

                for (int i = 0; i < batches.Count; i++)
                {
                    var batch = batches[i];
                    var batchNumber = i + 1;
                    var batchIds = originalIdBatches?[i];

                    _logger.LogInformation($"Export du lot {batchNumber}/{batches.Count} ({batch.Count} Return Orders)");

                    string fileNamePrefix = $"RECAT_COSMETIQUE_RETURN_ORDERS_LOT{batchNumber:D3}_{baseTimestamp}";
                    string fileName = $"{fileNamePrefix}.XML";
                    string filePath = Path.Combine(_exportDirectory, fileName);

                    var winDevReturnTable = new WinDevReturnTable
                    {
                        ReturnOrders = batch
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

                        var serializer = new XmlSerializer(typeof(WinDevReturnTable));
                        var namespaces = new XmlSerializerNamespaces();
                        namespaces.Add("", "");

                        serializer.Serialize(writer, winDevReturnTable, namespaces);
                    }

                    // ✅ NOUVEAU : Post-traitement pour forcer les balises fermantes
                    await ForceClosingTagsAsync(filePath);

                    if (File.Exists(filePath))
                    {
                        exportedFiles.Add(filePath);
                        _logger.LogInformation($"Lot {batchNumber} exporté : {fileName}");

                        if (batchIds != null && batchIds.Any())
                        {
                            await _returnOrderDatabaseService.MarkReturnOrdersAsExportedAsync(batchIds, fileName);
                        }
                    }
                }

                return exportedFiles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'export Return Orders par lots");
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
                    "SalesStatus", "ReturnDispositionCodeID", "SalesName", "InventLocationId"
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
            var testReturnOrders = new List<WinDevReturnOrder>();
            return await ExportToXmlAsync(testReturnOrders, null, "RECAT_COSMETIQUE_RETURN_ORDERS_TEST_VIDE");
        }
    }
}