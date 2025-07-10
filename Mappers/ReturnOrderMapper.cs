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
                    // ========== IDENTIFIANTS PRINCIPAUX ==========
                    // ✅ TRAITEMENT UTF-8 : Société Référence
                    OpeCcli = _textProcessor.ProcessCode(dynamics.dataAreaId?.ToUpper()) ?? "BR",

                    // ✅ TRAITEMENT UTF-8 : Expected Receipt Number
                    ReaRfce = _textProcessor.ProcessCode(dynamics.ReturnItemNum),

                    // ✅ TRAITEMENT UTF-8 : N° Commande Vente
                    ReaRfti = _textProcessor.ProcessCode(dynamics.SalesId),

                    // Date de réception prévue
                    ReaDalp = FormatDateForXml(dynamics.ReturnDeadline),

                    // ========== FOURNISSEUR/CLIENT ==========
                    // ✅ TRAITEMENT UTF-8 : Code tiers client
                    ReaCtaf = _textProcessor.ProcessCode(dynamics.CustAccount),

                    // ✅ TRAITEMENT UTF-8 : Nom tiers client (max 100 caractères)
                    SalesName = _textProcessor.ProcessName(dynamics.SalesName, 100),

                    // ========== DÉTAILS LIGNE ==========
                    // Numéro ligne (numérique)
                    ReaNoLr = dynamics.LineNum,

                    // ✅ TRAITEMENT UTF-8 : Référence article
                    ArtCode = _textProcessor.ProcessCode(dynamics.ItemId),

                    // Quantité prévue (numérique)
                    ReaQtre = dynamics.ExpectedRetQty,

                    // ========== TRAÇABILITÉ (avec RG7 et RG8) ==========
                    // ✅ TRAITEMENT UTF-8 : Lot 1
                    ReaLot1 = ApplyLotRule(dynamics.inventBatchId, dynamics.ItemId, 1),

                    // ✅ TRAITEMENT UTF-8 : Lot 2
                    ReaLot2 = ApplyLotRule(dynamics.inventSerialId, dynamics.ItemId, 2),

                    // Date DLUO
                    ReaDluo = FormatDateForXml(dynamics.expDate),

                    // ========== SUPPORT ET COMMENTAIRES ==========
                    // Numéro support - pas disponible dans vos données Return Order
                    ReaNoSu = "",

                    // ✅ TRAITEMENT UTF-8 : Commentaires (max 255 caractères)
                    ReaCom = _textProcessor.ProcessName(dynamics.Notes, 255),

                    // ========== CODE QUALITÉ (RG4) ==========
                    // ✅ TRAITEMENT UTF-8 : Code qualité
                    QuaCode = ApplyQualityCodeRule(dynamics.ReturnDispositionCodeId),

                    // ========== RÉFÉRENCE RÉSERVATION ==========
                    // Référence réservation - pas dans vos données
                    ReaRfaf = "",

                    // ========== VALEURS FIXES ==========
                    ActCode = "COSMETIQUE", // VALEUR FIXE
                    Ccli = "BR", // VALEUR FIXE

                    // ========== CHAMPS AVEC RÈGLES DE GESTION ==========
                    // ✅ TRAITEMENT UTF-8 : Contrôle qualité (RG5)
                    ReaAlpha5 = ApplyQualityControlRule(dynamics.ItemId),

                    // ✅ TRAITEMENT UTF-8 : Type de réception (RG6)
                    ReaAlpha1 = ApplyReceptionTypeRule(dynamics.ReturnItemNum),

                    ReaAlpha11 = "NIVEAU3", // VALEUR FIXE
                    ReaAlpha12 = "NORMAL", // VALEUR FIXE

                    // ========== CHAMPS SUPPLÉMENTAIRES ==========
                    // ✅ TRAITEMENT UTF-8 : Statut commande vente
                    SalesStatus = _textProcessor.ProcessText(dynamics.SalesStatus),

                    // ✅ TRAITEMENT UTF-8 : Code disposition
                    ReturnDispositionCodeId = _textProcessor.ProcessCode(dynamics.ReturnDispositionCodeId),

                    // ✅ TRAITEMENT UTF-8 : Entrepôt destinataire
                    InventLocationId = _textProcessor.ProcessCode(dynamics.InventLocationId)
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
                "STANDARD" or "STD" or "RETURN_STANDARD" => "STD",
                "BLOCKED_LOGISTICS" or "BQLOG" or "RETURN_BLOCKED_LOG" => "BQLOG",
                "BLOCKED_QA1" or "BQQA1" or "RETURN_BLOCKED_QA1" => "BQQA1",
                "BLOCKED_QA2" or "BQQA2" or "RETURN_BLOCKED_QA2" => "BQQA2",
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
        /// Formate une date pour le XML avec gestion du cas spécial 1900-01-01
        /// </summary>
        private string FormatDateForXml(DateTime? date)
        {
            if (date == null || date == DateTime.MinValue)
                return "";

            // Si la date est 1900-01-01, considérer comme vide
            if (date.Value.Year == 1900)
                return "";

            return date.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Log des statistiques de traitement UTF-8 pour diagnostic
        /// </summary>
        private void LogProcessingStats(DynamicsReturnOrder dynamics, WinDevReturnOrder winDev)
        {
            var nameStats = _textProcessor.GetProcessingStats(dynamics.SalesName, winDev.SalesName);
            var codeStats = _textProcessor.GetProcessingStats(dynamics.SalesId, winDev.ReaRfti);

            if (nameStats.TransformationApplied || codeStats.TransformationApplied)
            {
                _logger.LogDebug($"Transformations UTF-8 appliquées pour le Return Order {dynamics.SalesId}:");

                if (nameStats.TransformationApplied)
                {
                    _logger.LogDebug($"  Nom client: '{dynamics.SalesName}' → '{winDev.SalesName}' ({nameStats.OriginalLength}→{nameStats.ProcessedLength} chars)");
                }

                if (codeStats.TransformationApplied)
                {
                    _logger.LogDebug($"  Code vente: '{dynamics.SalesId}' → '{winDev.ReaRfti}' ({codeStats.OriginalLength}→{codeStats.ProcessedLength} chars)");
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
                   $"  SalesName: '{dynamics.SalesName}' → '{_textProcessor.ProcessName(dynamics.SalesName, 100)}' (traité UTF-8, max 100 chars)\n" +
                   $"  LineNum: {dynamics.LineNum} → REA_DAT.REA_NoLR\n" +
                   $"  ItemId: '{dynamics.ItemId}' → REA_DAT.ART_CODE (traité UTF-8)\n" +
                   $"  ExpectedRetQty: {dynamics.ExpectedRetQty} → REA_DAT.REA_QTRE\n" +
                   $"  ReturnDispositionCodeId: '{dynamics.ReturnDispositionCodeId}' → REA_DAT.QUA_CODE (RG4, traité UTF-8)\n" +
                   $"  ReceptionType: '{ApplyReceptionTypeRule(dynamics.ReturnItemNum)}' (RG6, traité UTF-8)\n" +
                   $"  Notes: '{dynamics.Notes}' → '{_textProcessor.ProcessName(dynamics.Notes, 255)}' (traité UTF-8, max 255 chars)\n" +
                   $"  ACT_CODE: 'COSMETIQUE' (fixe)\n" +
                   $"  CCLI: 'BR' (fixe)\n" +
                   $"  ✅ TOUS LES CHAMPS TEXTE TRAITÉS AVEC NORMALISATION UTF-8";
        }
    }
}