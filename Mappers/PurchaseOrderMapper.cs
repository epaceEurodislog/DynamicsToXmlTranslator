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
                    // ========== IDENTIFIANTS PRINCIPAUX ==========
                    // ✅ TRAITEMENT UTF-8 : Numéro attendu réception
                    ReaRfce = _textProcessor.ProcessCode(dynamics.PurchOrderDocNum),

                    // ✅ TRAITEMENT UTF-8 : N° Commande Achat
                    ReaRfti = _textProcessor.ProcessCode(dynamics.PurchId),

                    // Date de réception prévue (format date, pas de traitement UTF-8 nécessaire)
                    ReaDalp = FormatDateForXml(dynamics.ReceiptDate),

                    // ========== FOURNISSEUR ==========
                    // ✅ TRAITEMENT UTF-8 : Code tiers fournisseur
                    ReaCtaf = _textProcessor.ProcessCode(dynamics.OrderAccount),

                    // ✅ TRAITEMENT UTF-8 : Nom tiers fournisseur (max 100 caractères)
                    PurchName = _textProcessor.ProcessName(dynamics.PurchName, 100),

                    // ========== DÉTAILS LIGNE ==========
                    // Numéro ligne (numérique, pas de traitement UTF-8)
                    ReaNoLr = dynamics.LineNumber,

                    // ✅ TRAITEMENT UTF-8 : Référence article
                    ArtCode = _textProcessor.ProcessCode(dynamics.ItemId),

                    // Quantité prévue (numérique, pas de traitement UTF-8)
                    ReaQtre = dynamics.QtyOrdered,

                    // ========== TRAÇABILITÉ (avec RG7 et RG8) ==========
                    // ✅ TRAITEMENT UTF-8 : Lot 1
                    ReaLot1 = ApplyLotRule(dynamics.Lot, dynamics.ItemId, 1),

                    // ✅ TRAITEMENT UTF-8 : Lot 2
                    ReaLot2 = ApplyLotRule(dynamics.Lot2, dynamics.ItemId, 2),

                    // Date DLUO (format date)
                    ReaDluo = FormatDateForXml(dynamics.DLUO),

                    // ========== NUMÉRO SUPPORT ET COMMENTAIRES ==========
                    // ✅ TRAITEMENT UTF-8 : Numéro support (SSCC entrant)
                    ReaNoSu = _textProcessor.ProcessCode(dynamics.SupportNumber),

                    // ✅ TRAITEMENT UTF-8 : Commentaires (max 255 caractères)
                    ReaCom = _textProcessor.ProcessName(dynamics.Notes, 255),

                    // ========== CODE QUALITÉ (RG4) ==========
                    // ✅ TRAITEMENT UTF-8 : Code qualité
                    QuaCode = ApplyQualityCodeRule(dynamics.QualityCode),

                    // ========== RÉFÉRENCE RÉSERVATION ==========
                    // ✅ TRAITEMENT UTF-8 : Référence réservation
                    ReaRfaf = _textProcessor.ProcessCode(dynamics.ReservationRef),

                    // ========== VALEURS FIXES ==========
                    ActCode = "COSMETIQUE", // VALEUR FIXE
                    Ccli = "BR", // VALEUR FIXE

                    // ========== CHAMPS AVEC RÈGLES DE GESTION ==========
                    // ✅ TRAITEMENT UTF-8 : Contrôle qualité (RG5)
                    ReaAlpha5 = ApplyQualityControlRule(dynamics.ItemId),

                    // ✅ TRAITEMENT UTF-8 : Type de réception (RG6)
                    ReaAlpha1 = ApplyReceptionTypeRule(dynamics.SupportNumber),

                    ReaAlpha11 = "NIVEAU3", // VALEUR FIXE
                    ReaAlpha12 = "NORMAL", // VALEUR FIXE

                    // ========== CHAMPS SUPPLÉMENTAIRES ==========
                    // ✅ TRAITEMENT UTF-8 : Version table achat
                    PurchTableVersion = _textProcessor.ProcessText(dynamics.PurchTableVersion),

                    // ✅ TRAITEMENT UTF-8 : Statut 3PL
                    Int3PlStatus = _textProcessor.ProcessText(dynamics.INT3PLStatus),

                    // Dates supplémentaires
                    DeliveryDate = FormatDateForXml(dynamics.DeliveryDate),

                    // ✅ TRAITEMENT UTF-8 : Entrepôt destinataire
                    InventLocationId = _textProcessor.ProcessCode(dynamics.InventLocationId),

                    // ✅ TRAITEMENT UTF-8 : Statut document
                    DocumentState = _textProcessor.ProcessText(dynamics.DocumentState)
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
                "STANDARD" or "STD" => "STD",
                "BLOCKED_LOGISTICS" or "BQLOG" => "BQLOG",
                "BLOCKED_QA1" or "BQQA1" => "BQQA1",
                "BLOCKED_QA2" or "BQQA2" => "BQQA2",
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
        private void LogProcessingStats(DynamicsPurchaseOrder dynamics, WinDevPurchaseOrder winDev)
        {
            var nameStats = _textProcessor.GetProcessingStats(dynamics.PurchName, winDev.PurchName);
            var codeStats = _textProcessor.GetProcessingStats(dynamics.PurchId, winDev.ReaRfti);

            if (nameStats.TransformationApplied || codeStats.TransformationApplied)
            {
                _logger.LogDebug($"Transformations UTF-8 appliquées pour le Purchase Order {dynamics.PurchId}:");

                if (nameStats.TransformationApplied)
                {
                    _logger.LogDebug($"  Nom fournisseur: '{dynamics.PurchName}' → '{winDev.PurchName}' ({nameStats.OriginalLength}→{nameStats.ProcessedLength} chars)");
                }

                if (codeStats.TransformationApplied)
                {
                    _logger.LogDebug($"  Code PO: '{dynamics.PurchId}' → '{winDev.ReaRfti}' ({codeStats.OriginalLength}→{codeStats.ProcessedLength} chars)");
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
                   $"  PurchName: '{dynamics.PurchName}' → '{_textProcessor.ProcessName(dynamics.PurchName, 100)}' (traité UTF-8, max 100 chars)\n" +
                   $"  LineNumber: {dynamics.LineNumber} → REA_DAT.REA_NoLR\n" +
                   $"  ItemId: '{dynamics.ItemId}' → REA_DAT.ART_CODE (traité UTF-8)\n" +
                   $"  QtyOrdered: {dynamics.QtyOrdered} → REA_DAT.REA_QTRE\n" +
                   $"  SupportNumber: '{dynamics.SupportNumber}' → REA_DAT.REA_NoSU (traité UTF-8)\n" +
                   $"  QualityCode: '{dynamics.QualityCode}' → REA_DAT.QUA_CODE (RG4, traité UTF-8)\n" +
                   $"  ReceptionType: '{ApplyReceptionTypeRule(dynamics.SupportNumber)}' (RG6, traité UTF-8)\n" +
                   $"  Notes: '{dynamics.Notes}' → '{_textProcessor.ProcessName(dynamics.Notes, 255)}' (traité UTF-8, max 255 chars)\n" +
                   $"  ACT_CODE: 'COSMETIQUE' (fixe)\n" +
                   $"  CCLI: 'BR' (fixe)\n" +
                   $"  ✅ TOUS LES CHAMPS TEXTE TRAITÉS AVEC NORMALISATION UTF-8";
        }
    }
}