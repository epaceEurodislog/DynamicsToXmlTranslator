using System;
using System.Globalization;
using DynamicsToXmlTranslator.Models;
using DynamicsToXmlTranslator.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DynamicsToXmlTranslator.Mappers
{
    public class TransferOrderMapper
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<TransferOrderMapper> _logger;
        private readonly Utf8TextProcessor _textProcessor;

        public TransferOrderMapper(IConfiguration configuration, ILogger<TransferOrderMapper> logger, Utf8TextProcessor textProcessor)
        {
            _configuration = configuration;
            _logger = logger;
            _textProcessor = textProcessor;
        }

        /// <summary>
        /// Convertit un Transfer Order Dynamics en Transfer Order WINDEV selon le mapping fourni
        /// AVEC traitement UTF-8 des caractères spéciaux
        /// </summary>
        public WinDevTransferOrder? MapToWinDev(TransferOrder transferOrder)
        {
            if (transferOrder?.DynamicsData == null)
            {
                _logger.LogWarning($"Transfer Order {transferOrder?.TransferOrderId} n'a pas de données Dynamics");
                return null;
            }

            try
            {
                var dynamics = transferOrder.DynamicsData;

                var winDev = new WinDevTransferOrder
                {
                    // ========== IDENTIFIANTS PRINCIPAUX ==========
                    // ✅ TRAITEMENT UTF-8 : Expected Receipt Number
                    ReaRfce = _textProcessor.ProcessCode(dynamics.ExpectedReceiptNumber),

                    // ✅ TRAITEMENT UTF-8 : N° Commande Transfer
                    ReaRfti = _textProcessor.ProcessCode(dynamics.TransferId),

                    // Date de réception prévue
                    ReaDalp = FormatDateForXml(dynamics.ReceiveDate),

                    // ========== FOURNISSEUR/CLIENT ==========
                    // ✅ TRAITEMENT UTF-8 : Code tiers fournisseur (RG2)
                    ReaCtaf = ApplySupplierAccountRule(dynamics.TransferId),

                    // ✅ TRAITEMENT UTF-8 : Nom tiers (RG2)
                    XxxName = ApplySupplierNameRule(dynamics.TransferId),

                    // ========== DÉTAILS LIGNE ==========
                    // Numéro ligne (numérique)
                    ReaNoLr = dynamics.LineNum,

                    // ✅ TRAITEMENT UTF-8 : Référence article
                    ArtCode = _textProcessor.ProcessCode(dynamics.ItemId),

                    // Quantité prévue (numérique, RG3)
                    ReaQtre = dynamics.QtyShipped,

                    // ========== TRAÇABILITÉ (avec RG7 et RG8) ==========
                    // ✅ TRAITEMENT UTF-8 : Lot 1
                    ReaLot1 = ApplyLotRule(dynamics.inventBatchId, dynamics.ItemId, 1),

                    // ✅ TRAITEMENT UTF-8 : Lot 2
                    ReaLot2 = ApplyLotRule(dynamics.inventSerialId, dynamics.ItemId, 2),

                    // Date DLUO
                    ReaDluo = FormatDateForXml(dynamics.expDate),

                    // ========== SUPPORT ET COMMENTAIRES ==========
                    // ✅ TRAITEMENT UTF-8 : Numéro support (SSCC entrant)
                    ReaNoSu = _textProcessor.ProcessCode(dynamics.LicensePlateId),

                    // ✅ TRAITEMENT UTF-8 : Commentaires (max 255 caractères)
                    ReaCom = _textProcessor.ProcessName(dynamics.Notes, 255),

                    // ========== CODE QUALITÉ (RG4) ==========
                    // ✅ TRAITEMENT UTF-8 : Code qualité
                    QuaCode = ApplyQualityCodeRule(dynamics.PdsDispositionCode),

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
                    ReaAlpha1 = ApplyReceptionTypeRule(dynamics.LicensePlateId),

                    ReaAlpha11 = "NIVEAU3", // VALEUR FIXE
                    ReaAlpha12 = "NORMAL", // VALEUR FIXE

                    // ========== CHAMPS SUPPLÉMENTAIRES ==========
                    // ✅ TRAITEMENT UTF-8 : Statut 3PL
                    Int3PlStatus = _textProcessor.ProcessText(dynamics.INT3PLStatus),

                    // ✅ TRAITEMENT UTF-8 : Transaction ID
                    InventTransId = _textProcessor.ProcessCode(dynamics.InventTransId),

                    // ✅ TRAITEMENT UTF-8 : Entrepôt destinataire
                    InventLocationIdTo = _textProcessor.ProcessCode(dynamics.InventLocationIdTo)
                };

                // Log des transformations UTF-8 si en mode debug
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    LogProcessingStats(dynamics, winDev);
                }

                _logger.LogDebug($"Transfer Order mappé: {dynamics.TransferId} → REA_RFTI: {winDev.ReaRfti}");
                return winDev;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors du mapping du Transfer Order {transferOrder.TransferOrderId}");
                return null;
            }
        }

        /// <summary>
        /// RG2: Gestion du code tiers fournisseur avec traitement UTF-8
        /// </summary>
        private string ApplySupplierAccountRule(string? transferId)
        {
            if (string.IsNullOrEmpty(transferId))
                return "TRANSFER_GENERIC";

            // ✅ TRAITEMENT UTF-8 : Nettoyer le Transfer ID
            string cleanTransferId = _textProcessor.ProcessCode(transferId);

            return $"TRANSFER_{cleanTransferId}";
        }

        /// <summary>
        /// RG2: Gestion du nom tiers fournisseur avec traitement UTF-8
        /// </summary>
        private string ApplySupplierNameRule(string? transferId)
        {
            if (string.IsNullOrEmpty(transferId))
                return "Transfer Order Generic";

            // ✅ TRAITEMENT UTF-8 : Nettoyer le Transfer ID pour le nom
            string cleanTransferId = _textProcessor.ProcessCode(transferId);

            return _textProcessor.ProcessName($"Transfer Order {cleanTransferId}", 100);
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
                "LIBERE" or "STANDARD" or "STD" or "LIBRE" => "STD",
                "BLOQUE_LOGISTIQUE" or "BLOCKED_LOGISTICS" or "BQLOG" => "BQLOG",
                "BLOQUE_QA1" or "BLOCKED_QA1" or "BQQA1" => "BQQA1",
                "BLOQUE_QA2" or "BLOCKED_QA2" or "BQQA2" => "BQQA2",
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
        private string ApplyReceptionTypeRule(string? licensePlateId)
        {
            if (string.IsNullOrEmpty(licensePlateId))
                return "ORDERS";

            // ✅ TRAITEMENT UTF-8 : Nettoyer le License Plate ID
            string cleanLicensePlateId = _textProcessor.ProcessCode(licensePlateId);

            return !string.IsNullOrEmpty(cleanLicensePlateId) ? "DESADV" : "ORDERS";
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
        /// Formate une date pour le XML (format compatible WINDEV)
        /// </summary>
        private string FormatDateForXml(DateTime? date)
        {
            if (date == null || date == DateTime.MinValue)
                return "";

            return date.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Log des statistiques de traitement UTF-8 pour diagnostic
        /// </summary>
        private void LogProcessingStats(DynamicsTransferOrder dynamics, WinDevTransferOrder winDev)
        {
            var transferIdStats = _textProcessor.GetProcessingStats(dynamics.TransferId, winDev.ReaRfti);
            var notesStats = _textProcessor.GetProcessingStats(dynamics.Notes, winDev.ReaCom);

            if (transferIdStats.TransformationApplied || notesStats.TransformationApplied)
            {
                _logger.LogDebug($"Transformations UTF-8 appliquées pour le Transfer Order {dynamics.TransferId}:");

                if (transferIdStats.TransformationApplied)
                {
                    _logger.LogDebug($"  Transfer ID: '{dynamics.TransferId}' → '{winDev.ReaRfti}' ({transferIdStats.OriginalLength}→{transferIdStats.ProcessedLength} chars)");
                }

                if (notesStats.TransformationApplied)
                {
                    _logger.LogDebug($"  Notes: '{dynamics.Notes}' → '{winDev.ReaCom}' ({notesStats.OriginalLength}→{notesStats.ProcessedLength} chars)");
                }
            }
        }

        /// <summary>
        /// Valide qu'un Transfer Order a les données minimales requises
        /// </summary>
        public bool ValidateTransferOrder(TransferOrder transferOrder)
        {
            if (transferOrder?.DynamicsData == null)
                return false;

            var dynamics = transferOrder.DynamicsData;

            if (string.IsNullOrEmpty(dynamics.TransferId))
            {
                _logger.LogWarning("Transfer Order sans TransferId");
                return false;
            }

            if (string.IsNullOrEmpty(dynamics.ItemId))
            {
                _logger.LogWarning($"Transfer Order {dynamics.TransferId} sans ItemId");
                return false;
            }

            if (dynamics.QtyShipped <= 0)
            {
                _logger.LogWarning($"Transfer Order {dynamics.TransferId} avec quantité invalide: {dynamics.QtyShipped}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Méthode utilitaire pour obtenir un résumé du Transfer Order mappé avec info UTF-8
        /// </summary>
        public string GetMappingSummary(TransferOrder transferOrder)
        {
            if (transferOrder?.DynamicsData == null)
                return "Aucune donnée disponible";

            var dynamics = transferOrder.DynamicsData;

            return $"=== MAPPING TRANSFER ORDER (selon mapping + UTF-8) ===\n" +
                   $"API → SPEED:\n" +
                   $"  ExpectedReceiptNumber: '{dynamics.ExpectedReceiptNumber}' → REA_DAT.REA_RFCE (traité UTF-8)\n" +
                   $"  TransferId: '{dynamics.TransferId}' → REA_DAT.REA_RFTI (traité UTF-8)\n" +
                   $"  ReceiveDate: '{dynamics.ReceiveDate}' → REA_DAT.REA_DALP\n" +
                   $"  LineNum: {dynamics.LineNum} → REA_DAT.REA_NoLR\n" +
                   $"  ItemId: '{dynamics.ItemId}' → REA_DAT.ART_CODE (traité UTF-8)\n" +
                   $"  QtyShipped: {dynamics.QtyShipped} → REA_DAT.REA_QTRE (RG3)\n" +
                   $"  LicensePlateId: '{dynamics.LicensePlateId}' → REA_DAT.REA_NoSU (traité UTF-8)\n" +
                   $"  PdsDispositionCode: '{dynamics.PdsDispositionCode}' → REA_DAT.QUA_CODE (RG4, traité UTF-8)\n" +
                   $"  ReceptionType: '{ApplyReceptionTypeRule(dynamics.LicensePlateId)}' (RG6, traité UTF-8)\n" +
                   $"  Notes: '{dynamics.Notes}' → '{_textProcessor.ProcessName(dynamics.Notes, 255)}' (traité UTF-8, max 255 chars)\n" +
                   $"  InventLocationIdTo: '{dynamics.InventLocationIdTo}' → Entrepot destinataire (traité UTF-8)\n" +
                   $"  ACT_CODE: 'COSMETIQUE' (fixe)\n" +
                   $"  CCLI: 'BR' (fixe)\n" +
                   $"  ✅ TOUS LES CHAMPS TEXTE TRAITÉS AVEC NORMALISATION UTF-8";
        }
    }
}