using System;
using DynamicsToXmlTranslator.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DynamicsToXmlTranslator.Mappers
{
    public class ArticleMapper
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ArticleMapper> _logger;

        public ArticleMapper(IConfiguration configuration, ILogger<ArticleMapper> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Convertit un article Dynamics en article WINDEV selon la correspondance Excel
        /// </summary>
        public WinDevArticle? MapToWinDev(Article article)
        {
            if (article?.DynamicsData == null)
            {
                _logger.LogWarning($"Article {article?.ItemId} n'a pas de données Dynamics");
                return null;
            }

            try
            {
                var dynamics = article.DynamicsData;

                var winDev = new WinDevArticle
                {
                    // ========== IDENTIFIANTS PRINCIPAUX ==========
                    ActCode = "COSMETIQUE", // Fixe pour tous les articles
                    ArtCcli = dynamics.dataAreaId?.ToUpper() ?? "BR", // dataAreaId → ART_CCLI (code client/activité)
                    ArtCode = "BR" + (dynamics.ItemId ?? ""), // "BR" + ItemId → ART_CODE (format: BRSHSEBO500)
                    ArtCodc = dynamics.ItemId ?? "", // ItemId → ART_PAR.ART_CODC
                    ArtDesl = dynamics.Name ?? "", // Name → ART_PAR.ART_DESL

                    // ========== UNITÉ ET CODE-BARRES ==========
                    ArtAlpha2 = !string.IsNullOrEmpty(dynamics.UnitId) ? dynamics.UnitId : "UNITE", // RG11: UNITE par défaut
                    ArtEanu = dynamics.itemBarCode ?? "", // itemBarCode → ART_PAR.ART_EANU

                    // ========== CATÉGORIES ET GROUPES ==========
                    ArtAlpha17 = dynamics.Category ?? "", // Category → ART.ALPHA17
                    ArtAlpha3 = dynamics.OrigCountryRegionId ?? "", // OrigCountryRegionId → ART.ALPHA3

                    // ========== STATUT ET POIDS ==========
                    ArtStat = ConvertProductLifecycleState(dynamics.ProductLifecycleStateId), // RG21: Si 'Non' alors "2" sinon "3"
                    ArtPoiu = dynamics.GrossWeight, // GrossWeight → ART_PAR.ART_POIU

                    // ========== CONDITIONNEMENT ==========
                    ArtQtec = dynamics.FactorColli, // FactorColli → ART_PAR.ART_QTEC
                    ArtQtep = dynamics.FactorPallet == 0 ? 0 : dynamics.FactorPallet, // RG8: Si Gestion VL Palette = 0 alors vide

                    // ========== DURÉE DE VIE ==========
                    ArtNum19 = dynamics.PdsShelfLife > 0 ? dynamics.PdsShelfLife : 1620, // RG10: 1620 par défaut si non géré

                    // ========== IDENTIFIANTS EXTERNES ==========
                    ArtAlpha8 = !string.IsNullOrEmpty(dynamics.ExternalItemId) ? dynamics.ExternalItemId : dynamics.ItemId ?? "", // RG12: Code article par défaut

                    // ========== TRAÇABILITÉ ==========
                    ArtDluo = dynamics.TrackingDLCDDLUO, // TrackingDLCDDLUO → ART_PAR.ART_DLUO
                    ArtLot1 = dynamics.TrackingLot1, // TrackingLot1 → ART_PAR.ART_LOT1
                    ArtLot2 = dynamics.TrackingLot2, // TrackingLot2 → ART_PAR.ART_LOT2
                    ArtNss = dynamics.TrackingProoftag > 0 ? 1 : 0, // RG18: Si gestion des prooftag valeur 1 sinon 0

                    // ========== DIMENSIONS BRUTES ==========
                    ArtLonu = dynamics.grossDepth, // grossDepth → ART_PAR.ART_LONU
                    ArtLaru = dynamics.grossWidth, // grossWidth → ART_PAR.ART_LARU
                    ArtHauu = dynamics.grossHeight, // grossHeight → ART_PAR.ART_HAUU

                    // ========== MATIÈRE DANGEREUSE ==========
                    ArtTop17 = dynamics.HMIMIndicator, // HMIMIndicator → ART_PAR.ART_TOP17

                    // ========== DIMENSIONS NETTES ==========
                    ArtLonc = dynamics.Depth, // Depth → ART_PAR.ART_LONC
                    ArtLarc = dynamics.Width, // Width → ART_PAR.ART_LARC
                    ArtHauc = dynamics.Height, // Height → ART_PAR.ART_HAUC
                    ArtPoic = dynamics.Weight, // Weight → ART_PAR.ART_POIC

                    // ========== CHAMPS AVEC VALEURS PAR DÉFAUT (RG) ==========
                    ArtAlpha14 = "SEC", // RG2: SEC par défaut
                    ArtRstk = "00002", // RG3: 00002 par défaut
                    ArtUni = 1, // RG4: 1 par défaut
                    ArtSpcb = "0", // RG5: 0 par défaut
                    ArtColi = 1, // RG6: 1 par défaut
                    ArtPal = 0, // RG7: 0 par défaut
                    ArtNum18 = 1, // RG9: 1 par défaut
                    ArtAlpha18 = "1510@0@@@@", // RG13: 1510@0@@@@ par défaut
                    ArtAlpha24 = "500@300", // RG14: 500@300 par défaut (corrigé depuis votre "300@500")
                    ArtAlpha26 = "BR", // RG15: BR par défaut
                    ArtNse = "0", // RG17: 0 par défaut
                    ArtEanc = ""
                };

                _logger.LogDebug($"Article mappé: {dynamics.ItemId} → Code: {winDev.ArtCode}, ACT: {winDev.ActCode}");
                return winDev;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors du mapping de l'article {article.ItemId}");
                return null;
            }
        }

        /// <summary>
        /// Convertit ProductLifecycleStateId en code selon RG21
        /// Logique : "Non" = "2", tout le reste = "3"
        /// </summary>
        private string ConvertProductLifecycleState(string? lifecycleState)
        {
            if (string.IsNullOrEmpty(lifecycleState))
                return "3"; // Valeur par défaut pour champ vide

            // RG21: Si 'Non' alors 2 sinon 3
            return lifecycleState.ToUpper().Trim() switch
            {
                "NON" or "NO" => "2", // Cas spécifique NON/NO
                _ => "3" // Tout le reste
            };
        }

        /// <summary>
        /// Valide qu'un article a les données minimales requises
        /// </summary>
        public bool ValidateArticle(Article article)
        {
            if (article?.DynamicsData == null)
                return false;

            var dynamics = article.DynamicsData;

            // Vérifications minimales
            if (string.IsNullOrEmpty(dynamics.ItemId))
            {
                _logger.LogWarning("Article sans ItemId");
                return false;
            }

            if (string.IsNullOrEmpty(dynamics.Name))
            {
                _logger.LogWarning($"Article {dynamics.ItemId} sans nom");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Méthode utilitaire pour obtenir un résumé de l'article mappé
        /// </summary>
        public string GetMappingSummary(Article article)
        {
            if (article?.DynamicsData == null)
                return "Aucune donnée disponible";

            var dynamics = article.DynamicsData;

            return $"=== MAPPING ARTICLE (selon Excel + RG) ===\n" +
                   $"API → SPEED:\n" +
                   $"  dataAreaId: '{dynamics.dataAreaId}' → ART_CCLI (code client)\n" +
                   $"  ItemId: '{dynamics.ItemId}' → ART_PAR.ART_CODC\n" +
                   $"  ART_CODE: 'BR{dynamics.ItemId}' (format BR + ItemId)\n" +
                   $"  Name: '{dynamics.Name}' → ART_PAR.ART_DESL\n" +
                   $"  UnitId: '{dynamics.UnitId}' → ART_PAR.ART_ALPHA2 (RG11: UNITE si vide)\n" +
                   $"  itemBarCode: '{dynamics.itemBarCode}' → ART_PAR.ART_EANU\n" +
                   $"  Category: '{dynamics.Category}' → ART.ALPHA17\n" +
                   $"  OrigCountryRegionId: '{dynamics.OrigCountryRegionId}' → ART.ALPHA3\n" +
                   $"  GrossWeight: {dynamics.GrossWeight}g → ART_PAR.ART_POIU\n" +
                   $"  FactorColli: {dynamics.FactorColli} → ART_PAR.ART_QTEC\n" +
                   $"  FactorPallet: {dynamics.FactorPallet} → ART_PAR.ART_QTEP (RG8)\n" +
                   $"  PdsShelfLife: {dynamics.PdsShelfLife} → ART_PAR.ART_NUM19 (RG10: 1620 si vide)\n" +
                   $"  ACT_CODE: 'COSMETIQUE' (fixe)\n" +
                   $"  Suivi: L1={dynamics.TrackingLot1}, L2={dynamics.TrackingLot2}, DLUO={dynamics.TrackingDLCDDLUO}\n" +
                   $"  Prooftag: {dynamics.TrackingProoftag} → ART_NSS (RG18: 1 si >0, sinon 0)";
        }
    }
}