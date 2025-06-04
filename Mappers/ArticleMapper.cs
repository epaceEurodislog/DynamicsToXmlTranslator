using System;
using System.Globalization;
using DynamicsToXmlTranslator.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DynamicsToXmlTranslator.Mappers
{
    public class ArticleMapper
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ArticleMapper> _logger;
        private readonly string _defaultActCode;
        private readonly string _defaultTieCode;

        public ArticleMapper(IConfiguration configuration, ILogger<ArticleMapper> logger)
        {
            _configuration = configuration;
            _logger = logger;

            // Valeurs par défaut configurables
            _defaultActCode = _configuration["XmlExport:DefaultActCode"] ?? "GSCF";
            _defaultTieCode = _configuration["XmlExport:DefaultTieCode"] ?? "1040";
        }

        /// <summary>
        /// Convertit un article Dynamics en article WINDEV
        /// </summary>
        public WinDevArticle MapToWinDev(Article article)
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
                    // Code activité (à adapter selon votre logique métier)
                    ActCode = _defaultActCode,

                    // Code article
                    ArtCode = dynamics.ItemId ?? "",

                    // Code tiers (fournisseur)
                    TieCode = MapVendorCode(dynamics.VendorAccountNumber),

                    // Description courte
                    ArtDesc = TruncateString(dynamics.ItemName, 30),

                    // Description longue
                    ArtDesl = dynamics.ItemDescription ?? dynamics.ItemName ?? "",

                    // Code-barres EAN
                    ArtEanu = dynamics.BarcodeNumber ?? "",
                    ArtEanc = "", // Code-barres carton (si disponible)

                    // Quantités
                    ArtQtec = 1, // Quantité par colis (à adapter)
                    ArtQtep = 0, // Quantité en palette (à adapter)

                    // Statut (à adapter selon votre logique)
                    ArtStat = DetermineStatus(dynamics),

                    // Valeurs numériques
                    ArtNval = 0, // À définir selon votre besoin

                    // Unité de mesure
                    ArtAlpha2 = MapUnitOfMeasure(dynamics.SalesUnitSymbol),

                    // DLC (jours)
                    ArtDdlc = dynamics.ShelfLifePeriodDays > 0 ? 1 : 0,

                    // Code article alternatif
                    ArtAlpha8 = dynamics.VendorItemNumber ?? dynamics.ItemId ?? "",

                    // Unité de mesure pour le poids
                    ArtAlpha9 = "KGM", // Kilogrammes par défaut

                    // Poids (conversion en grammes si nécessaire)
                    ArtPoiu = ConvertToGrams(dynamics.NetWeight),

                    // Prix
                    ArtPrix = dynamics.SalesPrice,

                    // Type de produit
                    ArtAlpha14 = MapProductType(dynamics.ProductTypeId),

                    // Unité
                    ArtUni = 1,

                    // Durée de vie en jours
                    ArtNum19 = dynamics.ShelfLifePeriodDays,

                    // Autres valeurs par défaut
                    ArtNum21 = 0,
                    ArtAlpha18 = "1510@0@@@@", // Valeur par défaut du XML exemple
                    ArtAlpha15 = "",
                    ArtTpve = 0,
                    ArtTpvs = 0,
                    ArtAlpha26 = "LTRF", // À adapter selon votre logique
                    ArtAlpha25 = "",
                    ArtTop18 = 0,
                    ArtAlpha19 = ""
                };

                return winDev;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors du mapping de l'article {article.ItemId}");
                return null;
            }
        }

        /// <summary>
        /// Mappe le code fournisseur
        /// </summary>
        private string MapVendorCode(string vendorAccountNumber)
        {
            if (string.IsNullOrEmpty(vendorAccountNumber))
                return _defaultTieCode;

            // Vous pouvez ajouter ici une logique de mapping spécifique
            // Par exemple, une table de correspondance ou des règles métier
            return vendorAccountNumber;
        }

        /// <summary>
        /// Détermine le statut de l'article
        /// </summary>
        private int DetermineStatus(DynamicsArticle dynamics)
        {
            // Logique à adapter selon vos règles métier
            // Par exemple : 4 = actif, 2 = inactif, etc.
            return 4;
        }

        /// <summary>
        /// Mappe l'unité de mesure
        /// </summary>
        private string MapUnitOfMeasure(string salesUnitSymbol)
        {
            if (string.IsNullOrEmpty(salesUnitSymbol))
                return "COLIS";

            // Mapping des unités Dynamics vers WINDEV
            return salesUnitSymbol.ToUpper() switch
            {
                "PC" => "PIECE",
                "KG" => "KG",
                "L" => "LITRE",
                "M" => "METRE",
                _ => "COLIS"
            };
        }

        /// <summary>
        /// Convertit le poids en grammes
        /// </summary>
        private int ConvertToGrams(decimal weight)
        {
            // Supposant que le poids est en kg dans Dynamics
            return (int)(weight * 1000);
        }

        /// <summary>
        /// Mappe le type de produit
        /// </summary>
        private string MapProductType(string productTypeId)
        {
            if (string.IsNullOrEmpty(productTypeId))
                return "SEC";

            // Mapping selon vos règles métier
            return productTypeId.ToUpper() switch
            {
                "FROZEN" => "SURGELE",
                "FRESH" => "FRAIS",
                "DRY" => "SEC",
                _ => "SEC"
            };
        }

        /// <summary>
        /// Tronque une chaîne à la longueur maximale
        /// </summary>
        private string TruncateString(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }
}