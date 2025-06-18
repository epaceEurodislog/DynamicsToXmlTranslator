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
        /// Convertit un article Dynamics en article WINDEV - Mapping simplifié API uniquement
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
                    ActCode = dynamics.dataAreaId?.ToUpper() ?? "BR", // dataAreaId → ACT_CODE
                    ArtCode = dynamics.ItemId ?? "", // ItemId → ART_CODE
                    ArtDesl = dynamics.Name ?? "", // Name → ART_DESL

                    // ========== CODE-BARRES ==========
                    ArtEanu = dynamics.itemBarCode ?? "", // itemBarCode → ART_EANU

                    // ========== POIDS ==========
                    ArtPoiu = dynamics.GrossWeight, // GrossWeight → ART_POIU
                    ArtPoin = dynamics.Weight, // Weight → ART_POIN

                    // ========== DIMENSIONS UNITAIRES ==========
                    ArtHaut = dynamics.Height, // Height → ART_HAUT
                    ArtLarg = dynamics.Width, // Width → ART_LARG
                    ArtProf = dynamics.Depth, // Depth → ART_PROF

                    // ========== DIMENSIONS BRUTES ==========
                    ArtHautb = dynamics.grossHeight, // grossHeight → ART_HAUTB
                    ArtLargb = dynamics.grossWidth, // grossWidth → ART_LARGB
                    ArtProfb = dynamics.grossDepth, // grossDepth → ART_PROFB

                    // ========== FACTEURS DE CONDITIONNEMENT ==========
                    ArtColi = dynamics.FactorColli, // FactorColli → ART_COLI
                    ArtPal = dynamics.FactorPallet, // FactorPallet → ART_PAL

                    // ========== UNITÉ ==========
                    ArtUnite = dynamics.UnitId ?? "", // UnitId → ART_UNITE

                    // ========== GROUPES ET CATÉGORIES ==========
                    ArtGroupe = dynamics.ItemGroupId ?? "", // ItemGroupId → ART_GROUPE
                    ArtCateg = dynamics.Category ?? "", // Category → ART_CATEG

                    // ========== STATUTS ET INDICATEURS ==========
                    ArtStat3pl = dynamics.INT3PLStatus ?? "", // INT3PLStatus → ART_STAT3PL
                    ArtHmim = dynamics.HMIMIndicator, // HMIMIndicator → ART_HMIM
                    ArtLifecycle = dynamics.ProductLifecycleStateId ?? "", // ProductLifecycleStateId → ART_LIFECYCLE
                    ArtVersion = dynamics.ProducVersionAttribute ?? "", // ProducVersionAttribute → ART_VERSION

                    // ========== DURÉE DE VIE ==========
                    ArtDdlc = dynamics.PdsShelfLife, // PdsShelfLife → ART_DDLC

                    // ========== SUIVI ET TRAÇABILITÉ ==========
                    ArtLot1 = dynamics.TrackingLot1, // TrackingLot1 → ART_LOT1
                    ArtLot2 = dynamics.TrackingLot2, // TrackingLot2 → ART_LOT2
                    ArtDluo = dynamics.TrackingDLCDDLUO, // TrackingDLCDDLUO → ART_DLUO
                    ArtProof = dynamics.TrackingProoftag, // TrackingProoftag → ART_PROOF

                    // ========== RÉFÉRENCES EXTERNES ==========
                    ArtExtid = dynamics.ExternalItemId ?? "", // ExternalItemId → ART_EXTID
                    ArtIntrastat = dynamics.IntrastatCommodity ?? "", // IntrastatCommodity → ART_INTRASTAT
                    ArtOrigine = dynamics.OrigCountryRegionId ?? "", // OrigCountryRegionId → ART_ORIGINE

                    // ========== VOLUME CALCULÉ ==========
                    ArtVolume = CalculateVolume(dynamics.Height, dynamics.Width, dynamics.Depth)
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
        /// Calcule le volume en cm³ depuis les dimensions
        /// </summary>
        private decimal CalculateVolume(decimal height, decimal width, decimal depth)
        {
            if (height > 0 && width > 0 && depth > 0)
            {
                return height * width * depth;
            }
            return 0;
        }

        /// <summary>
        /// Méthode utilitaire pour obtenir un résumé de l'article mappé
        /// </summary>
        public string GetMappingSummary(Article article)
        {
            if (article?.DynamicsData == null)
                return "Aucune donnée disponible";

            var dynamics = article.DynamicsData;
            var volume = CalculateVolume(dynamics.Height, dynamics.Width, dynamics.Depth);

            return $"=== MAPPING ARTICLE ===\n" +
                   $"API → WINDEV:\n" +
                   $"  ItemId: '{dynamics.ItemId}' → ART_CODE\n" +
                   $"  Name: '{dynamics.Name}' → ART_DESL\n" +
                   $"  dataAreaId: '{dynamics.dataAreaId}' → ACT_CODE\n" +
                   $"  itemBarCode: '{dynamics.itemBarCode}' → ART_EANU\n" +
                   $"  GrossWeight: {dynamics.GrossWeight}g → ART_POIU\n" +
                   $"  Weight: {dynamics.Weight}g → ART_POIN\n" +
                   $"  Dimensions: {dynamics.Height}×{dynamics.Width}×{dynamics.Depth} → Volume: {volume}cm³\n" +
                   $"  FactorColli: {dynamics.FactorColli} → ART_COLI\n" +
                   $"  FactorPallet: {dynamics.FactorPallet} → ART_PAL\n" +
                   $"  Category: '{dynamics.Category}' → ART_CATEG\n" +
                   $"  ItemGroupId: '{dynamics.ItemGroupId}' → ART_GROUPE\n" +
                   $"  Suivi: L1={dynamics.TrackingLot1}, L2={dynamics.TrackingLot2}, DLUO={dynamics.TrackingDLCDDLUO}";
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
    }
}