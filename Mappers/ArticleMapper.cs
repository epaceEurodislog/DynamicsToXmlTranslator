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
        private readonly string _defaultActCode;

        public ArticleMapper(IConfiguration configuration, ILogger<ArticleMapper> logger)
        {
            _configuration = configuration;
            _logger = logger;

            // Valeurs par défaut configurables
            _defaultActCode = _configuration["XmlExport:DefaultActCode"] ?? "GSCF";
        }

        /// <summary>
        /// Convertit un article Dynamics en article WINDEV - Version simplifiée pour tests
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
                    // Code activité - toujours "BR" pour tous les articles
                    ActCode = "BR",

                    // Code article
                    ArtCode = dynamics.ItemId ?? "",

                    // Description courte - commenté car le champ n'existe pas encore
                    // ArtDesc = TruncateString(dynamics.ItemName, 30),

                    // Description longue (utilise DisplayProductNumber)
                    ArtDesl = dynamics.DisplayProductNumber ?? dynamics.ItemName ?? ""
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
        /// Tronque une chaîne à la longueur maximale
        /// </summary>
        private string TruncateString(string? value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }
}