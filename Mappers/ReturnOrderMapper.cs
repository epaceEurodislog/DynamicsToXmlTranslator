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
                    OpeCcli = dynamics.dataAreaId?.ToUpper() ?? "BR", // "br" → "BR"
                    ReaRfce = dynamics.ReturnItemNum ?? "", // "OR000021" → REA_DAT.REA_RFCE
                    ReaRfti = dynamics.SalesId ?? "", // "SO000191" → REA_DAT.REA_RFTI
                    ReaDalp = FormatDateForXml(dynamics.ReturnDeadline), // "2025-06-30T12:00:00Z"

                    // ========== FOURNISSEUR/CLIENT ==========
                    ReaCtaf = dynamics.CustAccount ?? "", // "C0000002" → REA_DAT.REA_CTAF
                    SalesName = dynamics.SalesName ?? "", // "ALMEDA ENTERPRISE COMPANY LTD"

                    // ========== DÉTAILS LIGNE ==========
                    ReaNoLr = dynamics.LineNum, // 1 → REA_DAT.REA_NoLR
                    ArtCode = dynamics.ItemId ?? "", // "BAINP200" → REA_DAT.ART_CODE
                    ReaQtre = dynamics.ExpectedRetQty, // 0 → REA_DAT.REA_QTRE

                    // ========== TRAÇABILITÉ (avec RG7 et RG8) ==========
                    ReaLot1 = ApplyLotRule(dynamics.inventBatchId, dynamics.ItemId, 1), // "" → REA_DAT.REA_LOT1
                    ReaLot2 = ApplyLotRule(dynamics.inventSerialId, dynamics.ItemId, 2), // "" → REA_DAT.REA_LOT2
                    ReaDluo = FormatDateForXml(dynamics.expDate), // "1900-01-01T12:00:00Z"

                    // ========== SUPPORT ET COMMENTAIRES ==========
                    ReaNoSu = "", // Numéro support - pas disponible dans vos données
                    ReaCom = dynamics.Notes ?? "", // "" → REA_DAT.REA_COM

                    // ========== CODE QUALITÉ (RG4) ==========
                    QuaCode = ApplyQualityCodeRule(dynamics.ReturnDispositionCodeId), // ✅ CORRIGÉ: Id au lieu de ID

                    // ========== RÉFÉRENCE RÉSERVATION ==========
                    ReaRfaf = "", // Référence réservation - pas dans vos données

                    // ========== VALEURS FIXES ==========
                    ActCode = "COSMETIQUE", // VALEUR FIXE
                    Ccli = "BR", // VALEUR FIXE

                    // ========== CHAMPS AVEC RÈGLES DE GESTION ==========
                    ReaAlpha5 = ApplyQualityControlRule(dynamics.ItemId), // RG5
                    ReaAlpha1 = ApplyReceptionTypeRule(dynamics.ReturnItemNum), // RG6 : "OR000021" → "DESADV"
                    ReaAlpha11 = "NIVEAU3", // VALEUR FIXE
                    ReaAlpha12 = "NORMAL", // VALEUR FIXE

                    // ========== CHAMPS SUPPLÉMENTAIRES ==========
                    SalesStatus = dynamics.SalesStatus ?? "", // "Backorder"
                    ReturnDispositionCodeId = dynamics.ReturnDispositionCodeId ?? "", // ✅ CORRIGÉ: Id au lieu de ID
                    InventLocationId = dynamics.InventLocationId ?? "", // "12"
                    
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
        /// Mapping depuis ReturnDispositionCodeId vers code qualité
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
        /// Gère le cas spécial "1900-01-01T12:00:00Z" qui signifie "pas de date"
        /// </summary>
        private string FormatDateForXml(DateTime? date)
        {
            if (date == null || date == DateTime.MinValue)
                return "";

            // Si la date est 1900-01-01, considérer comme vide
            if (date.Value.Year == 1900)
                return "";

            // Format date pour WINDEV
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

            if (dynamics.ExpectedRetQty < 0) // ✅ CORRIGÉ: Permet les quantités 0
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
                   $"  ReturnDispositionCodeId: '{dynamics.ReturnDispositionCodeId}' → REA_DAT.QUA_CODE (RG4)\n" + // ✅ CORRIGÉ
                   $"  ReceptionType: '{ApplyReceptionTypeRule(dynamics.ReturnItemNum)}' (RG6)\n" +
                   $"  ACT_CODE: 'COSMETIQUE' (fixe)\n" +
                   $"  CCLI: 'BR' (fixe)\n" +
                   $"  Traçabilité: Lot1={dynamics.inventBatchId}, Lot2={dynamics.inventSerialId}, DLUO={dynamics.expDate}";
        }
    }
}