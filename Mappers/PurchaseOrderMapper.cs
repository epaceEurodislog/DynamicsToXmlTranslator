using System;
using System.Globalization;
using DynamicsToXmlTranslator.Models;
using DynamicsToXmlTranslator.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DynamicsToXmlTranslator.Mappers
{
    public class PurchaseOrderMapper
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<PurchaseOrderMapper> _logger;
        private readonly Utf8TextProcessor _textProcessor;

        public PurchaseOrderMapper(IConfiguration configuration, ILogger<PurchaseOrderMapper> logger, Utf8TextProcessor textProcessor)
        {
            _configuration = configuration;
            _logger = logger;
            _textProcessor = textProcessor;
        }

        /// <summary>
        /// Convertit un Purchase Order Dynamics en Purchase Order WINDEV selon le mapping fourni
        /// AVEC traitement UTF-8 des caractères spéciaux
        /// </summary>
        public WinDevPurchaseOrder? MapToWinDev(PurchaseOrder purchaseOrder)
        {
            if (purchaseOrder?.DynamicsData == null)
            {
                _logger.LogWarning($"Purchase Order {purchaseOrder?.PurchaseOrderId} n'a pas de données Dynamics");
                return null;
            }

            try
            {
                var dynamics = purchaseOrder.DynamicsData;

                var winDev = new WinDevPurchaseOrder
                {
                    // ========== VALEURS FIXES ==========
                    ActCode = "COSMETIQUE",
                    ReaCcli = "BR",

                    // ========== IDENTIFIANTS PRINCIPAUX ==========
                    ReaRfce = _textProcessor.ProcessCode(dynamics.PurchId), // TransferId  
                    ReaRfti = _textProcessor.ProcessCode(dynamics.PurchOrderDocNum), // ExpectedReceiptNumber
                    ReaRfcl = dynamics.LineNumber, // Même valeur que REA_NoLR
                    ReaTyat = "001", // 001 pour Purchase Orders
                    ReaDalp = FormatDateForXml(dynamics.ReceiptDate),

                    // ========== FOURNISSEUR ==========
                    ReaCtaf = "BR" + _textProcessor.ProcessCode(dynamics.OrderAccount),

                    ReaAlpha3 = _textProcessor.ProcessCode(dynamics.PurchName),

                    // ========== DÉTAILS LIGNE ==========
                    ReaNoLr = dynamics.LineNumber,
                    ArtCode = "BR" + _textProcessor.ProcessCode(dynamics.ItemId),
                    ReaQtre = dynamics.QtyOrdered,
                    ReaQtrc = dynamics.QtyOrdered, // Même valeur que REA_QTRE

                    // ========== TRAÇABILITÉ ==========
                    ReaLot1 = ApplyLotRule(dynamics.Lot, dynamics.ItemId, 1),
                    ReaLot2 = ApplyLotRule(dynamics.Lot2, dynamics.ItemId, 2),
                    ReaDluo = FormatDateForXml(dynamics.DLUO),

                    // ========== SUPPORT ET COMMENTAIRES ==========
                    ReaNoSu = _textProcessor.ProcessCode(dynamics.SupportNumber),
                    ReaCom = _textProcessor.ProcessName(dynamics.Notes, 255),

                    // ========== CODE QUALITÉ ==========
                    QuaCode = ApplyQualityCodeRule(dynamics.QualityCode),

                    // ========== RÉFÉRENCE RÉSERVATION ==========
                    ReaRfaf = _textProcessor.ProcessCode(dynamics.ReservationRef),

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

                _logger.LogDebug($"Purchase Order mappé: {dynamics.PurchId} → REA_RFTI: {winDev.ReaRfti}");
                return winDev;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors du mapping du Purchase Order {purchaseOrder.PurchaseOrderId}");
                return null;
            }
        }

        /// <summary>
        /// RG4: Code Qualité avec traitement UTF-8
        /// </summary>
        private string ApplyQualityCodeRule(string? qualityCode)
        {
            if (string.IsNullOrEmpty(qualityCode))
                return "STD";

            // ✅ TRAITEMENT UTF-8 : Nettoyer avant traitement
            string cleanCode = _textProcessor.ProcessText(qualityCode).ToUpper().Trim();

            return cleanCode switch
            {
                "STANDARD" or "STD" or "RETURN_STANDARD" or "Libéré" or "Dérogé" or "LIBÉRÉ" or "LIBERE" or "DÉROGÉ" or "DEROGE" => "STD",
                "Bloqué" or "Recontrôle " or "En attente" or "BLOQUÉ" or "BLOQUE" or "RECONTRÔLE" or "RECONTROLE" or "EN ATTENTE" => "BQQA",
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
        private void LogProcessingStats(DynamicsPurchaseOrder dynamics, WinDevPurchaseOrder winDev)
        {
            var purchIdStats = _textProcessor.GetProcessingStats(dynamics.PurchId, winDev.ReaRfti);
            var notesStats = _textProcessor.GetProcessingStats(dynamics.Notes, winDev.ReaCom);

            if (purchIdStats.TransformationApplied || notesStats.TransformationApplied)
            {
                _logger.LogDebug($"Transformations UTF-8 appliquées pour le Purchase Order {dynamics.PurchId}:");

                if (purchIdStats.TransformationApplied)
                {
                    _logger.LogDebug($"  Purchase ID: '{dynamics.PurchId}' → '{winDev.ReaRfti}' ({purchIdStats.OriginalLength}→{purchIdStats.ProcessedLength} chars)");
                }

                if (notesStats.TransformationApplied)
                {
                    _logger.LogDebug($"  Notes: '{dynamics.Notes}' → '{winDev.ReaCom}' ({notesStats.OriginalLength}→{notesStats.ProcessedLength} chars)");
                }
            }
        }

        /// <summary>
        /// Valide qu'un Purchase Order a les données minimales requises
        /// </summary>
        public bool ValidatePurchaseOrder(PurchaseOrder purchaseOrder)
        {
            if (purchaseOrder?.DynamicsData == null)
                return false;

            var dynamics = purchaseOrder.DynamicsData;

            if (string.IsNullOrEmpty(dynamics.PurchId))
            {
                _logger.LogWarning("Purchase Order sans PurchId");
                return false;
            }

            if (string.IsNullOrEmpty(dynamics.ItemId))
            {
                _logger.LogWarning($"Purchase Order {dynamics.PurchId} sans ItemId");
                return false;
            }

            if (dynamics.QtyOrdered <= 0)
            {
                _logger.LogWarning($"Purchase Order {dynamics.PurchId} avec quantité invalide: {dynamics.QtyOrdered}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Méthode utilitaire pour obtenir un résumé du Purchase Order mappé avec info UTF-8
        /// </summary>
        public string GetMappingSummary(PurchaseOrder purchaseOrder)
        {
            if (purchaseOrder?.DynamicsData == null)
                return "Aucune donnée disponible";

            var dynamics = purchaseOrder.DynamicsData;

            return $"=== MAPPING PURCHASE ORDER (selon mapping + UTF-8) ===\n" +
                   $"API → SPEED:\n" +
                   $"  PurchOrderDocNum: '{dynamics.PurchOrderDocNum}' → REA_DAT.REA_RFCE (traité UTF-8)\n" +
                   $"  PurchId: '{dynamics.PurchId}' → REA_DAT.REA_RFTI (traité UTF-8)\n" +
                   $"  ReceiptDate: '{dynamics.ReceiptDate}' → REA_DAT.REA_DALP\n" +
                   $"  OrderAccount: '{dynamics.OrderAccount}' → REA_DAT.REA_CTAF (traité UTF-8)\n" +
                   $"  LineNumber: {dynamics.LineNumber} → REA_DAT.REA_NoLR\n" +
                   $"  ItemId: '{dynamics.ItemId}' → REA_DAT.ART_CODE (traité UTF-8)\n" +
                   $"  QtyOrdered: {dynamics.QtyOrdered} → REA_DAT.REA_QTRE\n" +
                   $"  SupportNumber: '{dynamics.SupportNumber}' → REA_DAT.REA_NoSU (traité UTF-8)\n" +
                   $"  QualityCode: '{dynamics.QualityCode}' → REA_DAT.QUA_CODE (RG4, traité UTF-8)\n" +
                   $"  ReceptionType: '{ApplyReceptionTypeRule(dynamics.SupportNumber)}' (RG6, traité UTF-8)\n" +
                   $"  Notes: '{dynamics.Notes}' → '{_textProcessor.ProcessName(dynamics.Notes, 255)}' (traité UTF-8, max 255 chars)\n" +
                   $"  LotID: '{dynamics.LotID}' → REA_DAT.REA_ALPHA2 (traité UTF-8)\n" +
                   $"  ACT_CODE: 'COSMETIQUE' (fixe)\n" +
                   $"  CCLI: 'BR' (fixe)\n" +
                   $"  ✅ TOUS LES CHAMPS TEXTE TRAITÉS AVEC NORMALISATION UTF-8";
        }
    }
}