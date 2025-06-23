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
                    ArtCodc = dynamics.ItemId ?? "", // ItemId → ART_PAR.ART_CODC
                    ArtDesl = dynamics.Name ?? "", // Name → ART_PAR.ART_DESL

                    // ========== UNITÉ ET CODE-BARRES ==========
                    ArtAlpha2 = dynamics.UnitId ?? "", // UnitId → ART_PAR.ART_ALPHA2
                    ArtEanu = dynamics.itemBarCode ?? "", // itemBarCode → ART_PAR.ART_EANU

                    // ========== CATÉGORIES ET GROUPES ==========
                    Alpha17 = dynamics.Category ?? "", // Category → ART.ALPHA17
                    Alpha3 = dynamics.OrigCountryRegionId ?? "", // OrigCountryRegionId → ART.ALPHA3

                    // ========== STATUT ET POIDS ==========
                    ArtStat = dynamics.ProductLifecycleStateId ?? "", // ProductLifecycleStateId → ART_PAR.ART_STAT
                    ArtPoiu = dynamics.GrossWeight, // GrossWeight → ART_PAR.ART_POIU

                    // ========== CONDITIONNEMENT ==========
                    ArtQtec = dynamics.FactorColli, // FactorColli → ART_PAR.ART_QTEC
                    ArtQtep = dynamics.FactorPallet, // FactorPallet → ART_PAR.ART_QTEP

                    // ========== DURÉE DE VIE ==========
                    ArtNum19 = dynamics.PdsShelfLife, // PdsShelfLife → ART_PAR.ART_NUM19

                    // ========== IDENTIFIANTS EXTERNES ==========
                    ArtAlpha8 = dynamics.ExternalItemId ?? "", // ExternalItemId → ART_PAR.ART_ALPHA8

                    // ========== TRAÇABILITÉ ==========
                    ArtDluo = dynamics.TrackingDLCDDLUO, // TrackingDLCDDLUO → ART_PAR.ART_DLUO
                    ArtLot1 = dynamics.TrackingLot1, // TrackingLot1 → ART_PAR.ART_LOT1
                    ArtLot2 = dynamics.TrackingLot2, // TrackingLot2 → ART_PAR.ART_LOT2
                    ArtNss = dynamics.TrackingProoftag, // TrackingProoftag → ART_PAR.ART_NSS

                    // ========== RÉTIQUETAGE ==========
                    Top1 = ConvertVersionAttributeToInt(dynamics.ProducVersionAttribute), // ProducVersionAttribute → ART.TOP1

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

                    // ========== CHAMPS NON MAPPÉS (valeurs par défaut) ==========
                    ArtAlpha14 = "",
                    ArtRstk = "",
                    ArtUni = 0,
                    ArtSpcb = "",
                    ArtColi = 0,
                    ArtPal = 0,
                    ArtNum18 = 0,
                    ArtAlpha18 = "",
                    ArtAlpha24 = "",
                    ArtAlpha26 = "",
                    ArtNse = "",
                    ArtEanc = ""
                };

                _logger.LogDebug($"Article mappé: {dynamics.ItemId} → Code: {winDev.ArtCodc}, ACT: {winDev.ActCode}");
                return winDev;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors du mapping de l'article {article.ItemId}");
                return null;
            }
        }

        /// <summary>
        /// Convertit ProducVersionAttribute en entier pour TOP1
        /// </summary>
        private int ConvertVersionAttributeToInt(string? versionAttribute)
        {
            if (string.IsNullOrEmpty(versionAttribute))
                return 0;

            // Logique de conversion selon vos besoins
            return versionAttribute.ToUpper() switch
            {
                "YES" or "OUI" or "Y" or "O" => 1,
                "NO" or "NON" or "N" => 0,
                _ => int.TryParse(versionAttribute, out var result) ? result : 0
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

            return $"=== MAPPING ARTICLE (selon Excel) ===\n" +
                   $"API → SPEED:\n" +
                   $"  dataAreaId: '{dynamics.dataAreaId}' → ART_CCLI (code client)\n" +
                   $"  ItemId: '{dynamics.ItemId}' → ART_PAR.ART_CODC\n" +
                   $"  Name: '{dynamics.Name}' → ART_PAR.ART_DESL\n" +
                   $"  UnitId: '{dynamics.UnitId}' → ART_PAR.ART_ALPHA2\n" +
                   $"  itemBarCode: '{dynamics.itemBarCode}' → ART_PAR.ART_EANU\n" +
                   $"  Category: '{dynamics.Category}' → ART.ALPHA17\n" +
                   $"  OrigCountryRegionId: '{dynamics.OrigCountryRegionId}' → ART.ALPHA3\n" +
                   $"  GrossWeight: {dynamics.GrossWeight}g → ART_PAR.ART_POIU\n" +
                   $"  FactorColli: {dynamics.FactorColli} → ART_PAR.ART_QTEC\n" +
                   $"  FactorPallet: {dynamics.FactorPallet} → ART_PAR.ART_QTEP\n" +
                   $"  PdsShelfLife: {dynamics.PdsShelfLife} → ART_PAR.ART_NUM19\n" +
                   $"  ACT_CODE: 'COSMETIQUE' (fixe)\n" +
                   $"  Suivi: L1={dynamics.TrackingLot1}, L2={dynamics.TrackingLot2}, DLUO={dynamics.TrackingDLCDDLUO}";
        }
    }
}