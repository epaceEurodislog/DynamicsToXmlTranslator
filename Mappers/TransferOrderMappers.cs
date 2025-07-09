using System;
using System.Globalization;
using DynamicsToXmlTranslator.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DynamicsToXmlTranslator.Mappers
{
    public class TransferOrderMapper
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<TransferOrderMapper> _logger;

        public TransferOrderMapper(IConfiguration configuration, ILogger<TransferOrderMapper> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Convertit un Transfer Order Dynamics en Transfer Order WINDEV selon le mapping fourni
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
                    ReaRfce = dynamics.ExpectedReceiptNumber ?? "", // ExpectedReceiptNumber → REA_DAT.REA_RFCE
                    ReaRfti = dynamics.TransferId ?? "", // TransferId → REA_DAT.REA_RFTI
                    ReaDalp = FormatDateForXml(dynamics.ReceiveDate), // ReceiveDate → REA_DAT.REA_DALP

                    // ========== FOURNISSEUR/CLIENT ==========
                    ReaCtaf = ApplySupplierAccountRule(dynamics.TransferId), // RG2: XXXAccount → REA_DAT.REA_CTAF
                    XxxName = ApplySupplierNameRule(dynamics.TransferId), // RG2: XXXName

                    // ========== DÉTAILS LIGNE ==========
                    ReaNoLr = dynamics.LineNum, // LineNum → REA_DAT.REA_NoLR
                    ArtCode = dynamics.ItemId ?? "", // ItemId → REA_DAT.ART_CODE
                    ReaQtre = dynamics.QtyShipped, // QtyShipped → REA_DAT.REA_QTRE (RG3)

                    // ========== TRAÇABILITÉ (avec RG7 et RG8) ==========
                    ReaLot1 = ApplyLotRule(dynamics.inventBatchId, dynamics.ItemId, 1), // RG7: Si article ART_PART.ART_LOT1=O
                    ReaLot2 = ApplyLotRule(dynamics.inventSerialId, dynamics.ItemId, 2), // RG8: Si article ART_PART.ART_LOT2=O
                    ReaDluo = FormatDateForXml(dynamics.expDate), // expDate → REA_DAT.REA_DLUO

                    // ========== SUPPORT ET COMMENTAIRES ==========
                    ReaNoSu = dynamics.LicensePlateId ?? "", // LicensePlateId → REA_DAT.REA_NoSU
                    ReaCom = dynamics.Notes ?? "", // Notes → REA_DAT.REA_COM

                    // ========== CODE QUALITÉ (RG4) ==========
                    QuaCode = ApplyQualityCodeRule(dynamics.PdsDispositionCode), // RG4: 4 valeurs possibles

                    // ========== RÉFÉRENCE RÉSERVATION ==========
                    ReaRfaf = "", // Référence réservation - pas dans vos données

                    // ========== VALEURS FIXES ==========
                    ActCode = "COSMETIQUE", // VALEUR FIXE
                    Ccli = "BR", // VALEUR FIXE

                    // ========== CHAMPS AVEC RÈGLES DE GESTION ==========
                    ReaAlpha5 = ApplyQualityControlRule(dynamics.ItemId), // RG5: Si catégorie = "PFRETAIL"
                    ReaAlpha1 = ApplyReceptionTypeRule(dynamics.LicensePlateId), // RG6: Si N° REA_NoSU alors 'DESADV'
                    ReaAlpha11 = "NIVEAU3", // VALEUR FIXE
                    ReaAlpha12 = "NORMAL", // VALEUR FIXE

                    // ========== CHAMPS SUPPLÉMENTAIRES ==========
                    Int3PlStatus = dynamics.INT3PLStatus ?? "", // INT3PLStatus
                    InventTransId = dynamics.InventTransId ?? "", // InventTransId
                    InventLocationIdTo = dynamics.InventLocationIdTo ?? "", // InventLocationIdTo
                };

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
        /// RG2: Gestion du code tiers fournisseur
        /// Si REA_DAT.REA_CTAF n'existe pas dans la table des tiers à créer TIE_PAR.TIE_CODE
        /// Pour les Transfer Orders, utiliser un code générique basé sur le TransferId
        /// </summary>
        private string ApplySupplierAccountRule(string? transferId)
        {
            if (string.IsNullOrEmpty(transferId))
                return "TRANSFER_GENERIC";

            // Générer un code tiers basé sur le Transfer ID
            return $"TRANSFER_{transferId}";
        }

        /// <summary>
        /// RG2: Gestion du nom tiers fournisseur
        /// Si REA_DAT.REA_CTAF n'existe pas dans la table des tiers à créer TIE_PAR.TIE_NOM
        /// </summary>
        private string ApplySupplierNameRule(string? transferId)
        {
            if (string.IsNullOrEmpty(transferId))
                return "Transfer Order Generic";

            return $"Transfer Order {transferId}";
        }

        /// <summary>
        /// RG4: Code Qualité - 4 valeurs possibles => STD, BQLOG, BQQA1, BQQA2
        /// Mapping depuis PdsDispositionCode vers code qualité
        /// </summary>
        private string ApplyQualityCodeRule(string? dispositionCode)
        {
            if (string.IsNullOrEmpty(dispositionCode))
                return "STD"; // Valeur par défaut

            // Transformation selon les valeurs de disposition D365
            return dispositionCode.ToUpper().Trim() switch
            {
                "LIBÉRÉ" or "STANDARD" or "STD" or "LIBRE" => "STD",
                "BLOQUÉ_LOGISTIQUE" or "BLOCKED_LOGISTICS" or "BQLOG" => "BQLOG",
                "BLOQUÉ_QA1" or "BLOCKED_QA1" or "BQQA1" => "BQQA1",
                "BLOQUÉ_QA2" or "BLOCKED_QA2" or "BQQA2" => "BQQA2",
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
        private string ApplyReceptionTypeRule(string? licensePlateId)
        {
            return !string.IsNullOrEmpty(licensePlateId) ? "DESADV" : "ORDERS";
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

            // Format date pour WINDEV
            return date.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Valide qu'un Transfer Order a les données minimales requises
        /// </summary>
        public bool ValidateTransferOrder(TransferOrder transferOrder)
        {
            if (transferOrder?.DynamicsData == null)
                return false;

            var dynamics = transferOrder.DynamicsData;

            // Vérifications minimales
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
        /// Méthode utilitaire pour obtenir un résumé du Transfer Order mappé
        /// </summary>
        public string GetMappingSummary(TransferOrder transferOrder)
        {
            if (transferOrder?.DynamicsData == null)
                return "Aucune donnée disponible";

            var dynamics = transferOrder.DynamicsData;

            return $"=== MAPPING TRANSFER ORDER (selon mapping fourni) ===\n" +
                   $"API → SPEED:\n" +
                   $"  ExpectedReceiptNumber: '{dynamics.ExpectedReceiptNumber}' → REA_DAT.REA_RFCE\n" +
                   $"  TransferId: '{dynamics.TransferId}' → REA_DAT.REA_RFTI\n" +
                   $"  ReceiveDate: '{dynamics.ReceiveDate}' → REA_DAT.REA_DALP\n" +
                   $"  LineNum: {dynamics.LineNum} → REA_DAT.REA_NoLR\n" +
                   $"  ItemId: '{dynamics.ItemId}' → REA_DAT.ART_CODE\n" +
                   $"  QtyShipped: {dynamics.QtyShipped} → REA_DAT.REA_QTRE (RG3)\n" +
                   $"  LicensePlateId: '{dynamics.LicensePlateId}' → REA_DAT.REA_NoSU\n" +
                   $"  PdsDispositionCode: '{dynamics.PdsDispositionCode}' → REA_DAT.QUA_CODE (RG4)\n" +
                   $"  ReceptionType: '{ApplyReceptionTypeRule(dynamics.LicensePlateId)}' (RG6)\n" +
                   $"  InventLocationIdTo: '{dynamics.InventLocationIdTo}' → Entrepot destinataire\n" +
                   $"  ACT_CODE: 'COSMETIQUE' (fixe)\n" +
                   $"  CCLI: 'BR' (fixe)\n" +
                   $"  Traçabilité: Lot1={dynamics.inventBatchId}, Lot2={dynamics.inventSerialId}, DLUO={dynamics.expDate}";
        }
    }
}