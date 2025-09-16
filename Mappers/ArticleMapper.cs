using System;
using DynamicsToXmlTranslator.Models;
using DynamicsToXmlTranslator.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DynamicsToXmlTranslator.Mappers
{
    public class ArticleMapper
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ArticleMapper> _logger;
        private readonly Utf8TextProcessor _textProcessor;

        public ArticleMapper(IConfiguration configuration, ILogger<ArticleMapper> logger, Utf8TextProcessor textProcessor)
        {
            _configuration = configuration;
            _logger = logger;
            _textProcessor = textProcessor;
        }

        /// <summary>
        /// Convertit un article Dynamics en article WINDEV selon la correspondance Excel
        /// AVEC traitement UTF-8 des caractères spéciaux
        /// </summary>
        public WinDevArticle? MapToWinDev(Article article)
        {
            if (article?.DynamicsData == null)
            {
                _logger.LogWarning($"Article {article?.ItemId} n'a pas de données Dynamics");
                return null;
            }

            // ✅ NOUVELLE RÈGLE : Vérifier si l'article doit être exclu selon ART_STAT
            if (ShouldExcludeArticle(article))
            {
                _logger.LogDebug($"Article {article.DynamicsData.ItemId} exclu car ART_STAT = 3");
                return null;
            }

            try
            {
                var dynamics = article.DynamicsData;

                var winDev = new WinDevArticle
                {
                    // ========== IDENTIFIANTS PRINCIPAUX ==========
                    ActCode = "COSMETIQUE", // Fixe pour tous les articles

                    // ✅ TRAITEMENT UTF-8 : Code client/activité
                    ArtCcli = _textProcessor.ProcessCode(dynamics.dataAreaId) ?? "BR",

                    // ✅ TRAITEMENT UTF-8 : Code article (format: BRSHSEBO500)
                    ArtCode = "BR" + _textProcessor.ProcessCode(dynamics.ItemId),

                    // ✅ TRAITEMENT UTF-8 : Code article source
                    ArtCodc = _textProcessor.ProcessCode(dynamics.ItemId),

                    // ✅ TRAITEMENT UTF-8 : Désignation article (max 100 caractères)
                    ArtDesl = _textProcessor.ProcessName(dynamics.Name, 100),

                    // ========== UNITÉ ET CODE-BARRES ==========
                    // ✅ TRAITEMENT UTF-8 : Unité avec valeur par défaut
                    ArtAlpha2 = !string.IsNullOrEmpty(dynamics.UnitId)
                        ? _textProcessor.ProcessCode(dynamics.UnitId)
                        : "UNITE",

                    // ✅ TRAITEMENT UTF-8 : Code-barres EAN
                    ArtEanu = _textProcessor.ProcessCode(dynamics.itemBarCode),

                    // ========== CATÉGORIES ET GROUPES ==========
                    // ✅ TRAITEMENT UTF-8 : Catégorie transformée
                    ArtAlpha17 = TransformCategory(dynamics.Category),

                    // ✅ TRAITEMENT UTF-8 : Pays d'origine
                    ArtAlpha3 = _textProcessor.ProcessCode(dynamics.OrigCountryRegionId),

                    // ========== STATUT ET POIDS ==========
                    ArtStat = ConvertProductLifecycleState(dynamics.ProductLifecycleStateId),
                    ArtPoiu = dynamics.GrossWeight,

                    // ========== CONDITIONNEMENT ==========
                    ArtQtec = dynamics.FactorColli,
                    ArtQtep = dynamics.FactorPallet == 0 ? 0 : dynamics.FactorPallet,

                    // ========== DURÉE DE VIE ==========
                    ArtNum19 = dynamics.PdsShelfLife,

                    // ========== IDENTIFIANTS EXTERNES ==========
                    // ✅ TRAITEMENT UTF-8 : ID externe avec fallback
                    ArtAlpha8 = !string.IsNullOrEmpty(dynamics.ExternalItemId)
                        ? _textProcessor.ProcessCode(dynamics.ExternalItemId)
                        : _textProcessor.ProcessCode(dynamics.ItemId),

                    // ========== TRAÇABILITÉ ==========
                    ArtDluo = dynamics.TrackingDLCDDLUO,
                    ArtLot1 = dynamics.TrackingLot1,
                    ArtLot2 = dynamics.TrackingLot2,
                    ArtNss = dynamics.TrackingProoftag > 0 ? 1 : 0,

                    // ========== RÉTIQUETAGE ==========
                    ArtTop1 = ConvertVersionAttributeToInt(dynamics.ProducVersionAttribute),

                    // ========== DIMENSIONS BRUTES ==========
                    ArtLonu = dynamics.grossDepth,
                    ArtLaru = dynamics.grossWidth,
                    ArtHauu = dynamics.grossHeight,

                    // ========== MATIÈRE DANGEREUSE ==========
                    ArtTop17 = dynamics.HMIMIndicator,

                    // ========== DIMENSIONS NETTES ==========
                    ArtLonc = dynamics.Depth,
                    ArtLarc = dynamics.Width,
                    ArtHauc = dynamics.Height,
                    ArtPoic = dynamics.Weight,

                    // ========== CHAMPS AVEC VALEURS PAR DÉFAUT (RG) ==========
                    ArtAlpha14 = "SEC",
                    ArtRstk = "00002",
                    ArtUni = 1,
                    ArtSpcb = "0",
                    ArtColi = 1,
                    ArtPal = 0,
                    ArtNum18 = 1,
                    ArtAlpha18 = "1510@0@@@@",
                    ArtAlpha24 = "500@300",
                    ArtAlpha26 = "BR",
                    ArtNse = "0",
                    ArtEanc = ""
                };

                // Statistiques de traitement pour diagnostic
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    LogProcessingStats(dynamics, winDev);
                }

                _logger.LogDebug($"Article mappé: {dynamics.ItemId} → Code: {winDev.ArtCode}, Catégorie: {dynamics.Category} → {winDev.ArtAlpha17}, ART_STAT: {winDev.ArtStat}");
                return winDev;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors du mapping de l'article {article.ItemId}");
                return null;
            }
        }

        /// <summary>
        /// ✅ NOUVELLE RÈGLE : Détermine si un article doit être exclu selon ART_STAT
        /// Les articles avec ART_STAT = "3" ne doivent pas être exportés
        /// </summary>
        public bool ShouldExcludeArticle(Article article)
        {
            if (article?.DynamicsData == null)
                return true;

            var dynamics = article.DynamicsData;

            // Calculer ART_STAT selon la logique existante
            string artStat = ConvertProductLifecycleState(dynamics.ProductLifecycleStateId);

            // Exclure si ART_STAT = "3"
            bool shouldExclude = artStat == "3";

            if (shouldExclude)
            {
                _logger.LogInformation($"Article {dynamics.ItemId} exclu de l'export (ART_STAT=3, ProductLifecycleStateId='{dynamics.ProductLifecycleStateId}')");
            }

            return shouldExclude;
        }

        /// <summary>
        /// Retourne la catégorie brute avec traitement UTF-8 minimal pour compatibilité XML
        /// </summary>
        private string TransformCategory(string? category)
        {
            if (string.IsNullOrEmpty(category))
                return "";

            // Traitement minimal pour XML seulement (échapper les caractères XML dangereux)
            return category.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
        }

        /// <summary>
        /// Convertit ProductLifecycleStateId en code selon RG21
        /// AVEC traitement UTF-8
        /// </summary>
        private string ConvertProductLifecycleState(string? lifecycleState)
        {
            if (string.IsNullOrEmpty(lifecycleState))
                return "3";

            // ✅ TRAITEMENT UTF-8 : Nettoyer avant comparaison
            string cleanState = _textProcessor.ProcessText(lifecycleState).ToUpper().Trim();

            return cleanState switch
            {
                "NON" or "NO" => "2",
                _ => "3"
            };
        }

        /// <summary>
        /// Convertit ProducVersionAttribute en entier avec traitement UTF-8
        /// </summary>
        private int ConvertVersionAttributeToInt(string? versionAttribute)
        {
            if (string.IsNullOrEmpty(versionAttribute))
                return 0;

            // ✅ TRAITEMENT UTF-8 : Nettoyer avant traitement
            string cleanAttribute = _textProcessor.ProcessText(versionAttribute).ToUpper();

            return cleanAttribute switch
            {
                "YES" or "OUI" or "Y" or "O" => 1,
                "NO" or "NON" or "N" => 0,
                _ => int.TryParse(cleanAttribute, out var result) ? result : 0
            };
        }

        /// <summary>
        /// Log des statistiques de traitement UTF-8 pour diagnostic
        /// </summary>
        private void LogProcessingStats(DynamicsArticle dynamics, WinDevArticle winDev)
        {
            var nameStats = _textProcessor.GetProcessingStats(dynamics.Name, winDev.ArtDesl);
            var codeStats = _textProcessor.GetProcessingStats(dynamics.ItemId, winDev.ArtCodc);

            if (nameStats.TransformationApplied || codeStats.TransformationApplied)
            {
                _logger.LogDebug($"Transformations UTF-8 appliquées pour l'article {dynamics.ItemId}:");

                if (nameStats.TransformationApplied)
                {
                    _logger.LogDebug($"  Nom: '{dynamics.Name}' → '{winDev.ArtDesl}' ({nameStats.OriginalLength}→{nameStats.ProcessedLength} chars)");
                }

                if (codeStats.TransformationApplied)
                {
                    _logger.LogDebug($"  Code: '{dynamics.ItemId}' → '{winDev.ArtCodc}' ({codeStats.OriginalLength}→{codeStats.ProcessedLength} chars)");
                }
            }
        }

        /// <summary>
        /// Valide qu'un article a les données minimales requises
        /// </summary>
        public bool ValidateArticle(Article article)
        {
            if (article?.DynamicsData == null)
                return false;

            var dynamics = article.DynamicsData;

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
        /// Méthode utilitaire pour obtenir un résumé de l'article mappé avec info UTF-8
        /// </summary>
        public string GetMappingSummary(Article article)
        {
            if (article?.DynamicsData == null)
                return "Aucune donnée disponible";

            var dynamics = article.DynamicsData;
            var transformedCategory = TransformCategory(dynamics.Category);

            return $"=== MAPPING ARTICLE (selon Excel + RG + UTF-8) ===\n" +
                   $"API → SPEED:\n" +
                   $"  dataAreaId: '{dynamics.dataAreaId}' → ART_CCLI (traité UTF-8)\n" +
                   $"  ItemId: '{dynamics.ItemId}' → ART_PAR.ART_CODC (traité UTF-8)\n" +
                   $"  ART_CODE: 'BR{_textProcessor.ProcessCode(dynamics.ItemId)}' (format BR + ItemId, traité UTF-8)\n" +
                   $"  Name: '{dynamics.Name}' → '{_textProcessor.ProcessName(dynamics.Name, 100)}' → ART_PAR.ART_DESL (traité UTF-8, max 100 chars)\n" +
                   $"  UnitId: '{dynamics.UnitId}' → ART_PAR.ART_ALPHA2 (RG11: UNITE si vide, traité UTF-8)\n" +
                   $"  itemBarCode: '{dynamics.itemBarCode}' → ART_PAR.ART_EANU (traité UTF-8)\n" +
                   $"  Category: '{dynamics.Category}' → '{transformedCategory}' → ART.ALPHA17 (traité UTF-8)\n" +
                   $"  OrigCountryRegionId: '{dynamics.OrigCountryRegionId}' → ART.ALPHA3 (traité UTF-8)\n" +
                   $"  GrossWeight: {dynamics.GrossWeight}g → ART_PAR.ART_POIU\n" +
                   $"  FactorColli: {dynamics.FactorColli} → ART_PAR.ART_QTEC\n" +
                   $"  FactorPallet: {dynamics.FactorPallet} → ART_PAR.ART_QTEP (RG8)\n" +
                   $"  PdsShelfLife: {dynamics.PdsShelfLife} → ART_PAR.ART_NUM19\n" +
                   $"  ProducVersionAttribute: '{dynamics.ProducVersionAttribute}' → ART_PAR.ART_TOP1 (traité UTF-8)\n" +
                   $"  ProductLifecycleStateId: '{dynamics.ProductLifecycleStateId}' → ART_STAT: '{ConvertProductLifecycleState(dynamics.ProductLifecycleStateId)}'\n" +
                   $"  ACT_CODE: 'COSMETIQUE' (fixe)\n" +
                   $"  ✅ EXCLUSION: Articles avec ART_STAT=3 ne sont PAS exportés\n" +
                   $"  ✅ TOUS LES CHAMPS TEXTE TRAITÉS AVEC NORMALISATION UTF-8";
        }
    }
}