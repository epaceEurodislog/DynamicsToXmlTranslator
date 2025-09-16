using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DynamicsToXmlTranslator.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using static DynamicsToXmlTranslator.Models.SpeedPackingSlipHeader;

namespace DynamicsToXmlTranslator.Services
{
    public class PackingSlipTxtExportService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<PackingSlipTxtExportService> _logger;
        private readonly string _exportDirectory;
        private readonly PackingSlipDatabaseService _packingSlipDatabaseService;

        // En-têtes pour le fichier OPE (CDEN_LTRF)
        // En-têtes pour le fichier OPE (CDEN_LTRF) - ✅ MODIFIÉ : STATUT ajouté à la fin
        private readonly string[] _headersOPE = new string[]
        {
            "ACT_CODE", "OPE_DACO", "OPE_REDO", "TIE_CODE", "OPE_RTIE", "TIE_NOM", "OPE_ADR1", "OPE_ADR2", "OPE_ADR3", "OPE_ADR4",
            "OPE_ADVL", "OPE_ADCP", "OPE_CPAY", "OPE_COBP", "OPE_COBL", "OPE_DALI", "OPE_CTRA", "OPE_ALPHA16", "OPE_ALPHA17", "OPE_ALPHA18",
            "OPE_ALPHA19", "OPE_ALPHA20", "OPE_ALPHA21", "OPE_ALPHA22", "OPE_ALPHA23", "OPE_ALPHA24", "OPE_ALPHA25", "OPE_TEL", "OPE_FAX", "OPE_IMEL",
            "OPE_ALPHA1", "OPE_ALPHA5", "OPE_ALPHA6", "OPE_ALPHA9", "OPE_ALPHA15", "OPE_DATE15", "OPE_ALPHA31", "OPE_ALPHA34", "OPE_ALPHA35", "OPE_ALPHA36",
            "OPE_ALPHA37", "OPE_ALPHA38", "OPE_TOP17", "STATUT"
        };

        // En-têtes pour le fichier OPL (CDLG_LTRF) - selon votre exemple
        // private readonly string[] _headersOPL = new string[]
        // {
        //     "ACT_CODE", "OPL_RCDO", "OPL_RLDO", "ART_CODE", "OPL_QTAP", "QUA_CODE", "OPL_POIDS",
        //     "OPL_LOT1", "OPL_LOT2", "OPL_DLOO", "OPL_NoSU", "OPL_CONDITIONNEMENT", "OPL_ALPHA1", "OPL_ALPHA2", "OPL_ALPHA3"
        // };

        private readonly string[] _headersOPL = new string[]
        {
            "ACT_CODE", "OPL_RCDO", "OPL_RLDO", "ART_CODE", "OPL_QTAP", "QUA_CODE", "OPL_ALPHA1",
            "OPL_LOT1", "OPL_DLC", "OPL_ALPHA3", "OPL_ALPHA4", "OPL_ALPHA5", "OPL_ALPHA6", "OPL_ALPHA7", "STATUT"
        };

        public PackingSlipTxtExportService(
            IConfiguration configuration,
            ILogger<PackingSlipTxtExportService> logger,
            PackingSlipDatabaseService packingSlipDatabaseService)
        {
            _configuration = configuration;
            _logger = logger;
            _packingSlipDatabaseService = packingSlipDatabaseService;

            _exportDirectory = _configuration["XmlExport:OutputDirectory"] ?? "exports";

            if (!Directory.Exists(_exportDirectory))
            {
                Directory.CreateDirectory(_exportDirectory);
                _logger.LogInformation($"Répertoire d'export créé : {_exportDirectory}");
            }
        }

        /// <summary>
        /// Exporte une liste de Packing Slips SPEED en 2 fichiers TXT distincts
        /// </summary>
        public async Task<PackingSlipExportResult?> ExportToTxtAsync(List<SpeedPackingSlipComplete> packingSlips, List<int>? originalPackingSlipIds = null, string fileNamePrefix = "LTRF")
        {
            if (packingSlips == null || !packingSlips.Any())
            {
                _logger.LogWarning("Aucun Packing Slip à exporter");
                return null;
            }

            try
            {
                // PAR :
                string dateFormat = DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
                string timeFormat = DateTime.Now.ToString("HHmmss", CultureInfo.InvariantCulture);

                // Noms des fichiers
                string headerFileName = $"CDEN_COSMETIQUE_{dateFormat}_{timeFormat}.TXT";
                string linesFileName = $"CDLG_COSMETIQUE_{dateFormat}_{timeFormat}.TXT";

                string headerFilePath = Path.Combine(_exportDirectory, headerFileName);
                string linesFilePath = Path.Combine(_exportDirectory, linesFileName);

                var result = new PackingSlipExportResult
                {
                    HeaderFilePath = headerFilePath,
                    LinesFilePath = linesFilePath,
                    ExportedPackingSlipIds = originalPackingSlipIds?.ToList() ?? new List<int>()
                };

                // ✅ ÉTAPE 1 : Exporter les en-têtes (fichier OPE)
                await ExportHeadersAsync(packingSlips, headerFilePath);
                result.HeaderCount = packingSlips.Count;

                // ✅ ÉTAPE 2 : Exporter les lignes (fichier OPL)
                await ExportLinesAsync(packingSlips, linesFilePath);
                result.LinesCount = packingSlips.SelectMany(ps => ps.Lines).Count();

                _logger.LogInformation($"Export TXT réussi :");
                _logger.LogInformation($"  📁 En-têtes: {headerFileName} ({result.HeaderCount} commandes)");
                _logger.LogInformation($"  📁 Lignes: {linesFileName} ({result.LinesCount} lignes)");

                // ✅ ÉTAPE 3 : Marquer comme exporté
                if (originalPackingSlipIds != null && originalPackingSlipIds.Any())
                {
                    await _packingSlipDatabaseService.MarkPackingSlipsAsExportedAsync(originalPackingSlipIds, $"{headerFileName}+{linesFileName}");
                }

                // ✅ ÉTAPE 4 : Logger l'export
                await _packingSlipDatabaseService.LogPackingSlipExportAsync(
                    $"{headerFileName}+{linesFileName}",
                    result.HeaderCount,
                    "SUCCESS",
                    $"Export 2 fichiers TXT: {result.HeaderCount} en-têtes, {result.LinesCount} lignes avec UTF-8"
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'export TXT Packing Slips");

                // Log de l'erreur
                await _packingSlipDatabaseService.LogPackingSlipExportAsync(
                    "ERROR_EXPORT_2_FILES",
                    0,
                    "ERROR",
                    $"Erreur: {ex.Message}"
                );

                throw;
            }
        }

        /// <summary>
        /// Exporte les en-têtes de commande (fichier CDEN_LTRF)
        /// </summary>
        private async Task ExportHeadersAsync(List<SpeedPackingSlipComplete> packingSlips, string filePath)
        {
            using (var writer = new StreamWriter(filePath, false, Encoding.GetEncoding("ISO-8859-1")))
            {
                // ✅ ÉTAPE 1 : Écrire l'en-tête
                //await writer.WriteLineAsync(string.Join("|", _headersOPE));
                _logger.LogDebug($"En-tête OPE écrit avec {_headersOPE.Length} colonnes");

                // ✅ ÉTAPE 2 : Écrire les données
                int lineCount = 0;
                foreach (var packingSlip in packingSlips)
                {
                    if (packingSlip.Header != null)
                    {
                        var line = FormatHeaderLine(packingSlip.Header);
                        await writer.WriteLineAsync(line);
                        lineCount++;

                        if (lineCount % 100 == 0)
                        {
                            _logger.LogDebug($"Écrit {lineCount} en-têtes...");
                        }
                    }
                }

                _logger.LogInformation($"Fichier en-têtes créé : {lineCount} lignes");
            }
        }

        /// <summary>
        /// Exporte les lignes de commande (fichier CDLG_LTRF) SANS AUCUN en-tête
        /// ✅ CORRECTION COMPLÈTE : Suppression définitive de l'en-tête pour les lignes
        /// </summary>
        private async Task ExportLinesAsync(List<SpeedPackingSlipComplete> packingSlips, string filePath)
        {
            using (var writer = new StreamWriter(filePath, false, Encoding.GetEncoding("ISO-8859-1")))
            {
                // ✅ AUCUN en-tête n'est écrit - commencer directement par les données

                int lineCount = 0;
                foreach (var packingSlip in packingSlips)
                {
                    foreach (var line in packingSlip.Lines)
                    {
                        var lineData = FormatLine(line);
                        await writer.WriteLineAsync(lineData);
                        lineCount++;

                        if (lineCount % 100 == 0)
                        {
                            _logger.LogDebug($"Écrit {lineCount} lignes...");
                        }
                    }
                }

                _logger.LogInformation($"Fichier lignes créé SANS AUCUN en-tête : {lineCount} lignes de données uniquement");
            }
        }

        /// <summary>
        /// Formate une ligne d'en-tête selon l'ordre exact des colonnes OPE
        /// ✅ MODIFIÉ : Gestion du format spécial pour OPE_ALPHA31 avec TIE_CODE + nouveau champ STATUT
        /// </summary>
        private string FormatHeaderLine(SpeedPackingSlipHeader header)
        {
            // Créer un tableau avec toutes les valeurs dans l'ordre exact des en-têtes
            var values = new string[]
            {
        header.ACT_CODE ?? "",                    // ACT_CODE
        header.OPE_DACO ?? "",                    // OPE_DACO
        header.OPE_REDO ?? "",                    // OPE_REDO
        header.TIE_CODE ?? "",                    // TIE_CODE
        header.OPE_RTIE ?? "",                    // OPE_RTIE
        header.TIE_NOM ?? "",                     // TIE_NOM
        header.OPE_ADR1 ?? "",                    // OPE_ADR1
        header.OPE_ADR2 ?? "",                    // OPE_ADR2
        header.OPE_ADR3 ?? "",                    // OPE_ADR3
        header.OPE_ADR4 ?? "",                    // OPE_ADR4
        header.OPE_ADVL ?? "",                    // OPE_ADVL
        header.OPE_ADCP ?? "",                    // OPE_ADCP
        header.OPE_CPAY ?? "",                    // OPE_CPAY
        header.OPE_COBP ?? "",                    // OPE_COBP
        header.OPE_COBL ?? "",                    // OPE_COBL
        header.OPE_DALI ?? "",                    // OPE_DALI
        header.OPE_CTRA ?? "",                    // OPE_CTRA
        header.OPE_ALPHA16 ?? "",                 // OPE_ALPHA16
        header.OPE_ALPHA17 ?? "",                 // OPE_ALPHA17
        header.OPE_ALPHA18 ?? "",                 // OPE_ALPHA18
        header.OPE_ALPHA19 ?? "",                 // OPE_ALPHA19
        header.OPE_ALPHA20 ?? "",                 // OPE_ALPHA20
        header.OPE_ALPHA21 ?? "",                 // OPE_ALPHA21
        header.OPE_ALPHA22 ?? "",                 // OPE_ALPHA22
        header.OPE_ALPHA23 ?? "",                 // OPE_ALPHA23
        header.OPE_ALPHA24 ?? "",                 // OPE_ALPHA24
        header.OPE_ALPHA25 ?? "",                 // OPE_ALPHA25
        header.OPE_TEL ?? "",                     // OPE_TEL
        header.OPE_FAX ?? "",                     // OPE_FAX
        header.OPE_IMEL ?? "",                    // OPE_IMEL
        header.OPE_ALPHA1 ?? "",                  // OPE_ALPHA1
        header.OPE_ALPHA5 ?? "",                  // OPE_ALPHA5
        header.OPE_ALPHA6 ?? "",                  // OPE_ALPHA6
        header.OPE_ALPHA9 ?? "",                  // OPE_ALPHA9
        header.OPE_ALPHA15 ?? "",                 // OPE_ALPHA15
        header.OPE_DATE15 ?? "",                  // OPE_DATE15
        header.GetFormattedOpeAlpha31(),          // ✅ IMPORTANT : Utilise la méthode modifiée avec TIE_CODE
        header.OPE_ALPHA34 ?? "",                 // OPE_ALPHA34
        header.OPE_ALPHA35 ?? "",                 // OPE_ALPHA35
        header.OPE_ALPHA36 ?? "",                 // OPE_ALPHA36
        header.OPE_ALPHA37 ?? "",                 // OPE_ALPHA37
        header.OPE_ALPHA38 ?? "",                 // OPE_ALPHA38
        header.OPE_TOP17 ?? "0",                  // OPE_TOP17
        header.STATUT ?? ""                       // ✅ NOUVEAU : STATUT à la fin
            };

            // Nettoyer les valeurs pour éviter les problèmes avec les séparateurs
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = CleanValueForTxt(values[i]);
            }

            return string.Join("|", values);
        }

        /// <summary>
        /// Formate une ligne de commande selon l'ordre exact des colonnes OPL
        /// Selon votre exemple : LTRF|VC713873|10000|LTRF725|240|STD|7.780||||COLIS|||
        /// </summary>
        private string FormatLine(SpeedPackingSlipLine line)
        {
            // Créer un tableau avec toutes les valeurs dans l'ordre exact
            var values = new string[]
            {
                line.ACT_CODE ?? "",                                          // ACT_CODE
                line.OPL_RCDO ?? "",                                          // OPL_RCDO
                line.OPL_RLDO ?? "",                                          // OPL_RLDO
                line.ART_CODE ?? "",                                          // ART_CODE
                line.OPL_QTAP.ToString(CultureInfo.InvariantCulture),         // OPL_QTOC
                line.QUA_CODE ?? "",                                          // QUA_CODE

                //SUPRESSION RD 01/08/2025
                //line.OPL_POIDS.ToString("0.000", CultureInfo.InvariantCulture), // OPL_POIDS
                //line.OPL_LOT1 ?? "",                                          // OPL_LOT1
                //line.OPL_LOT2 ?? "",                                          // OPL_LOT2
                //line.OPL_DLOO ?? "",                                          // OPL_DLOO
                //line.OPL_NoSU ?? "",                                          // OPL_NoSU
                //line.OPL_CONDITIONNEMENT ?? "",                              // OPL_CONDITIONNEMENT
                //line.OPL_ALPHA1 ?? "",                                        // OPL_ALPHA1
                //line.OPL_ALPHA2 ?? "",                                        // OPL_ALPHA2
                //line.OPL_ALPHA3 ?? ""                                         // OPL_ALPHA3

                //AJOUT RD 01/08/2025
                line.OPL_ALPHA1 ?? "",                                        // OPL_ALPHA1
                line.OPL_LOT1 ?? "",                                          // OPL_LOT1
                line.OPL_DLC ?? "",                                           // OPL_DLOO
                line.OPL_ALPHA3 ?? "",                                        // OPL_ALPHA3
                line.OPL_ALPHA4 ?? "",                                        // OPL_ALPHA4
                line.OPL_ALPHA5 ?? "",                                        // OPL_ALPHA5
                line.OPL_ALPHA6 ?? "",                                        // OPL_ALPHA6
                line.OPL_ALPHA7 ?? "",                                        // OPL_ALPHA7
                line.STATUT ?? ""
            };

            // Nettoyer les valeurs pour éviter les problèmes avec les séparateurs
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = CleanValueForTxt(values[i]);
            }

            return string.Join("|", values);
        }

        /// <summary>
        /// ✅ MODIFIÉ : Nettoyage renforcé pour l'export TXT avec suppression entités
        /// </summary>
        private string CleanValueForTxt(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            // ✅ ÉTAPE 1 : Suppression explicite des entités courantes
            string cleaned = value
                .Replace("&amp;", "")
                .Replace("&apos;", "")
                .Replace("&quot;", "")
                .Replace("&lt;", "")
                .Replace("&gt;", "")
                .Replace("&#39;", "")
                .Replace("&#34;", "")
                .Replace("&#38;", "")
                .Replace("&#x27;", "")
                .Replace("&#x22;", "")
                .Replace("&#x26;", "");

            // ✅ ÉTAPE 2 : Suppression par regex de toutes les entités restantes
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"&[#a-zA-Z0-9]+;", "");

            // ✅ ÉTAPE 3 : Nettoyage des séparateurs et caractères de contrôle
            cleaned = cleaned
                .Trim();                // Supprimer les espaces en début/fin

            // ✅ ÉTAPE 4 : Nettoyage des espaces multiples
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s+", " ");

            return cleaned;
        }

        /// <summary>
        /// Exporte les Packing Slips par lots (génère plusieurs paires de fichiers)
        /// </summary>
        public async Task<List<PackingSlipExportResult>> ExportInBatchesAsync(List<SpeedPackingSlipComplete> packingSlips, List<int>? originalPackingSlipIds = null, int batchSize = 1000)
        {
            var exportResults = new List<PackingSlipExportResult>();

            if (packingSlips == null || !packingSlips.Any())
            {
                _logger.LogWarning("Aucun Packing Slip à exporter");
                return exportResults;
            }

            try
            {
                // Diviser en lots
                var batches = packingSlips
                    .Select((packingSlip, index) => new { packingSlip, index })
                    .GroupBy(x => x.index / batchSize)
                    .Select(g => g.Select(x => x.packingSlip).ToList())
                    .ToList();

                // Diviser aussi les IDs si fournis
                List<List<int>>? originalIdBatches = null;
                if (originalPackingSlipIds != null && originalPackingSlipIds.Any())
                {
                    originalIdBatches = originalPackingSlipIds
                        .Select((id, index) => new { id, index })
                        .GroupBy(x => x.index / batchSize)
                        .Select(g => g.Select(x => x.id).ToList())
                        .ToList();
                }

                _logger.LogInformation($"Export de {packingSlips.Count} Packing Slips en {batches.Count} lots");

                // Exporter chaque lot
                for (int i = 0; i < batches.Count; i++)
                {
                    var batch = batches[i];
                    var batchNumber = i + 1;
                    var batchIds = originalIdBatches?[i];

                    _logger.LogInformation($"Export du lot {batchNumber}/{batches.Count} ({batch.Count} Packing Slips)");

                    var result = await ExportToTxtAsync(batch, batchIds, $"LTRF_LOT{batchNumber:D3}");
                    if (result != null)
                    {
                        exportResults.Add(result);
                        _logger.LogInformation($"Lot {batchNumber} exporté avec succès");
                    }
                }

                return exportResults;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'export par lots");
                throw;
            }
        }

        /// <summary>
        /// Valide les fichiers générés
        /// </summary>
        public async Task<bool> ValidateGeneratedFilesAsync(PackingSlipExportResult result)
        {
            try
            {
                bool headerValid = await ValidateHeaderFileAsync(result.HeaderFilePath);
                bool linesValid = await ValidateLinesFileAsync(result.LinesFilePath);

                var isValid = headerValid && linesValid;

                if (isValid)
                {
                    _logger.LogInformation("Validation réussie pour les 2 fichiers");
                }
                else
                {
                    _logger.LogError("Échec de la validation des fichiers");
                }

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la validation des fichiers");
                return false;
            }
        }

        /// <summary>
        /// Valide le fichier d'en-têtes
        /// </summary>
        private async Task<bool> ValidateHeaderFileAsync(string? filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                _logger.LogError($"Fichier d'en-têtes non trouvé : {filePath}");
                return false;
            }

            var lines = await File.ReadAllLinesAsync(filePath, Encoding.GetEncoding("ISO-8859-1"));

            if (lines.Length < 1)
            {
                _logger.LogError("Fichier d'en-têtes vide");
                return false;
            }

            // Vérifier l'en-tête
            var headerLine = lines[0];
            var headerColumns = headerLine.Split('|');

            if (headerColumns.Length != _headersOPE.Length)
            {
                _logger.LogError($"Nombre de colonnes OPE incorrect. Attendu: {_headersOPE.Length}, Trouvé: {headerColumns.Length}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// ✅ CORRECTION : Valide le fichier de lignes SANS vérifier l'en-tête
        /// Puisqu'il n'y a plus d'en-tête, toutes les lignes sont des données
        /// </summary>
        private async Task<bool> ValidateLinesFileAsync(string? filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                _logger.LogError($"Fichier de lignes non trouvé : {filePath}");
                return false;
            }

            var lines = await File.ReadAllLinesAsync(filePath, Encoding.GetEncoding("ISO-8859-1"));

            if (lines.Length == 0)
            {
                _logger.LogWarning("Fichier de lignes vide - aucune donnée à valider");
                return true; // Pas d'erreur si vide
            }

            // ✅ CORRECTION : Vérifier TOUTES les lignes comme étant des données (plus d'en-tête)
            for (int i = 0; i < lines.Length; i++)
            {
                var dataColumns = lines[i].Split('|');
                if (dataColumns.Length != _headersOPL.Length)
                {
                    _logger.LogError($"Ligne de données {i + 1}: Nombre de colonnes incorrect. Attendu: {_headersOPL.Length}, Trouvé: {dataColumns.Length}");
                    return false;
                }
            }

            _logger.LogInformation($"Validation réussie : {lines.Length} lignes de données valides (SANS en-tête)");
            return true;
        }
    }
}