using System;
using System.Globalization;
using DynamicsToXmlTranslator.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DynamicsToXmlTranslator.Mappers
{
    public class ReturnOrderMapper
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ReturnOrderMapper> _logger;

        public ReturnOrderMapper(IConfiguration configuration, ILogger<ReturnOrderMapper> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Convertit un Return Order Dynamics en Return Order WINDEV selon le mapping fourni
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
                    OpeCcli = dynamics.dataAreaId ?? "BR", // dataAreaId → OPE_DAT.OPE_CCLI (Société Référence)
                    ReaRfce = dynamics.ReturnItemNum ?? "", // ReturnItemNum → REA_DAT.REA_RFCE (Expected Receipt Number)
                    ReaRfti = dynamics.SalesId ?? "", // SalesId → REA_DAT.REA_RFTI (N° Commande Vente)
                    ReaDalp = FormatDateForXml(dynamics.ReturnDeadline), // ReturnDeadline → REA_DAT.REA_DALP (Date de réception prévue)

                    // ========== FOURNISSEUR/CLIENT ==========
                    ReaCtaf = dynamics.CustAccount ?? "", // CustAccount → REA_DAT.REA_CTAF (Code tiers fournisseurs/Client)
                    SalesName = dynamics.SalesName ?? "", // SalesName (Nom tiers fournisseurs/Client)

                    // ========== DÉTAILS LIGNE ==========
                    ReaNoLr = dynamics.LineNum, // LineNum → REA_DAT.REA_NoLR (Numéro ligne de commande)
                    ArtCode = dynamics.ItemId ?? "", // ItemId → REA_DAT.ART_CODE (Référence article)
                    ReaQtre = dynamics.ExpectedRetQty, // ExpectedRetQty → REA_DAT.REA_QTRE (Quantité prévue)

                    // ========== TRAÇABILITÉ (avec RG7 et RG8) ==========
                    ReaLot1 = ApplyLotRule(dynamics.inventBatchId, dynamics.ItemId, 1), // RG7: Si article ART_PART.ART_LOT1=O
                    ReaLot2 = ApplyLotRule(dynamics.inventSerialId, dynamics.ItemId, 2), // RG8: Si article ART_PART.ART_LOT2=O
                    ReaDluo = FormatDateForXml(dynamics.expDate), // expDate → REA_DAT.REA_DLUO (DLUO)

                    // ========== SUPPORT ET COMMENTAIRES ==========
                    ReaNoSu = "", // Numéro support (SSCC entrant) - pas dans le mapping source
                    ReaCom = dynamics.Notes ?? "", // Notes → REA_DAT.REA_COM (Commentaires)

                    // ========== CODE QUALITÉ (RG4) ==========
                    QuaCode = ApplyQualityCodeRule(dynamics.ReturnDispositionCodeID), // RG4: Code disposition vers code qualité

                    // ========== RÉFÉRENCE RÉSERVATION ==========
                    ReaRfaf = "", // Référence réservation - pas dans le mapping source

                    // ========== VALEURS FIXES ==========
                    ActCode = "COSMETIQUE", // VALEUR FIXE = COSMETIQUE
                    Ccli = "BR", // VALEUR FIXE = BR

                    // ========== CHAMPS AVEC RÈGLES DE GESTION ==========
                    ReaAlpha5 = ApplyQualityControlRule(dynamics.ItemId), // RG5: Si catégorie = "PFRETAIL"
                    ReaAlpha1 = ApplyReceptionTypeRule(dynamics.ReturnItemNum), // RG6: Si N° REA_NoSU alors 'DESADV'
                    ReaAlpha11 = "NIVEAU3", // VALEUR FIXE = NIVEAU3
                    ReaAlpha12 = "NORMAL", // VALEUR FIXE = NORMAL

                    // ========== CHAMPS SUPPLÉMENTAIRES ==========
                    SalesStatus = dynamics.SalesStatus ?? "", // Statut Commande Vente
                    ReturnDispositionCodeId = dynamics.ReturnDispositionCodeID ?? "", // Code disposition
                    InventLocationId = dynamics.InventLocationId ?? "" // Entrepot destinataire D365
                };

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
        /// RG4: Code Qualité - 4 valeurs possibles => STD, BQLOG, BQQA1, BQQA2
        /// Mapping depuis ReturnDispositionCodeID vers code qualité
        /// </summary>
        private string ApplyQualityCodeRule(string? dispositionCode)
        {
            if (string.IsNullOrEmpty(dispositionCode))
                return "STD"; // Valeur par défaut

            // Transformation selon les valeurs de disposition D365
            return dispositionCode.ToUpper().Trim() switch
            {
                "STANDARD" or "STD" or "RETURN_STANDARD" => "STD",
                "BLOCKED_LOGISTICS" or "BQLOG" or "RETURN_BLOCKED_LOG" => "BQLOG",
                "BLOCKED_QA1" or "BQQA1" or "RETURN_BLOCKED_QA1" => "BQQA1",
                "BLOCKED_QA2" or "BQQA2" or "RETURN_BLOCKED_QA2" => "BQQA2",
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
        /// Valide qu'un Return Order a les données minimales requises
        /// </summary>
        public bool ValidateReturnOrder(ReturnOrder returnOrder)
        {
            if (returnOrder?.DynamicsData == null)
                return false;

            var dynamics = returnOrder.DynamicsData;

            // Vérifications minimales
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

            if (dynamics.ExpectedRetQty <= 0)
            {
                _logger.LogWarning($"Return Order {dynamics.SalesId} avec quantité invalide: {dynamics.ExpectedRetQty}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Méthode utilitaire pour obtenir un résumé du Return Order mappé
        /// </summary>
        public string GetMappingSummary(ReturnOrder returnOrder)
        {
            if (returnOrder?.DynamicsData == null)
                return "Aucune donnée disponible";

            var dynamics = returnOrder.DynamicsData;

            return $"=== MAPPING RETURN ORDER (selon mapping fourni) ===\n" +
                   $"API → SPEED:\n" +
                   $"  dataAreaId: '{dynamics.dataAreaId}' → OPE_DAT.OPE_CCLI\n" +
                   $"  ReturnItemNum: '{dynamics.ReturnItemNum}' → REA_DAT.REA_RFCE\n" +
                   $"  SalesId: '{dynamics.SalesId}' → REA_DAT.REA_RFTI\n" +
                   $"  ReturnDeadline: '{dynamics.ReturnDeadline}' → REA_DAT.REA_DALP\n" +
                   $"  CustAccount: '{dynamics.CustAccount}' → REA_DAT.REA_CTAF\n" +
                   $"  SalesName: '{dynamics.SalesName}' → Nom tiers client\n" +
                   $"  LineNum: {dynamics.LineNum} → REA_DAT.REA_NoLR\n" +
                   $"  ItemId: '{dynamics.ItemId}' → REA_DAT.ART_CODE\n" +
                   $"  ExpectedRetQty: {dynamics.ExpectedRetQty} → REA_DAT.REA_QTRE\n" +
                   $"  ReturnDispositionCodeID: '{dynamics.ReturnDispositionCodeID}' → REA_DAT.QUA_CODE (RG4)\n" +
                   $"  ReceptionType: '{ApplyReceptionTypeRule(dynamics.ReturnItemNum)}' (RG6)\n" +
                   $"  ACT_CODE: 'COSMETIQUE' (fixe)\n" +
                   $"  CCLI: 'BR' (fixe)\n" +
                   $"  Traçabilité: Lot1={dynamics.inventBatchId}, Lot2={dynamics.inventSerialId}, DLUO={dynamics.expDate}";
        }
    }
}