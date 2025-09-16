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
                    // ========== VALEURS FIXES ==========
                    ActCode = "COSMETIQUE",
                    ReaCcli = "BR",

                    // ========== IDENTIFIANTS PRINCIPAUX ==========
                    ReaRfce = _textProcessor.ProcessCode(dynamics.TransferId),
                    ReaRfti = _textProcessor.ProcessCode(dynamics.ExpectedReceiptNumber),
                    ReaRfcl = dynamics.LineNum, // Même valeur que REA_NoLR
                    ReaTyat = "001", // 001 pour Transfer Orders
                    ReaDalp = FormatDateForXml(dynamics.ReceiveDate),

                    // ========== FOURNISSEUR/CLIENT ==========
                    ReaCtaf = ApplySupplierAccountRule(dynamics.TransferId),

                    // ========== DÉTAILS LIGNE ==========
                    ReaNoLr = dynamics.LineNum,
                    ArtCode = "BR" + _textProcessor.ProcessCode(dynamics.ItemId),
                    ReaQtre = dynamics.QtyShipped,
                    ReaQtrc = dynamics.QtyShipped, // Même valeur que REA_QTRE

                    // ========== TRAÇABILITÉ ==========
                    ReaLot1 = ApplyLotRule(dynamics.inventBatchId, dynamics.ItemId, 1),
                    ReaLot2 = ApplyLotRule(dynamics.inventSerialId, dynamics.ItemId, 2),
                    ReaDluo = FormatDateForXml(dynamics.expDate),

                    // ========== SUPPORT ET COMMENTAIRES ==========
                    ReaNoSu = _textProcessor.ProcessCode(dynamics.LicensePlateId),
                    ReaCom = _textProcessor.ProcessName(dynamics.Notes, 255),

                    // ========== CODE QUALITÉ ==========
                    QuaCode = ApplyQualityCodeRule(dynamics.PdsDispositionCode),

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
                "STANDARD" or "STD" or "RETURN_STANDARD" or "LIBÉRÉ" or "LIBERE" or "DÉROGÉ" or "DEROGE" => "STD",
                "BLOQUÉ" or "BLOQUE" or "RECONTRÔLE" or "RECONTROLE" or "EN ATTENTE" => "BQQA",
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
        /// Formate une date pour le XML (format YYYYMMDD sans tirets)
        /// </summary>
        private string FormatDateForXml(DateTime? date)
        {
            if (date == null || date == DateTime.MinValue)
                return "";

            return date.Value.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
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
                   $"  LotID: '{dynamics.LotID}' → REA_DAT.REA_ALPHA2 (traité UTF-8)\n" +
                   $"  ACT_CODE: 'COSMETIQUE' (fixe)\n" +
                   $"  CCLI: 'BR' (fixe)\n" +
                   $"  ✅ TOUS LES CHAMPS TEXTE TRAITÉS AVEC NORMALISATION UTF-8";
        }
    }
}