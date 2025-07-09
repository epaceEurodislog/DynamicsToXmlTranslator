using System;
using System.Globalization;
using DynamicsToXmlTranslator.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DynamicsToXmlTranslator.Mappers
{
    public class PurchaseOrderMapper
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<PurchaseOrderMapper> _logger;

        public PurchaseOrderMapper(IConfiguration configuration, ILogger<PurchaseOrderMapper> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Convertit un Purchase Order Dynamics en Purchase Order WINDEV selon le mapping fourni
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
                    ReaRfce = dynamics.PurchOrderDocNum ?? "", // PurchOrderDocNum → REA_DAT.REA_RFCE
                    ReaRfti = dynamics.PurchId ?? "", // PurchId → REA_DAT.REA_RFTI
                    ReaDalp = FormatDateForXml(dynamics.ReceiptDate), // ReceiptDate → REA_DAT.REA_DALP

                    // ========== FOURNISSEUR ==========
                    ReaCtaf = dynamics.OrderAccount ?? "", // OrderAccount → REA_DAT.REA_CTAF
                    PurchName = dynamics.PurchName ?? "", // PurchName (nom fournisseur)

                    // ========== DÉTAILS LIGNE ==========
                    ReaNoLr = dynamics.LineNumber, // LineNumber → REA_DAT.REA_NoLR
                    ArtCode = dynamics.ItemId ?? "", // ItemId → REA_DAT.ART_CODE
                    ReaQtre = dynamics.QtyOrdered, // QtyOrdered → REA_DAT.REA_QTRE

                    // ========== TRAÇABILITÉ (avec RG7 et RG8) ==========
                    ReaLot1 = ApplyLotRule(dynamics.Lot, dynamics.ItemId, 1), // RG7: Si article ART_PART.ART_LOT1=O
                    ReaLot2 = ApplyLotRule(dynamics.Lot2, dynamics.ItemId, 2), // RG8: Si article ART_PART.ART_LOT2=O
                    ReaDluo = FormatDateForXml(dynamics.DLUO), // DLUO → REA_DAT.REA_DLUO

                    // ========== NUMÉRO SUPPORT ET COMMENTAIRES ==========
                    ReaNoSu = dynamics.SupportNumber ?? "", // SupportNumber → REA_DAT.REA_NoSU
                    ReaCom = dynamics.Notes ?? "", // Notes → REA_DAT.REA_COM

                    // ========== CODE QUALITÉ (RG4) ==========
                    QuaCode = ApplyQualityCodeRule(dynamics.QualityCode), // RG4: 4 valeurs possibles

                    // ========== RÉFÉRENCE RÉSERVATION ==========
                    ReaRfaf = dynamics.ReservationRef ?? "", // ReservationRef → REA_DAT.REA_RFAF

                    // ========== VALEURS FIXES ==========
                    ActCode = "COSMETIQUE", // VALEUR FIXE = COSMETIQUE
                    Ccli = "BR", // VALEUR FIXE = BR

                    // ========== CHAMPS AVEC RÈGLES DE GESTION ==========
                    ReaAlpha5 = ApplyQualityControlRule(dynamics.ItemId), // RG5: Si catégorie = "PFRETAIL"
                    ReaAlpha1 = ApplyReceptionTypeRule(dynamics.SupportNumber), // RG6: Si N° REA_NoSU alors 'DESADV'
                    ReaAlpha11 = "NIVEAU3", // VALEUR FIXE = NIVEAU3
                    ReaAlpha12 = "NORMAL", // VALEUR FIXE = NORMAL

                    // ========== CHAMPS SUPPLÉMENTAIRES ==========
                    PurchTableVersion = dynamics.PurchTableVersion ?? "",
                    Int3PlStatus = dynamics.INT3PLStatus ?? "",
                    DeliveryDate = FormatDateForXml(dynamics.DeliveryDate),
                    InventLocationId = dynamics.InventLocationId ?? "",
                    DocumentState = dynamics.DocumentState ?? ""
                };

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
        /// RG4: Code Qualité - 4 valeurs possibles => STD, BQLOG, BQQA1, BQQA2
        /// Correspondance à établir fonction des valeurs de D365
        /// </summary>
        private string ApplyQualityCodeRule(string? qualityCode)
        {
            if (string.IsNullOrEmpty(qualityCode))
                return "STD"; // Valeur par défaut

            // Transformation selon les valeurs D365
            return qualityCode.ToUpper().Trim() switch
            {
                "STANDARD" or "STD" => "STD",
                "BLOCKED_LOGISTICS" or "BQLOG" => "BQLOG",
                "BLOCKED_QA1" or "BQQA1" => "BQQA1",
                "BLOCKED_QA2" or "BQQA2" => "BQQA2",
                _ => "STD" // Valeur par défaut pour les cas non mappés
            };
        }

        /// <summary>
        /// RG5: Si catégorie de l'article (RHY_NOM3) dans attendu = "PFRETAIL" alors tag à Oui sinon Non
        /// TODO: Nécessite l'accès aux données article pour vérifier la catégorie
        /// </summary>
        private string ApplyQualityControlRule(string? itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return "Non";

            // TODO: Implémenter la vérification de la catégorie article
            // Pour l'instant, retourne "Non" par défaut
            // Il faudrait interroger la base pour récupérer la catégorie de l'article
            return "Non";
        }

        /// <summary>
        /// RG6: Si annonce du N° REA_NoSU alors 'DESADV' sinon 'ORDERS'
        /// </summary>
        private string ApplyReceptionTypeRule(string? supportNumber)
        {
            return !string.IsNullOrEmpty(supportNumber) ? "DESADV" : "ORDERS";
        }

        /// <summary>
        /// RG7 et RG8: Gestion des lots selon les paramètres article
        /// Si article ART_PART.ART_LOT1=O alors valeur source champ lot sinon vide
        /// TODO: Nécessite l'accès aux données article pour vérifier les paramètres de lot
        /// </summary>
        private string ApplyLotRule(string? lotValue, string? itemId, int lotNumber)
        {
            if (string.IsNullOrEmpty(lotValue) || string.IsNullOrEmpty(itemId))
                return "";

            // TODO: Implémenter la vérification des paramètres de lot de l'article
            // Pour l'instant, retourne la valeur du lot si elle existe
            // Il faudrait interroger la base pour récupérer ART_PART.ART_LOT1 et ART_PART.ART_LOT2
            return lotValue;
        }

        /// <summary>
        /// Formate une date pour le XML (format compatible WINDEV)
        /// </summary>
        private string FormatDateForXml(DateTime? date)
        {
            if (date == null || date == DateTime.MinValue)
                return "";

            // Format date pour WINDEV (à adapter selon vos besoins)
            return date.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Valide qu'un Purchase Order a les données minimales requises
        /// </summary>
        public bool ValidatePurchaseOrder(PurchaseOrder purchaseOrder)
        {
            if (purchaseOrder?.DynamicsData == null)
                return false;

            var dynamics = purchaseOrder.DynamicsData;

            // Vérifications minimales
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
        /// Méthode utilitaire pour obtenir un résumé du Purchase Order mappé
        /// </summary>
        public string GetMappingSummary(PurchaseOrder purchaseOrder)
        {
            if (purchaseOrder?.DynamicsData == null)
                return "Aucune donnée disponible";

            var dynamics = purchaseOrder.DynamicsData;

            return $"=== MAPPING PURCHASE ORDER (selon mapping fourni) ===\n" +
                   $"API → SPEED:\n" +
                   $"  PurchOrderDocNum: '{dynamics.PurchOrderDocNum}' → REA_DAT.REA_RFCE\n" +
                   $"  PurchId: '{dynamics.PurchId}' → REA_DAT.REA_RFTI\n" +
                   $"  ReceiptDate: '{dynamics.ReceiptDate}' → REA_DAT.REA_DALP\n" +
                   $"  OrderAccount: '{dynamics.OrderAccount}' → REA_DAT.REA_CTAF\n" +
                   $"  PurchName: '{dynamics.PurchName}' → Nom fournisseur\n" +
                   $"  LineNumber: {dynamics.LineNumber} → REA_DAT.REA_NoLR\n" +
                   $"  ItemId: '{dynamics.ItemId}' → REA_DAT.ART_CODE\n" +
                   $"  QtyOrdered: {dynamics.QtyOrdered} → REA_DAT.REA_QTRE\n" +
                   $"  SupportNumber: '{dynamics.SupportNumber}' → REA_DAT.REA_NoSU\n" +
                   $"  QualityCode: '{dynamics.QualityCode}' → REA_DAT.QUA_CODE (RG4)\n" +
                   $"  ReceptionType: '{ApplyReceptionTypeRule(dynamics.SupportNumber)}' (RG6)\n" +
                   $"  ACT_CODE: 'COSMETIQUE' (fixe)\n" +
                   $"  CCLI: 'BR' (fixe)\n" +
                   $"  Traçabilité: Lot1={dynamics.Lot}, Lot2={dynamics.Lot2}, DLUO={dynamics.DLUO}";
        }
    }
}