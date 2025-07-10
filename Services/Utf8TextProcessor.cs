using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace DynamicsToXmlTranslator.Services
{
    /// <summary>
    /// Service de traitement des caractères spéciaux et normalisation UTF-8 pour export XML
    /// À placer dans le fichier : Services/Utf8TextProcessor.cs
    /// </summary>
    public class Utf8TextProcessor
    {
        private readonly ILogger<Utf8TextProcessor> _logger;

        // Dictionnaire de mapping des caractères spéciaux vers leurs équivalents ASCII/XML
        private readonly Dictionary<string, string> _characterMapping;

        // Regex pour détecter les caractères non-ASCII
        private readonly Regex _nonAsciiRegex;

        public Utf8TextProcessor(ILogger<Utf8TextProcessor> logger)
        {
            _logger = logger;
            _characterMapping = InitializeCharacterMapping();
            _nonAsciiRegex = new Regex(@"[^\x00-\x7F]", RegexOptions.Compiled);
        }

        /// <summary>
        /// Traite et normalise un texte pour l'export XML
        /// </summary>
        /// <param name="input">Texte d'entrée pouvant contenir des caractères spéciaux</param>
        /// <param name="maxLength">Longueur maximale du texte de sortie (optionnel)</param>
        /// <returns>Texte normalisé compatible XML</returns>
        public string ProcessText(string? input, int? maxLength = null)
        {
            if (string.IsNullOrEmpty(input))
                return "";

            try
            {
                // Étape 1: Normalisation Unicode (décomposition puis recomposition)
                string normalized = input.Normalize(NormalizationForm.FormKD);

                // Étape 2: Remplacement des caractères spéciaux connus
                string processed = ReplaceSpecialCharacters(normalized);

                // Étape 3: Suppression des caractères de contrôle et invisibles
                processed = RemoveControlCharacters(processed);

                // Étape 4: Conversion des caractères accentués restants
                processed = RemoveAccents(processed);

                // Étape 5: Échappement des caractères XML spéciaux
                processed = EscapeXmlCharacters(processed);

                // Étape 6: Limitation de longueur si spécifiée
                if (maxLength.HasValue && processed.Length > maxLength.Value)
                {
                    processed = processed.Substring(0, maxLength.Value);
                    _logger.LogDebug($"Texte tronqué à {maxLength.Value} caractères: '{input}' → '{processed}'");
                }

                // Étape 7: Validation finale
                ValidateXmlCompatibility(processed);

                if (input != processed)
                {
                    _logger.LogTrace($"Texte transformé: '{input}' → '{processed}'");
                }

                return processed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors du traitement du texte: '{input}'");

                // En cas d'erreur, retourner une version basique nettoyée
                return CleanBasicText(input, maxLength);
            }
        }

        /// <summary>
        /// Traite spécifiquement les codes articles et identifiants
        /// </summary>
        /// <param name="code">Code à traiter</param>
        /// <returns>Code normalisé</returns>
        public string ProcessCode(string? code)
        {
            if (string.IsNullOrEmpty(code))
                return "";

            // Pour les codes, on est plus strict : uniquement alphanumériques et quelques caractères spéciaux
            string processed = ProcessText(code);

            // Remplacer les espaces par des underscores dans les codes
            processed = processed.Replace(" ", "_");

            // Supprimer tous les caractères non autorisés dans les codes
            processed = Regex.Replace(processed, @"[^a-zA-Z0-9_\-.]", "");

            return processed.ToUpper(); // Codes en majuscules par convention
        }

        /// <summary>
        /// Traite les noms et descriptions avec préservation maximale
        /// </summary>
        /// <param name="name">Nom ou description à traiter</param>
        /// <param name="maxLength">Longueur maximale</param>
        /// <returns>Nom normalisé</returns>
        public string ProcessName(string? name, int? maxLength = null)
        {
            if (string.IsNullOrEmpty(name))
                return "";

            // Pour les noms, on préserve plus de caractères
            string processed = ProcessText(name, maxLength);

            // Nettoyer les espaces multiples
            processed = Regex.Replace(processed, @"\s+", " ");

            // Supprimer les espaces en début/fin
            processed = processed.Trim();

            return processed;
        }

        /// <summary>
        /// Initialise le dictionnaire de mapping des caractères spéciaux
        /// </summary>
        private Dictionary<string, string> InitializeCharacterMapping()
        {
            return new Dictionary<string, string>
            {
                // ✅ RÈGLE SPÉCIALE : & devient "et" (pas d'échappement XML)
                {"&", "et"},
                
                // ✅ RÈGLES COMPLÉMENTAIRES FRANÇAISES
                {"&amp;", "et"}, // Au cas où & serait déjà échappé
                {" & ", " et "}, // & entouré d'espaces
                {" et ", " et "}, // Normalisation (éviter double transformation)
                
                // Caractères français courants (é → e, etc.)
                {"à", "a"}, {"á", "a"}, {"â", "a"}, {"ã", "a"}, {"ä", "a"}, {"å", "a"},
                {"è", "e"}, {"é", "e"}, {"ê", "e"}, {"ë", "e"},
                {"ì", "i"}, {"í", "i"}, {"î", "i"}, {"ï", "i"},
                {"ò", "o"}, {"ó", "o"}, {"ô", "o"}, {"õ", "o"}, {"ö", "o"},
                {"ù", "u"}, {"ú", "u"}, {"û", "u"}, {"ü", "u"},
                {"ç", "c"}, {"ñ", "n"},
                {"ÿ", "y"}, {"ý", "y"},
                
                // Majuscules
                {"À", "A"}, {"Á", "A"}, {"Â", "A"}, {"Ã", "A"}, {"Ä", "A"}, {"Å", "A"},
                {"È", "E"}, {"É", "E"}, {"Ê", "E"}, {"Ë", "E"},
                {"Ì", "I"}, {"Í", "I"}, {"Î", "I"}, {"Ï", "I"},
                {"Ò", "O"}, {"Ó", "O"}, {"Ô", "O"}, {"Õ", "O"}, {"Ö", "O"},
                {"Ù", "U"}, {"Ú", "U"}, {"Û", "U"}, {"Ü", "U"},
                {"Ç", "C"}, {"Ñ", "N"},
                {"Ÿ", "Y"}, {"Ý", "Y"},
                
                // Caractères spéciaux courants
                {"œ", "oe"}, {"Œ", "OE"},
                {"æ", "ae"}, {"Æ", "AE"},
                {"ß", "ss"},
                
                // Devises et symboles
                {"€", "EUR"}, {"$", "USD"}, {"£", "GBP"},
                {"°", "deg"}, {"©", "(C)"}, {"®", "(R)"}, {"™", "(TM)"},
                
                // Guillemets et apostrophes
                {""", "\""}, {""", "\""}, {"'", "'"}, {"'", "'"},
                {"«", "\""}, {"»", "\""},
                
                // Tirets et espaces spéciaux
                {"–", "-"}, {"—", "-"}, {" ", " "}, {" ", " "},
                
                // Caractères mathématiques courants
                {"×", "x"}, {"÷", "/"}, {"±", "+/-"},
                
                // Caractères de ponctuation spéciaux
                {"…", "..."}, {"‚", ","}, {"„", "\""},
                
                // Caractères problématiques pour XML
                {"\u0000", ""}, {"\u0001", ""}, {"\u0002", ""}, {"\u0003", ""}, {"\u0004", ""},
                {"\u0005", ""}, {"\u0006", ""}, {"\u0007", ""}, {"\u0008", ""},
                {"\u000B", ""}, {"\u000C", ""}, {"\u000E", ""}, {"\u000F", ""},
                {"\u0010", ""}, {"\u0011", ""}, {"\u0012", ""}, {"\u0013", ""}, {"\u0014", ""},
                {"\u0015", ""}, {"\u0016", ""}, {"\u0017", ""}, {"\u0018", ""}, {"\u0019", ""},
                {"\u001A", ""}, {"\u001B", ""}, {"\u001C", ""}, {"\u001D", ""}, {"\u001E", ""},
                {"\u001F", ""}, {"\u007F", ""}
            };
        }

        /// <summary>
        /// Remplace les caractères spéciaux connus
        /// </summary>
        private string ReplaceSpecialCharacters(string input)
        {
            foreach (var mapping in _characterMapping)
            {
                input = input.Replace(mapping.Key, mapping.Value);
            }
            return input;
        }

        /// <summary>
        /// Supprime les caractères de contrôle Unicode
        /// </summary>
        private string RemoveControlCharacters(string input)
        {
            var result = new StringBuilder();

            foreach (char c in input)
            {
                // Garder les caractères imprimables et les espaces/tabulations/retours ligne
                if (!char.IsControl(c) || c == '\t' || c == '\n' || c == '\r')
                {
                    result.Append(c);
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Supprime les accents des caractères restants
        /// </summary>
        private string RemoveAccents(string input)
        {
            var normalizedString = input.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

        /// <summary>
        /// Échappe les caractères spéciaux XML (SAUF & qui est déjà traité comme "et")
        /// </summary>
        private string EscapeXmlCharacters(string input)
        {
            // ✅ IMPORTANT : Ne PAS échapper & car il a déjà été remplacé par "et" dans ReplaceSpecialCharacters
            return input
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }

        /// <summary>
        /// Valide que le texte est compatible XML
        /// </summary>
        private void ValidateXmlCompatibility(string text)
        {
            // Vérifier qu'il n'y a pas de caractères interdits en XML 1.0
            foreach (char c in text)
            {
                if (IsInvalidXmlChar(c))
                {
                    _logger.LogWarning($"Caractère XML invalide détecté: U+{((int)c):X4} dans '{text}'");
                }
            }
        }

        /// <summary>
        /// Vérifie si un caractère est invalide en XML 1.0
        /// </summary>
        private bool IsInvalidXmlChar(char c)
        {
            // XML 1.0 : caractères autorisés
            // #x9 | #xA | #xD | [#x20-#xD7FF] | [#xE000-#xFFFD] | [#x10000-#x10FFFF]
            return !(c == 0x09 || c == 0x0A || c == 0x0D ||
                    (c >= 0x20 && c <= 0xD7FF) ||
                    (c >= 0xE000 && c <= 0xFFFD));
        }

        /// <summary>
        /// Nettoyage basique en cas d'erreur
        /// </summary>
        private string CleanBasicText(string input, int? maxLength)
        {
            if (string.IsNullOrEmpty(input))
                return "";

            // Nettoyage très basique : garder uniquement ASCII imprimable
            var result = new StringBuilder();

            foreach (char c in input)
            {
                if (c >= 32 && c <= 126) // ASCII imprimable
                {
                    result.Append(c);
                }
                else if (c == ' ' || c == '\t')
                {
                    result.Append(' ');
                }
            }

            string cleaned = result.ToString().Trim();

            if (maxLength.HasValue && cleaned.Length > maxLength.Value)
            {
                cleaned = cleaned.Substring(0, maxLength.Value);
            }

            return cleaned;
        }

        /// <summary>
        /// Statistiques de traitement pour diagnostic
        /// </summary>
        public TextProcessingStats GetProcessingStats(string originalText, string processedText)
        {
            return new TextProcessingStats
            {
                OriginalLength = originalText?.Length ?? 0,
                ProcessedLength = processedText?.Length ?? 0,
                HasSpecialCharacters = _nonAsciiRegex.IsMatch(originalText ?? ""),
                TransformationApplied = originalText != processedText
            };
        }

        /// <summary>
        /// ✅ NOUVEAU : Méthode de test pour illustrer les transformations
        /// Exemples de transformations appliquées selon vos règles
        /// </summary>
        public void LogTransformationExamples(ILogger logger)
        {
            var examples = new Dictionary<string, string>
            {
                // ✅ RÈGLE SPÉCIALE : & devient "et"
                {"L'Oréal & Co", "L'Oreal et Co"},
                {"Beauté & Santé", "Beaute et Sante"},
                {"Shampoing & Soin", "Shampoing et Soin"},
                
                // ✅ RÈGLE STANDARD : accents supprimés (é → e)
                {"Crème hydratante", "Creme hydratante"},
                {"Sérum régénérant", "Serum regenerant"},
                {"Après-shampoing", "Apres-shampoing"},
                {"Démaquillant", "Demaquillant"},
                
                // ✅ EXEMPLES MIXTES
                {"L'Occitane en Provence & Cie", "L'Occitane en Provence et Cie"},
                {"Garnier Fructis - Fortifiant & Réparateur", "Garnier Fructis - Fortifiant et Reparateur"},
                {"Nivea Crème & Huile Corporelle", "Nivea Creme et Huile Corporelle"}
            };

            logger.LogInformation("=== EXEMPLES DE TRANSFORMATIONS UTF-8 ===");
            foreach (var example in examples)
            {
                var processed = ProcessText(example.Key);
                var expected = example.Value;
                var status = processed == expected ? "✅" : "❌";

                logger.LogInformation($"{status} '{example.Key}' → '{processed}'");
                if (processed != expected)
                {
                    logger.LogWarning($"   Attendu: '{expected}'");
                }
            }
            logger.LogInformation("=== FIN EXEMPLES TRANSFORMATIONS ===");
        }
    }

    /// <summary>
    /// Statistiques de traitement de texte
    /// </summary>
    public class TextProcessingStats
    {
        public int OriginalLength { get; set; }
        public int ProcessedLength { get; set; }
        public bool HasSpecialCharacters { get; set; }
        public bool TransformationApplied { get; set; }
    }
}