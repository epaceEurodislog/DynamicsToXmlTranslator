using System;
using System.Globalization;
using DynamicsToXmlTranslator.Models;
using DynamicsToXmlTranslator.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DynamicsToXmlTranslator.Mappers
{
    public class ReturnOrderMapper
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ReturnOrderMapper> _logger;
        private readonly Utf8TextProcessor _textProcessor;

        public ReturnOrderMapper(IConfiguration configuration, ILogger<ReturnOrderMapper> logger, Utf8TextProcessor textProcessor)
        {
            _configuration = configuration;
            _logger = logger;
            _textProcessor = textProcessor;
        }

        /// <summary>
        /// Convertit un Return Order Dynamics en Return Order WINDEV selon le mapping fourni
        /// AVEC traitement UTF-8 des caractères spéciaux
        /// </summary>
        public WinDevReturnOrder? MapToWinDev(ReturnOrder returnOrder)
        {
            if (returnOrder?.DynamicsData == null)
            {
                _logger.LogWarning($"Return Order {returnOrder?.ReturnOrderId} n'a pas de données Dynamics");
                return null;
            }

            try
            {
                var dynamics = returnOrder.DynamicsData;

                var winDev = new WinDevReturnOrder
                {
                    // ========== VALEURS FIXES ==========
                    ActCode = "COSMETIQUE",
                    ReaCcli = "BR",

                    // ========== IDENTIFIANTS PRINCIPAUX ==========
                    ReaRfce = _textProcessor.ProcessCode(dynamics.SalesId),
                    ReaRfti = _textProcessor.ProcessCode(dynamics.ReturnItemNum),
                    ReaRfcl = dynamics.LineNum, // Même valeur que REA_NoLR
                    ReaTyat = "100", // 100 pour Return Orders
                    ReaDalp = FormatDateForXml(dynamics.ReturnDeadline),

                    // ========== FOURNISSEUR/CLIENT ==========
                    ReaCtaf = "BR" + _textProcessor.ProcessCode(dynamics.CustAccount),

                    // ========== DÉTAILS LIGNE ==========
                    ReaNoLr = dynamics.LineNum,
                    ArtCode = "BR" + _textProcessor.ProcessCode(dynamics.ItemId),
                    ReaQtre = dynamics.ExpectedRetQty,
                    ReaQtrc = dynamics.ExpectedRetQty, // Même valeur que REA_QTRE

                    // ========== TRAÇABILITÉ ==========
                    ReaLot1 = ApplyLotRule(dynamics.inventBatchId, dynamics.ItemId, 1),
                    ReaLot2 = ApplyLotRule(dynamics.inventSerialId, dynamics.ItemId, 2),
                    ReaDluo = FormatDateForXml(dynamics.expDate),

                    // ========== SUPPORT ET COMMENTAIRES ==========
                    ReaNoSu = "", // Pas disponible dans Return Orders
                    ReaCom = _textProcessor.ProcessName(dynamics.Notes, 255),

                    // ========== CODE QUALITÉ ==========
                    QuaCode = ApplyQualityCodeRule(dynamics.ReturnDispositionCodeId),

                    // ========== RÉFÉRENCE RÉSERVATION ==========
                    ReaRfaf = "", // Pas dans vos données

                    ReaAlpha2 = _textProcessor.ProcessCode(dynamics.LotID),

                    // ========== CHAMPS AVEC RÈGLES DE GESTION ==========
                    ReaAlpha5 = "",
                    ReaAlpha1 = "",
                    ReaAlpha11 = "NIVEAU3",
                    ReaAlpha12 = "NORMAL"
                };

                // Log des transformations UTF-8 si en mode debug
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    LogProcessingStats(dynamics, winDev);
                }

                _logger.LogDebug($"Return Order mappé: {dynamics.SalesId} → REA_RFTI: {winDev.ReaRfti}");
                return winDev;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors du mapping du Return Order {returnOrder.ReturnOrderId}");
                return null;
            }
        }

        /// <summary>
        /// RG4: Code Qualité avec traitement UTF-8
        /// </summary>
        private string ApplyQualityCodeRule(string? dispositionCode)
        {
            if (string.IsNullOrEmpty(dispositionCode))
                return "STD";

            // ✅ TRAITEMENT UTF-8 : Nettoyer avant traitement
            string cleanCode = _textProcessor.ProcessText(dispositionCode).ToUpper().Trim();

            return cleanCode switch
            {
                "STANDARD" or "STD" or "RETURN_STANDARD" or "Libéré" or "Dérogé" => "STD",
                "Bloqué" or "Recontrôle " or "En attente" => "BQQA",
                _ => "STD"
            };
        }

        /// <summary>
        /// RG5: Contrôle qualité avec traitement UTF-8
        /// </summary>
        private string ApplyQualityControlRule(string? itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return "Non";

            // ✅ TRAITEMENT UTF-8 : Nettoyer l'ID article
            string cleanItemId = _textProcessor.ProcessCode(itemId);

            // TODO: Implémenter la vérification de la catégorie article
            return "Non";
        }

        /// <summary>
        /// RG6: Type de réception avec traitement UTF-8
        /// </summary>
        private string ApplyReceptionTypeRule(string? supportNumber)
        {
            if (string.IsNullOrEmpty(supportNumber))
                return "ORDERS";

            // ✅ TRAITEMENT UTF-8 : Nettoyer le numéro de support
            string cleanSupportNumber = _textProcessor.ProcessCode(supportNumber);

            return !string.IsNullOrEmpty(cleanSupportNumber) ? "DESADV" : "ORDERS";
        }

        /// <summary>
        /// RG7 et RG8: Gestion des lots avec traitement UTF-8
        /// </summary>
        private string ApplyLotRule(string? lotValue, string? itemId, int lotNumber)
        {
            if (string.IsNullOrEmpty(lotValue) || string.IsNullOrEmpty(itemId))
                return "";

            // ✅ TRAITEMENT UTF-8 : Nettoyer la valeur du lot
            string cleanLot = _textProcessor.ProcessCode(lotValue);

            // TODO: Implémenter la vérification des paramètres de lot de l'article
            return cleanLot;
        }

        /// <summary>
        /// Formate une date pour le XML avec gestion du cas spécial 1900-01-01 (format YYYYMMDD)
        /// </summary>
        private string FormatDateForXml(DateTime? date)
        {
            if (date == null || date == DateTime.MinValue)
                return "";

            // Si la date est 1900-01-01, considérer comme vide
            if (date.Value.Year == 1900)
                return "";

            return date.Value.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Log des statistiques de traitement UTF-8 pour diagnostic
        /// </summary>
        private void LogProcessingStats(DynamicsReturnOrder dynamics, WinDevReturnOrder winDev)
        {
            var salesIdStats = _textProcessor.GetProcessingStats(dynamics.SalesId, winDev.ReaRfti);
            var notesStats = _textProcessor.GetProcessingStats(dynamics.Notes, winDev.ReaCom);

            if (salesIdStats.TransformationApplied || notesStats.TransformationApplied)
            {
                _logger.LogDebug($"Transformations UTF-8 appliquées pour le Return Order {dynamics.SalesId}:");

                if (salesIdStats.TransformationApplied)
                {
                    _logger.LogDebug($"  Sales ID: '{dynamics.SalesId}' → '{winDev.ReaRfti}' ({salesIdStats.OriginalLength}→{salesIdStats.ProcessedLength} chars)");
                }

                if (notesStats.TransformationApplied)
                {
                    _logger.LogDebug($"  Notes: '{dynamics.Notes}' → '{winDev.ReaCom}' ({notesStats.OriginalLength}→{notesStats.ProcessedLength} chars)");
                }
            }
        }

        /// <summary>
        /// Valide qu'un Return Order a les données minimales requises
        /// </summary>
        public bool ValidateReturnOrder(ReturnOrder returnOrder)
        {
            if (returnOrder?.DynamicsData == null)
                return false;

            var dynamics = returnOrder.DynamicsData;

            if (string.IsNullOrEmpty(dynamics.SalesId))
            {
                _logger.LogWarning("Return Order sans SalesId");
                return false;
            }

            if (string.IsNullOrEmpty(dynamics.ItemId))
            {
                _logger.LogWarning($"Return Order {dynamics.SalesId} sans ItemId");
                return false;
            }

            if (dynamics.ExpectedRetQty < 0)
            {
                _logger.LogWarning($"Return Order {dynamics.SalesId} avec quantité invalide: {dynamics.ExpectedRetQty}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Méthode utilitaire pour obtenir un résumé du Return Order mappé avec info UTF-8
        /// </summary>
        public string GetMappingSummary(ReturnOrder returnOrder)
        {
            if (returnOrder?.DynamicsData == null)
                return "Aucune donnée disponible";

            var dynamics = returnOrder.DynamicsData;

            return $"=== MAPPING RETURN ORDER (selon mapping + UTF-8) ===\n" +
                   $"API → SPEED:\n" +
                   $"  dataAreaId: '{dynamics.dataAreaId}' → OPE_DAT.OPE_CCLI (traité UTF-8)\n" +
                   $"  ReturnItemNum: '{dynamics.ReturnItemNum}' → REA_DAT.REA_RFCE (traité UTF-8)\n" +
                   $"  SalesId: '{dynamics.SalesId}' → REA_DAT.REA_RFTI (traité UTF-8)\n" +
                   $"  ReturnDeadline: '{dynamics.ReturnDeadline}' → REA_DAT.REA_DALP\n" +
                   $"  CustAccount: '{dynamics.CustAccount}' → REA_DAT.REA_CTAF (traité UTF-8)\n" +
                   $"  LineNum: {dynamics.LineNum} → REA_DAT.REA_NoLR\n" +
                   $"  ItemId: '{dynamics.ItemId}' → REA_DAT.ART_CODE (traité UTF-8)\n" +
                   $"  ExpectedRetQty: {dynamics.ExpectedRetQty} → REA_DAT.REA_QTRE\n" +
                   $"  ReturnDispositionCodeId: '{dynamics.ReturnDispositionCodeId}' → REA_DAT.QUA_CODE (RG4, traité UTF-8)\n" +
                   $"  ReceptionType: '{ApplyReceptionTypeRule(dynamics.ReturnItemNum)}' (RG6, traité UTF-8)\n" +
                   $"  Notes: '{dynamics.Notes}' → '{_textProcessor.ProcessName(dynamics.Notes, 255)}' (traité UTF-8, max 255 chars)\n" +
                   $"  LotID: '{dynamics.LotID}' → REA_DAT.REA_ALPHA2 (traité UTF-8)\n" +
                   $"  ACT_CODE: 'COSMETIQUE' (fixe)\n" +
                   $"  CCLI: 'BR' (fixe)\n" +
                   $"  ✅ TOUS LES CHAMPS TEXTE TRAITÉS AVEC NORMALISATION UTF-8";
        }
    }
}